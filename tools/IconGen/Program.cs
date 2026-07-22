using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Svg;

// Generates every raster icon asset from the single source-of-truth SVG (foldfinch.svg).
//
//   src/Foldfinch.App/Assets/foldfinch.png   256x256 PNG — window + toolbar icon
//   src/Foldfinch.App/Assets/foldfinch.ico   multi-resolution ICO (16..256) — the .exe ApplicationIcon
//   landing-icon.png                          512x512 PNG — the README header logo
//
// Re-run after editing foldfinch.svg:  dotnet run --project tools/IconGen
// (or tools/gen-icons.ps1, which also restores packages.)

string repoRoot = FindRepoRoot(AppContext.BaseDirectory);
string svgPath = Path.Combine(repoRoot, "foldfinch.svg");
if (!File.Exists(svgPath))
{
    Console.Error.WriteLine($"Source SVG not found: {svgPath}");
    return 1;
}

Console.WriteLine($"Source: {svgPath}");
var doc = SvgDocument.Open(svgPath);

// The file declares its size in millimetres; force user-unit (px) sizing so the library renders in
// the same coordinate system as doc.ViewBox / doc.Bounds (both user units), which our transform maps.
if (doc.ViewBox.Width > 0 && doc.ViewBox.Height > 0)
{
    doc.Width = new SvgUnit(SvgUnitType.User, doc.ViewBox.Width);
    doc.Height = new SvgUnit(SvgUnitType.User, doc.ViewBox.Height);
}

// The artwork doesn't fill its viewBox — there's transparent margin. Crop to the drawn content,
// re-center it in a square, and scale that to fill the frame so the icon uses as much of its pixel
// canvas as possible. PAD keeps a sliver of breathing room so anti-aliased edges don't clip.
const float PAD = 0.02f;
var fit = ComputeFit(doc, PAD);
float vbSide = doc.ViewBox.Width > 0 ? doc.ViewBox.Width : fit.Side;
Console.WriteLine($"Fit: content {fit.Box.Width:0.0}x{fit.Box.Height:0.0} cropped from {vbSide:0}x{vbSide:0} viewBox ({vbSide / fit.Side:0.00}x larger)");

// The .ico ships a true frame at each of these sizes so Windows never downscales at runtime.
int[] icoSizes = { 16, 24, 32, 48, 64, 128, 256 };

string assetsDir = Path.Combine(repoRoot, "src", "Foldfinch.App", "Assets");
WritePng(doc, fit, Path.Combine(assetsDir, "foldfinch.png"), 256);
WritePng(doc, fit, Path.Combine(repoRoot, "landing-icon.png"), 512);
WriteIco(doc, fit, Path.Combine(assetsDir, "foldfinch.ico"), icoSizes);

Console.WriteLine("Done.");
return 0;

// Renders the cropped, re-centered content square at the given pixel size with high-quality
// anti-aliasing and a transparent background.
static Bitmap Render(SvgDocument doc, Fit fit, int size)
{
    var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    bmp.SetResolution(96, 96);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    g.Clear(Color.Transparent);
    float scale = size / fit.Side;
    g.ScaleTransform(scale, scale);
    g.TranslateTransform(-fit.OriginX, -fit.OriginY);
    doc.Draw(g);
    return bmp;
}

static void WritePng(SvgDocument doc, Fit fit, string path, int size)
{
    using var bmp = Render(doc, fit, size);
    bmp.Save(path, ImageFormat.Png);
    Console.WriteLine($"  {Path.GetFileName(path)}  {size}x{size}");
}

// Writes a Vista+ ICO whose frames are PNG-compressed (keeps the file small and supports 256px).
static void WriteIco(SvgDocument doc, Fit fit, string path, int[] sizes)
{
    var frames = new List<byte[]>();
    foreach (var size in sizes)
    {
        using var bmp = Render(doc, fit, size);
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        frames.Add(ms.ToArray());
    }

    using var fs = File.Create(path);
    using var w = new BinaryWriter(fs);

    w.Write((ushort)0);             // reserved
    w.Write((ushort)1);             // type = icon
    w.Write((ushort)sizes.Length);  // image count

    int offset = 6 + sizes.Length * 16;
    for (int i = 0; i < sizes.Length; i++)
    {
        int size = sizes[i];
        w.Write((byte)(size >= 256 ? 0 : size)); // width  (0 = 256)
        w.Write((byte)(size >= 256 ? 0 : size)); // height (0 = 256)
        w.Write((byte)0);                        // palette count
        w.Write((byte)0);                        // reserved
        w.Write((ushort)1);                      // colour planes
        w.Write((ushort)32);                     // bits per pixel
        w.Write(frames[i].Length);               // bytes of image data
        w.Write(offset);                         // offset of image data
        offset += frames[i].Length;
    }

    foreach (var frame in frames)
        w.Write(frame);

    Console.WriteLine($"  {Path.GetFileName(path)}  [{string.Join(", ", sizes)}]");
}

// Walks up from the tool's binary location to the directory that holds foldfinch.svg.
static string FindRepoRoot(string start)
{
    var dir = new DirectoryInfo(start);
    while (dir != null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "foldfinch.svg")))
            return dir.FullName;
        dir = dir.Parent;
    }
    return Path.GetFullPath(Path.Combine(start, "..", "..", "..", "..", ".."));
}

// Computes the square crop that tightly frames the SVG's drawn content, centered, grown by `pad`.
static Fit ComputeFit(SvgDocument doc, float pad)
{
    var box = doc.Bounds;
    if (box.Width <= 0 || box.Height <= 0)
    {
        float w = doc.ViewBox.Width > 0 ? doc.ViewBox.Width : 1f;
        float h = doc.ViewBox.Height > 0 ? doc.ViewBox.Height : 1f;
        box = new RectangleF(0, 0, w, h);
    }
    float side = Math.Max(box.Width, box.Height);
    side += side * pad * 2f;
    float cx = box.X + box.Width / 2f;
    float cy = box.Y + box.Height / 2f;
    return new Fit(box, side, cx - side / 2f, cy - side / 2f);
}

// The crop geometry: content bounds, the padded square's side, and its top-left in SVG user units.
readonly record struct Fit(RectangleF Box, float Side, float OriginX, float OriginY);
