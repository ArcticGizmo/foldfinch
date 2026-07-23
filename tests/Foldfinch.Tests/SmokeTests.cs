using Foldfinch.App;
using Foldfinch.App.ViewModels;

namespace Foldfinch.Tests;

public class SmokeTests
{
    [Fact]
    public void MainWindowViewModel_constructs_with_an_editor()
    {
        var vm = new MainWindowViewModel(new AppServices());

        Assert.NotNull(vm.Editor);
        Assert.True(vm.Editor.IsEmpty);
    }
}
