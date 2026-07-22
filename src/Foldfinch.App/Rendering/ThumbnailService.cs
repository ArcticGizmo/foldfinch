using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using Foldfinch.Core.Pdf;

namespace Foldfinch.App.Rendering;

/// <summary>
/// Turns PDF pages into cached Avalonia <see cref="Bitmap"/>s for the page grid. Rendering runs on a
/// background thread (PDFium is single-threaded, so renders are serialised); results are cached by
/// (path, page, rotation, width) so reordering or reopening never re-renders the same thumbnail.
/// </summary>
public sealed class ThumbnailService(IPdfRenderer renderer)
{
    private readonly ConcurrentDictionary<string, Bitmap> _cache = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Renders (or returns cached) a thumbnail; null if the renderer produced nothing.</summary>
    public async Task<Bitmap?> GetAsync(string path, int pageIndex, int rotationDegrees, int targetWidth)
    {
        var key = $"{path}|{pageIndex}|{rotationDegrees}|{targetWidth}";
        if (_cache.TryGetValue(key, out var cached)) return cached;

        byte[] png = [];
        await _gate.WaitAsync();
        try
        {
            png = await Task.Run(() => renderer.RenderPagePng(path, pageIndex, rotationDegrees, targetWidth));
        }
        finally
        {
            _gate.Release();
        }

        if (png.Length == 0) return null;

        using var ms = new MemoryStream(png);
        var bitmap = new Bitmap(ms);
        _cache[key] = bitmap;
        return bitmap;
    }
}
