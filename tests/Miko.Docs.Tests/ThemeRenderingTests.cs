using System.Net;
using Shouldly;

namespace Miko.Docs.Tests;

public class ThemeRenderingTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/miko/")]
    public void Should_publish_brand_icons_and_resolve_them_on_nested_pages(string basePath)
    {
        using var temporary = new DocsTestDirectory();
        var output = Path.Combine(temporary.Root, "output");
        new SiteBuilder().Build(Path.Combine(RepoPaths.Root, "docs", "Miko.Docs.Web"), output, basePath);
        foreach (var page in new[] { "index.html", "guide/introduction/index.html", "404.html" })
        {
            var html = File.ReadAllText(Path.Combine(output, page));
            html.ShouldContain($"<link rel=\"icon\" href=\"{basePath}logo.svg\">");
            html.ShouldContain($"<link rel=\"apple-touch-icon\" href=\"{basePath}apple-touch-icon.png\">");
            html.ShouldContain($"<img class=\"logo\" alt=\"\" src=\"{basePath}logo.svg\">");
        }
        foreach (var asset in new[] { "logo.svg", "favicon.ico", "apple-touch-icon.png" })
            File.Exists(Path.Combine(output, asset)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//example.com/icon.svg")]
    public void Should_validate_icon_urls(string url)
    {
        Should.Throw<InvalidDataException>(() => new SiteConfig { Favicon = url }.Validate());
        Should.Throw<InvalidDataException>(() => new SiteConfig { AppleTouchIcon = url }.Validate());
    }

    [Fact]
    public void Should_preserve_code_source_and_use_an_icon_only_copy_button()
    {
        var page = new MarkdownRenderer().Render("```csharp {2} :line-numbers\nvar text = \"<script>&😀\";\n// second line\n```");
        page.Html.ShouldContain("aria-label=\"Copy code\"></button>");
        page.Html.ShouldNotContain("⧉");
        page.Html.ShouldContain("line-numbers-mode");
        page.Html.ShouldContain("class=\"line highlighted\">// second line</span>");
        page.Html.ShouldNotContain("<script>");
        WebUtility.HtmlDecode(page.Html).ShouldContain("var text = \"<script>&😀\";");
    }

    [Theory]
    [InlineData("c#")]
    [InlineData("cshtml")]
    public void Should_preserve_language_aliases_for_the_browser_highlighter(string language)
    {
        var page = new MarkdownRenderer().Render("~~~" + language + "\nvar value = 1;\n~~~");
        page.Html.ShouldContain("class=\"language-" + language + "\"");
    }

    [Fact]
    public void Should_preserve_emoji_and_supplementary_unicode_in_document_text()
    {
        var page = new MarkdownRenderer().Render("# 🖥️ Miko\n\n🪶 **Small** and 😀 interactive.\n\n- 👩‍💻 Code\n- 𠮷 Unicode");
        var html = WebUtility.HtmlDecode(page.Html);
        html.ShouldContain("🪶");
        html.ShouldContain("👩‍💻");
        html.ShouldContain("𠮷");
        html.ShouldNotContain("\uFFFD");
        page.PlainText.ShouldContain("😀");
    }

    [Fact]
    public void Should_render_original_navigation_and_local_fonts_with_deployment_base()
    {
        var root = Path.Combine(RepoPaths.Root, "docs", "Miko.Docs.Web");
        using var temporary = new DocsTestDirectory();
        var output = Path.Combine(temporary.Root, "output");
        try
        {
            new SiteBuilder().Build(root, output, "/miko/");
            var html = File.ReadAllText(Path.Combine(output, "guide", "introduction", "index.html"));
            html.ShouldContain("VPNavBar has-sidebar");
            html.ShouldContain("class=\"search-label\">Search</span>");
            html.ShouldContain("class=\"caret-icon\"");
            html.ShouldContain("aria-label=\"GitHub\"");
            html.ShouldContain("/miko/assets/inter-roman-latin.woff2");
            html.ShouldContain("/miko/assets/fonts.css");
            html.ShouldContain("<script src=\"/miko/assets/highlight.js\" defer></script>");
            html.ShouldContain("aria-current=\"page\"");
            html.ShouldNotContain("class=\"level-3\"");
            File.Exists(Path.Combine(output, "assets", "inter-roman-latin.woff2")).ShouldBeTrue();
            File.Exists(Path.Combine(output, "assets", "FONT-LICENSE.txt")).ShouldBeTrue();
            File.Exists(Path.Combine(output, "assets", "highlight.js")).ShouldBeTrue();
            File.Exists(Path.Combine(output, "assets", "SHIKI-LICENSE.txt")).ShouldBeTrue();
        }
        finally { if (Directory.Exists(output)) Directory.Delete(output, true); }
    }
}
