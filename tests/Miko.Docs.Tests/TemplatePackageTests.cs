using System.Diagnostics;
using System.IO.Compression;
using Miko.DocGen;
using Shouldly;

namespace Miko.Docs.Tests;

/// <summary>
/// Packs <c>templates/Miko.Templates.csproj</c> once, then checks that the package carries the
/// agent assets and the docs exactly as <see cref="AgentPackageLayout"/> describes, and that an
/// app scaffolded from that package has the complete reference with every link resolving.
/// </summary>
public sealed class TemplatePackageFixture : IDisposable
{
    public TemplatePackageFixture()
    {
        Root = Directory.CreateTempSubdirectory("miko-templates-").FullName;
        Run("dotnet", "pack", Path.Combine(RepoPaths.Root, "templates", "Miko.Templates.csproj"),
            "-o", Path.Combine(Root, "pkg"), "-p:Version=1.0.0", "--nologo");
        PackagePath = Directory.GetFiles(Path.Combine(Root, "pkg"), "Miko.Templates.*.nupkg").Single();

        // A private template hive: installing here never touches the user's own templates.
        Hive = Path.Combine(Root, "hive");
        Run("dotnet", "new", "install", PackagePath, "--debug:custom-hive", Hive);
    }

    public string Root { get; }
    public string PackagePath { get; }
    public string Hive { get; }

    /// <summary>Scaffolds an app and returns its directory.</summary>
    public string Scaffold(string template, string agent)
    {
        var output = Path.Combine(Root, "apps", $"{template}-{agent}", "MyApp");
        if (!Directory.Exists(output))
            Run("dotnet", "new", template, "-n", "MyApp", "-o", output, "--agent", agent, "--debug:custom-hive", Hive);
        return output;
    }

    public static string Run(string fileName, params string[] arguments)
    {
        var start = new ProcessStartInfo(fileName == "dotnet" ? Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet" : fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = RepoPaths.Root,
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_NOLOGO"] = "1";

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        var output = stdout.Result + stderr.Result;
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"`{fileName} {string.Join(' ', arguments)}` failed ({process.ExitCode}):\n{output}");
        return output;
    }

    public void Dispose()
    {
        try { Directory.Delete(Root, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}

public class TemplatePackageTests(TemplatePackageFixture fixture) : IClassFixture<TemplatePackageFixture>
{
    private static readonly string[] TemplateRoots = ["MikoRazorApp", "MikoMultiplatformApp"];

    public static TheoryData<string> Templates() => new(TemplateRoots);

    [Theory]
    [MemberData(nameof(Templates))]
    public void Should_pack_agent_assets_and_docs_into_template(string templateRoot)
    {
        using var package = ZipFile.OpenRead(fixture.PackagePath);
        var prefix = $"content/{templateRoot}/";
        var packed = package.Entries
            .Where(entry => entry.FullName.StartsWith(prefix, StringComparison.Ordinal))
            .ToDictionary(entry => Uri.UnescapeDataString(entry.FullName[prefix.Length..]), StringComparer.Ordinal);

        foreach (var (appPath, repoPath) in AgentPackageLayout.Files(RepoPaths.Root))
        {
            packed.TryGetValue(appPath, out var entry).ShouldBeTrue($"{repoPath} should ship as {templateRoot}/{appPath}");
            using var stream = entry!.Open();
            using var reader = new StreamReader(stream);
            reader.ReadToEnd().ShouldBe(File.ReadAllText(Path.Combine(RepoPaths.Root, repoPath)), $"{appPath} differs from {repoPath}");
        }

        // Nothing under .miko/ that the layout does not know about (e.g. a stale or a .razor file).
        var expected = AgentPackageLayout.Files(RepoPaths.Root).Keys.ToHashSet(StringComparer.Ordinal);
        packed.Keys.Where(path => path.StartsWith(".miko/", StringComparison.Ordinal))
            .Where(path => !expected.Contains(path))
            .ShouldBeEmpty();
    }

    [Theory]
    [InlineData("miko-razor", "claude")]
    [InlineData("miko-razor", "codex")]
    [InlineData("miko-razor", "cursor")]
    [InlineData("miko-multiplatform", "claude")]
    public void Should_scaffold_complete_agent_reference(string template, string agent)
    {
        var app = fixture.Scaffold(template, agent);

        var docs = AgentPackageLayout.ShippedDocs(RepoPaths.Root).ToList();
        docs.ShouldContain("components/ionic/button.md");
        foreach (var doc in docs)
        {
            var path = Path.Combine(app, ".miko", "docs", doc);
            File.Exists(path).ShouldBeTrue($".miko/docs/{doc} is missing from the scaffolded app");
            File.ReadAllText(path).ShouldBe(File.ReadAllText(Path.Combine(RepoPaths.Root, "docs", doc)), $".miko/docs/{doc} was altered by scaffolding");
        }
        File.Exists(Path.Combine(app, ".miko", "llms.txt")).ShouldBeTrue();
        File.Exists(Path.Combine(app, ".miko", "rules.md")).ShouldBeTrue();
        Directory.EnumerateFiles(Path.Combine(app, ".miko"), "*.razor", SearchOption.AllDirectories).ShouldBeEmpty(
            "doc examples must not ship: the app would compile them");

        var entry = agent switch
        {
            "claude" => "CLAUDE.md",
            "codex" => "AGENTS.md",
            _ => Path.Combine(".cursor", "rules", "miko.mdc"),
        };
        File.Exists(Path.Combine(app, entry)).ShouldBeTrue();

        // Every relative link in every shipped Markdown file resolves inside the app.
        var broken = new List<string>();
        foreach (var file in Directory.EnumerateFiles(app, "*", SearchOption.AllDirectories)
                     .Where(f => Path.GetExtension(f) is ".md" or ".txt" or ".mdc")
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")))
        {
            foreach (var target in DocsChecks.Links(File.ReadAllText(file)))
            {
                var resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, target));
                if (!File.Exists(resolved) && !File.Exists(Path.Combine(resolved, "index.md")) && !Directory.Exists(resolved))
                    broken.Add($"{Path.GetRelativePath(app, file)} → {target}");
            }
        }
        broken.ShouldBeEmpty();
    }

    [Fact]
    public void Should_not_scaffold_agent_files_by_default()
    {
        var app = fixture.Scaffold("miko-razor", "none");

        Directory.Exists(Path.Combine(app, ".miko")).ShouldBeFalse();
        File.Exists(Path.Combine(app, "CLAUDE.md")).ShouldBeFalse();
        File.Exists(Path.Combine(app, "AGENTS.md")).ShouldBeFalse();
    }
}
