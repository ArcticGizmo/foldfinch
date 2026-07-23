using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Foldfinch.App.Views;

/// <summary>The user's answer to the "unsaved changes" prompt shown when closing.</summary>
public enum CloseChoice { Save, Discard, Cancel }

/// <summary>A minimal modal asking whether to save, discard, or cancel when closing with unsaved changes.</summary>
public sealed class ConfirmDialog : Window
{
    private ConfirmDialog()
    {
        Width = 420;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Title = "Unsaved changes";
        Background = new SolidColorBrush(Color.Parse("#F3F3F6"));

        var heading = new TextBlock
        {
            Text = "Save changes before closing?",
            FontSize = 16,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#11131A")),
        };
        var body = new TextBlock
        {
            Text = "Your document has unsaved changes. They'll be lost if you close without saving.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#6B7280")),
            Margin = new(0, 8, 0, 0),
        };

        var save = Button("Save As…", CloseChoice.Save, primary: true);
        var discard = Button("Discard", CloseChoice.Discard, primary: false);
        var cancel = Button("Cancel", CloseChoice.Cancel, primary: false);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new(0, 20, 0, 0),
            Children = { cancel, discard, save },
        };

        Content = new StackPanel
        {
            Margin = new(24),
            Children = { heading, body, buttons },
        };
    }

    private Button Button(string text, CloseChoice choice, bool primary)
    {
        var button = new Button
        {
            Content = text,
            Padding = new(14, 8),
            CornerRadius = new(6),
            Cursor = new(Avalonia.Input.StandardCursorType.Hand),
            Background = new SolidColorBrush(Color.Parse(primary ? "#2563EB" : "#EDEDF2")),
            Foreground = new SolidColorBrush(Color.Parse(primary ? "#FFFFFF" : "#1F2430")),
        };
        button.Click += (_, _) => Close(choice);
        return button;
    }

    /// <summary>Shows the dialog over <paramref name="owner"/> and returns the user's choice.</summary>
    public static Task<CloseChoice> AskAsync(Window owner) => new ConfirmDialog().ShowDialog<CloseChoice>(owner);
}
