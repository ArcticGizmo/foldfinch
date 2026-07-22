namespace Foldfinch.Core.Pdf;

/// <summary>
/// Renders a single PDF page to a PNG image. Kept UI-agnostic (returns bytes, not a UI bitmap) so
/// the App layer can decode it into whatever image type it needs.
/// </summary>
public interface IPdfRenderer
{
    /// <summary>
    /// Renders page <paramref name="pageIndex"/> of the PDF at <paramref name="pdfPath"/> to a PNG,
    /// scaled to <paramref name="targetWidth"/> pixels wide (aspect preserved) and rotated clockwise
    /// by <paramref name="rotationDegrees"/> (0/90/180/270). Returns the PNG bytes.
    /// </summary>
    byte[] RenderPagePng(string pdfPath, int pageIndex, int rotationDegrees, int targetWidth);
}
