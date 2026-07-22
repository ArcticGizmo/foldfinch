using System;
using System.IO;
using SkiaSharp;

namespace Foldfinch.App.Rendering;

/// <summary>
/// Generates the app icon (a folded document mark on an accent tile) as a PNG and a multi-size ICO.
/// Dev-time only, invoked via <c>foldfinch gen-icon &lt;dir&gt;</c>; the produced assets are committed.
/// </summary>
internal static class IconGenerator
{
    public static int Generate(string outDir)
    {
        try
        {
            Directory.CreateDirectory(outDir);

            var png256 = RenderPng(256);
            File.WriteAllBytes(Path.Combine(outDir, "foldfinch.png"), png256);
            WriteIco(Path.Combine(outDir, "foldfinch.ico"), [RenderPng(256), RenderPng(64), RenderPng(32)]);

            Console.WriteLine($"icon assets written to {Path.GetFullPath(outDir)}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"icon generation failed: {ex.Message}");
            return 1;
        }
    }

    static byte[] RenderPng(int size)
    {
        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        float s = size;
        float radius = s * 0.22f;

        // Accent rounded-square tile.
        using (var bg = new SKPaint { Color = new SKColor(0x25, 0x63, 0xEB), IsAntialias = true })
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(0, 0, s, s), radius), bg);

        // White page with a folded top-right corner.
        float m = s * 0.24f;         // page margin
        float fold = s * 0.20f;      // fold size
        var left = m; var top = m * 0.9f; var right = s - m; var bottom = s - m * 0.9f;

        using var page = new SKPaint { Color = SKColors.White, IsAntialias = true };
        using var body = new SKPath();
        body.MoveTo(left, top);
        body.LineTo(right - fold, top);
        body.LineTo(right, top + fold);
        body.LineTo(right, bottom);
        body.LineTo(left, bottom);
        body.Close();
        canvas.DrawPath(body, page);

        // The folded corner (slightly darker).
        using var foldPaint = new SKPaint { Color = new SKColor(0xC7, 0xD2, 0xFE), IsAntialias = true };
        using var foldPath = new SKPath();
        foldPath.MoveTo(right - fold, top);
        foldPath.LineTo(right - fold, top + fold);
        foldPath.LineTo(right, top + fold);
        foldPath.Close();
        canvas.DrawPath(foldPath, foldPaint);

        // A few "text" lines.
        using var line = new SKPaint { Color = new SKColor(0xD1, 0xD5, 0xDB), IsAntialias = true };
        float lx = left + s * 0.06f;
        float lw = right - lx - s * 0.06f;
        float ly = top + fold + s * 0.06f;
        float lh = s * 0.035f;
        float gap = s * 0.075f;
        for (var i = 0; i < 4; i++)
        {
            var w = i == 3 ? lw * 0.6f : lw;
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(lx, ly, lx + w, ly + lh), lh / 2), line);
            ly += gap;
        }

        canvas.Flush();
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>Writes a PNG-compressed ICO (Vista+) containing each image in <paramref name="pngs"/>.</summary>
    static void WriteIco(string path, byte[][] pngs)
    {
        using var fs = File.Create(path);
        using var w = new BinaryWriter(fs);

        w.Write((short)0);            // reserved
        w.Write((short)1);            // type = icon
        w.Write((short)pngs.Length);  // image count

        var offset = 6 + 16 * pngs.Length;
        foreach (var png in pngs)
        {
            var size = SizeOf(png);
            w.Write((byte)(size >= 256 ? 0 : size)); // width (0 = 256)
            w.Write((byte)(size >= 256 ? 0 : size)); // height
            w.Write((byte)0);         // palette
            w.Write((byte)0);         // reserved
            w.Write((short)1);        // colour planes
            w.Write((short)32);       // bits per pixel
            w.Write(png.Length);      // bytes in resource
            w.Write(offset);          // offset
            offset += png.Length;
        }
        foreach (var png in pngs) w.Write(png);
    }

    static int SizeOf(byte[] png)
    {
        // PNG IHDR width is a big-endian int at byte offset 16.
        return (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
    }
}
