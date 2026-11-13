using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using SkiaSharp;
using Svg.Skia;

// All assets are rendered from the hand-drawn vector master; no screenshot or font is used.
if (args.Any(arg => arg != "--check"))
    throw new ArgumentException("Usage: dotnet run --project tools/Miko.Branding -- [--check]");
var check = args.Contains("--check");
var root = new DirectoryInfo(AppContext.BaseDirectory);
while (root is not null && !File.Exists(Path.Combine(root.FullName, "miko.slnx"))) root = root.Parent;
if (root is null) throw new DirectoryNotFoundException("Run from a Miko repository checkout.");
var masterPath = Path.Combine(root.FullName, "assets/branding/miko.svg");
var master = XDocument.Load(masterPath);
var cache = new Dictionary<(string Variant, int Size), byte[]>();
var stale = new List<string>();
var generated = new HashSet<string>(StringComparer.Ordinal);

Write("assets/branding/miko.png", Png("rounded", 1024));
Write("assets/branding/icon.png", Png("rounded", 128));
var ico = Ico([16, 24, 32, 48, 64, 128, 256]);
Write("assets/branding/miko.ico", ico);

foreach (var webRoot in new[] { "docs/Miko.Docs.Web/Content/public", "docs/Miko.Docs.Demo.Browser/wwwroot" })
{
    Write($"{webRoot}/logo.svg", File.ReadAllBytes(masterPath));
    Write($"{webRoot}/favicon.ico", Ico([16, 32, 48]));
    Write($"{webRoot}/apple-touch-icon.png", Png("square", 180));
}

Write("templates/MikoRazorApp/app.ico", ico);
var apps = new[] { "templates/MikoMultiplatformApp" }
    .Concat(Directory.EnumerateDirectories(Path.Combine(root.FullName, "examples/Multiplatform"))
        .Select(path => Path.GetRelativePath(root.FullName, path).Replace('\\', '/')));
foreach (var app in apps)
{
    var name = Path.GetFileName(app);
    foreach (var head in new[] { "Desktop", "Simulator" })
        Write($"{app}/{name}.{head}/app.ico", ico);

    var android = $"{app}/{name}.Android/Resources";
    foreach (var (density, fallback, layer) in new[]
    {
        ("mdpi", 48, 108), ("hdpi", 72, 162), ("xhdpi", 96, 216),
        ("xxhdpi", 144, 324), ("xxxhdpi", 192, 432)
    })
    {
        Write($"{android}/mipmap-{density}/appicon.png", Png("rounded", fallback));
        Write($"{android}/mipmap-{density}/appicon_foreground.png", Png("adaptive", layer));
        Write($"{android}/mipmap-{density}/appicon_background.png", Png("background", layer));
    }

    var catalog = $"{app}/{name}.iOS/Assets.xcassets";
    using var contents = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, catalog, "AppIcon.appiconset/Contents.json")));
    foreach (var entry in contents.RootElement.GetProperty("images").EnumerateArray())
    {
        var filename = entry.GetProperty("filename").GetString()!;
        var points = double.Parse(entry.GetProperty("size").GetString()!.Split('x')[0], CultureInfo.InvariantCulture);
        var scale = double.Parse(entry.GetProperty("scale").GetString()!.TrimEnd('x'), CultureInfo.InvariantCulture);
        Write($"{catalog}/AppIcon.appiconset/{filename}", Png("square", (int)Math.Round(points * scale)));
    }
    foreach (var scale in new[] { 1, 2, 3 })
        Write($"{catalog}/LaunchLogo.imageset/logo{scale}x.png", Png("mark", 120 * scale));
    Write($"{catalog}/LaunchLogo.imageset/Contents.json", Encoding.UTF8.GetBytes("""
        {
          "images": [
            { "idiom": "universal", "filename": "logo1x.png", "scale": "1x" },
            { "idiom": "universal", "filename": "logo2x.png", "scale": "2x" },
            { "idiom": "universal", "filename": "logo3x.png", "scale": "3x" }
          ],
          "info": { "version": 1, "author": "Miko" }
        }
        """ + "\n"));
}

// The checked-in desktop examples use the same starter branding as the templates.
foreach (var project in Directory.EnumerateDirectories(Path.Combine(root.FullName, "examples/Windows")))
    Write(Path.GetRelativePath(root.FullName, Path.Combine(project, "app.ico")), ico);

if (stale.Count > 0)
{
    Console.Error.WriteLine("Missing or outdated branding assets:\n" + string.Join('\n', stale));
    return 1;
}
Console.WriteLine($"{(check ? "Verified" : "Generated")} {generated.Count} branding assets from assets/branding/miko.svg.");
return 0;

void Write(string relative, byte[] data)
{
    if (!generated.Add(relative)) return;
    var path = Path.Combine(root.FullName, relative);
    if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(data)) return;
    if (check) { stale.Add(relative); return; }
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllBytes(path, data);
}

byte[] Png(string variant, int size)
{
    if (cache.TryGetValue((variant, size), out var data)) return data;
    var svg = new XDocument(master);
    var background = svg.Descendants().Single(element => (string?)element.Attribute("id") == "background");
    var mark = svg.Descendants().Single(element => (string?)element.Attribute("id") == "mark");
    if (variant is "square" or "background") background.Attribute("rx")!.Remove();
    if (variant == "background") mark.Remove();
    if (variant is "mark" or "adaptive") background.Remove();
    // Android's 108dp layers are clipped by the launcher. Keep the entire mark inside
    // the central 66dp safe circle, even when the launcher uses a circular mask.
    if (variant == "adaptive") mark.SetAttributeValue("transform", "translate(12 12) scale(0.9)");
    using var input = new MemoryStream(Encoding.UTF8.GetBytes(svg.ToString()));
    using var renderer = new SKSvg();
    var picture = renderer.Load(input) ?? throw new InvalidDataException("Cannot render Miko SVG.");
    // Render each size independently at 4x for clean small icons (never resize a PNG).
    var opaque = variant is "square" or "background";
    using var large = SKSurface.Create(new SKImageInfo(size * 4, size * 4, SKColorType.Rgba8888,
        opaque ? SKAlphaType.Opaque : SKAlphaType.Premul));
    large.Canvas.Clear(opaque ? SKColor.Parse("#e9e8ff") : SKColors.Transparent);
    large.Canvas.Scale(size * 4f / 240);
    large.Canvas.DrawPicture(picture);
    using var snapshot = large.Snapshot();
    using var output = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888,
        opaque ? SKAlphaType.Opaque : SKAlphaType.Premul));
    output.Canvas.Clear(opaque ? SKColor.Parse("#e9e8ff") : SKColors.Transparent);
    output.Canvas.DrawImage(snapshot, new SKRect(0, 0, size, size), new SKSamplingOptions(SKCubicResampler.Mitchell));
    using var image = output.Snapshot();
    using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
    return cache[(variant, size)] = encoded.ToArray();
}

byte[] Ico(int[] sizes)
{
    var frames = sizes.Select(size => Png("rounded", size)).ToArray();
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream);
    writer.Write((ushort)0);
    writer.Write((ushort)1);
    writer.Write((ushort)sizes.Length);
    var offset = 6 + 16 * sizes.Length;
    for (var i = 0; i < sizes.Length; i++)
    {
        writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
        writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(frames[i].Length);
        writer.Write(offset);
        offset += frames[i].Length;
    }
    foreach (var frame in frames) writer.Write(frame);
    return stream.ToArray();
}
