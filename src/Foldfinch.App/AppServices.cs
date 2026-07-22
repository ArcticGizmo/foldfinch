using Foldfinch.App.Rendering;
using Foldfinch.App.Services;
using Foldfinch.Core.Pdf;

namespace Foldfinch.App;

/// <summary>
/// Composition root: holds the services the UI drives. PDF work in <c>Foldfinch.Core</c> is
/// synchronous/blocking, so ViewModels run it off the UI thread via <see cref="RunAsync"/>.
/// </summary>
public sealed class AppServices
{
    /// <summary>PDF load/save engine (PDFsharp-backed).</summary>
    public PdfEditor Editor { get; }

    /// <summary>Open/save file pickers.</summary>
    public IFileDialogService FileDialogs { get; }

    /// <summary>Cached page-thumbnail rendering for the grid.</summary>
    public ThumbnailService Thumbnails { get; }

    public AppServices(IFileDialogService? fileDialogs = null, IPdfRenderer? renderer = null)
    {
        Editor = new PdfEditor();
        FileDialogs = fileDialogs ?? new StorageFileDialogService();
        Thumbnails = new ThumbnailService(renderer ?? new PdfiumRenderer());
    }

    /// <summary>Run a blocking Core call on a background thread (keeps the UI responsive).</summary>
    public static Task<T> RunAsync<T>(Func<T> work) => Task.Run(work);

    public static Task RunAsync(Action work) => Task.Run(work);
}
