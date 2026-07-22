using Avalonia.Controls;
using Avalonia.Interactivity;
using Foldfinch.App.ViewModels;

namespace Foldfinch.App.Views;

public partial class MainWindow : Window
{
    // Set once the user has resolved the unsaved-changes prompt, so the second Close() goes through.
    private bool _forceClose;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_forceClose) return;
        if (DataContext is not MainWindowViewModel vm || !vm.Editor.IsDirty) return;

        e.Cancel = true; // hold the close until the user answers
        var choice = await ConfirmDialog.AskAsync(this);

        if (choice == CloseChoice.Cancel) return;
        if (choice == CloseChoice.Save)
        {
            await vm.Editor.SaveCommand.ExecuteAsync(null);
            if (vm.Editor.IsDirty) return; // save failed or was cancelled -> stay open
        }

        _forceClose = true;
        Close();
    }

    private void OnAboutClick(object? sender, RoutedEventArgs e) => new AboutWindow().ShowDialog(this);
}
