using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Foldfinch.Core.Pdf;

namespace Foldfinch.App.ViewModels;

/// <summary>
/// The main editing surface: an ordered set of pages drawn from one or more source PDFs.
/// M2 wires the document lifecycle (open / add / save); the visual page grid and per-page
/// actions arrive in M3/M4.
/// </summary>
public partial class EditorViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private PdfDocumentModel? _model;

    /// <summary>One row per source file currently contributing pages (for the M2 summary panel).</summary>
    public ObservableCollection<string> SourceSummaries { get; } = [];

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

        await RunPdfWork(() =>
        {
            var model = _services.Editor.Open(path);
            _model = model;
            DocumentName = model.Sources[0].DisplayName;
            SavePath = null;                 // require a Save As target the first time (see M6 for overwrite)
            IsDirty = false;
            Sync();
            Status = $"Opened {DocumentName}";
        }, "Couldn't open that PDF");
    }

    [RelayCommand(CanExecute = nameof(CanAddPdf))]
    private async Task AddPdfAsync()
    {
        var path = await _services.FileDialogs.OpenPdfAsync("Add PDF to combine");
        if (path is null || _model is null) return;

        await RunPdfWork(() =>
        {
            _services.Editor.AddPdf(_model, path);
            IsDirty = true;
            Sync();
            Status = $"Added {System.IO.Path.GetFileName(path)}";
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
        await RunPdfWork(() =>
        {
            _services.Editor.Save(model, path);
            SavePath = path;
            IsDirty = false;
            Status = $"Saved to {System.IO.Path.GetFileName(path)}";
        }, "Couldn't save the PDF");
    }

    /// <summary>Runs a blocking PDF call off the UI thread, guarding busy state and surfacing errors.</summary>
    private async Task RunPdfWork(Action work, string errorPrefix)
    {
        IsBusy = true;
        try
        {
            await AppServices.RunAsync(work);
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

    /// <summary>Refreshes derived state from the model after any mutation.</summary>
    private void Sync()
    {
        PageCount = _model?.PageCount ?? 0;
        IsEmpty = PageCount == 0;

        SourceSummaries.Clear();
        if (_model is not null)
            foreach (var s in _model.Sources)
                SourceSummaries.Add($"{s.DisplayName} — {s.PageCount} page{(s.PageCount == 1 ? "" : "s")}");
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
