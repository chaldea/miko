using System.Text;
using System.Xml;
using System.Xml.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Miko.Components.Reader;

internal static class EpubReader
{
    public static ReaderDocument ReadDirectory(string fallbackTitle, Func<string, byte[]> read)
    {
        var container = Xml(read("META-INF/container.xml"));
        string packagePath = container.Descendants().FirstOrDefault(e => e.Name.LocalName == "rootfile")?.Attribute("full-path")?.Value
            ?? throw new InvalidDataException("EPUB container has no package document.");
        packagePath = Resolve("", packagePath);
        var package = Xml(read(packagePath));
        string title = package.Descendants().FirstOrDefault(e => e.Name.LocalName == "title")?.Value ?? fallbackTitle;
        var manifest = package.Descendants().Where(e => e.Name.LocalName == "item")
            .ToDictionary(e => Required(e, "id"), StringComparer.Ordinal);
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        var nav = manifest.Values.FirstOrDefault(e => ((string?)e.Attribute("properties") ?? "").Split(' ').Contains("nav"));
        if (nav != null)
        {
            string navPath = Resolve(packagePath, Required(nav, "href"));
            using var stream = new MemoryStream(read(navPath));
            using var navigation = new HtmlParser().ParseDocument(stream);
            var toc = navigation.QuerySelectorAll("nav").FirstOrDefault(e => (e.GetAttribute("epub:type") ?? "").Split(' ').Contains("toc"));
            foreach (var a in toc?.QuerySelectorAll("a[href]").AsEnumerable() ?? [])
                labels.TryAdd(Resolve(navPath, a.GetAttribute("href")!), a.TextContent.Trim());
        }
        else
        {
            var ncx = manifest.Values.FirstOrDefault(e => (string?)e.Attribute("media-type") == "application/x-dtbncx+xml");
            if (ncx != null)
            {
                string ncxPath = Resolve(packagePath, Required(ncx, "href"));
                foreach (var point in Xml(read(ncxPath)).Descendants().Where(e => e.Name.LocalName == "navPoint"))
                {
                    var content = point.Elements().FirstOrDefault(e => e.Name.LocalName == "content");
                    var label = point.Elements().FirstOrDefault(e => e.Name.LocalName == "navLabel");
                    if (content != null && label != null) labels.TryAdd(Resolve(ncxPath, Required(content, "src")), label.Value.Trim());
                }
            }
        }
        var entries = new List<ReaderEntry>();
        foreach (var reference in package.Descendants().Where(e => e.Name.LocalName == "itemref" && (string?)e.Attribute("linear") != "no"))
        {
            if (!manifest.TryGetValue(Required(reference, "idref"), out var item)) throw new InvalidDataException("EPUB spine references an unknown item.");
            if ((string?)item.Attribute("media-type") is not ("application/xhtml+xml" or "text/html"))
                throw new NotSupportedException("This EPUB spine contains non-text content.");
            string path = Resolve(packagePath, Required(item, "href"));
            entries.Add(new(path, labels.GetValueOrDefault(path, $"Chapter {entries.Count + 1}")));
        }
        return new(title, entries.AsReadOnly());
    }

    public static string ReadText(byte[] bytes)
    {
        // The inert HTML parser understands XHTML's named entities without fetching an external DTD.
        // It does not load resources or execute scripts; XML remains reserved for container/OPF/NCX.
        using var stream = new MemoryStream(bytes);
        using var document = new HtmlParser().ParseDocument(stream);
        var body = document.Body ?? document.DocumentElement;
        var text = new StringBuilder();
        Visit(body);
        return text.ToString().Trim();

        void Break() { if (text.Length > 0 && text[^1] != '\n') text.Append('\n'); }
        void Visit(INode node)
        {
            if (node is IText t)
            {
                foreach (char ch in t.Data)
                    if (!char.IsWhiteSpace(ch)) text.Append(ch);
                    else if (text.Length > 0 && !char.IsWhiteSpace(text[^1])) text.Append(' ');
                return;
            }
            if (node is not IElement element) return;
            string name = element.LocalName;
            if (name is "script" or "style" or "head") return;
            bool block = name is "p" or "div" or "section" or "article" or "li" or "h1" or "h2" or "h3" or "h4" or "blockquote" or "tr" or "br";
            if (block) Break();
            foreach (var child in element.ChildNodes) Visit(child);
            if (block) Break();
        }
    }

    internal static string Resolve(string basePath, string href)
    {
        string relative = Uri.UnescapeDataString(href.Split('#')[0]);
        if (relative.Contains(':') || relative.StartsWith('/') || relative.Contains('\\'))
            throw new InvalidDataException("EPUB resources must be relative archive paths.");
        var segments = new List<string>();
        string combined = string.IsNullOrEmpty(relative) ? basePath : basePath[..(basePath.LastIndexOf('/') + 1)] + relative;
        foreach (string segment in combined.Split('/'))
        {
            if (segment is "" or ".") continue;
            if (segment == "..")
            {
                if (segments.Count == 0) throw new InvalidDataException("EPUB path escapes the archive.");
                segments.RemoveAt(segments.Count - 1);
            }
            else segments.Add(segment);
        }
        return string.Join('/', segments);
    }

    private static string Required(XElement element, string attribute) => (string?)element.Attribute(attribute)
        ?? throw new InvalidDataException($"Missing EPUB attribute: {attribute}");

    private static XDocument Xml(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null, MaxCharactersInDocument = 64 * 1024 * 1024 });
        return XDocument.Load(reader);
    }
}
