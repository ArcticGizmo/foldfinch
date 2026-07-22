using Foldfinch.Core.Pdf;

namespace Foldfinch.Tests.Pdf;

public class PdfiumRendererTests
{
    // PNG files start with this 8-byte signature.
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void Renders_a_page_to_a_png()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        PdfFixtures.Create(src, 300, 300);

        var png = new PdfiumRenderer().RenderPagePng(src, pageIndex: 0, rotationDegrees: 0, targetWidth: 120);

        Assert.NotEmpty(png);
        Assert.Equal(PngSignature, png.Take(8));
    }

    [Fact]
    public void Rotation_is_accepted_for_all_right_angles()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        PdfFixtures.Create(src, 300);

        var renderer = new PdfiumRenderer();
        foreach (var deg in new[] { 0, 90, 180, 270 })
        {
            var png = renderer.RenderPagePng(src, 0, deg, 120);
            Assert.NotEmpty(png);
        }
    }
}
