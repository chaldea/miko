using System.Text.Json;
using Miko.DocGen;
using Shouldly;

namespace Miko.Docs.Tests;

public class SiteDeploymentTests
{
    [Fact]
    public void Should_hide_generation_comments_but_preserve_fenced_examples()
    {
        var markdown = "# Title\n<!-- miko:api IonButton -->\nVisible\n<!-- /miko:api -->\n\n<!--\ninternal notes\n-->\n\n~~~html\n<!-- example -->\n~~~";
        var page = new MarkdownRenderer().Render(markdown);
        page.Html.ShouldContain("Visible");
        page.Html.ShouldNotContain("miko:api");
        page.PlainText.ShouldNotContain("internal notes");
        page.Html.ShouldContain("&lt;!-- example --&gt;");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/miko/")]
    public void Should_build_all_site_pages_without_modifying_config_or_demo(string basePath)
    {
        var project = Path.Combine(RepoPaths.Root, "docs", "Miko.Docs.Web");
        var configPath = Path.Combine(project, "miko.docs.json");
        var before = File.ReadAllText(configPath);
        using var temporary = new DocsTestDirectory();
        var output = Path.Combine(temporary.Root, "output");
        try
        {
            Directory.CreateDirectory(Path.Combine(output, "demo", "_framework"));
            var runtime = Path.Combine(output, "demo", "_framework", "runtime.wasm");
            File.WriteAllBytes(runtime, [0, 97, 115, 109]);
            var result = new SiteBuilder().Build(project, output, basePath);
            result.PageCount.ShouldBe(Directory.EnumerateFiles(Path.Combine(RepoPaths.Root, AgentPackageLayout.DocsSource), "*.md", SearchOption.AllDirectories).Count());
            result.PageCount.ShouldBeGreaterThan(80);
            var home = File.ReadAllText(Path.Combine(output, "index.html"));
            home.ShouldContain("href=\"" + basePath + "demo/\"");
            home.ShouldContain("href=\"" + basePath + "assets/miko.css\"");
            File.ReadAllText(Path.Combine(output, "components", "ionic", "button", "index.html")).ShouldNotContain("&lt;!-- miko:");
            using var search = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "assets", "search-index.json")));
            search.RootElement.GetArrayLength().ShouldBe(result.PageCount);
            foreach (var page in search.RootElement.EnumerateArray())
                page.GetProperty("url").GetString()!.ShouldStartWith(basePath);
            File.ReadAllBytes(runtime).ShouldBe(new byte[] { 0, 97, 115, 109 });
            File.ReadAllText(configPath).ShouldBe(before);
        }
        finally { if (Directory.Exists(output)) Directory.Delete(output, true); }
    }

    [Fact]
    public void Should_validate_publish_base_before_writing()
    {
        var project = Path.Combine(RepoPaths.Root, "docs", "Miko.Docs.Web");
        using var temporary = new DocsTestDirectory();
        var output = Path.Combine(temporary.Root, "output");
        Should.Throw<InvalidDataException>(() => new SiteBuilder().Build(project, output, "//bad/"));
        Directory.Exists(output).ShouldBeFalse();
    }
}
