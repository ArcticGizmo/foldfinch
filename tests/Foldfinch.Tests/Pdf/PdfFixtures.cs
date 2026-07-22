using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Foldfinch.Tests.Pdf;

/// <summary>
/// Builds throwaway PDFs whose pages each carry a distinct width (in points) as an identity marker,
/// so tests can assert the exact page order/content after a save round-trip without extracting text.
/// </summary>
internal static class PdfFixtures
{
    /// <summary>Creates a PDF at <paramref name="path"/> with one page per entry in <paramref name="pageWidths"/>.</summary>
    public static void Create(string path, params int[] pageWidths)
    {
        using var doc = new PdfDocument();
        foreach (var width in pageWidths)
        {
            var page = doc.AddPage();
            page.Width = XUnit.FromPoint(width);
            page.Height = XUnit.FromPoint(800);
        }
        doc.Save(path);
    }

    /// <summary>Reads back each page's width (rounded to int points) — the identity markers, in order.</summary>
    public static List<int> ReadPageWidths(string path)
    {
        using var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        return [.. doc.Pages.Cast<PdfPage>().Select(p => (int)Math.Round(p.Width.Point))];
    }

    /// <summary>Reads back each page's rotation (degrees), in order.</summary>
    public static List<int> ReadPageRotations(string path)
    {
        using var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        return [.. doc.Pages.Cast<PdfPage>().Select(p => p.Rotate)];
    }
}

/// <summary>A temp directory that deletes itself on dispose — one per test that needs files.</summary>
internal sealed class TempDir : IDisposable
{
    public string Path { get; }

    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "foldfinch-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path);
    }

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch { /* best effort */ }
    }
}
