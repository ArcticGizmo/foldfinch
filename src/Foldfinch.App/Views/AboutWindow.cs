using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Foldfinch.App.Changelog;
using Foldfinch.App.Updates;
using Foldfinch.Core.Changelog;

namespace Foldfinch.App.Views;

/// <summary>The About dialog: version, a manual check-for-updates/install flow, and a "what's new" link.</summary>
public sealed class AboutWindow : Window
{
    private readonly TextBlock _updateStatus;
    private readonly Button _installButton;
    private UpdateCheckResult? _lastCheck;

    public AboutWindow()
    {
        Title = "About Foldfinch";
        Width = 420;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush("FormBgBrush");

        var name = new TextBlock { Text = "Foldfinch", FontSize = 22, FontWeight = FontWeight.SemiBold, Foreground = Brush("TitleBrush") };
        var version = new TextBlock { Text = $"Version {AppVersion.Current}", FontSize = 13, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 2, 0, 0) };
        var blurb = new TextBlock
        {
            Text = "Remove, combine, reorder, and rotate PDF pages.",
            FontSize = 13, Foreground = Brush("FgBrush"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0),
        };

        _updateStatus = new TextBlock { FontSize = 12, Foreground = Brush("MutedBrush"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 14, 0, 0) };

        var check = FlatButton("Check for updates", accent: false);
        check.Click += async (_, _) => await CheckAsync();

        _installButton = FlatButton("Download & install", accent: true);
        _installButton.IsVisible = false;
        _installButton.Click += async (_, _) => await InstallAsync();

        var whatsNew = FlatButton("What's new", accent: false);
        whatsNew.Click += (_, _) => ShowChangelog();

        var close = FlatButton("Close", accent: true);
        close.Click += (_, _) => Close();

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 16, 0, 0), Children = { check, _installButton } };
        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0), Children = { whatsNew, close },
        };

        Content = new StackPanel { Margin = new Thickness(24), Children = { name, version, blurb, _updateStatus, actions, footer } };
    }

    private async System.Threading.Tasks.Task CheckAsync()
    {
        _installButton.IsVisible = false;
        _updateStatus.Text = "Checking for updates…";
        _lastCheck = await UpdateChecker.CheckDetailedAsync();
        _updateStatus.Text = _lastCheck.Availability switch
        {
            UpdateAvailability.Available => $"Version {_lastCheck.AvailableVersion} is available (you have {_lastCheck.CurrentVersion}).",
            UpdateAvailability.UpToDate => "You're on the latest version.",
            UpdateAvailability.NotApplicable => "Updates aren't available in this build (no release feed configured).",
            _ => "Couldn't check for updates — try again later.",
        };
        _installButton.IsVisible = _lastCheck.Availability == UpdateAvailability.Available;
    }

    private async System.Threading.Tasks.Task InstallAsync()
    {
        if (_lastCheck is not { Availability: UpdateAvailability.Available }) return;
        _updateStatus.Text = $"Downloading version {_lastCheck.AvailableVersion}…";
        try { await UpdateChecker.ApplyAsync(_lastCheck); } // restarts on success
        catch { _updateStatus.Text = "Update failed to install — try again later."; }
    }

    private void ShowChangelog()
    {
        var markdown = ChangelogMarkdown.LoadEmbedded();
        var sections = markdown is null ? [] : ChangelogParser.Parse(markdown).ToList();
        new ChangelogWindow("What's new in Foldfinch", "Recent releases", sections).ShowDialog(this);
    }

    static Button FlatButton(string text, bool accent) => new()
    {
        Content = text,
        Padding = new Thickness(14, 8),
        CornerRadius = new CornerRadius(6),
        FontWeight = accent ? FontWeight.SemiBold : FontWeight.Normal,
        Background = Brush(accent ? "AccentBrush" : "ButtonBgBrush"),
        Foreground = accent ? new SolidColorBrush(Colors.White) : Brush("FgBrush"),
        Cursor = new Cursor(StandardCursorType.Hand),
    };

    static IBrush? Brush(string key)
        => Application.Current is { } app && app.TryFindResource(key, out var v) && v is IBrush b ? b : null;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(); e.Handled = true; }
        base.OnKeyDown(e);
    }
}
