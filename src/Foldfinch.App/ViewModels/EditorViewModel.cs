using CommunityToolkit.Mvvm.ComponentModel;

namespace Foldfinch.App.ViewModels;

/// <summary>
/// The main editing surface: an ordered grid of pages drawn from one or more source PDFs.
/// M0 is the empty shell; open/remove/reorder/rotate/save are wired in from M2 onward.
/// </summary>
public partial class EditorViewModel : ViewModelBase
{
    private readonly AppServices _services;

    /// <summary>True until a document is opened — drives the empty-state prompt.</summary>
    [ObservableProperty] private bool _isEmpty = true;

    public EditorViewModel(AppServices services)
    {
        _services = services;
    }
}
