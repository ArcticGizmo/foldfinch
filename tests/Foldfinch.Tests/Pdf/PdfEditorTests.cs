using Foldfinch.Core.Pdf;

namespace Foldfinch.Tests.Pdf;

public class PdfEditorTests
{
    private readonly PdfEditor _editor = new();

    [Fact]
    public void Open_reports_page_count()
    {
        using var tmp = new TempDir();
        var path = tmp.File("a.pdf");
        PdfFixtures.Create(path, 101, 102, 103);

        var model = _editor.Open(path);

        Assert.Single(model.Sources);
        Assert.Equal(3, model.PageCount);
        Assert.Equal("a.pdf", model.Sources[0].DisplayName);
    }

    [Fact]
    public void Save_round_trips_all_pages_in_order()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        var outp = tmp.File("out.pdf");
        PdfFixtures.Create(src, 101, 102, 103);

        var model = _editor.Open(src);
        _editor.Save(model, outp);

        Assert.Equal([101, 102, 103], PdfFixtures.ReadPageWidths(outp));
    }

    [Fact]
    public void Save_after_remove_keeps_only_remaining_pages()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        var outp = tmp.File("out.pdf");
        PdfFixtures.Create(src, 101, 102, 103, 104);

        var model = _editor.Open(src);
        model.RemoveAt([1, 3]); // drop 102 and 104
        _editor.Save(model, outp);

        Assert.Equal([101, 103], PdfFixtures.ReadPageWidths(outp));
    }

    [Fact]
    public void Save_after_reorder_writes_new_order()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        var outp = tmp.File("out.pdf");
        PdfFixtures.Create(src, 101, 102, 103);

        var model = _editor.Open(src);
        model.Move(0, 2); // 101 -> end
        _editor.Save(model, outp);

        Assert.Equal([102, 103, 101], PdfFixtures.ReadPageWidths(outp));
    }

    [Fact]
    public void Combine_two_pdfs_interleaves_and_saves()
    {
        using var tmp = new TempDir();
        var a = tmp.File("a.pdf");
        var b = tmp.File("b.pdf");
        var outp = tmp.File("out.pdf");
        PdfFixtures.Create(a, 101, 102);
        PdfFixtures.Create(b, 201, 202);

        var model = _editor.Open(a);
        _editor.AddPdf(model, b);

        // Interleave: a0, b0, a1, b1  ->  101, 201, 102, 202
        var reordered = new[] { model.Pages[0], model.Pages[2], model.Pages[1], model.Pages[3] };
        model.Reorder(reordered);
        _editor.Save(model, outp);

        Assert.Equal([101, 201, 102, 202], PdfFixtures.ReadPageWidths(outp));
    }

    [Fact]
    public void Save_applies_rotation_per_page()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        var outp = tmp.File("out.pdf");
        PdfFixtures.Create(src, 101, 102, 103);

        var model = _editor.Open(src);
        model.Rotate(0, 90);
        model.Rotate(1, 180);
        // page 2 left at 0
        _editor.Save(model, outp);

        Assert.Equal([90, 180, 0], PdfFixtures.ReadPageRotations(outp));
    }

    [Fact]
    public void Save_with_no_pages_throws()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        PdfFixtures.Create(src, 101);

        var model = _editor.Open(src);
        model.RemoveAt([0]);

        Assert.Throws<PdfOperationException>(() => _editor.Save(model, tmp.File("out.pdf")));
    }

    [Fact]
    public void Load_missing_file_throws()
    {
        using var tmp = new TempDir();
        Assert.Throws<PdfOperationException>(() => _editor.Open(tmp.File("nope.pdf")));
    }

    [Fact]
    public void Corrupt_file_reports_a_friendly_error()
    {
        using var tmp = new TempDir();
        var bogus = tmp.File("bogus.pdf");
        File.WriteAllText(bogus, "this is not a pdf");

        var ex = Assert.Throws<PdfOperationException>(() => _editor.Open(bogus));
        Assert.Contains("valid PDF", ex.Message);
    }

    [Fact]
    public void Can_overwrite_the_source_file_in_place()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        PdfFixtures.Create(src, 101, 102, 103);

        var model = _editor.Open(src);
        model.RemoveAt([1]); // drop page 102
        _editor.Save(model, src); // overwrite the file we opened

        Assert.Equal([101, 103], PdfFixtures.ReadPageWidths(src));
    }
}
