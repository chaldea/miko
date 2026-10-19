using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Miko.DocGen;

/// <summary>
/// Reads the compiler-generated XML documentation file next to an assembly and renders a
/// member's <c>&lt;summary&gt;</c> as one line of Markdown, suitable for a table cell.
/// <para>
/// Razor components keep the <c>///</c> comments written in their <c>@code</c> blocks, so a
/// parameter's summary is the same text a developer sees in IntelliSense.
/// </para>
/// </summary>
public sealed class XmlDocumentation
{
    private readonly Dictionary<string, XElement> _members = new(StringComparer.Ordinal);

    public XmlDocumentation(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var path = Path.ChangeExtension(assembly.Location, ".xml");
            if (!File.Exists(path))
                continue;
            // PreserveWhitespace: the space between two adjacent inline elements
            // (<c>a</c> <c>b</c>) is a whitespace-only text node, which the default load drops.
            foreach (var member in XDocument.Load(path, LoadOptions.PreserveWhitespace).Descendants("member"))
            {
                var name = member.Attribute("name")?.Value;
                if (name is not null)
                    _members[name] = member;
            }
        }
    }

    /// <summary>The rendered summary, or null when the member carries none.</summary>
    /// <param name="member">The documented member.</param>
    /// <param name="crefRenderer">Turns a <c>cref</c> target into Markdown (e.g. a link to the
    /// page documenting that component).</param>
    public string? Summary(MemberInfo member, Func<string, string> crefRenderer)
    {
        var id = MemberId(member);
        if (id is null || !_members.TryGetValue(id, out var element))
            return null;
        var summary = element.Element("summary");
        if (summary is null)
            return null;
        var text = Clean(Render(summary, crefRenderer));
        return text.Length == 0 ? null : text;
    }

    private static string? MemberId(MemberInfo member) => member switch
    {
        Type type => "T:" + TypeId(type),
        PropertyInfo property => $"P:{TypeId(property.DeclaringType!)}.{property.Name}",
        FieldInfo field => $"F:{TypeId(field.DeclaringType!)}.{field.Name}",
        MethodInfo method => MethodId(method),
        _ => null,
    };

    private static string TypeId(Type type)
    {
        if (type.IsGenericType && !type.IsGenericTypeDefinition)
            type = type.GetGenericTypeDefinition();
        return (type.FullName ?? type.Name).Replace('+', '.');
    }

    private static string MethodId(MethodInfo method)
    {
        var builder = new StringBuilder("M:").Append(TypeId(method.DeclaringType!)).Append('.').Append(method.Name);
        if (method.IsGenericMethodDefinition)
            builder.Append("``").Append(method.GetGenericArguments().Length);
        var parameters = method.GetParameters();
        if (parameters.Length > 0)
        {
            builder.Append('(');
            builder.AppendJoin(',', parameters.Select(p => ParameterTypeId(p.ParameterType, method)));
            builder.Append(')');
        }
        return builder.ToString();
    }

    private static string ParameterTypeId(Type type, MethodInfo method)
    {
        if (type.IsGenericMethodParameter)
            return "``" + type.GenericParameterPosition;
        if (type.IsGenericTypeParameter)
            return "`" + type.GenericParameterPosition;
        if (type.IsByRef)
            return ParameterTypeId(type.GetElementType()!, method) + "@";
        if (type.IsArray)
            return ParameterTypeId(type.GetElementType()!, method) + "[]";
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var name = (definition.FullName ?? definition.Name).Replace('+', '.');
            name = name[..name.IndexOf('`')];
            return $"{name}{{{string.Join(",", type.GetGenericArguments().Select(a => ParameterTypeId(a, method)))}}}";
        }
        return (type.FullName ?? type.Name).Replace('+', '.');
    }

    private static string Render(XElement element, Func<string, string> crefRenderer)
    {
        var builder = new StringBuilder();
        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XText text:
                    builder.Append(Markdown.EscapeText(text.Value));
                    break;
                case XElement child:
                    builder.Append(RenderElement(child, crefRenderer));
                    break;
            }
        }
        return builder.ToString();
    }

    private static string RenderElement(XElement element, Func<string, string> crefRenderer)
    {
        switch (element.Name.LocalName)
        {
            case "c":
            case "code":
                return Markdown.Code(Collapse(element.Value));
            case "see":
            case "seealso":
            {
                var inner = Collapse(element.Value);
                if (element.Attribute("langword") is { } langword)
                    return Markdown.Code(langword.Value);
                if (element.Attribute("href") is { } href)
                    return $"[{(inner.Length > 0 ? Markdown.EscapeText(inner) : href.Value)}]({href.Value})";
                if (element.Attribute("cref") is { } cref)
                    return inner.Length > 0 ? Markdown.EscapeText(inner) : crefRenderer(cref.Value);
                return Markdown.EscapeText(inner);
            }
            case "paramref":
            case "typeparamref":
                return Markdown.Code(element.Attribute("name")?.Value ?? "");
            case "para":
            case "br":
                return " " + Render(element, crefRenderer) + " ";
            case "b":
            case "strong":
                return "**" + Render(element, crefRenderer).Trim() + "**";
            case "i":
            case "em":
                return "*" + Render(element, crefRenderer).Trim() + "*";
            case "list":
                return " " + string.Join("; ", element.Elements("item").Select(item => Render(item, crefRenderer).Trim())) + " ";
            default:
                return Render(element, crefRenderer);
        }
    }

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    // Issue numbers are maintainers' breadcrumbs; in user docs they are noise.
    private static readonly Regex IssueReference = new(@"\s*\((?:see\s+)?ISSUE-\d+(?:\s*[,/]\s*ISSUE-\d+)*\)", RegexOptions.Compiled);

    private static string Clean(string text)
    {
        text = IssueReference.Replace(text, "");
        text = Collapse(text);
        // Inline elements leave a space before trailing punctuation ("`x` ." → "`x`.").
        return Regex.Replace(text, @"\s+([.,;:])(?=\s|$)", "$1");
    }
}
