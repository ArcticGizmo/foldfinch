using Foldfinch.App;
using Foldfinch.App.ViewModels;
using Foldfinch.Tests.Pdf;

namespace Foldfinch.Tests.App;

public class EditorViewModelTests
{
    [Fact]
    public async Task Open_then_save_as_writes_the_document()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        var outp = tmp.File("out.pdf");
        PdfFixtures.Create(src, 101, 102);

        var dialogs = new FakeFileDialogService();
        dialogs.OpenResults.Enqueue(src);
        dialogs.SaveResult = outp;
        var vm = new EditorViewModel(new AppServices(dialogs, new StubPdfRenderer()));

        await vm.AddPdfCommand.ExecuteAsync(null);
        Assert.False(vm.IsEmpty);
        Assert.Equal(2, vm.PageCount);
        Assert.Equal("a.pdf", vm.DocumentName);

        await vm.SaveAsCommand.ExecuteAsync(null); // explicit Save As to the chosen path
        Assert.True(File.Exists(outp));
        Assert.Equal([101, 102], PdfFixtures.ReadPageWidths(outp));
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public async Task Add_pdf_combines_and_marks_dirty()
    {
        using var tmp = new TempDir();
        var a = tmp.File("a.pdf");
        var b = tmp.File("b.pdf");
        var outp = tmp.File("out.pdf");
        PdfFixtures.Create(a, 101, 102);
        PdfFixtures.Create(b, 201);

        var dialogs = new FakeFileDialogService();
        dialogs.OpenResults.Enqueue(a);
        dialogs.OpenResults.Enqueue(b);
        dialogs.SaveResult = outp;
        var vm = new EditorViewModel(new AppServices(dialogs, new StubPdfRenderer()));

        await vm.AddPdfCommand.ExecuteAsync(null);
        await vm.AddPdfCommand.ExecuteAsync(null);

        Assert.Equal(3, vm.PageCount);
        Assert.True(vm.IsDirty);
        Assert.Equal(3, vm.Pages.Count);
        Assert.Equal(2, vm.Pages.Select(p => p.SourcePath).Distinct().Count());

        await vm.SaveAsCommand.ExecuteAsync(null);
        Assert.Equal([101, 102, 201], PdfFixtures.ReadPageWidths(outp));
    }

    [Fact]
    public async Task Save_as_never_touches_the_source_file()
    {
        using var tmp = new TempDir();
        var src = tmp.File("a.pdf");
        var outp = tmp.File("out.pdf");
        PdfFixtures.Create(src, 101, 102, 103);

        var dialogs = new FakeFileDialogService();
        dialogs.OpenResults.Enqueue(src);
        dialogs.SaveResult = outp;
        var vm = new EditorViewModel(new AppServices(dialogs, new StubPdfRenderer()));
        await vm.AddPdfCommand.ExecuteAsync(null);

        vm.SelectSingle(vm.Pages[0]);
        vm.RemoveSelectedCommand.Execute(null);
        await vm.SaveAsCommand.ExecuteAsync(null);

        Assert.Equal([102, 103], PdfFixtures.ReadPageWidths(outp)); // edits went to the chosen file
        Assert.Equal([101, 102, 103], PdfFixtures.ReadPageWidths(src)); // source is untouched
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public async Task Cancelling_open_leaves_editor_empty()
    {
        var dialogs = new FakeFileDialogService(); // no queued path -> returns null (cancelled)
        var vm = new EditorViewModel(new AppServices(dialogs, new StubPdfRenderer()));

        await vm.AddPdfCommand.ExecuteAsync(null);

        Assert.True(vm.IsEmpty);
        Assert.Equal(0, vm.PageCount);
    }

    [Fact]
    public void Save_disabled_when_empty_but_add_is_always_available()
    {
        var vm = new EditorViewModel(new AppServices(new FakeFileDialogService(), new StubPdfRenderer()));
        Assert.False(vm.SaveAsCommand.CanExecute(null));
        Assert.True(vm.AddPdfCommand.CanExecute(null));
    }

    [Fact]
    public async Task Insert_pdf_places_pages_at_the_chosen_spot()
    {
        using var tmp = new TempDir();
        var a = tmp.File("a.pdf");
        var b = tmp.File("b.pdf");
        var outp = tmp.File("out.pdf");
        PdfFixtures.Create(a, 101, 102);
        PdfFixtures.Create(b, 201);

        var dialogs = new FakeFileDialogService();
        dialogs.OpenResults.Enqueue(a); // first add
        dialogs.OpenResults.Enqueue(b); // insert
        dialogs.SaveResult = outp;
        var vm = new EditorViewModel(new AppServices(dialogs, new StubPdfRenderer()));

        await vm.AddPdfCommand.ExecuteAsync(null);
        await vm.InsertPdfCommand.ExecuteAsync(vm.Pages[1]); // insert before page 2

        Assert.Equal(3, vm.PageCount);
        Assert.True(vm.IsDirty);
        Assert.True(vm.UndoCommand.CanExecute(null)); // an insert is undoable

        await vm.SaveAsCommand.ExecuteAsync(null);
        Assert.Equal([101, 201, 102], PdfFixtures.ReadPageWidths(outp));
    }
}
