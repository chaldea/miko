// Migrated from the standalone Miko.Docs regression runner.
using System.Text.Json;
using Miko.Docs;

namespace Miko.Docs.Tests;

public class SiteGeneratorTests
{
private static readonly (string Name, Action Run)[] Cases = new (string Name, Action Run)[]
{
    ("Markdown structure, Unicode headings and duplicate anchors", () =>
    {
        var page = Render("# 文档\n\n## Hello *world*\n\n## Hello world\n\n## 中文 标题 {#custom}\n\n## Hello world-1");
        Equal("文档", page.Title);
        Equal("hello-world", page.Headings[1].Id);
        Equal("hello-world-1", page.Headings[2].Id);
        Equal("custom", page.Headings[3].Id);
        Equal("hello-world-1-1", page.Headings[4].Id);
        Has(page.Html, "<em>world</em>");
    }),
    ("Front matter, quoted values and unsupported metadata", () =>
    {
        var page = Render("---\ntitle: 'A: title'\ndescription: \"quoted description\"\nlayout: page\nsidebar: false\noutline: false\n---\n# Content");
        Equal("A: title", page.Title); Equal("page", page.Layout);
        True(!page.Sidebar && !page.Outline, "page flags");
        Throws(() => Render("---\ntitle: Missing terminator"));
        Throws(() => Render("---\nhero:\n  name: nope\n---\n# A"));
    }),
    ("C# headings retain language suffixes and tables preserve code pipes", () =>
    {
        var page = Render("# C#\n\n## F# ##\n\n| Code | Value |\n| --- | --- |\n| `a | b` | x \\| y |");
        Equal("C#", page.Title); Equal("F#", page.Headings[1].Text);
        Has(page.Html, "<code>a | b</code>"); Has(page.Html, ">x | y</td>");
    }),
    ("Inline escaping, code isolation, entities and emphasis", () =>
    {
        var page = Render("# Safe\n\n<script>alert(1)</script> **bold** *italic* ~~gone~~ `**literal**` &amp; \\*escaped\\*\n\n***both***\n\nline  \nbreak");
        Has(page.Html, "&lt;script&gt;"); Has(page.Html, "<strong>bold</strong>");
        Has(page.Html, "<code>**literal**</code>"); Has(page.Html, "<strong><em>both</em></strong>");
        Has(page.Html, "<br>"); True(!page.Html.Contains("<script>"), "HTML must be escaped");
    }),
    ("Relative links, base path, queries, fragments and images", () =>
    {
        var page = new MarkdownRenderer().Render("# A\n\n[Next](../api/index.md?q=1#方法) ![logo](/logo.svg) [local](#a) [external](https://example.com/a(b))", new SiteConfig { Base = "/miko/" }, "guide/start.md");
        Has(page.Html, "href=\"/miko/api/?q=1#"); Has(page.Html, "src=\"/miko/logo.svg\"");
        Has(page.Html, "href=\"#a\""); Has(page.Html, "https://example.com/a(b)");
        var flat = new MarkdownRenderer().Render("[Next](next.md#ok)", new SiteConfig { CleanUrls = false }, "guide/start.md");
        Has(flat.Html, "href=\"/guide/next.html#ok\"");
    }),
    ("Unsafe URLs and escaping site root are rejected", () =>
    {
        foreach (var url in new[] { "javascript:alert(1)", "data:text/html,bad", "//evil.example", "../../outside.md", "java&#115;cript:alert(1)" })
            Throws(() => Render($"[bad]({url})"));
    }),
    ("Fenced code, highlighting, line numbers and fence lengths", () =>
    {
        var page = Render("````csharp {2}:line-numbers\n<script>\n```\nlast\n````");
        Has(page.Html, "language-csharp line-numbers-mode"); Has(page.Html, "class=\"line highlighted\">```");
        Has(page.Html, "&lt;script&gt;"); Has(page.Html, "line-numbers-wrapper");
    }),
    ("Containers nest and ignore delimiters inside code", () =>
    {
        var page = Render("::: tip Outer\nText\n\n::: details Inner\n```txt\n:::\n```\n:::\n:::");
        Has(page.Html, "custom-block tip"); Has(page.Html, "<details class=\"custom-block details\">");
        Has(page.Html, "class=\"line\">:::");
        Throws(() => Render("::: warning\nUnclosed"));
    }),
    ("Tables, nested lists, tasks, quotes and reference links", () =>
    {
        var page = Render("| Left | Right |\n| :--- | ---: |\n| **bold** | value |\n\n- parent\n  - child\n- [x] done\n\n> quote\n\n[API][ref]\n\n[ref]: api.md \"Reference\"");
        Has(page.Html, "<table>"); Has(page.Html, "text-align:right");
        Has(page.Html, "<li><p>parent</p>\n<ul>"); Has(page.Html, " checked>");
        Has(page.Html, "<li class=\"task-list-item\"><p><input");
        Has(page.Html, "<blockquote>"); Has(page.Html, "href=\"/api/\" title=\"Reference\"");
    }),
    ("Build outputs, resources, search and stale file cleanup", () => WithSite((root, output) =>
    {
        Write(root, "docs/guide/start.md", "# Start\n\n## Section\n\nSearchable words");
        Write(root, "docs/public/logo.svg", "<svg/>");
        Write(root, "docs/diagram.svg", "<svg/>");
        var result = new SiteBuilder().Build(root, output);
        Equal(2, result.PageCount);
        foreach (var file in new[] { "index.html", "guide/start/index.html", "404.html", "logo.svg", "diagram.svg", "assets/miko.js", "assets/vars.css", "assets/search-index.json" })
            True(File.Exists(Path.Combine(output, file)), $"Missing {file}");
        Has(File.ReadAllText(Path.Combine(output, "assets/search-index.json")), "Searchable words");
        File.Delete(Path.Combine(root, "docs/guide/start.md"));
        Write(output, "user-owned.txt", "keep");
        new SiteBuilder().Build(root, output);
        True(!File.Exists(Path.Combine(output, "guide/start/index.html")), "stale page removed");
        True(File.Exists(Path.Combine(output, "user-owned.txt")), "unmanaged files preserved");
    })),
    ("Failed builds preserve previous successful output", () => WithSite((root, output) =>
    {
        new SiteBuilder().Build(root, output);
        var previous = File.ReadAllText(Path.Combine(output, "index.html"));
        Write(root, "docs/index.md", "# Updated");
        Write(root, "docs/conflict.md", "# A"); Write(root, "docs/conflict/index.md", "# B");
        Throws(() => new SiteBuilder().Build(root, output));
        Equal(previous, File.ReadAllText(Path.Combine(output, "index.html")));
    })),
    ("Base deployment, clean URL off, sidebar and home layout", () => WithSite((root, output) =>
    {
        var config = new SiteConfig { Base = "/manual/", CleanUrls = false, Home = new HomeConfig { Text = "Hello" } };
        config.ThemeConfig.Sidebar.Add(new NavItem { Text = "Guide", Link = "/guide" });
        Write(root, "miko.docs.json", JsonSerializer.Serialize(config, SiteConfig.JsonOptions));
        Write(root, "docs/index.md", "---\nlayout: home\n---\n");
        Write(root, "docs/guide.md", "# Guide\n\n## Topic\n\n[Home](index.md)");
        new SiteBuilder().Build(root, output);
        var html = File.ReadAllText(Path.Combine(output, "guide.html"));
        Has(html, "/manual/assets/miko.css"); Has(html, "aria-current=\"page\""); Has(html, "VPDocAsideOutline");
        Has(html, "href=\"/manual/index.html\"");
        Has(File.ReadAllText(Path.Combine(output, "index.html")), "VPHomeHero");
    })),
    ("Output isolation, asset collisions and malformed config", () => WithSite((root, output) =>
    {
        Throws(() => new SiteBuilder().Build(root, root));
        Throws(() => new SiteBuilder().Build(root, Path.Combine(root, "docs/out")));
        Write(root, "docs/public/index.html", "collision");
        Throws(() => new SiteBuilder().Build(root, output));
        File.Delete(Path.Combine(root, "docs/public/index.html"));
        Write(root, "miko.docs.json", "{\"base\":\"//evil/\"}");
        Throws(() => new SiteBuilder().Build(root, output));
    })),
    ("Manifest traversal cannot delete files outside output", () => WithSite((root, output) =>
    {
        Write(root, "keep.txt", "keep");
        Write(output, ".miko-manifest.json", "[\"../keep.txt\"]");
        Throws(() => new SiteBuilder().Build(root, output));
        Equal("keep", File.ReadAllText(Path.Combine(root, "keep.txt")));
    }))
};

    public static IEnumerable<object[]> CaseNames => Cases.Select(test => new object[] { test.Name });

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void Should_pass_generator_regressions(string name) => Cases.Single(test => test.Name == name).Run();

static MarkdownPage Render(string text) => new MarkdownRenderer().Render(text);
static void Has(string actual, string expected) => True(actual.Contains(expected, StringComparison.Ordinal), $"Expected substring: {expected}\nActual: {actual}");
static void Equal<T>(T expected, T actual) => True(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected {expected}, got {actual}");
static void True(bool condition, string message) { if (!condition) throw new Exception(message); }
static void Throws(Action action)
{
    try { action(); } catch (Exception ex) when (ex is InvalidDataException or JsonException or ArgumentException) { return; }
    throw new Exception("Expected a validation error.");
}
static void Write(string root, string path, string content)
{
    var target = Path.Combine(root, path); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.WriteAllText(target, content);
}
static void WithSite(Action<string, string> action)
{
    var root = Path.Combine(Path.GetTempPath(), "miko-docs-tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    try { Write(root, "docs/index.md", "# Home"); action(root, Path.Combine(root, "output")); }
    finally { Directory.Delete(root, true); }
}
}
