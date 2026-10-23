using System.Text.RegularExpressions;

namespace Miko.DocGen;

/// <summary>
/// Consistency checks that keep the docs trustworthy for both audiences: every component is
/// documented, every example is used, and every relative link resolves — in the docs site and
/// in the copy shipped to scaffolded apps.
/// </summary>
public static class DocsChecks
{
    private static readonly Regex Fence = new(@"^\s*```.*?^\s*```", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex InlineCode = new(@"`[^`\n]*`", RegexOptions.Compiled);
    private static readonly Regex HtmlComment = new(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex Link = new(@"\]\((?<target>[^)\s]+)(?:\s+""[^""]*"")?\)", RegexOptions.Compiled);

    public static IEnumerable<string> Run(string repoRoot, ComponentCatalog catalog, IReadOnlyDictionary<string, string> pages)
    {
        foreach (var name in catalog.Components.Keys)
        {
            if (!ComponentCatalog.InfrastructureComponents.Contains(name) && !pages.ContainsKey(name))
                yield return $"{name} is not documented: add it to a <!-- miko:api --> region of a page in {DocsGenerator.ComponentDocsDir}/.";
        }

        foreach (var problem in InertParameters(catalog))
            yield return problem;
        foreach (var problem in OrphanExamples(repoRoot))
            yield return problem;
        foreach (var problem in SiteLinks(repoRoot))
            yield return problem;
        foreach (var problem in PackagedLinks(repoRoot))
            yield return problem;
        foreach (var problem in TemplateTokens(repoRoot))
            yield return problem;
    }

    /// <summary>
    /// Wording that admits a parameter does nothing. A public parameter must take effect: the 1.0
    /// rule is to implement it or take it off the public surface, never to keep it "for API
    /// parity" with Ionic and say so in its summary.
    /// </summary>
    public static readonly IReadOnlyList<string> InertParameterPhrases =
    [
        "api parity", "for parity", "kept for", "marker only", "has no effect", "is ignored",
        "not supported", "no-op",
    ];

    /// <summary>Public component parameters whose <c>///</c> summary uses <see cref="InertParameterPhrases"/>.</summary>
    public static IEnumerable<string> InertParameters(ComponentCatalog catalog)
    {
        foreach (var (name, component) in catalog.Components)
        {
            foreach (var parameter in catalog.Parameters(component))
            {
                var summary = catalog.Documentation.Summary(parameter.Property, cref => cref);
                if (summary is null) continue;
                var phrase = InertParameterPhrases.FirstOrDefault(p => summary.Contains(p, StringComparison.OrdinalIgnoreCase));
                if (phrase is not null)
                    yield return $"{name}.{parameter.Property.Name}: its summary says \"{phrase}\". A public parameter must take effect — implement it or remove it from the public surface.";
            }
        }
    }

    /// <summary>
    /// <c>dotnet new</c> rewrites the template's source name and every <c>replaces</c> value in
    /// all content files — shipped docs included — so a page mentioning one would reach the app
    /// altered (e.g. <c>"Miko Razor App"</c> turning into the app's name).
    /// </summary>
    private static IEnumerable<string> TemplateTokens(string repoRoot)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var template in Directory.EnumerateFiles(Path.Combine(repoRoot, "templates"), "template.json", SearchOption.AllDirectories))
        {
            using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(template));
            if (json.RootElement.TryGetProperty("sourceName", out var sourceName))
                tokens.Add(sourceName.GetString()!);
            if (!json.RootElement.TryGetProperty("symbols", out var symbols))
                continue;
            foreach (var symbol in symbols.EnumerateObject())
            {
                if (symbol.Value.TryGetProperty("replaces", out var replaces) && replaces.GetString() is { Length: > 0 } value
                    && !(symbol.Value.TryGetProperty("datatype", out var type) && type.GetString() == "choice"))
                    tokens.Add(value);
            }
        }

