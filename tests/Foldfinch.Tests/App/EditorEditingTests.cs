using Foldfinch.App;
using Foldfinch.App.ViewModels;
using Foldfinch.Tests.Pdf;

namespace Foldfinch.Tests.App;

/// <summary>Selection, remove, reorder and undo/redo behaviour on the editor view-model.</summary>
public class EditorEditingTests
{
    private static async Task<EditorViewModel> OpenAsync(string path)
    {
        var dialogs = new FakeFileDialogService();
        dialogs.OpenResults.Enqueue(path);
        var vm = new EditorViewModel(new AppServices(dialogs, new StubPdfRenderer()));
        await vm.AddPdfCommand.ExecuteAsync(null);
        return vm;
    }

    private static IEnumerable<int> Widths(EditorViewModel vm) =>
        vm.Pages.Select(p => p.Page.SourcePageIndex);

    [Fact]
    public async Task Remove_selected_drops_those_pages()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        PdfFixtures.Create(src, 100, 100, 100, 100); // 4 pages, indices 0..3
        var vm = await OpenAsync(src);

        vm.SelectSingle(vm.Pages[1]);
        vm.ToggleSelect(vm.Pages[3]);
        Assert.Equal(2, vm.SelectionCount);

        vm.RemoveSelectedCommand.Execute(null);

        Assert.Equal(2, vm.PageCount);
        Assert.Equal([0, 2], Widths(vm));
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public async Task ShiftClick_selects_a_range()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        PdfFixtures.Create(src, 100, 100, 100, 100, 100);
        var vm = await OpenAsync(src);

        vm.SelectSingle(vm.Pages[1]);
        vm.SelectRange(vm.Pages[3]); // 1..3

        Assert.Equal(3, vm.SelectionCount);
        Assert.True(vm.Pages[1].IsSelected && vm.Pages[2].IsSelected && vm.Pages[3].IsSelected);
        Assert.False(vm.Pages[0].IsSelected || vm.Pages[4].IsSelected);
    }

    [Fact]
    public async Task Move_selection_to_end_reorders()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        PdfFixtures.Create(src, 100, 100, 100); // indices 0,1,2
        var vm = await OpenAsync(src);

        vm.SelectSingle(vm.Pages[0]);
        vm.MoveSelectionTo(3); // move page index 0 to the end

        Assert.Equal([1, 2, 0], Widths(vm));
        Assert.True(vm.Pages[2].IsSelected); // selection follows the moved page
    }

    [Fact]
    public async Task Move_multiple_preserves_relative_order()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        PdfFixtures.Create(src, 100, 100, 100, 100); // 0,1,2,3
        var vm = await OpenAsync(src);

        vm.SelectSingle(vm.Pages[0]);
        vm.ToggleSelect(vm.Pages[2]);
        vm.MoveSelectionTo(4); // move 0 and 2 to the end, keeping 0 before 2

        Assert.Equal([1, 3, 0, 2], Widths(vm));
    }

    [Fact]
    public async Task Undo_and_redo_restore_page_order()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        PdfFixtures.Create(src, 100, 100, 100);
        var vm = await OpenAsync(src);

        vm.SelectSingle(vm.Pages[0]);
        vm.RemoveSelectedCommand.Execute(null);
        Assert.Equal([1, 2], Widths(vm));
        Assert.True(vm.UndoCommand.CanExecute(null));

        vm.UndoCommand.Execute(null);
        Assert.Equal([0, 1, 2], Widths(vm));

        vm.RedoCommand.Execute(null);
        Assert.Equal([1, 2], Widths(vm));
    }

    [Fact]
    public async Task Rotate_selected_is_applied_and_undoable()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        PdfFixtures.Create(src, 100, 100, 100);
        var vm = await OpenAsync(src);

        vm.SelectSingle(vm.Pages[1]);
        vm.RotateClockwiseCommand.Execute(null);
        Assert.Equal(90, vm.Pages[1].Page.Rotation);
        Assert.True(vm.Pages[1].IsSelected); // selection survives rotation

        vm.RotateClockwiseCommand.Execute(null);
        Assert.Equal(180, vm.Pages[1].Page.Rotation);

        vm.UndoCommand.Execute(null);
        Assert.Equal(90, vm.Pages[1].Page.Rotation);
    }

    [Fact]
    public async Task Save_persists_rotation()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        var outp = tmp.File("out.pdf");
        PdfFixtures.Create(src, 100, 100);

        var dialogs = new FakeFileDialogService();
        dialogs.OpenResults.Enqueue(src);
        dialogs.SaveResult = outp;
        var vm = new EditorViewModel(new AppServices(dialogs, new StubPdfRenderer()));
        await vm.AddPdfCommand.ExecuteAsync(null);

        vm.SelectSingle(vm.Pages[0]);
        vm.RotateCounterClockwiseCommand.Execute(null); // 270
        await vm.SaveAsCommand.ExecuteAsync(null);

        Assert.Equal([270, 0], PdfFixtures.ReadPageRotations(outp));
    }

    [Fact]
    public async Task First_add_is_a_clean_baseline_then_further_adds_are_dirty()
    {
        using var tmp = new TempDir();
        var a = tmp.File("a.pdf");
        var b = tmp.File("b.pdf");
        PdfFixtures.Create(a, 100, 100);
        PdfFixtures.Create(b, 100, 100, 100);

        var dialogs = new FakeFileDialogService();
        dialogs.OpenResults.Enqueue(a);
        dialogs.OpenResults.Enqueue(b);
        var vm = new EditorViewModel(new AppServices(dialogs, new StubPdfRenderer()));

        await vm.AddPdfCommand.ExecuteAsync(null); // first add establishes the document
        Assert.False(vm.IsDirty);
        Assert.False(vm.UndoCommand.CanExecute(null)); // nothing to undo yet

        await vm.AddPdfCommand.ExecuteAsync(null); // combining more is a dirtying, undoable change
        Assert.True(vm.IsDirty);
        Assert.True(vm.UndoCommand.CanExecute(null));
        Assert.Equal(5, vm.PageCount);
    }
}
