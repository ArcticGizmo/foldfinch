using System.Collections.ObjectModel;
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

    /// <summary>The page tiles shown in the grid, in document order.</summary>
    public ObservableCollection<PageThumbnailViewModel> Pages { get; } = [];

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
            RebuildPages();
            Status = $"Opened {DocumentName}";
        }, "Couldn't open that PDF");
    }

    [RelayCommand(CanExecute = nameof(CanAddPdf))]
    private async Task AddPdfAsync()
    {
        var path = await _services.FileDialogs.OpenPdfAsync("Add PDF to combine");
        if (path is null || _model is null) return;
        var model = _model;

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

    /// <summary>Rebuilds the page tiles from the model and kicks off thumbnail rendering.</summary>
    private void RebuildPages()
    {
        Pages.Clear();
        if (_model is not null)
        {
            var number = 1;
            foreach (var pageRef in _model.Pages)
            {
                var source = _model.FindSource(pageRef.SourceId)!;
                Pages.Add(new PageThumbnailViewModel(pageRef, source.Path, number++));
            }
        }

        PageCount = _model?.PageCount ?? 0;
        IsEmpty = PageCount == 0;
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
    }
}
