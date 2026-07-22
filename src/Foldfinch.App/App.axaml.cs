using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Foldfinch.App.ViewModels;
using Foldfinch.App.Views;

namespace Foldfinch.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new AppServices();
            desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel(services) };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
