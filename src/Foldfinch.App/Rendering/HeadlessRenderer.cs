using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using Foldfinch.App.Services;
using Foldfinch.App.ViewModels;
using Foldfinch.App.Views;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

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

            // Empty state.
            Capture(new MainWindowViewModel(new AppServices()), Path.Combine(outDir, "main_empty.png"));

            // Loaded state: author a real sample PDF and open it so the grid shows true thumbnails.
            var sample = Path.Combine(Path.GetTempPath(), "foldfinch-render", "sample.pdf");
            Directory.CreateDirectory(Path.GetDirectoryName(sample)!);
            CreateSamplePdf(sample, 6);

            var services = new AppServices(new SingleFileDialogs(sample));
            var vm = new MainWindowViewModel(services);
            // Async work posts continuations to the (idle) UI thread; pump it rather than block-wait,
            // which would deadlock the dispatcher.
            PumpUntilComplete(vm.Editor.AddPdfCommand.ExecuteAsync(null));
            PumpUntilComplete(vm.Editor.LoadThumbnailsAsync());
            Capture(vm, Path.Combine(outDir, "main_loaded.png"));

            // Selection state: highlight a couple of pages so the selection styling shows.
            if (vm.Editor.Pages.Count >= 4)
            {
                vm.Editor.SelectSingle(vm.Editor.Pages[1]);
                vm.Editor.ToggleSelect(vm.Editor.Pages[3]);
                Capture(vm, Path.Combine(outDir, "main_selection.png"));
            }

            // Rotated state: rotate one page so the thumbnail (and toolbar) reflect it.
            if (vm.Editor.Pages.Count >= 3)
            {
                vm.Editor.SelectSingle(vm.Editor.Pages[2]);
                vm.Editor.RotateClockwiseCommand.Execute(null);
                PumpUntilComplete(vm.Editor.LoadThumbnailsAsync());
                Capture(vm, Path.Combine(outDir, "main_rotated.png"));
            }

            // Combined state: open one PDF, add a second, so per-source chips appear.
            var sample2 = Path.Combine(Path.GetTempPath(), "foldfinch-render", "appendix.pdf");
            CreateSamplePdf(sample2, 3);
            var combineServices = new AppServices(new QueueFileDialogs(sample, sample2));
            var combined = new MainWindowViewModel(combineServices);
            PumpUntilComplete(combined.Editor.AddPdfCommand.ExecuteAsync(null));
            PumpUntilComplete(combined.Editor.AddPdfCommand.ExecuteAsync(null));
            PumpUntilComplete(combined.Editor.LoadThumbnailsAsync());
            Capture(combined, Path.Combine(outDir, "main_combined.png"));

            Console.WriteLine($"rendered to {Path.GetFullPath(outDir)}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"headless render failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>Runs the dispatcher's queued jobs until <paramref name="task"/> finishes (headless helper).</summary>
    static void PumpUntilComplete(Task task)
    {
        while (!task.IsCompleted)
            Dispatcher.UIThread.RunJobs();
        task.GetAwaiter().GetResult(); // observe any exception
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

    /// <summary>Authors a simple multi-page PDF (vector shapes only, no fonts) for the render snapshot.</summary>
    static void CreateSamplePdf(string path, int pages)
    {
        XColor[] bars = [XColors.SteelBlue, XColors.SeaGreen, XColors.IndianRed, XColors.Goldenrod, XColors.MediumPurple, XColors.Teal];
        using var doc = new PdfDocument();
        for (var i = 0; i < pages; i++)
        {
            var page = doc.AddPage();
            page.Width = XUnit.FromPoint(420);
            page.Height = XUnit.FromPoint(560);
            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawRectangle(XBrushes.White, 0, 0, 420, 560);
            gfx.DrawRectangle(new XSolidBrush(bars[i % bars.Length]), 40, 40, 340, 90);
            for (var line = 0; line < 8; line++)
                gfx.DrawRectangle(XBrushes.Gainsboro, 40, 170 + line * 34, 340 - (line % 3) * 40, 14);
        }
        doc.Save(path);
    }

    /// <summary>A file-dialog stub that returns one preset path from Open (for headless rendering).</summary>
    private sealed class SingleFileDialogs(string path) : IFileDialogService
    {
        public Task<IReadOnlyList<string>> OpenPdfsAsync(string title) => Task.FromResult<IReadOnlyList<string>>([path]);
        public Task<string?> SavePdfAsync(string suggestedName) => Task.FromResult<string?>(null);
    }

    /// <summary>A file-dialog stub that returns successive preset paths from Open (open then add).</summary>
    private sealed class QueueFileDialogs(params string[] paths) : IFileDialogService
    {
        private readonly Queue<string> _paths = new(paths);
        public Task<IReadOnlyList<string>> OpenPdfsAsync(string title) =>
            Task.FromResult<IReadOnlyList<string>>(_paths.Count > 0 ? [_paths.Dequeue()] : []);
        public Task<string?> SavePdfAsync(string suggestedName) => Task.FromResult<string?>(null);
    }
}
