using System.Text;
using System.Text.RegularExpressions;

namespace Miko.DocGen;

/// <summary>
/// Markdown building blocks. Output is read raw by agents and compiled by VitePress, whose pages
/// are Vue templates: a stray <c>&lt;IonItem&gt;</c> or <c>{{</c> in prose would be parsed as
/// markup, so free text is escaped and tag-like tokens become code spans.
/// </summary>
public static class Markdown
{
    private static readonly Regex TagLike = new(@"</?[A-Za-z][\w.-]*(?:\s[^<>]*)?/?>", RegexOptions.Compiled);

    public static string Code(string text)
    {
        text = text.Replace("|", "\\|");
        var fence = text.Contains('`') ? "``" : "`";
        var pad = text.StartsWith('`') || text.EndsWith('`') ? " " : "";
        return $"{fence}{pad}{text}{pad}{fence}";
    }

    public static string EscapeText(string text)
    {
        var builder = new StringBuilder();
        var last = 0;
        foreach (Match match in TagLike.Matches(text))
        {
            builder.Append(EscapePlain(text[last..match.Index]));
            builder.Append(Code(match.Value));
            last = match.Index + match.Length;
        }
        builder.Append(EscapePlain(text[last..]));
        return builder.ToString();
    }

    private static string EscapePlain(string text) => text
        .Replace("|", "\\|")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("{{", "{ {")
        .Replace("}}", "} }");

    public static string Table(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        builder.Append("| ").AppendJoin(" | ", headers).AppendLine(" |");
        builder.Append('|').Append(string.Concat(headers.Select(_ => " --- |"))).AppendLine();
        foreach (var row in rows)
            builder.Append("| ").AppendJoin(" | ", row.Select(cell => cell.Length == 0 ? " " : cell)).AppendLine(" |");
        return builder.ToString();
    }

    /// <summary>The relative link from one repo file to another, with forward slashes.</summary>
    public static string RelativeLink(string fromFile, string toFile)
    {
        var relative = Path.GetRelativePath(Path.GetDirectoryName(fromFile)!, toFile).Replace('\\', '/');
        return relative.StartsWith('.') ? relative : "./" + relative;
    }

    /// <summary>The anchor VitePress derives for a heading made of a single identifier.</summary>
    public static string Anchor(string heading) => heading.ToLowerInvariant();
}
