using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Foldfinch.Core.Pdf;

namespace Foldfinch.App.ViewModels;

/// <summary>
/// The main editing surface: an ordered set of pages drawn from one or more source PDFs, shown as a
/// grid of thumbnails. M3 renders the grid; per-page remove/reorder/rotate arrive in M4/M6.
/// </summary>
public partial class EditorViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private PdfDocumentModel? _model;

    // Undo/redo of page-list state (order + rotation). Each entry is a full page snapshot.
    private readonly Stack<IReadOnlyList<PageRef>> _undo = new();
    private readonly Stack<IReadOnlyList<PageRef>> _redo = new();

    // Anchor for shift-click range selection (index into Pages).
    private int _anchorIndex;

    /// <summary>The page tiles shown in the grid, in document order.</summary>
    public ObservableCollection<PageThumbnailViewModel> Pages { get; } = [];

    /// <summary>Number of currently selected pages.</summary>
    [ObservableProperty] private int _selectionCount;

    /// <summary>True until a document with pages is open — drives the empty-state prompt.</summary>
    [ObservableProperty] private bool _isEmpty = true;

    /// <summary>Number of pages in the working document.</summary>
    [ObservableProperty] private int _pageCount;

    /// <summary>Display name of the document (first opened file), shown in the summary/title.</summary>
    [ObservableProperty] private string? _documentName;

    /// <summary>Path a plain "Save" writes to; null until the document has been saved once.</summary>
    [ObservableProperty] private string? _savePath;

    /// <summary>True when there are unsaved changes.</summary>
    [ObservableProperty] private bool _isDirty;

    /// <summary>Transient status line (e.g. "Saved to …", or an error message).</summary>
    [ObservableProperty] private string? _status;

    /// <summary>True while a background PDF operation is running (disables the toolbar).</summary>
    [ObservableProperty] private bool _isBusy;

    public EditorViewModel(AppServices services)
    {
        _services = services;
    }

    private bool HasDocument => _model is not null;
    private bool HasPages => PageCount > 0;
    private bool NotBusy => !IsBusy;

    [RelayCommand(CanExecute = nameof(NotBusy))]
    private async Task OpenAsync()
    {
        var path = await _services.FileDialogs.OpenPdfAsync("Open PDF");
        if (path is null) return;

        await Do(() => _services.Editor.Open(path), model =>
        {
            _model = model;
            DocumentName = model.Sources[0].DisplayName;
            SavePath = null;                 // require a Save As target the first time (see M6 for overwrite)
            IsDirty = false;
            _undo.Clear();
            _redo.Clear();
            RebuildPages();
            NotifyCommands();
            Status = $"Opened {DocumentName}";
        }, "Couldn't open that PDF");
    }

    [RelayCommand(CanExecute = nameof(CanAddPdf))]
    private async Task AddPdfAsync()
    {
        var path = await _services.FileDialogs.OpenPdfAsync("Add PDF to combine");
        if (path is null || _model is null) return;
        var model = _model;

        PushUndo(); // adding pages is undoable
        await Do(() => { _services.Editor.AddPdf(model, path); return path; }, added =>
        {
            IsDirty = true;
            RebuildPages();
            Status = $"Added {System.IO.Path.GetFileName(added)}";
        }, "Couldn't add that PDF");
    }

    private bool CanAddPdf => NotBusy && HasDocument;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (SavePath is null) { await SaveAsAsync(); return; }
        await SaveTo(SavePath);
    }

    private bool CanSave => NotBusy && HasPages;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsAsync()
    {
        var suggested = System.IO.Path.GetFileNameWithoutExtension(DocumentName ?? "document") + ".pdf";
        var path = await _services.FileDialogs.SavePdfAsync(suggested);
        if (path is null) return;
        await SaveTo(path);
    }

    private async Task SaveTo(string path)
    {
        if (_model is null) return;
        var model = _model;
        await Do(() => { _services.Editor.Save(model, path); return path; }, saved =>
        {
            SavePath = saved;
            IsDirty = false;
            Status = $"Saved to {System.IO.Path.GetFileName(saved)}";
        }, "Couldn't save the PDF");
    }

    // ---- Selection -------------------------------------------------------------------------

    /// <summary>Selects only <paramref name="tile"/> (a plain click), and sets it as the range anchor.</summary>
    public void SelectSingle(PageThumbnailViewModel tile)
    {
        foreach (var p in Pages) p.IsSelected = ReferenceEquals(p, tile);
        _anchorIndex = Pages.IndexOf(tile);
        UpdateSelectionCount();
    }

    /// <summary>Toggles <paramref name="tile"/> in the selection (a Ctrl/Cmd click).</summary>
    public void ToggleSelect(PageThumbnailViewModel tile)
    {
        tile.IsSelected = !tile.IsSelected;
        _anchorIndex = Pages.IndexOf(tile);
        UpdateSelectionCount();
    }

    /// <summary>Selects the contiguous range from the anchor to <paramref name="tile"/> (a Shift click).</summary>
    public void SelectRange(PageThumbnailViewModel tile)
    {
        var target = Pages.IndexOf(tile);
        if (target < 0) return;
        if (_anchorIndex < 0 || _anchorIndex >= Pages.Count) _anchorIndex = target;

        var lo = System.Math.Min(_anchorIndex, target);
        var hi = System.Math.Max(_anchorIndex, target);
        for (var i = 0; i < Pages.Count; i++) Pages[i].IsSelected = i >= lo && i <= hi;
        UpdateSelectionCount();
    }

    [RelayCommand(CanExecute = nameof(HasPages))]
    private void SelectAll()
    {
        foreach (var p in Pages) p.IsSelected = true;
        UpdateSelectionCount();
    }

    public void ClearSelection()
    {
        foreach (var p in Pages) p.IsSelected = false;
        UpdateSelectionCount();
    }

    private IReadOnlyList<int> SelectedIndices() =>
        [.. Pages.Select((p, i) => (p, i)).Where(t => t.p.IsSelected).Select(t => t.i)];

    private void UpdateSelectionCount()
    {
        SelectionCount = Pages.Count(p => p.IsSelected);
        RemoveSelectedCommand.NotifyCanExecuteChanged();
    }

    /// <summary>True when at least one page is selected (drives Remove/rotate + the status hint).</summary>
    public bool HasSelection => SelectionCount > 0;

    partial void OnSelectionCountChanged(int value) => OnPropertyChanged(nameof(HasSelection));

    // ---- Mutations -------------------------------------------------------------------------

    /// <summary>Removes the selected pages (never leaves the document at zero — the UI guards Save separately).</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RemoveSelected()
    {
        if (_model is null || SelectionCount == 0) return;
        var indices = SelectedIndices();

        Mutate(() => _model.RemoveAt(indices), reselect: null);
        Status = indices.Count == 1 ? "Removed 1 page" : $"Removed {indices.Count} pages";
    }

    /// <summary>
    /// Moves the selected pages so they land before the page currently at <paramref name="targetIndex"/>
    /// (or at the end if past the last page), preserving their relative order. This backs drag-and-drop.
    /// </summary>
    public void MoveSelectionTo(int targetIndex)
    {
        if (_model is null || SelectionCount == 0) return;

        var selected = new HashSet<PageRef>(
            Pages.Where(p => p.IsSelected).Select(p => p.Page), ReferenceEqualityComparer.Instance);
        var moving = _model.Pages.Where(selected.Contains).ToList();
        var remaining = _model.Pages.Where(p => !selected.Contains(p)).ToList();

        // Anchor on the page that was at the target slot, then find where it sits among the remaining.
        var anchor = targetIndex >= 0 && targetIndex < _model.Pages.Count ? _model.Pages[targetIndex] : null;
        var insertAt = anchor is null || selected.Contains(anchor)
            ? remaining.Count
            : remaining.IndexOf(anchor);
        if (insertAt < 0) insertAt = remaining.Count;

        remaining.InsertRange(insertAt, moving);
        if (remaining.SequenceEqual(_model.Pages)) return; // no-op move

        Mutate(() => _model.Restore(remaining), reselect: selected);
        Status = moving.Count == 1 ? "Moved 1 page" : $"Moved {moving.Count} pages";
    }

    /// <summary>Rotates every selected page clockwise (used by M6 rotate; harmless if nothing selected).</summary>
    public void RotateSelected(int deltaDegrees)
    {
        if (_model is null || SelectionCount == 0) return;
        var indices = SelectedIndices();
        var selectedRefs = new HashSet<PageRef>(
            indices.Select(i => _model.Pages[i]), ReferenceEqualityComparer.Instance);

        // Rotation replaces PageRef instances, so reselect by (source, page) identity after rebuild.
        Mutate(() =>
        {
            foreach (var i in indices) _model.Rotate(i, deltaDegrees);
        }, reselect: null);

        // Reselect the rotated pages by their source/index (instances changed).
        var keys = selectedRefs.Select(r => (r.SourceId, r.SourcePageIndex)).ToHashSet();
        foreach (var tile in Pages)
            tile.IsSelected = keys.Contains((tile.Page.SourceId, tile.Page.SourcePageIndex));
        UpdateSelectionCount();
    }

    /// <summary>Snapshot → run the model mutation → rebuild tiles (preserving selection where asked).</summary>
    private void Mutate(Action mutation, IReadOnlySet<PageRef>? reselect)
    {
        if (_model is null) return;
        PushUndo();
        mutation();
        IsDirty = true;
        RebuildPagesReselecting(reselect);
    }

    private void RebuildPagesReselecting(IReadOnlySet<PageRef>? reselect)
    {
        RebuildPages();
        if (reselect is not null)
            foreach (var tile in Pages)
                tile.IsSelected = reselect.Contains(tile.Page);
        UpdateSelectionCount();
    }

    // ---- Undo / redo -----------------------------------------------------------------------

    private void PushUndo()
    {
        if (_model is null) return;
        _undo.Push(_model.Snapshot());
        _redo.Clear();
        NotifyCommands();
    }

    private bool CanUndo => _undo.Count > 0 && NotBusy;
    private bool CanRedo => _redo.Count > 0 && NotBusy;

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_model is null || _undo.Count == 0) return;
        _redo.Push(_model.Snapshot());
        _model.Restore(_undo.Pop());
        IsDirty = true;
        RebuildPages();
        NotifyCommands();
        Status = "Undid last change";
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (_model is null || _redo.Count == 0) return;
        _undo.Push(_model.Snapshot());
        _model.Restore(_redo.Pop());
        IsDirty = true;
        RebuildPages();
        NotifyCommands();
        Status = "Redid change";
    }

    /// <summary>
    /// Runs a blocking PDF call off the UI thread, then applies <paramref name="after"/> back on the
    /// UI thread (so observable collections are only mutated there). Errors surface in the status line.
    /// </summary>
    private async Task Do<T>(Func<T> work, Action<T> after, string errorPrefix)
    {
        IsBusy = true;
        try
        {
            var result = await AppServices.RunAsync(work);
            after(result);
        }
        catch (Exception ex)
        {
            Status = $"{errorPrefix}: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Rebuilds the page tiles from the model and kicks off thumbnail rendering. Selection is
    /// preserved for any page (by reference) still present, so reorder/undo keep the highlight.
    /// Thumbnails are cached by (path, page, rotation), so rebuilding is cheap — only genuinely new
    /// renders (e.g. a rotated page) hit PDFium.
    /// </summary>
    private void RebuildPages()
    {
        var previouslySelected = new HashSet<PageRef>(
            Pages.Where(p => p.IsSelected).Select(p => p.Page), ReferenceEqualityComparer.Instance);

        Pages.Clear();
        if (_model is not null)
        {
            var number = 1;
            foreach (var pageRef in _model.Pages)
            {
                var source = _model.FindSource(pageRef.SourceId)!;
                Pages.Add(new PageThumbnailViewModel(pageRef, source.Path, number++)
                {
                    IsSelected = previouslySelected.Contains(pageRef),
                });
            }
        }

        PageCount = _model?.PageCount ?? 0;
        IsEmpty = PageCount == 0;
        UpdateSelectionCount();
        _ = LoadThumbnailsAsync();
    }

    /// <summary>Renders thumbnails one at a time (PDFium is single-threaded) for the current tiles.</summary>
    public async Task LoadThumbnailsAsync()
    {
        foreach (var tile in Pages.ToArray())
            await tile.LoadAsync(_services.Thumbnails);
    }

    partial void OnIsBusyChanged(bool value) => NotifyCommands();
    partial void OnPageCountChanged(int value) => NotifyCommands();

    private void NotifyCommands()
    {
        OpenCommand.NotifyCanExecuteChanged();
        AddPdfCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        SaveAsCommand.NotifyCanExecuteChanged();
        SelectAllCommand.NotifyCanExecuteChanged();
        RemoveSelectedCommand.NotifyCanExecuteChanged();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }
}
