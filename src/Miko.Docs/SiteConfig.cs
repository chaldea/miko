// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Miko.Docs;

public sealed class SiteConfig
{
    public string Title { get; set; } = "Miko Docs";
    public string Description { get; set; } = "";
    public string Lang { get; set; } = "zh-CN";
    public string Base { get; set; } = "/";
    public string SrcDir { get; set; } = "docs";
    public bool CleanUrls { get; set; } = true;
    public string? Favicon { get; set; }
    public string? AppleTouchIcon { get; set; }
    public ThemeConfig ThemeConfig { get; set; } = new();
    public HomeConfig Home { get; set; } = new();
    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static SiteConfig Load(string projectDirectory)
    {
        var path = Path.Combine(projectDirectory, "miko.docs.json");
        var config = File.Exists(path)
            ? JsonSerializer.Deserialize<SiteConfig>(File.ReadAllText(path), JsonOptions)
                ?? throw new InvalidDataException("miko.docs.json cannot be null.")
            : new SiteConfig();
        config.Validate();
        return config;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SrcDir) || Path.IsPathRooted(SrcDir) || SrcDir.Split('/', '\\').Contains(".."))
            throw new InvalidDataException("srcDir must be a directory within the documentation project.");
        if (string.IsNullOrWhiteSpace(Base) || !Regex.IsMatch(Base, @"^/(?:[\p{L}\p{N}_-]+/)*$"))
            throw new InvalidDataException("base must start and end with '/', for example '/' or '/miko/'.");
        if (ThemeConfig is null || Home is null || ThemeConfig.Nav is null || ThemeConfig.Sidebar is null || ThemeConfig.SocialLinks is null ||
            Home.Actions is null || Home.Features is null)
            throw new InvalidDataException("Theme, navigation and home configuration must not be null.");
        foreach (var item in Flatten(ThemeConfig.Nav.Concat(ThemeConfig.Sidebar)))
        {
            if (item.Link is not null) Urls.Safe(item.Link);
        }
        if (ThemeConfig.Logo is not null) Urls.Safe(ThemeConfig.Logo);
        if (Favicon is not null) Urls.Safe(Favicon);
        if (AppleTouchIcon is not null) Urls.Safe(AppleTouchIcon);
        if (ThemeConfig.EditLink is not null) Urls.Safe(ThemeConfig.EditLink);
        if (ThemeConfig.OutlineLevel is < 2 or > 3) throw new InvalidDataException("outlineLevel must be 2 or 3.");
        foreach (var social in ThemeConfig.SocialLinks)
        {
            if (social is null || string.IsNullOrWhiteSpace(social.Link)) throw new InvalidDataException("Social links must include a URL.");
            Urls.Safe(social.Link);
        }
        foreach (var action in Home.Actions)
        {
            if (action is null || string.IsNullOrWhiteSpace(action.Link)) throw new InvalidDataException("Home actions must include a link.");
            Urls.Safe(action.Link);
        }
        if (Home.Features.Any(feature => feature is null)) throw new InvalidDataException("Home features must not be null.");
    }

    internal static IEnumerable<NavItem> Flatten(IEnumerable<NavItem> items)
    {
        foreach (var item in items)
        {
            if (item is null || item.Items is null) throw new InvalidDataException("Navigation items must not be null.");
            yield return item;
            foreach (var child in Flatten(item.Items)) yield return child;
        }
    }
}

public sealed class ThemeConfig
{
    public string? Logo { get; set; }
    public List<NavItem> Nav { get; set; } = [];
    public List<NavItem> Sidebar { get; set; } = [];
    public string OutlineTitle { get; set; } = "本页目录";
    public string Footer { get; set; } = "Built with Miko.Docs";
    public string? EditLink { get; set; }
    public bool Search { get; set; } = true;
    public bool Appearance { get; set; } = true;
    public int OutlineLevel { get; set; } = 2;
    public List<SocialLink> SocialLinks { get; set; } = [];
}

public sealed class SocialLink
{
    public string Icon { get; set; } = "github";
    public string Link { get; set; } = "";
}

public sealed class NavItem
{
    public string Text { get; set; } = "";
    public string? Link { get; set; }
    public bool Collapsed { get; set; }
    public List<NavItem> Items { get; set; } = [];
}

public sealed class HomeConfig
{
    public string? Name { get; set; }
    public string? Text { get; set; }
    public string? Tagline { get; set; }
    public List<HomeAction> Actions { get; set; } = [];
    public List<HomeFeature> Features { get; set; } = [];
}

public sealed class HomeAction
{
    public string Text { get; set; } = "";
    public string Link { get; set; } = "/";
    public string Theme { get; set; } = "brand";
}

public sealed class HomeFeature
{
    public string Icon { get; set; } = "✦";
    public string Title { get; set; } = "";
    public string Details { get; set; } = "";
}

internal static class Urls
{
    public static string Safe(string url)
    {
        url = System.Net.WebUtility.HtmlDecode(url).Trim();
        if (url.Any(char.IsControl) || url.Contains('\\') || url.StartsWith("//", StringComparison.Ordinal))
            throw new InvalidDataException($"Unsafe URL: {url}");
        if (Regex.IsMatch(url, @"^[\w+.-]+:") &&
            !Regex.IsMatch(url, @"^(https?|mailto|tel):", RegexOptions.IgnoreCase))
            throw new InvalidDataException($"Unsupported URL scheme: {url}");
        return url;
    }

    public static string Resolve(string url, SiteConfig config, string source = "index.md")
    {
        url = Safe(url);
        if (url.StartsWith('#') || url.StartsWith('?') || Regex.IsMatch(url, @"^[\w+.-]+:")) return url;
        var suffixIndex = url.IndexOfAny(['?', '#']);
        var suffix = suffixIndex < 0 ? "" : url[suffixIndex..];
        var path = suffixIndex < 0 ? url : url[..suffixIndex];
        if (!path.StartsWith('/')) path = "/" + (Path.GetDirectoryName(source)?.Replace('\\', '/') is { Length: > 0 } parent ? parent + "/" : "") + path;
        var parts = new List<string>();
        foreach (var part in path.Split('/'))
        {
            var decoded = Uri.UnescapeDataString(part);
            if (decoded == "." || decoded.Length == 0) continue;
            if (decoded == "..")
            {
                if (parts.Count == 0) throw new InvalidDataException($"Link escapes site root: {url}");
                parts.RemoveAt(parts.Count - 1);
            }
            else parts.Add(part);
        }
        path = string.Join('/', parts) + (path.EndsWith('/') && parts.Count > 0 ? "/" : "");
        if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            path = config.CleanUrls ? path[..^3] : path[..^3] + ".html";
        if (config.CleanUrls)
        {
            if (path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) path = path[..^5];
            if (path == "index") path = "";
            else if (path.EndsWith("/index", StringComparison.OrdinalIgnoreCase)) path = path[..^5];
            else if (path.Length > 0 && !path.EndsWith('/') && !Path.HasExtension(path)) path += "/";
        }
        else if (path.Length > 0 && !path.EndsWith('/') && !Path.HasExtension(path)) path += ".html";
        return config.Base + path + suffix;
    }
}
