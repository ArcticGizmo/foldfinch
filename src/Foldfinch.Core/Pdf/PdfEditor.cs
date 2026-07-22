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
        if (!File.Exists(path)) throw new PdfOperationException("the file no longer exists.");

        var full = System.IO.Path.GetFullPath(path);
        using var doc = OpenForImport(full);
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
    /// Writes the working document to <paramref name="outputPath"/>. To make overwriting a source
    /// file safe, the result is written to a temp file in the same directory and then atomically
    /// moved into place (sources are read fully and closed before the move).
    /// </summary>
    public void Save(PdfDocumentModel model, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path is required.", nameof(outputPath));
        if (model.PageCount == 0) throw new PdfOperationException("there are no pages to save.");

        var target = System.IO.Path.GetFullPath(outputPath);
        var dir = System.IO.Path.GetDirectoryName(target) ?? ".";
        var temp = System.IO.Path.Combine(dir, $".foldfinch-{Guid.NewGuid():n}.tmp.pdf");

        try
        {
            BuildInto(model, temp);              // opens + closes all sources
            File.Move(temp, target, overwrite: true);
        }
        catch (PdfOperationException)
        {
            SafeDelete(temp);
            throw;
        }
        catch (Exception ex)
        {
            SafeDelete(temp);
            throw new PdfOperationException($"the file couldn't be written ({ex.Message}).", ex);
        }
    }

    /// <summary>Builds the output document at <paramref name="path"/>, importing pages in model order.</summary>
    private static void BuildInto(PdfDocumentModel model, string path)
    {
        var opened = new Dictionary<string, PdfDocument>();
        try
        {
            using var output = new PdfDocument();
            foreach (var pageRef in model.Pages)
            {
                var source = model.FindSource(pageRef.SourceId)
                    ?? throw new PdfOperationException($"a page refers to a missing source ('{pageRef.SourceId}').");

                if (!opened.TryGetValue(source.Id, out var input))
                {
                    input = OpenForImport(source.Path);
                    opened[source.Id] = input;
                }

                if (pageRef.SourcePageIndex < 0 || pageRef.SourcePageIndex >= input.PageCount)
                    throw new PdfOperationException($"page {pageRef.SourcePageIndex + 1} is out of range for '{source.DisplayName}'.");

                var added = output.AddPage(input.Pages[pageRef.SourcePageIndex]);
                if (pageRef.Rotation != 0)
                    added.Rotate = (added.Rotate + pageRef.Rotation) % 360;
            }

            output.Save(path);
        }
        finally
        {
            foreach (var doc in opened.Values) doc.Dispose();
        }
    }

    /// <summary>Opens a PDF for import, translating PDFsharp's read errors into user-facing messages.</summary>
    private static PdfDocument OpenForImport(string path)
    {
        try
        {
            return PdfReader.Open(path, PdfDocumentOpenMode.Import);
        }
        catch (Exception ex) when (ex is not PdfOperationException)
        {
            // PDFsharp reports encrypted files via PdfReaderException and corrupt files via
            // InvalidOperationException ("not a valid PDF document"); map both to friendly text.
            if (LooksEncrypted(ex.Message))
                throw new PdfOperationException("it's password-protected — encrypted PDFs aren't supported yet.", ex);
            if (ex is IOException)
                throw new PdfOperationException($"the file couldn't be read ({ex.Message}).", ex);
            throw new PdfOperationException("it isn't a valid PDF, or the file is damaged.", ex);
        }
    }

    private static bool LooksEncrypted(string message) =>
        message.Contains("password", StringComparison.OrdinalIgnoreCase)
        || message.Contains("encrypt", StringComparison.OrdinalIgnoreCase);

    private static void SafeDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }
}
