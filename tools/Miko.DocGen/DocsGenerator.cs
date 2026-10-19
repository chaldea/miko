using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Miko.DocGen;

/// <summary>The outcome of a run: files whose generated regions were out of date, and problems.</summary>
public sealed record DocsRunResult(IReadOnlyList<string> StaleFiles, IReadOnlyList<string> Problems)
{
    public bool IsClean => StaleFiles.Count == 0 && Problems.Count == 0;
}

/// <summary>
/// Fills the generated regions of the Markdown docs. A region is a pair of HTML comments:
/// <code>
/// &lt;!-- miko:api IonButton IonButtons --&gt;
/// …generated, overwritten on every run…
/// &lt;!-- /miko:api --&gt;
/// </code>
/// Everything outside regions is hand-written and left untouched. Region kinds:
/// <list type="bullet">
/// <item><c>api &lt;Component…&gt;</c> — parameter / event / slot tables.</item>
/// <item><c>tokens &lt;ThemeProperty&gt;</c> — a theme token with its md and ios defaults.</item>
/// <item><c>type &lt;Type…&gt;</c> — properties and methods of an options, event-args or controller type.</item>
/// <item><c>example &lt;path.razor&gt;</c> — a compiled example file inlined as a code block.</item>
/// <item><c>component-index &lt;link-prefix&gt; [table|list]</c> — every component page.</item>
/// </list>
/// </summary>
public static class DocsGenerator
{
    public const string ComponentDocsDir = "docs/components/ionic";

    private static readonly Regex Region = new(
        @"(?<open><!--\s*miko:(?<kind>[a-z-]+)(?<args>(?:[ \t]+[^\s]+?)*)\s*-->)(?<body>.*?)(?<close><!--\s*/miko:\k<kind>\s*-->)",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex AnyOpening = new(@"<!--\s*miko:([a-z-]+)", RegexOptions.Compiled);

    /// <param name="repoRoot">The repository root.</param>
    /// <param name="write">Write regenerated files back (otherwise only report them as stale).</param>
    /// <param name="only">When given, only these repo-relative files are written; the rest are
    /// still rendered and checked. Lets several people regenerate their own pages at once.</param>
    public static DocsRunResult Run(string repoRoot, bool write, IReadOnlyCollection<string>? only = null)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            return RunCore(Path.GetFullPath(repoRoot), write, only);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    private static DocsRunResult RunCore(string repoRoot, bool write, IReadOnlyCollection<string>? only)
    {
        var problems = new List<string>();
        var catalog = new ComponentCatalog(repoRoot);
        var documents = SourceFiles(repoRoot)
            .Select(path => new DocumentFile(repoRoot, path, File.ReadAllText(path)))
            .ToList();

        // Which page documents which type, so a <see cref> in one summary can link to another page.
        var pages = new Dictionary<string, DocumentFile>(StringComparer.Ordinal);
        foreach (var document in documents)
        {
            foreach (Match match in Region.Matches(document.Text))
            {
                var kind = match.Groups["kind"].Value;
                if (kind is not ("api" or "type"))
                    continue;
                foreach (var name in Arguments(match))
                {
                    if (pages.TryGetValue(name, out var other))
                        problems.Add($"{document.RelativePath}: {name} is already documented in {other.RelativePath}; document each type on one page.");
                    else
                        pages[name] = document;
                }
            }
        }

        var renderer = new RegionRenderer(repoRoot, catalog, pages, documents, problems);
        var stale = new List<string>();
        foreach (var document in documents)
        {
            var updated = Region.Replace(document.Text, match =>
            {
                var body = renderer.Render(document, match.Groups["kind"].Value, Arguments(match));
                return $"{match.Groups["open"].Value}\n{body.TrimEnd('\n')}\n{match.Groups["close"].Value}";
            });

            var regionCount = Region.Matches(document.Text).Count;
            var openingCount = AnyOpening.Matches(document.Text).Count;
            if (openingCount != regionCount)
                problems.Add($"{document.RelativePath}: a <!-- miko:… --> marker has no matching <!-- /miko:… --> close.");

            if (updated == document.Text)
                continue;
            stale.Add(document.RelativePath);
            if (write && (only is null || only.Contains(document.RelativePath)))
                File.WriteAllText(document.Path, updated);
        }

        problems.AddRange(DocsChecks.Run(repoRoot, catalog, pages.ToDictionary(p => p.Key, p => p.Value.RelativePath)));
        return new DocsRunResult(stale, problems);
    }

    private static string[] Arguments(Match match) =>
        match.Groups["args"].Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Every file that may carry generated regions.</summary>
    public static IEnumerable<string> SourceFiles(string repoRoot)
    {
        var docs = Path.Combine(repoRoot, "docs");
        foreach (var file in Directory.EnumerateFiles(docs, "*.md", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(docs, file).Replace('\\', '/');
            if (!relative.StartsWith(".vitepress/", StringComparison.Ordinal) && !relative.StartsWith("node_modules/", StringComparison.Ordinal))
                yield return file;
        }

        var agent = Path.Combine(repoRoot, "agent");
        foreach (var file in Directory.EnumerateFiles(agent, "*", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(file) is ".md" or ".txt" or ".mdc")
                yield return file;
        }
    }

    /// <summary>The first paragraph's first sentence, used as a page summary.</summary>
    public static string Summary(string markdown)
    {
        var paragraph = new StringBuilder();
        var inFence = false;
        var pastTitle = false;
        foreach (var raw in markdown.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }
            if (inFence)
                continue;
            if (!pastTitle)
            {
                pastTitle = line.StartsWith("# ", StringComparison.Ordinal);
                continue;
            }
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("<!--", StringComparison.Ordinal)
                || line.StartsWith(":::", StringComparison.Ordinal) || line.StartsWith('|') || line.StartsWith('>'))
            {
                if (paragraph.Length > 0)
                    break;
                continue;
            }
            paragraph.Append(line).Append(' ');
        }

        var text = paragraph.ToString().Trim();
        var end = Regex.Match(text, @"(?<=[.!?])\s");
        return end.Success ? text[..end.Index] : text;
    }
}

public sealed class DocumentFile
{
    public DocumentFile(string repoRoot, string path, string text)
    {
        Path = path;
        RelativePath = System.IO.Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
        Text = text;
    }

    public string Path { get; }
    public string RelativePath { get; }
    public string Text { get; }

    public string Title =>
        Text.Split('\n').Select(line => line.Trim()).FirstOrDefault(line => line.StartsWith("# ", StringComparison.Ordinal))?[2..].Trim()
        ?? System.IO.Path.GetFileNameWithoutExtension(Path);
}
