using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Foldfinch.App.Updates;

namespace Foldfinch.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>The single editor surface (page grid + toolbar).</summary>
    public EditorViewModel Editor { get; }

    /// <summary>Non-null when a newer version is available; drives the top notification bar.</summary>
    [ObservableProperty] private string? _updateNotice;

    public MainWindowViewModel(AppServices services)
    {
        Editor = new EditorViewModel(services);
        _ = CheckForUpdatesAsync();
    }

    [RelayCommand]
    private void DismissUpdateNotice() => UpdateNotice = null;

    private async Task CheckForUpdatesAsync() => UpdateNotice = await UpdateChecker.CheckAsync();
}
