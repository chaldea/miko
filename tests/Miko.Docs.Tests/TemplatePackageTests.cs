using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;
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

    /// <summary>Scaffolds an app under the given project name and returns its directory.</summary>
    public string ScaffoldNamed(string template, string name, params string[] options)
    {
        var output = Path.Combine(Root, "named", $"{template}-{name}-{string.Join('-', options).Replace("--", "")}", "app");
        if (!Directory.Exists(output))
            Run("dotnet", ["new", template, "-n", name, "-o", output, .. options, "--debug:custom-hive", Hive]);
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

public partial class TemplatePackageTests(TemplatePackageFixture fixture) : IClassFixture<TemplatePackageFixture>
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

    // Release-1.0.0 D3: `-n my-app` produced the Android package name `com.miko.my-app`, which is
    // invalid, and head projects referencing a shared project that did not exist under that name.
    [Theory]
    [InlineData("my-app", "com.miko.myapp")]
    [InlineData("My.Cool_App", "com.miko.mycoolapp")]
    [InlineData("2048Game", "com.miko.app2048game")]
    public void Should_derive_a_valid_application_id_from_the_project_name(string name, string expected)
    {
        var app = fixture.ScaffoldNamed("miko-multiplatform", name);

        ApplicationIds(app).ShouldAllBe(id => id == expected);
    }

    [Fact]
    public void Should_use_the_app_id_option_for_android_and_ios()
    {
        var app = fixture.ScaffoldNamed("miko-multiplatform", "my-app", "--appId", "com.example.shop");

        ApplicationIds(app).ShouldAllBe(id => id == "com.example.shop");
    }

    [Theory]
    [InlineData("miko-multiplatform")]
    [InlineData("miko-razor")]
    public void Should_resolve_every_project_reference_when_the_name_is_not_an_identifier(string template)
    {
        var app = fixture.ScaffoldNamed(template, "my-app");

        var projects = Directory.EnumerateFiles(app, "*.csproj", SearchOption.AllDirectories).ToList();
        projects.ShouldNotBeEmpty();
        foreach (var project in projects)
        {
            foreach (var reference in XDocument.Load(project).Descendants("ProjectReference"))
            {
                var include = reference.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar);
                File.Exists(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project)!, include)))
                    .ShouldBeTrue($"{Path.GetRelativePath(app, project)} references missing {include}");
            }
        }
    }

    /// <summary>Android ApplicationId, iOS ApplicationId and iOS CFBundleIdentifier, in that order.</summary>
    private static string[] ApplicationIds(string app)
    {
        static string Single(string root, string pattern) => Directory.GetFiles(root, pattern, SearchOption.AllDirectories).Single();

        var android = XDocument.Load(Single(app, "*.Android.csproj")).Descendants("ApplicationId").Single().Value;
        var ios = XDocument.Load(Single(app, "*.iOS.csproj")).Descendants("ApplicationId").Single().Value;
        var plistKeys = XDocument.Load(Single(app, "Info.plist")).Descendants("key").ToList();
        var bundleId = ((XElement)plistKeys.Single(k => k.Value == "CFBundleIdentifier").NextNode!).Value;
        string[] ids = [android, ios, bundleId];
        ids.ShouldAllBe(id => AndroidPackageName().IsMatch(id), $"invalid application id in [{string.Join(", ", ids)}]");
        return ids;
    }

    // Android package names: two or more dot-separated segments, each starting with a letter.
    [System.Text.RegularExpressions.GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]*(\\.[a-zA-Z][a-zA-Z0-9_]*)+$")]
    private static partial System.Text.RegularExpressions.Regex AndroidPackageName();

    [Fact]
    public void Should_not_scaffold_agent_files_by_default()
    {
        var app = fixture.Scaffold("miko-razor", "none");

        Directory.Exists(Path.Combine(app, ".miko")).ShouldBeFalse();
        File.Exists(Path.Combine(app, "CLAUDE.md")).ShouldBeFalse();
        File.Exists(Path.Combine(app, "AGENTS.md")).ShouldBeFalse();
    }
}
