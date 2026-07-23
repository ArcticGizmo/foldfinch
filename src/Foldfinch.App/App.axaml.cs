using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Foldfinch.App.Changelog;
using Foldfinch.App.ViewModels;
using Foldfinch.App.Views;
using Foldfinch.Core.Changelog;

namespace Foldfinch.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new AppServices();
            var main = new MainWindow { DataContext = new MainWindowViewModel(services) };
            desktop.MainWindow = main;
            MaybeShowChangelog(services, main);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// On the first launch after an update, pops a "what's new" window listing the changelog entries
    /// newer than the version that last ran here. Records the running version so it only shows once
    /// per update, and honours the user's "don't show" preference.
    /// </summary>
    static void MaybeShowChangelog(AppServices services, Window owner)
    {
        var settings = services.Settings.Get();
        var current = AppVersion.Current;

        if (settings.ShowChangelogOnUpdate && ChangelogMarkdown.LoadEmbedded() is { } markdown)
        {
            var unseen = ChangelogParser.UnseenSince(markdown, settings.LastSeenVersion, current).ToList();
            if (unseen.Count > 0)
            {
                var window = new ChangelogWindow("What's new in Foldfinch", $"Updated to v{current}", unseen,
                    onSuppress: () =>
                    {
                        var s = services.Settings.Get();
                        s.ShowChangelogOnUpdate = false;
                        services.Settings.Save(s);
                    });
                owner.Opened += (_, _) => window.Show(owner);
            }
        }

        if (settings.LastSeenVersion != current)
        {
            settings.LastSeenVersion = current;
            services.Settings.Save(settings);
        }
    }
}
