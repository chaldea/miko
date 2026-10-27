using System.Text.Json;
using System.Xml.Linq;

namespace Miko.TemplateE2E;

/// <summary>A <c>dotnet new</c> template under templates/, read from its <c>.template.config/template.json</c>.</summary>
/// <param name="ShortName">The name passed to <c>dotnet new</c>.</param>
/// <param name="Layouts">The choices of its <c>layout</c> parameter.</param>
/// <param name="Heads">Its head projects; empty for a single-project template, which is built as a whole.</param>
sealed record Template(string ShortName, IReadOnlyList<string> Layouts, IReadOnlyList<Head> Heads);

/// <summary>A head project (<c>MikoMultiplatformApp.Android</c> → <c>Android</c>) and the platform of
/// its target framework (<c>net10.0-android</c> → <c>android</c>, empty for plain <c>net10.0</c>).</summary>
sealed record Head(string Name, string Platform);

/// <summary>One app to scaffold and the one project of it to build.</summary>
sealed record BuildJob(Template Template, string Layout, Head? Head)
{
    public string Label => Head is null ? $"{Template.ShortName}/{Layout}" : $"{Template.ShortName}/{Layout}/{Head.Name}";

    /// <summary>Why this machine cannot build the job, or <c>null</c>.</summary>
    public string? SkipReason { get; init; }

    /// <summary>Arguments the head's platform needs on top of the common <c>dotnet build</c> ones.</summary>
    public IReadOnlyList<string> BuildArguments { get; init; } = [];
}

static class BuildPlan
{
    public static IReadOnlyList<Template> ReadTemplates(string repoRoot)
    {
        var templates = new List<Template>();
        foreach (var directory in Directory.GetDirectories(Path.Combine(repoRoot, "templates")).Order(StringComparer.Ordinal))
        {
            var config = Path.Combine(directory, ".template.config", "template.json");
            if (!File.Exists(config))
                continue;

            using var json = JsonDocument.Parse(File.ReadAllText(config), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            var root = json.RootElement;
            var sourceName = root.GetProperty("sourceName").GetString()!;
            var layouts = root.GetProperty("symbols").GetProperty("layout").GetProperty("choices").EnumerateArray()
                .Select(choice => choice.GetProperty("choice").GetString()!)
                .ToList();

            // MikoMultiplatformApp.Android/MikoMultiplatformApp.Android.csproj, … — not the shared
            // MikoMultiplatformApp/ library (a "Name.*" search pattern would match it too).
            var heads = new List<Head>();
            foreach (var headDirectory in Directory.GetDirectories(directory).Order(StringComparer.Ordinal))
            {
                var name = Path.GetFileName(headDirectory);
                var project = Path.Combine(headDirectory, name + ".csproj");
                if (!name.StartsWith(sourceName + ".", StringComparison.Ordinal) || !File.Exists(project))
                    continue;
                var framework = XDocument.Load(project).Descendants("TargetFramework").Single().Value;
                var dash = framework.IndexOf('-');
                heads.Add(new Head(name[(sourceName.Length + 1)..], dash < 0 ? "" : framework[(dash + 1)..]));
            }

            templates.Add(new Template(root.GetProperty("shortName").GetString()!, layouts, heads));
        }
        return templates;
    }

    /// <summary>The jobs for the selected layouts and heads, the slowest first.</summary>
    public static IReadOnlyList<BuildJob> Create(IReadOnlyList<Template> templates, Options options, SdkPacks packs)
    {
        var layouts = Select("layout", options.Layouts, templates.SelectMany(t => t.Layouts));
        var heads = Select("head", options.Heads, templates.SelectMany(t => t.Heads).Select(h => h.Name));

        var jobs = new List<BuildJob>();
        foreach (var template in templates)
        {
            foreach (var layout in template.Layouts.Where(layouts.Contains))
            {
                if (template.Heads.Count == 0)
                {
                    jobs.Add(new BuildJob(template, layout, null));
                    continue;
                }
                foreach (var head in template.Heads.Where(h => heads.Contains(h.Name)))
                {
                    var (skipReason, arguments) = Requirements(head, packs);
                    jobs.Add(new BuildJob(template, layout, head) { SkipReason = skipReason, BuildArguments = arguments });
                }
            }
        }

        // Longest first, so the last builds to finish are short ones rather than a lone iOS build.
        return jobs.OrderByDescending(Cost).ToList();
    }

    // Look for the platform SDK pack rather than a workload id: the iOS SDK also arrives through
    // the maui workload, in which case `dotnet workload list` shows no "ios" entry.
    private static (string? SkipReason, string[] Arguments) Requirements(Head head, SdkPacks packs) => head.Platform switch
    {
        "" => (null, []),
        "android" => (packs.Contains("Microsoft.Android.Sdk.") ? null : "no android workload", []),
        // A device build that needs no signing identity or provisioning profile.
        "ios" => (OperatingSystem.IsMacOS() && packs.Contains("Microsoft.iOS.Sdk.") ? null : "needs macOS + ios workload",
                  ["-r", "ios-arm64", "-p:EnableCodeSigning=false", "-p:ValidateXcodeVersion=false"]),
        _ => throw new NotSupportedException(
            $"The {head.Name} head targets '{head.Platform}'; add its workload check and build arguments to BuildPlan.Requirements."),
    };

    // Seconds a build takes on its own (Apple M-series, every layout alike); only the order matters.
    private static int Cost(BuildJob job) => job.Head?.Platform switch { "ios" => 60, "android" => 30, _ => 2 };

    // The canonical names of the requested values (case-insensitive), or all of them.
    private static HashSet<string> Select(string kind, IReadOnlyList<string>? requested, IEnumerable<string> offered)
    {
        var known = offered.Distinct().ToList();
        if (requested is null)
            return [.. known];

        var selected = new HashSet<string>();
        foreach (var name in requested)
        {
            var match = known.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase))
                ?? throw new UsageException($"Unknown {kind} '{name}'; the templates offer: {string.Join(", ", known)}.");
            selected.Add(match);
        }
        return selected;
    }
}

/// <summary>The platform SDK packs (<c>&lt;dotnet root&gt;/packs</c>) of the dotnet the builds run on.</summary>
sealed class SdkPacks(string? directory)
{
    public static async Task<SdkPacks> LocateAsync()
    {
        // "10.0.401 [/usr/local/share/dotnet/sdk]": the root is the parent of the bracketed path.
        // Asking dotnet itself also sees through shims, where resolving the PATH entry would not.
        var output = new StringWriter();
        await Dotnet.RunAsync(output, ["--list-sdks"], CancellationToken.None);
        var line = output.ToString().Split('\n').FirstOrDefault(l => l.Contains('[') && l.Contains(']'));
        if (line is null)
            return new SdkPacks(null);
        var sdk = line[(line.IndexOf('[') + 1)..line.LastIndexOf(']')];
        return new SdkPacks(Path.Combine(Path.GetDirectoryName(sdk)!, "packs"));
    }

    public bool Contains(string prefix) =>
        directory is not null && Directory.Exists(directory)
        && Directory.EnumerateDirectories(directory, prefix + "*", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).Any();
}
