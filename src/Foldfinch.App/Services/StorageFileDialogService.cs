using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Foldfinch.App.Services;

/// <summary>
/// File dialogs backed by Avalonia's <see cref="IStorageProvider"/>. The top-level window is resolved
/// lazily at call time (not at construction), so this can be created before the window exists.
/// </summary>
public sealed class StorageFileDialogService : IFileDialogService
{
    private static readonly FilePickerFileType PdfType = new("PDF document") { Patterns = ["*.pdf"] };

    public async Task<string?> OpenPdfAsync(string title)
    {
        var provider = StorageProvider;
        if (provider is null) return null;

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [PdfType],
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<string?> SavePdfAsync(string suggestedName)
    {
        var provider = StorageProvider;
        if (provider is null) return null;

        var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save PDF",
            SuggestedFileName = suggestedName,
            DefaultExtension = "pdf",
            FileTypeChoices = [PdfType],
        });

        return file?.TryGetLocalPath();
    }

    private static IStorageProvider? StorageProvider =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.StorageProvider;
}
