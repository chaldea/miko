// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Text;
using System.Text.Json;

namespace Miko.Docs;

public sealed record BuildResult(string OutputDirectory, int PageCount, SiteConfig Config);
internal sealed record SitePage(string Source, string Url, string Output, MarkdownPage Content);

public sealed class SiteBuilder
{
    private const string Manifest = ".miko-manifest.json";

    public BuildResult Build(string projectDirectory, string outputDirectory, string? basePath = null)
    {
        projectDirectory = Path.GetFullPath(projectDirectory);
        outputDirectory = Path.GetFullPath(outputDirectory);
        var config = SiteConfig.Load(projectDirectory);
        if (basePath is not null)
        {
            config.Base = basePath;
            config.Validate();
        }
        var sourceDirectory = Paths.Within(projectDirectory, config.SrcDir);
        if (!Directory.Exists(sourceDirectory)) throw new DirectoryNotFoundException($"Documentation directory not found: {sourceDirectory}");
        if (Paths.Contains(sourceDirectory, outputDirectory) || Paths.Contains(outputDirectory, sourceDirectory) || Paths.Contains(outputDirectory, projectDirectory))
            throw new InvalidDataException("The output directory must be separate from source files and cannot contain the project directory.");
        Paths.NoLinks(sourceDirectory);
        Paths.NoLinks(outputDirectory);
        var renderer = new MarkdownRenderer();
        var files = EnumerateSource(sourceDirectory).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        var pages = new List<SitePage>();
        foreach (var file in files.Where(file => file.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            var source = Path.GetRelativePath(sourceDirectory, file).Replace('\\', '/');
            if (source.StartsWith("public/", StringComparison.Ordinal)) continue;
            try
            {
                var content = renderer.Render(File.ReadAllText(file), config, source);
                var stem = source[..^3];
                var output = source == "404.md" ? "404.html" : config.CleanUrls && Path.GetFileName(stem) != "index" ? stem + "/index.html" : stem + ".html";
                pages.Add(new SitePage(source, Urls.Resolve("/" + source, config), output, content));
            }
            catch (Exception ex) when (ex is InvalidDataException or JsonException)
            { throw new InvalidDataException($"{source}: {ex.Message}", ex); }
        }
        if (pages.Count == 0) throw new InvalidDataException($"No Markdown pages found in {sourceDirectory}.");

        // Build everything in memory first: invalid input must leave the previous working site intact.
        var generated = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        void Add(string path, byte[] data)
        {
            if (!generated.TryAdd(path, data)) throw new InvalidDataException($"Output collision: {path}. Check page names and public assets.");
        }
        void Text(string path, string data) => Add(path, Encoding.UTF8.GetBytes(data));
        foreach (var page in pages) Text(page.Output, HtmlTemplate.Render(page, pages, config));
        if (!pages.Any(page => page.Output == "404.html"))
        {
            var missing = renderer.Render("# 404\n\n页面不存在。\n\n[返回首页](/)", config, "404.md");
            Text("404.html", HtmlTemplate.Render(new SitePage("404.md", config.Base + "404.html", "404.html", missing), pages, config));
        }
        var assembly = typeof(SiteBuilder).Assembly;
        foreach (var name in assembly.GetManifestResourceNames().Where(name => name.StartsWith("Miko.Docs.Assets.", StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var buffer = new MemoryStream(); stream.CopyTo(buffer);
            Add("assets/" + name["Miko.Docs.Assets.".Length..], buffer.ToArray());
        }
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(sourceDirectory, file).Replace('\\', '/');
            var isPublic = relative.StartsWith("public/", StringComparison.Ordinal);
            if (!isPublic && file.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) continue;
            if (isPublic) relative = relative[7..];
            if (relative == Manifest || relative.StartsWith("__miko/", StringComparison.Ordinal))
                throw new InvalidDataException($"Reserved output path: {relative}");
            Add(relative, File.ReadAllBytes(file));
        }
        Text("assets/search-index.json", JsonSerializer.Serialize(config.ThemeConfig.Search ? pages.Where(page => page.Source != "404.md")
            .Select(page => new { title = page.Content.Title, url = page.Url, text = page.Content.PlainText }).ToArray() : [], SiteConfig.JsonOptions));
        Text("site-info.json", JsonSerializer.Serialize(new { config.Base }, SiteConfig.JsonOptions));

        var oldManifest = Path.Combine(outputDirectory, Manifest);
        var oldFiles = File.Exists(oldManifest) ? JsonSerializer.Deserialize<string[]>(File.ReadAllText(oldManifest)) ?? [] : [];
        // Validate all paths (including a previous manifest) before writing or removing anything.
        foreach (var file in generated.Keys.Concat(oldFiles)) Paths.NoLinks(Paths.Within(outputDirectory, file));
        Directory.CreateDirectory(outputDirectory);
        foreach (var (file, data) in generated)
        {
            var destination = Paths.Within(outputDirectory, file);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (!File.Exists(destination) || !File.ReadAllBytes(destination).AsSpan().SequenceEqual(data))
            {
                var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllBytes(temporary, data);
                File.Move(temporary, destination, true);
            }
        }
        foreach (var old in oldFiles.Where(file => !generated.ContainsKey(file))) File.Delete(Paths.Within(outputDirectory, old));
        File.WriteAllText(oldManifest, JsonSerializer.Serialize(generated.Keys.Order(), SiteConfig.JsonOptions));
        return new BuildResult(outputDirectory, pages.Count, config);
    }

    private static IEnumerable<string> EnumerateSource(string root)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(root))
        {
            var name = Path.GetFileName(entry);
            if (name.StartsWith('.') || name is "bin" or "obj" or "node_modules") continue;
            if (File.GetAttributes(entry).HasFlag(FileAttributes.ReparsePoint)) throw new InvalidDataException($"Symbolic links are not supported in documentation sources: {entry}");
            if (Directory.Exists(entry))
            {
                foreach (var file in EnumerateSource(entry)) yield return file;
            }
            else yield return entry;
        }
    }
}

internal static class Paths
{
    private static readonly StringComparison Comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    public static bool Contains(string root, string path) => path.Equals(root, Comparison) || path.StartsWith(Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar, Comparison);
    public static string Within(string root, string relative)
    {
        if (Path.IsPathRooted(relative)) throw new InvalidDataException($"Expected relative path: {relative}");
        var result = Path.GetFullPath(Path.Combine(root, relative));
        if (!Contains(root, result) || result.Equals(root, Comparison)) throw new InvalidDataException($"Path escapes directory: {relative}");
        return result;
    }
    public static void NoLinks(string path)
    {
        for (var current = path; current is not null; current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                throw new InvalidDataException($"Symbolic links are not supported in build paths: {current}");
    }
}
