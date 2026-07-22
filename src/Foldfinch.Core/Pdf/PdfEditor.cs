using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Foldfinch.Core.Pdf;

/// <summary>
/// The only component that touches the filesystem / PDFsharp. It loads source metadata and writes
/// the working <see cref="PdfDocumentModel"/> back out as a single PDF, importing each page from its
/// source in the order the model dictates and applying the user's rotation.
/// </summary>
public sealed class PdfEditor
{
    /// <summary>Opens a PDF's metadata (page count) without loading its content into the model.</summary>
    public SourceDocument Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
        if (!File.Exists(path)) throw new FileNotFoundException("PDF not found.", path);

        var full = System.IO.Path.GetFullPath(path);
        // Import mode is enough to read the page count; InformationOnly is a no-op in PDFsharp 6.
        using var doc = PdfReader.Open(full, PdfDocumentOpenMode.Import);
        return new SourceDocument
        {
            Id = Guid.NewGuid().ToString("n"),
            Path = full,
            DisplayName = System.IO.Path.GetFileName(full),
            PageCount = doc.PageCount,
        };
    }

    /// <summary>Opens a PDF as a fresh working document containing all of its pages.</summary>
    public PdfDocumentModel Open(string path)
    {
        var model = new PdfDocumentModel();
        model.AddSource(Load(path));
        return model;
    }

    /// <summary>Appends another PDF's pages to an existing working document (the "combine" operation).</summary>
    public void AddPdf(PdfDocumentModel model, string path)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.AddSource(Load(path));
    }

    /// <summary>
    /// Writes the working document to <paramref name="outputPath"/>. Each source file is opened once
    /// and reused, so combining many pages from the same file stays cheap.
    /// </summary>
    public void Save(PdfDocumentModel model, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path is required.", nameof(outputPath));
        if (model.PageCount == 0) throw new InvalidOperationException("Cannot save a document with no pages.");

        var opened = new Dictionary<string, PdfDocument>();
        try
        {
            using var output = new PdfDocument();
            foreach (var pageRef in model.Pages)
            {
                var source = model.FindSource(pageRef.SourceId)
                    ?? throw new InvalidOperationException($"Page references unknown source '{pageRef.SourceId}'.");

                if (!opened.TryGetValue(source.Id, out var input))
                {
                    input = PdfReader.Open(source.Path, PdfDocumentOpenMode.Import);
                    opened[source.Id] = input;
                }

                if (pageRef.SourcePageIndex < 0 || pageRef.SourcePageIndex >= input.PageCount)
                    throw new InvalidOperationException(
                        $"Page {pageRef.SourcePageIndex} is out of range for '{source.DisplayName}'.");

                var added = output.AddPage(input.Pages[pageRef.SourcePageIndex]);
                if (pageRef.Rotation != 0)
                    added.Rotate = (added.Rotate + pageRef.Rotation) % 360;
            }

            output.Save(outputPath);
        }
        finally
        {
            foreach (var doc in opened.Values) doc.Dispose();
        }
    }
}
