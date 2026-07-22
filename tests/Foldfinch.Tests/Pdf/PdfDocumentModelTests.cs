using Foldfinch.Core.Pdf;

namespace Foldfinch.Tests.Pdf;

public class PdfDocumentModelTests
{
    private static SourceDocument Source(string id, int pages) =>
        new() { Id = id, Path = $"/{id}.pdf", DisplayName = $"{id}.pdf", PageCount = pages };

    [Fact]
    public void AddSource_appends_all_pages_in_order()
    {
        var model = new PdfDocumentModel();
        model.AddSource(Source("a", 3));

        Assert.Equal(3, model.PageCount);
        Assert.All(model.Pages, p => Assert.Equal("a", p.SourceId));
        Assert.Equal([0, 1, 2], model.Pages.Select(p => p.SourcePageIndex));
    }

    [Fact]
    public void AddSource_second_source_combines_pages()
    {
        var model = new PdfDocumentModel();
        model.AddSource(Source("a", 2));
        model.AddSource(Source("b", 2));

        Assert.Equal(4, model.PageCount);
        Assert.Equal(["a", "a", "b", "b"], model.Pages.Select(p => p.SourceId));
    }

    [Fact]
    public void AddSource_rejects_duplicate_id()
    {
        var model = new PdfDocumentModel();
        model.AddSource(Source("a", 1));
        Assert.Throws<ArgumentException>(() => model.AddSource(Source("a", 1)));
    }

    [Fact]
    public void RemoveAt_drops_the_right_pages()
    {
        var model = new PdfDocumentModel();
        model.AddSource(Source("a", 4)); // indices 0,1,2,3

        model.RemoveAt([1, 3]);

        Assert.Equal([0, 2], model.Pages.Select(p => p.SourcePageIndex));
    }

    [Fact]
    public void RemoveAt_handles_unsorted_and_duplicate_indices()
    {
        var model = new PdfDocumentModel();
        model.AddSource(Source("a", 4));

        model.RemoveAt([3, 1, 1]);

        Assert.Equal([0, 2], model.Pages.Select(p => p.SourcePageIndex));
    }

    [Fact]
    public void Move_reorders_a_single_page()
    {
        var model = new PdfDocumentModel();
        model.AddSource(Source("a", 3)); // 0,1,2

        model.Move(0, 2); // move first page to the end

        Assert.Equal([1, 2, 0], model.Pages.Select(p => p.SourcePageIndex));
    }

    [Fact]
    public void Reorder_accepts_a_permutation()
    {
        var model = new PdfDocumentModel();
        model.AddSource(Source("a", 3));
        var reversed = model.Pages.Reverse().ToList();

        model.Reorder(reversed);

        Assert.Equal([2, 1, 0], model.Pages.Select(p => p.SourcePageIndex));
    }

    [Fact]
    public void Reorder_rejects_non_permutation()
    {
        var model = new PdfDocumentModel();
        model.AddSource(Source("a", 3));
        var bogus = model.Pages.Take(2).ToList();

        Assert.Throws<ArgumentException>(() => model.Reorder(bogus));
    }

    [Theory]
    [InlineData(90, 90)]
    [InlineData(360, 0)]
    [InlineData(-90, 270)]
    [InlineData(450, 90)]
    public void Rotate_normalises_to_right_angles(int delta, int expected)
    {
        var model = new PdfDocumentModel();
        model.AddSource(Source("a", 1));

        model.Rotate(0, delta);

        Assert.Equal(expected, model.Pages[0].Rotation);
    }

    [Fact]
    public void Rotate_accumulates()
    {
        var model = new PdfDocumentModel();
        model.AddSource(Source("a", 1));

        model.Rotate(0, 90);
        model.Rotate(0, 90);

        Assert.Equal(180, model.Pages[0].Rotation);
    }

    [Fact]
    public void Out_of_range_index_throws()
    {
        var model = new PdfDocumentModel();
        model.AddSource(Source("a", 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => model.Rotate(5, 90));
        Assert.Throws<ArgumentOutOfRangeException>(() => model.RemoveAt([9]));
    }
}
