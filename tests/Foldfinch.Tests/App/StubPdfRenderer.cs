using Foldfinch.Core.Pdf;

namespace Foldfinch.Tests.App;

/// <summary>
/// A renderer that produces no image — keeps EditorViewModel tests off the real PDFium/Avalonia
/// path (unit tests have no Avalonia platform to decode a bitmap against).
/// </summary>
internal sealed class StubPdfRenderer : IPdfRenderer
{
    public byte[] RenderPagePng(string pdfPath, int pageIndex, int rotationDegrees, int targetWidth) => [];
}
