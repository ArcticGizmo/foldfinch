using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using Foldfinch.App.ViewModels;
using Foldfinch.App.Views;

namespace Foldfinch.App.Rendering;

/// <summary>
/// Renders the app's views to PNG on a headless Skia platform, so the UI can be eyeballed without a
/// display. Invoked via <c>foldfinch render &lt;dir&gt;</c>. Best-effort — capture problems are logged,
/// never fatal.
/// </summary>
internal static class HeadlessRenderer
{
    public static int RenderAll(string outDir)
    {
        Directory.CreateDirectory(outDir);
        try
        {
            AppBuilder.Configure<App>()
                .UseSkia()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
                .WithInterFont()
                .SetupWithoutStarting();

            var services = new AppServices();

            // Empty state.
            Capture(new MainWindowViewModel(services), Path.Combine(outDir, "main_empty.png"));

            // Loaded state: the live store can't be populated headlessly (no file picker), so the
            // editor's display state is set directly for the snapshot.
            var loaded = new MainWindowViewModel(services);
            loaded.Editor.DocumentName = "report.pdf";
            loaded.Editor.SourceSummaries.Add("report.pdf — 12 pages");
            loaded.Editor.SourceSummaries.Add("appendix.pdf — 3 pages");
            loaded.Editor.PageCount = 15;
            loaded.Editor.IsEmpty = false;
            loaded.Editor.Status = "Added appendix.pdf";
            Capture(loaded, Path.Combine(outDir, "main_loaded.png"));

            Console.WriteLine($"rendered to {Path.GetFullPath(outDir)}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"headless render failed: {ex.Message}");
            return 1;
        }
    }

    static void Capture(MainWindowViewModel vm, string path)
    {
        var window = new MainWindow { DataContext = vm };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var frame = window.CaptureRenderedFrame();
        frame?.Save(path);
        window.Close();
    }
}
