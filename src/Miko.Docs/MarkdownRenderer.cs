// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Miko.Docs;

public sealed record Heading(int Level, string Id, string Text);
public sealed record MarkdownPage(string Html, string Title, string Description, string Layout, bool Sidebar,
    bool Outline, IReadOnlyList<Heading> Headings, string PlainText);

/// <summary>A dependency-free Markdown renderer for documentation. Raw HTML is escaped.</summary>
public sealed class MarkdownRenderer
{
    public MarkdownPage Render(string markdown, SiteConfig? config = null, string sourcePath = "index.md")
    {
        config ??= new SiteConfig();
        var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').TrimStart('\uFEFF').Split('\n').ToList();
        var metadata = ReadFrontMatter(lines);
        var context = new Context(config, sourcePath);
        var html = context.Blocks(lines, 0);
        var title = metadata.GetValueOrDefault("title") ?? context.Headings.FirstOrDefault(h => h.Level == 1)?.Text
            ?? Path.GetFileNameWithoutExtension(sourcePath);
        var layout = metadata.GetValueOrDefault("layout", "doc");
        if (layout is not ("doc" or "home" or "page")) throw new InvalidDataException($"Unsupported layout '{layout}' in {sourcePath}.");
        var plain = WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]*>", " "));
        return new MarkdownPage(html, title, metadata.GetValueOrDefault("description", config.Description), layout,
            metadata.GetValueOrDefault("sidebar") != "false", metadata.GetValueOrDefault("outline") != "false",
            context.Headings, Regex.Replace(plain, @"\s+", " ").Trim());
    }

    private static Dictionary<string, string> ReadFrontMatter(List<string> lines)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (lines.Count == 0 || lines[0] != "---") return result;
        var end = lines.FindIndex(1, line => line == "---");
        if (end < 0) throw new InvalidDataException("Front matter must end with '---'.");
        for (var i = 1; i < end; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#')) continue;
            var match = Regex.Match(line, @"^(title|description|layout|sidebar|outline):\s*(.*)$", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new InvalidDataException($"Unsupported front matter '{line}'. Use scalar title, description, layout, sidebar, outline; configure home in miko.docs.json.");
            var value = match.Groups[2].Value.Trim();
            if (value.StartsWith('"'))
                value = System.Text.Json.JsonSerializer.Deserialize<string>(value) ?? "";
            else if (value.StartsWith('\'') && value.EndsWith('\'')) value = value[1..^1].Replace("''", "'");
            else value = Regex.Replace(value, @"\s+#.*$", "").Trim();
            result[match.Groups[1].Value] = value;
        }
        lines.RemoveRange(0, end + 1);
        return result;
    }

    private sealed class Context(SiteConfig config, string source)
    {
        public List<Heading> Headings { get; } = [];
        private readonly HashSet<string> ids = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (string Url, string Title)> references = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Regex ListMarker = new(@"^( {0,3})([-+*]|\d+[.)])\s+(.*)$");
        private static readonly Regex Fence = new(@"^ {0,3}(`{3,}|~{3,})(.*)$");
        private static readonly Regex Container = new(@"^:::\s*(info|note|tip|important|warning|danger|caution|details)(?:\s+(.*))?$");

        public string Blocks(IReadOnlyList<string> lines, int depth)
        {
            if (depth > 32) throw new InvalidDataException("Markdown nesting exceeds 32 levels.");
            var output = new StringBuilder();
            // Definitions may appear after their first use.
            foreach (var line in lines)
            {
                var reference = Regex.Match(line, "^ {0,3}\\[([^\\]]+)\\]:\\s*(\\S+)(?:\\s+\"(.*)\")?\\s*$");
                if (reference.Success) references[reference.Groups[1].Value] = (reference.Groups[2].Value, reference.Groups[3].Value);
            }
            for (var i = 0; i < lines.Count;)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line) || Regex.IsMatch(line, @"^ {0,3}\[[^\]]+\]:\s*\S+")) { i++; continue; }
                var fence = Fence.Match(line);
                if (fence.Success)
                {
                    var body = new List<string>();
                    var marker = fence.Groups[1].Value;
                    i++;
                    while (i < lines.Count && !Regex.IsMatch(lines[i], "^ {0,3}" + Regex.Escape(marker[0].ToString()) + "{" + marker.Length + @",}\s*$"))
                        body.Add(lines[i++]);
                    if (i < lines.Count) i++;
                    output.Append(Code(body, fence.Groups[2].Value));
                    continue;
                }
                // DocGen markers are source metadata. Keep comments in code fences above,
                // but omit standalone HTML comments from rendered content and search.
                if (Regex.IsMatch(line, @"^ {0,3}<!--"))
                {
                    var comment = new StringBuilder(line);
                    i++;
                    while (!comment.ToString().Contains("-->", StringComparison.Ordinal) && i < lines.Count)
                        comment.Append('\n').Append(lines[i++]);
                    var text = comment.ToString();
                    var end = text.IndexOf("-->", StringComparison.Ordinal);
                    if (end >= 0 && !string.IsNullOrWhiteSpace(text[(end + 3)..]))
                        output.Append("<p>").Append(Inline(text[(end + 3)..])).Append("</p>\n");
                    continue;
                }
                var container = Container.Match(line);
                if (container.Success)
                {
                    var body = new List<string>();
                    var nesting = 1;
                    string? innerFence = null;
                    i++;
                    while (i < lines.Count)
                    {
                        var current = lines[i++];
                        var currentFence = Fence.Match(current);
                        if (innerFence is not null)
                        {
                            if (Regex.IsMatch(current, "^ {0,3}" + Regex.Escape(innerFence[0].ToString()) + "{" + innerFence.Length + @",}\s*$")) innerFence = null;
                        }
                        else if (currentFence.Success) innerFence = currentFence.Groups[1].Value;
                        else if (Container.IsMatch(current)) nesting++;
                        else if (current.Trim() == ":::" && --nesting == 0) break;
                        body.Add(current);
                    }
                    if (nesting != 0) throw new InvalidDataException($"Unclosed custom container in {source}.");
                    var kind = container.Groups[1].Value;
                    var title = container.Groups[2].Success ? container.Groups[2].Value : kind.ToUpperInvariant();
                    output.Append(kind == "details" ? "<details" : "<div").Append($" class=\"custom-block {kind}\">");
                    output.Append(kind == "details" ? "<summary>" : "<p class=\"custom-block-title\">").Append(Inline(title))
                        .Append(kind == "details" ? "</summary>" : "</p>");
                    output.Append(Blocks(body, depth + 1)).Append(kind == "details" ? "</details>\n" : "</div>\n");
                    continue;
                }
                var heading = Regex.Match(line, @"^ {0,3}(#{1,6})\s+(.+?)\s*$");
                if (heading.Success)
                {
                    var headingText = Regex.Replace(heading.Groups[2].Value, @"\s+#+\s*$", "");
                    output.Append(RenderHeading(heading.Groups[1].Length, headingText)); i++; continue;
                }
                if (i + 1 < lines.Count && Regex.IsMatch(lines[i + 1], @"^ {0,3}(=+|-+)\s*$"))
                {
                    output.Append(RenderHeading(lines[i + 1].TrimStart()[0] == '=' ? 1 : 2, line)); i += 2; continue;
                }
                if (Regex.IsMatch(line, @"^ {0,3}((\*\s*){3,}|(-\s*){3,}|(_\s*){3,})$"))
                { output.Append("<hr>\n"); i++; continue; }
                if (line.TrimStart().StartsWith('>'))
                {
                    var quote = new List<string>();
                    while (i < lines.Count && Regex.IsMatch(lines[i], @"^ {0,3}>")) quote.Add(Regex.Replace(lines[i++], @"^ {0,3}> ?", ""));
                    output.Append("<blockquote>").Append(Blocks(quote, depth + 1)).Append("</blockquote>\n"); continue;
                }
                var list = ListMarker.Match(line);
                if (list.Success)
                {
                    var ordered = char.IsDigit(list.Groups[2].Value[0]);
                    var tag = ordered ? "ol" : "ul";
                    var start = ordered ? Regex.Match(list.Groups[2].Value, @"\d+").Value : "";
                    output.Append('<').Append(tag).Append(ordered ? $" start=\"{start}\"" : "").Append('>');
                    while (i < lines.Count)
                    {
                        var item = ListMarker.Match(lines[i]);
                        if (!item.Success || char.IsDigit(item.Groups[2].Value[0]) != ordered) break;
                        var indent = item.Groups[3].Index;
                        var body = new List<string> { item.Groups[3].Value };
                        i++;
                        while (i < lines.Count)
                        {
                            if (string.IsNullOrWhiteSpace(lines[i]))
                            {
                                if (i + 1 < lines.Count && lines[i + 1].TakeWhile(c => c == ' ').Count() >= indent)
                                { body.Add(""); i++; continue; }
                                break;
                            }
                            if (lines[i].TakeWhile(c => c == ' ').Count() < indent) break;
                            body.Add(lines[i++][indent..]);
                        }
                        var task = Regex.Match(body[0], @"^\[([ xX])\]\s+(.*)$");
                        output.Append(task.Success ? "<li class=\"task-list-item\">" : "<li>");
                        var checkbox = "";
                        if (task.Success)
                        {
                            body[0] = task.Groups[2].Value;
                            checkbox = "<input class=\"task-list-item-checkbox\" type=\"checkbox\" disabled aria-label=\"Task\"" +
                                (task.Groups[1].Value != " " ? " checked>" : ">");
                        }
                        var itemHtml = Blocks(body, depth + 1);
                        output.Append(task.Success && itemHtml.StartsWith("<p>", StringComparison.Ordinal)
                            ? "<p>" + checkbox + itemHtml[3..] : checkbox + itemHtml).Append("</li>");
                        while (i < lines.Count && string.IsNullOrWhiteSpace(lines[i])) i++;
                    }
                    output.Append("</").Append(tag).Append(">\n"); continue;
                }
                if (i + 1 < lines.Count && line.Contains('|') && IsTableDelimiter(lines[i + 1]))
                {
                    var headers = Cells(line);
                    var aligns = Cells(lines[i + 1]);
                    output.Append("<table><thead><tr>");
                    for (var col = 0; col < headers.Count; col++) output.Append(TableCell("th", headers[col], aligns.ElementAtOrDefault(col)));
                    output.Append("</tr></thead><tbody>"); i += 2;
                    while (i < lines.Count && !string.IsNullOrWhiteSpace(lines[i]) && lines[i].Contains('|'))
                    {
                        var cells = Cells(lines[i++]); output.Append("<tr>");
                        for (var col = 0; col < headers.Count; col++) output.Append(TableCell("td", cells.ElementAtOrDefault(col) ?? "", aligns.ElementAtOrDefault(col)));
                        output.Append("</tr>");
                    }
                    output.Append("</tbody></table>\n"); continue;
                }
                if (line.StartsWith("    ", StringComparison.Ordinal))
                {
                    var body = new List<string>();
                    while (i < lines.Count && (lines[i].StartsWith("    ", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(lines[i])))
                    { body.Add(lines[i].Length >= 4 ? lines[i][4..] : ""); i++; }
                    output.Append(Code(body, "")); continue;
                }
                var paragraph = new List<string> { line }; i++;
                while (i < lines.Count && !string.IsNullOrWhiteSpace(lines[i]) && !StartsBlock(lines[i]) &&
                    !(i + 1 < lines.Count && (IsTableDelimiter(lines[i + 1]) || Regex.IsMatch(lines[i + 1], @"^ {0,3}(=+|-+)\s*$"))))
                    paragraph.Add(lines[i++]);
                output.Append("<p>").Append(Inline(string.Join('\n', paragraph))).Append("</p>\n");
            }
            return output.ToString();
        }

        private static bool StartsBlock(string line) => Fence.IsMatch(line) || Container.IsMatch(line) || ListMarker.IsMatch(line) || Regex.IsMatch(line, @"^ {0,3}<!--") ||
            Regex.IsMatch(line, @"^ {0,3}(#{1,6}\s|>|([-*_]\s*){3,}$)");

        private string RenderHeading(int level, string text)
        {
            var explicitId = Regex.Match(text, @"\s+\{#([\p{L}\p{N}_-]+)\}$");
            if (explicitId.Success) text = text[..explicitId.Index];
            var html = Inline(text);
            var plain = WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]*>", ""));
            var stem = explicitId.Success ? explicitId.Groups[1].Value : Regex.Replace(plain.ToLowerInvariant(), @"[^\p{L}\p{N}_\s-]", "");
            stem = Regex.Replace(stem.Trim(), @"\s+", "-");
            if (stem.Length == 0) stem = "section";
            var id = stem;
            for (var suffix = 1; !ids.Add(id); suffix++) id = stem + "-" + suffix;
            Headings.Add(new Heading(level, id, plain));
            return $"<h{level} id=\"{E(id)}\" tabindex=\"-1\">{html}<a class=\"header-anchor\" href=\"#{E(id)}\" aria-label=\"Permalink to {E(plain)}\"></a></h{level}>\n";
        }

        private string Inline(string text, int depth = 0)
        {
            if (depth > 24) return E(text);
            var result = new StringBuilder();
            for (var i = 0; i < text.Length;)
            {
                var rest = text[i..];
                if (text[i] == '\\' && i + 1 < text.Length && char.IsPunctuation(text[i + 1]))
                { result.Append(E(text[i + 1].ToString())); i += 2; continue; }
                if (text[i] == '`')
                {
                    var length = rest.TakeWhile(c => c == '`').Count();
                    var delimiter = new string('`', length);
                    var end = text.IndexOf(delimiter, i + length, StringComparison.Ordinal);
                    if (end >= 0)
                    {
                        var value = text[(i + length)..end].Replace('\n', ' ');
                        if (value.Length > 1 && value.StartsWith(' ') && value.EndsWith(' ') && value.Trim().Length > 0) value = value[1..^1];
                        result.Append("<code>").Append(E(value)).Append("</code>"); i = end + length; continue;
                    }
                }
                if (text[i] is '[' or '!')
                {
                    var image = rest.StartsWith("![", StringComparison.Ordinal);
                    var labelStart = i + (image ? 2 : 1);
                    var labelEnd = text.IndexOf(']', labelStart);
                    if ((text[i] == '[' || image) && labelEnd >= 0)
                    {
                        string? url = null; var title = ""; var end = labelEnd + 1;
                        if (end < text.Length && text[end] == '(')
                        {
                            var balance = 1; var close = end + 1;
                            for (; close < text.Length && balance > 0; close++)
                            {
                                if (text[close] == '(') balance++;
                                if (text[close] == ')') balance--;
                            }
                            if (balance == 0)
                            {
                                var target = text[(end + 1)..(close - 1)].Trim();
                                var match = Regex.Match(target, "^(.*?)(?:\\s+\"(.*)\")?$");
                                url = match.Groups[1].Value.Trim('<', '>'); title = match.Groups[2].Value; end = close;
                            }
                        }
                        else
                        {
                            var key = text[labelStart..labelEnd];
                            if (end < text.Length && text[end] == '[' && text.IndexOf(']', end + 1) is var close && close >= 0)
                            { key = close == end + 1 ? key : text[(end + 1)..close]; end = close + 1; }
                            if (references.TryGetValue(key, out var reference)) (url, title) = reference;
                        }
                        if (url is not null)
                        {
                            var resolved = Urls.Resolve(url, config, source);
                            var label = text[labelStart..labelEnd];
                            var titleAttr = title.Length > 0 ? $" title=\"{E(title)}\"" : "";
                            result.Append(image ? $"<img src=\"{E(resolved)}\" alt=\"{E(label)}\"{titleAttr} loading=\"lazy\">"
                                : $"<a href=\"{E(resolved)}\"{titleAttr}>{Inline(label, depth + 1)}</a>");
                            i = end; continue;
                        }
                    }
                }
                var handled = false;
                foreach (var (marker, tag) in new[] { ("***", "strong><em"), ("___", "strong><em"), ("**", "strong"), ("__", "strong"), ("~~", "s"), ("*", "em"), ("_", "em") })
                {
                    if (!rest.StartsWith(marker, StringComparison.Ordinal) || rest.Length <= marker.Length || char.IsWhiteSpace(rest[marker.Length])) continue;
                    if (marker[0] == '_' && i > 0 && char.IsLetterOrDigit(text[i - 1])) continue;
                    var end = text.IndexOf(marker, i + marker.Length, StringComparison.Ordinal);
                    if (end <= i + marker.Length || char.IsWhiteSpace(text[end - 1])) continue;
                    result.Append('<').Append(tag).Append('>').Append(Inline(text[(i + marker.Length)..end], depth + 1))
                        .Append(tag == "strong><em" ? "</em></strong>" : $"</{tag}>");
                    i = end + marker.Length; handled = true; break;
                }
                if (handled) continue;
                var auto = Regex.Match(rest, @"^<(https?://[^<>\s]+|mailto:[^<>\s]+)>");
                if (auto.Success)
                { result.Append($"<a href=\"{E(Urls.Safe(auto.Groups[1].Value))}\">{E(auto.Groups[1].Value)}</a>"); i += auto.Length; continue; }
                var entity = Regex.Match(rest, @"^&(?:#\d+|#x[0-9a-fA-F]+|[a-zA-Z]+);");
                if (entity.Success) { result.Append(E(WebUtility.HtmlDecode(entity.Value))); i += entity.Length; continue; }
                if (rest.StartsWith("  \n", StringComparison.Ordinal) || rest.StartsWith("\\\n", StringComparison.Ordinal))
                { result.Append("<br>\n"); i += rest[0] == '\\' ? 2 : 3; continue; }
                // Encode a full Unicode scalar, not each UTF-16 surrogate separately.
                var scalarLength = char.IsSurrogatePair(text, i) ? 2 : 1;
                result.Append(text[i] == '\n' ? "\n" : E(text.Substring(i, scalarLength)));
                i += scalarLength;
            }
            return result.ToString();
        }

        private static bool IsTableDelimiter(string line)
        {
            var cells = Cells(line);
            return line.Contains('|') && cells.Count > 0 && cells.All(cell => Regex.IsMatch(cell, @"^:?-{3,}:?$"));
        }

        private static List<string> Cells(string line)
        {
            line = line.Trim();
            if (line.StartsWith('|')) line = line[1..];
            if (line.EndsWith('|') && !line.EndsWith("\\|", StringComparison.Ordinal)) line = line[..^1];
            var cells = new List<string>();
            var value = new StringBuilder();
            var codeDelimiter = 0;
            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == '\\' && i + 1 < line.Length && line[i + 1] == '|')
                { value.Append('|'); i++; continue; }
                if (line[i] == '`')
                {
                    var length = 1;
                    while (i + length < line.Length && line[i + length] == '`') length++;
                    if (codeDelimiter == length) codeDelimiter = 0;
                    else if (codeDelimiter == 0 && line.IndexOf(new string('`', length), i + length, StringComparison.Ordinal) >= 0) codeDelimiter = length;
                    value.Append('`', length); i += length - 1; continue;
                }
                if (line[i] == '|' && codeDelimiter == 0)
                { cells.Add(value.ToString().Trim()); value.Clear(); }
                else value.Append(line[i]);
            }
            cells.Add(value.ToString().Trim());
            return cells;
        }

        private string TableCell(string tag, string content, string? delimiter)
        {
            var align = delimiter?.EndsWith(':') == true ? (delimiter.StartsWith(':') ? "center" : "right") : "left";
            return $"<{tag} style=\"text-align:{align}\">{Inline(content)}</{tag}>";
        }

        private static string Code(List<string> lines, string info)
        {
            var language = Regex.Match(info.Trim(), @"^[\w+#-]+").Value;
            var numbers = info.Contains(":line-numbers", StringComparison.Ordinal) && !info.Contains(":no-line-numbers", StringComparison.Ordinal);
            var highlighted = new HashSet<int>();
            var ranges = Regex.Match(info, @"\{([\d, -]+)\}");
            if (ranges.Success)
                foreach (var range in ranges.Groups[1].Value.Split(','))
                {
                    var bounds = range.Trim().Split('-');
                    if (int.TryParse(bounds[0], out var first) && first > 0)
                    {
                        var last = bounds.Length > 1 && int.TryParse(bounds[1], out var parsed) ? parsed : first;
                        for (var n = first; n <= Math.Min(last, lines.Count); n++) highlighted.Add(n);
                    }
                }
            var result = new StringBuilder($"<div class=\"language-{E(language.Length > 0 ? language : "text")}{(numbers ? " line-numbers-mode" : "")}\" dir=\"ltr\"><button class=\"copy\" type=\"button\" title=\"Copy code\" aria-label=\"Copy code\"></button><span class=\"lang\">{E(language)}</span><pre tabindex=\"0\"><code>");
            for (var i = 0; i < lines.Count; i++)
                result.Append($"<span class=\"line{(highlighted.Contains(i + 1) ? " highlighted" : "")}\">{E(lines[i])}</span>\n");
            result.Append("</code></pre>");
            if (numbers) result.Append("<div class=\"line-numbers-wrapper\" aria-hidden=\"true\">")
                .Append(string.Join("<br>", Enumerable.Range(1, lines.Count))).Append("</div>");
            return result.Append("</div>\n").ToString();
        }
    }

    internal static string E(string? text) => WebUtility.HtmlEncode(text ?? "");
}
