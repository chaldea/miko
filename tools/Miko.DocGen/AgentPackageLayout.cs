namespace Miko.DocGen;

/// <summary>
/// Where every agent-facing file lands inside a scaffolded app. This mirrors the
/// <c>_AgentAsset</c> / <c>_AgentDoc</c> items of <c>templates/Miko.Templates.csproj</c>:
/// <c>agent/**</c> goes to the app root and the Markdown pages of <c>docs/</c> go to
/// <c>.miko/docs/</c>, so the agent docs are the developer docs rather than a second copy.
/// tests/Miko.Docs.Tests packs the templates and checks the package against this layout.
/// </summary>
public static class AgentPackageLayout
{
    public const string AgentSource = "agent";
    public const string DocsSource = "docs";
    public const string DocsTarget = ".miko/docs";

    /// <summary>Repo-only files under <c>agent/</c> (it documents the directory itself).</summary>
    public static readonly IReadOnlyList<string> ExcludedAgentFiles = ["README.md"];

    /// <summary>
    /// Pages under <c>docs/</c> that do not ship: the VitePress landing page (hero layout, no
    /// content) and the Ionic theming design notes (implementation detail; the usage lives in
    /// <c>components/ionic/theming.md</c>).
    /// </summary>
    public static readonly IReadOnlyList<string> ExcludedDocs = ["index.md", "ionic-theme.md"];

    /// <summary>The shipped files: app-relative path → repo-relative path, '/'-separated.</summary>
    public static IReadOnlyDictionary<string, string> Files(string repoRoot)
    {
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);

        var agentRoot = Path.Combine(repoRoot, AgentSource);
        foreach (var file in Directory.EnumerateFiles(agentRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Relative(agentRoot, file);
            if (ExcludedAgentFiles.Contains(relative) || Path.GetFileName(file) == ".DS_Store")
                continue;
            files[relative] = $"{AgentSource}/{relative}";
        }

        foreach (var relative in ShippedDocs(repoRoot))
            files[$"{DocsTarget}/{relative}"] = $"{DocsSource}/{relative}";

        return files;
    }

    /// <summary>The docs pages that ship, relative to <c>docs/</c>.</summary>
    public static IEnumerable<string> ShippedDocs(string repoRoot)
    {
        var docsRoot = Path.Combine(repoRoot, DocsSource);
        return Directory.EnumerateFiles(docsRoot, "*.md", SearchOption.AllDirectories)
            .Select(file => Relative(docsRoot, file))
            .Where(relative => !relative.StartsWith(".vitepress/", StringComparison.Ordinal)
                               && !relative.StartsWith("node_modules/", StringComparison.Ordinal)
                               && !ExcludedDocs.Contains(relative))
            .Order(StringComparer.Ordinal);
    }

    private static string Relative(string root, string file) =>
        Path.GetRelativePath(root, file).Replace('\\', '/');
}
