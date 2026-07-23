using PDFtoImage;
using SkiaSharp;

namespace Foldfinch.Core.Pdf;

/// <summary>
/// <see cref="IPdfRenderer"/> backed by PDFtoImage (PDFium + SkiaSharp). PDFium is not thread-safe,
/// so callers must not invoke this concurrently — the App renders thumbnails one at a time.
/// </summary>
public sealed class PdfiumRenderer : IPdfRenderer
{
    public byte[] RenderPagePng(string pdfPath, int pageIndex, int rotationDegrees, int targetWidth)
    {
        var pdf = File.ReadAllBytes(pdfPath);
        var options = new RenderOptions(
            Width: targetWidth,
            WithAspectRatio: true,
            Rotation: ToPdfRotation(rotationDegrees),
            BackgroundColor: SKColors.White);

        using var bitmap = Conversion.ToImage(pdf, page: pageIndex, password: null, options: options);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    private static PdfRotation ToPdfRotation(int degrees) => Rotations.Normalize(degrees) switch
    {
        90 => PdfRotation.Rotate90,
        180 => PdfRotation.Rotate180,
        270 => PdfRotation.Rotate270,
        _ => PdfRotation.Rotate0,
    };
}