        foreach (var (appPath, repoPath) in AgentPackageLayout.Files(repoRoot))
        {
            var text = File.ReadAllText(Path.Combine(repoRoot, repoPath));
            foreach (var token in tokens.Where(token => text.Contains(token, StringComparison.Ordinal)))
                yield return $"{repoPath}: contains '{token}', which `dotnet new` replaces in generated apps; reword it.";
        }
    }

    private static IEnumerable<string> OrphanExamples(string repoRoot)
    {
        var docs = Path.Combine(repoRoot, "docs");
        var referenced = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in DocsGenerator.SourceFiles(repoRoot).Where(f => f.StartsWith(docs, StringComparison.Ordinal)))
        {
            foreach (Match match in Regex.Matches(File.ReadAllText(file), @"<!--\s*miko:example\s+(\S+)\s*-->"))
                referenced.Add(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, match.Groups[1].Value)));
        }

        foreach (var example in Directory.EnumerateFiles(docs, "*.razor", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(example);
            if (name.StartsWith('_'))
                continue; // _Imports.razor and friends
            if (!referenced.Contains(Path.GetFullPath(example)))
                yield return $"{Path.GetRelativePath(repoRoot, example).Replace('\\', '/')} is not inlined by any <!-- miko:example --> region.";
        }
    }

    /// <summary>Links between docs pages, as VitePress sees them.</summary>
    private static IEnumerable<string> SiteLinks(string repoRoot)
    {
        var docs = Path.Combine(repoRoot, "docs");
        foreach (var file in DocsGenerator.SourceFiles(repoRoot).Where(f => f.StartsWith(docs, StringComparison.Ordinal)))
        {
            var relativeFile = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
            foreach (var target in Links(File.ReadAllText(file)))
            {
                if (target.StartsWith('/'))
                {
                    yield return $"{relativeFile}: link '{target}' is site-absolute; use a relative .md link so it also resolves in the copy shipped to apps.";
                    continue;
                }
                if (!Exists(Path.GetDirectoryName(file)!, target))
                    yield return $"{relativeFile}: link '{target}' does not resolve.";
            }
        }
    }

    /// <summary>
    /// Links as an agent sees them inside a scaffolded app: every target must be a file the
    /// template package actually ships (see <see cref="AgentPackageLayout"/>).
    /// </summary>
    private static IEnumerable<string> PackagedLinks(string repoRoot)
    {
        var files = AgentPackageLayout.Files(repoRoot);
        var shipped = files.Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var (appPath, repoPath) in files)
        {
            if (Path.GetExtension(appPath) is not (".md" or ".txt" or ".mdc"))
                continue;
            var directory = Path.GetDirectoryName(appPath)!.Replace('\\', '/');
            foreach (var target in Links(File.ReadAllText(Path.Combine(repoRoot, repoPath))))
            {
                var resolved = Normalize(directory.Length == 0 ? target : $"{directory}/{target}");
                var ok = resolved is not null
                         && (shipped.Contains(resolved) || shipped.Contains(resolved.TrimEnd('/') + "/index.md"));
                if (!ok)
                    yield return $"{repoPath}: link '{target}' does not resolve in a scaffolded app (it becomes '{appPath}').";
            }
        }
    }

    /// <summary>Relative link targets of a Markdown text, ignoring code and comments.</summary>
    public static IEnumerable<string> Links(string markdown)
    {
        var text = HtmlComment.Replace(InlineCode.Replace(Fence.Replace(markdown, ""), ""), "");
        foreach (Match match in Link.Matches(text))
        {
            var target = match.Groups["target"].Value;
            if (target.StartsWith('#') || target.Contains("://", StringComparison.Ordinal) || target.StartsWith("mailto:", StringComparison.Ordinal))
                continue;
            var hash = target.IndexOf('#');
            yield return hash >= 0 ? target[..hash] : target;
        }
    }

    private static bool Exists(string directory, string target)
    {
        var path = Path.GetFullPath(Path.Combine(directory, target));
        return File.Exists(path) || (Directory.Exists(path) && File.Exists(Path.Combine(path, "index.md")));
    }

    private static string? Normalize(string path)
    {
        var parts = new List<string>();
        foreach (var part in path.Split('/'))
        {
            if (part is "" or ".")
                continue;
            if (part == "..")
            {
                if (parts.Count == 0)
                    return null;
                parts.RemoveAt(parts.Count - 1);
                continue;
            }
            parts.Add(part);
        }
        var normalized = string.Join('/', parts);
        return path.EndsWith('/') ? normalized + "/" : normalized;
    }
}
