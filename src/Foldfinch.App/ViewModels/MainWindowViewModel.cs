namespace Foldfinch.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>The single editor surface (page grid + toolbar). More pages can be added later.</summary>
    public EditorViewModel Editor { get; }

    public MainWindowViewModel(AppServices services)
    {
        Editor = new EditorViewModel(services);
    }
}
