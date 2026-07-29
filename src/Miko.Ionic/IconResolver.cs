using System.Collections.Concurrent;
using Miko.Common;
using Miko.Platform.Resources;

namespace Miko.Ionic;

/// <summary>
/// Resolves Ionicons SVG icon names and external icon sources to <see cref="BackgroundImage"/>
/// instances. Built-in names (kebab-case, see <see cref="Ionicons"/>) are backed by the SVG
/// resources embedded in this assembly (<c>Resources/svg/&lt;name&gt;.svg</c>); external
/// sources (<c>res://</c> / <c>file://</c> / bare paths) go through the registered
/// <see cref="IResourceAssemblyProvider"/> assemblies and the file system — the same resolution
/// paths <see cref="ResourceManager"/> uses for <c>&lt;img&gt;</c>, so assemblies registered via
/// <c>builder.AddResourceAssembly(...)</c> are honored. Results are cached so the same SVG is
/// only decoded once.
/// </summary>
public static class IconResolver
{
    private const string ResourcePrefix = "Miko.Ionic.Resources.svg.";

    private static readonly ConcurrentDictionary<string, BackgroundImage?> _cache = new();

    /// <summary>
    /// Loads the built-in icon with the given kebab-case name (e.g. <c>"triangle"</c>, see
    /// <see cref="Ionicons"/> for strongly-typed constants). Returns <c>null</c> if no
    /// matching SVG resource is embedded. The image is a monochrome template tinted with the
    /// element's <c>color</c> at draw time (CSS <c>fill: currentColor</c>).
    /// </summary>
    public static BackgroundImage? Load(string? iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
            return null;

        return _cache.GetOrAdd("name:" + iconName, static (_, name) =>
        {
            var resourceName = ResourcePrefix + name + ".svg";
            try
            {
                var image = BackgroundImage.FromResource(typeof(IconResolver).Assembly, resourceName);
                // Ionicons glyphs are monochrome masks tinted via `color` (CSS fill: currentColor).
                image.IsTemplate = true;
                return image;
            }
            catch (InvalidOperationException)
            {
                // Unknown icon name — resource not found.
                return null;
            }
        }, iconName);
    }

    /// <summary>
    /// Loads an external icon source (Ionic's <c>src</c>): <c>res://</c> embedded resources are
    /// searched across the assemblies supplied by <paramref name="assemblyProvider"/> (mirroring
    /// <see cref="ResourceManager"/>), <c>file://</c> and bare paths are read from disk. Returns
    /// <c>null</c> when the source cannot be located or decoded. External images render as-is
    /// (no template tinting) — unlike the built-in monochrome glyphs.
    /// </summary>
    public static BackgroundImage? LoadSource(string? src, IResourceAssemblyProvider? assemblyProvider)
    {
        var source = MediaSource.Parse(src);
        if (source.IsEmpty || source.IsNetwork)
            return null;

        // Negative results are NOT cached: the provider/assembly set can grow after the first
        // lookup (e.g. AddResourceAssembly called later, or a test harness that renders once
        // before services are injected) — a cached null would poison every later resolve.
        var key = "src:" + source.Raw;
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var image = LoadSourceCore(source, assemblyProvider);
        if (image is not null)
            _cache[key] = image;
        return image;
    }

    private static BackgroundImage? LoadSourceCore(MediaSource source, IResourceAssemblyProvider? assemblyProvider)
    {
        try
        {
            return source.Scheme switch
            {
                MediaSourceScheme.Resource => LoadFromAssemblies(source.Value, assemblyProvider),
                MediaSourceScheme.File => LoadFromFile(source.Value),
                MediaSourceScheme.Data => LoadFromData(source.Value),
                _ => null,
            };
        }
        catch
        {
            // A missing/undecodable icon must not break rendering — render the empty icon box.
            return null;
        }
    }

    private static BackgroundImage? LoadFromAssemblies(string resourceName, IResourceAssemblyProvider? assemblyProvider)
    {
        foreach (var assembly in assemblyProvider?.GetResourceAssemblies() ?? Enumerable.Empty<System.Reflection.Assembly>())
        {
            try
            {
                return BackgroundImage.FromResource(assembly, resourceName);
            }
            catch (InvalidOperationException)
            {
                // Not in this assembly — try the next registered one.
            }
        }
        return null;
    }

    private static BackgroundImage? LoadFromFile(string path)
    {
        // 与 ResourceManager.DecodeFile 一致：绝对路径直接用，相对路径按应用目录解析。
        var resolved = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
        return File.Exists(resolved) ? BackgroundImage.FromFile(resolved) : null;
    }

    private static BackgroundImage? LoadFromData(string dataUri)
    {
        // data:[<mime>];base64,<payload> — 仅支持 base64 形式。
        var comma = dataUri.IndexOf(',');
        if (comma < 0) return null;
        var meta = dataUri.Substring(0, comma);
        if (!meta.Contains("base64", StringComparison.OrdinalIgnoreCase)) return null;
        return BackgroundImage.FromBase64(dataUri.Substring(comma + 1));
    }
}
