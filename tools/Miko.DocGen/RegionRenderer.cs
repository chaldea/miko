using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Miko.Ionic;

namespace Miko.DocGen;

/// <summary>Renders the body of one generated region.</summary>
public sealed class RegionRenderer
{
    private const string ChildContentDescription = "Content placed between the component's tags.";

    private readonly string _repoRoot;
    private readonly ComponentCatalog _catalog;
    private readonly IReadOnlyDictionary<string, DocumentFile> _pages;
    private readonly IReadOnlyList<DocumentFile> _documents;
    private readonly List<string> _problems;
    private readonly Lazy<(IonicTheme Md, IonicTheme Ios)> _themes = new(() => (IonicTheme.CreateMd(), IonicTheme.CreateIos()));

    public RegionRenderer(
        string repoRoot,
        ComponentCatalog catalog,
        IReadOnlyDictionary<string, DocumentFile> pages,
        IReadOnlyList<DocumentFile> documents,
        List<string> problems)
    {
        _repoRoot = repoRoot;
        _catalog = catalog;
        _pages = pages;
        _documents = documents;
        _problems = problems;
    }

    public string Render(DocumentFile document, string kind, IReadOnlyList<string> arguments) => kind switch
    {
        "api" => Api(document, arguments),
        "tokens" => Tokens(document, arguments),
        "type" => Types(document, arguments),
        "example" => Example(document, arguments),
        "component-index" => ComponentIndex(document, arguments),
        "elements" => Elements(document),
        "include" => Include(document, arguments),
        _ => Problem(document, $"unknown region kind '{kind}'."),
    };

    private string Problem(DocumentFile document, string message)
    {
        _problems.Add($"{document.RelativePath}: {message}");
        return $"<!-- {message} -->";
    }

    // --- api -------------------------------------------------------------------------------

    private string Api(DocumentFile document, IReadOnlyList<string> names)
    {
        var builder = new StringBuilder();
        var conventions = Markdown.RelativeLink(document.Path, Path.Combine(_repoRoot, DocsGenerator.ComponentDocsDir, "index.md"));
        builder.AppendLine($"Every component also accepts `Class` (extra CSS classes), `Style` (an inline `Style` object) and `Id` (the root element's id) — see [Conventions]({conventions}#conventions).");

        foreach (var name in names)
        {
            builder.AppendLine();
            if (!_catalog.Components.TryGetValue(name, out var component))
            {
                builder.AppendLine(Problem(document, $"'{name}' is not a public Miko.Ionic component."));
                continue;
            }

            builder.AppendLine($"### {name}");
            builder.AppendLine();
            var parameters = _catalog.Parameters(component);
            if (parameters.Count == 0)
            {
                builder.AppendLine("No parameters besides `Class`, `Style` and `Id`.");
                continue;
            }

            var names_ = parameters.Select(p => p.Property.Name).ToHashSet(StringComparer.Ordinal);
            string Describe(ComponentParameter parameter)
            {
                var property = parameter.Property;
                var summary = _catalog.Documentation.Summary(property, cref => Cref(document, cref, component));
                if (summary is null)
                {
                    if (property.Name == "ChildContent")
                        summary = ChildContentDescription;
                    else
                        _problems.Add($"{document.RelativePath}: {name}.{property.Name} has no XML <summary>; document the parameter in the component source.");
                }
                if (parameter.Kind == ParameterKind.Property && names_.Contains(property.Name + "Changed")
                    && summary?.Contains("@bind", StringComparison.Ordinal) != true)
                {
                    summary = $"{summary} Two-way bindable: `@bind-{property.Name}`.".Trim();
                }
                return summary ?? "";
            }

            var groups = new (ParameterKind Kind, string Header)[]
            {
                (ParameterKind.Property, "Parameter"),
                (ParameterKind.Event, "Event"),
                (ParameterKind.Slot, "Slot"),
            };
            var first = true;
            foreach (var (kind, header) in groups)
            {
                var rows = parameters.Where(p => p.Kind == kind).ToList();
                if (rows.Count == 0)
                    continue;
                if (!first)
                    builder.AppendLine();
                first = false;
                if (kind == ParameterKind.Property)
                {
                    builder.Append(Markdown.Table(
                        [header, "Type", "Default", "Description"],
                        rows.Select(p => (IReadOnlyList<string>)[Markdown.Code(p.Property.Name), Markdown.Code(p.TypeName), p.DefaultValue is null ? "—" : Markdown.Code(p.DefaultValue), Describe(p)])));
                }
                else
                {
                    builder.Append(Markdown.Table(
                        [header, "Type", "Description"],
                        rows.Select(p => (IReadOnlyList<string>)[Markdown.Code(p.Property.Name), Markdown.Code(p.TypeName), Describe(p)])));
                }
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// A <c>cref</c> as Markdown: a link when the target type is documented on some page,
    /// otherwise a code span with the member's short name.
    /// </summary>
    private string Cref(DocumentFile document, string cref, Type? context)
    {
        var colon = cref.IndexOf(':');
        var kind = colon > 0 ? cref[..colon] : "";
        var target = colon > 0 ? cref[(colon + 1)..] : cref;
        var paren = target.IndexOf('(');
        if (paren >= 0)
            target = target[..paren];
        target = target.Replace("``", "`");

        var segments = target.Split('.');
        string typeName, display;
        if (kind == "T")
        {
            typeName = StripArity(segments[^1]);
            display = typeName;
        }
        else
        {
            typeName = segments.Length > 1 ? StripArity(segments[^2]) : "";
            var member = StripArity(segments[^1]);
            display = context is not null && typeName == context.Name ? member : $"{typeName}.{member}";
        }

        if (_pages.TryGetValue(typeName, out var page))
        {
            var link = page == document ? "" : Markdown.RelativeLink(document.Path, page.Path);
            return $"[{Markdown.Code(display)}]({link}#{Markdown.Anchor(typeName)})";
        }
        return Markdown.Code(display);
    }

    private static string StripArity(string name)
    {
        var tick = name.IndexOf('`');
        return tick >= 0 ? name[..tick] : name;
    }

    // --- tokens ----------------------------------------------------------------------------

    private string Tokens(DocumentFile document, IReadOnlyList<string> names)
    {
        var builder = new StringBuilder();
        var theming = Markdown.RelativeLink(document.Path, Path.Combine(_repoRoot, DocsGenerator.ComponentDocsDir, "theming.md"));
        foreach (var name in names)
        {
            if (!ComponentCatalog.ThemeTokens.TryGetValue(name, out var themeProperty))
            {
                builder.AppendLine(Problem(document, $"'{name}' is not a token property of IonicTheme."));
                continue;
            }

            var tokenType = themeProperty.PropertyType;
            if (names.Count > 1)
                builder.AppendLine($"#### {tokenType.Name}").AppendLine();

            var link = document.Path.EndsWith("theming.md", StringComparison.Ordinal) ? "" : $" See [Theming]({theming}).";
            builder.AppendLine(
                $"Theme token `IonicTheme.{name}` (`{tokenType.Name}`). Assign only the values you want to change — the rest keep the mode defaults below. " +
                $"App-wide: `builder.AddIonic(o => o.Theme.{name} = new {tokenType.Name} {{ … }})`; for one subtree: a `ConfigProvider`.{link}");
            builder.AppendLine();

            var md = themeProperty.GetValue(_themes.Value.Md);
            var ios = themeProperty.GetValue(_themes.Value.Ios);
            var properties = tokenType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p is { CanRead: true, CanWrite: true } && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToList();
            var summaries = properties.ToDictionary(p => p, p => _catalog.Documentation.Summary(p, cref => Cref(document, cref, tokenType)));
            // Token names are self-describing and most carry no summary; a mostly empty column
            // would be noise, so the few notes that exist become a list under the table instead.
            var withDescription = summaries.Values.Count(s => s is not null) * 2 >= properties.Count;

            string Cell(object? value) => Formatting.Value(value) is { } text ? Markdown.Code(text) : "—";
            var headers = withDescription
                ? new[] { "Token", "Type", "md", "ios", "Description" }
                : new[] { "Token", "Type", "md", "ios" };
            builder.Append(Markdown.Table(headers, properties.Select(p =>
            {
                var row = new List<string> { Markdown.Code(p.Name), Markdown.Code(Formatting.TypeName(p)), Cell(p.GetValue(md)), Cell(p.GetValue(ios)) };
                if (withDescription)
                    row.Add(summaries[p] ?? "");
                return (IReadOnlyList<string>)row;
            })));
            if (!withDescription)
            {
                var notes = properties.Where(p => summaries[p] is not null).ToList();
                if (notes.Count > 0)
                {
                    builder.AppendLine();
                    foreach (var p in notes)
                        builder.AppendLine($"- {Markdown.Code(p.Name)}: {summaries[p]}");
                }
            }
            builder.AppendLine();
        }
        return builder.ToString();
    }

    // --- type ------------------------------------------------------------------------------

    private static readonly HashSet<string> RecordBoilerplate = new(StringComparer.Ordinal)
    {
        "Equals", "GetHashCode", "ToString", "Deconstruct", "PrintMembers", "<Clone>$", "GetType",
    };

    private string Types(DocumentFile document, IReadOnlyList<string> names)
    {
        var builder = new StringBuilder();
        foreach (var name in names)
        {
            var type = _catalog.FindType(name);
            if (type is null)
            {
                builder.AppendLine(Problem(document, $"'{name}' is not a public type of Miko.Ionic."));
                continue;
            }

            builder.AppendLine($"### {name}").AppendLine();
            if (_catalog.Documentation.Summary(type, cref => Cref(document, cref, type)) is { } summary)
                builder.AppendLine(summary).AppendLine();

            if (type.IsEnum)
            {
                builder.Append(Markdown.Table(["Value", "Description"],
                    type.GetFields(BindingFlags.Public | BindingFlags.Static).Select(field => (IReadOnlyList<string>)
                        [Markdown.Code(field.Name), _catalog.Documentation.Summary(field, cref => Cref(document, cref, type)) ?? ""])));
                builder.AppendLine();
                continue;
            }

            object? instance = null;
            if (type.GetConstructor(Type.EmptyTypes) is not null && !type.IsAbstract)
            {
                try { instance = Activator.CreateInstance(type); } catch { instance = null; }
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0 && p.Name != "EqualityContract" && p.DeclaringType != typeof(object))
                .OrderBy(p => p.MetadataToken)
                .ToList();
            if (properties.Count > 0)
            {
                builder.Append(Markdown.Table(["Property", "Type", "Default", "Description"], properties.Select(p =>
                {
                    string defaultValue = "—";
                    if (instance is not null && p.CanRead)
                    {
                        try { defaultValue = Formatting.Value(p.GetValue(instance)) is { } text ? Markdown.Code(text) : "—"; }
                        catch { defaultValue = "—"; }
                    }
                    var readOnly = p.SetMethod is not { IsPublic: true } ? " (read-only)" : "";
                    return (IReadOnlyList<string>)[Markdown.Code(p.Name), Markdown.Code(Formatting.TypeName(p)), defaultValue,
                        (_catalog.Documentation.Summary(p, cref => Cref(document, cref, type)) ?? "") + readOnly];
                })));
                builder.AppendLine();
            }

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && !RecordBoilerplate.Contains(m.Name) && m.GetCustomAttribute<CompilerGeneratedAttribute>() is null)
                .OrderBy(m => m.Name, StringComparer.Ordinal)
                .ThenBy(m => m.GetParameters().Length)
                .ToList();
            if (methods.Count > 0)
            {
                builder.Append(Markdown.Table(["Method", "Returns", "Description"], methods.Select(m => (IReadOnlyList<string>)
                    [Markdown.Code(Signature(m)), Markdown.Code(Formatting.TypeName(m.ReturnParameter)), _catalog.Documentation.Summary(m, cref => Cref(document, cref, type)) ?? ""])));
                builder.AppendLine();
            }
        }
        return builder.ToString();
    }

    private static string Signature(MethodInfo method)
    {
        var builder = new StringBuilder(method.Name);
        if (method.IsGenericMethodDefinition)
            builder.Append('<').AppendJoin(", ", method.GetGenericArguments().Select(a => a.Name)).Append('>');
        builder.Append('(');
        builder.AppendJoin(", ", method.GetParameters().Select(p =>
        {
            var text = $"{Formatting.TypeName(p)} {p.Name}";
            if (p.HasDefaultValue)
                text += " = " + (p.DefaultValue is null ? (p.ParameterType.IsValueType ? "default" : "null") : Formatting.Value(p.DefaultValue));
            return text;
        }));
        builder.Append(')');
        return builder.ToString();
    }

    // --- example ---------------------------------------------------------------------------

    private string Example(DocumentFile document, IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 1)
            return Problem(document, "an example region names exactly one .razor file.");
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(document.Path)!, arguments[0]));
        if (!File.Exists(path))
            return Problem(document, $"example file '{arguments[0]}' does not exist.");
        var code = File.ReadAllText(path).Replace("\r\n", "\n").Trim('\n').TrimEnd();
        return $"```razor\n{code}\n```";
    }

    // --- elements --------------------------------------------------------------------------

    private string Elements(DocumentFile document)
    {
        ElementCatalog catalog;
        try
        {
            catalog = ElementCatalog.Load(CompilerLocation.Path);
        }
        catch (Exception e)
        {
            return Problem(document, $"could not read the Razor compiler's element table: {e.Message}");
        }

        var builder = new StringBuilder();
        builder.AppendLine("Every element accepts these attributes:");
        builder.AppendLine();
        builder.Append(Markdown.Table(["Attribute", "Sets"],
            catalog.CommonAttributes.Where(a => !a.Name.StartsWith("on", StringComparison.Ordinal))
                .Select(a => (IReadOnlyList<string>)[Markdown.Code(a.Name), Markdown.Code(a.Property)])));
        builder.AppendLine();
        builder.AppendLine("Event attributes (`@onclick=\"Handler\"` etc.): " +
                           string.Join(", ", catalog.CommonAttributes.Where(a => a.Name.StartsWith("on", StringComparison.Ordinal))
                               .Select(a => Markdown.Code("@" + a.Name))) + ".");
        builder.AppendLine();

        // A declared @bind only works when its value attribute is one the element applies.
        bool Applies(BindMapping bind) =>
            catalog.CommonAttributes.Any(a => a.Name == bind.ValueAttribute)
            || catalog.Tags.Any(t => string.Equals(t.Tag, bind.Element, StringComparison.OrdinalIgnoreCase)
                                     && t.Attributes.Any(a => a.Name == bind.ValueAttribute));

        string Binding(ElementTag tag)
        {
            var binds = catalog.Binds.Where(b => string.Equals(b.Element, tag.Tag, StringComparison.OrdinalIgnoreCase) && Applies(b)).ToList();
            if (binds.Count == 0)
                return "—";
            if (tag.Tag == "input")
            {
                var typed = binds.Where(b => b.InputType is not null)
                    .GroupBy(b => b.ValueAttribute)
                    .Select(g => $"{Markdown.Code(g.Key)} ({string.Join(", ", g.Select(b => b.InputType))})");
                return "`@bind` → " + string.Join("; ", typed);
            }
            return "`@bind` → " + string.Join(", ", binds.Select(b => Markdown.Code(b.ValueAttribute)).Distinct());
        }

        builder.Append(Markdown.Table(["Tag", "Element class", "Extra attributes", "Binding"],
            catalog.Tags.Select(tag => (IReadOnlyList<string>)
            [
                Markdown.Code($"<{tag.Tag}>"),
                Markdown.Code(tag.ElementType.Name),
                tag.Attributes.Count == 0 ? "—" : string.Join(", ", tag.Attributes.Select(a => Markdown.Code(a.Name))),
                Binding(tag),
            ])));

        var broken = catalog.Binds.Where(b => !Applies(b)).ToList();
        if (broken.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("::: warning Declared but not applied");
            foreach (var bind in broken)
                builder.AppendLine($"`<{bind.Element} @bind>` compiles, but the element does not apply `{bind.ValueAttribute}`, so the bound value is not shown. Read changes through `@on{bind.ChangeAttribute[2..]}` instead, or use a component.");
            builder.AppendLine(":::");
        }

        var codeOnly = catalog.CodeOnlyElements.ToList();
        if (codeOnly.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Element classes with no Razor tag — create them from C# only: " +
                               string.Join(", ", codeOnly.Select(t => $"{Markdown.Code(t.Name)} ({Markdown.Code(ElementCatalog.TagNameOf(t)!)})")) + ".");
        }
        return builder.ToString();
    }

    // --- include ---------------------------------------------------------------------------

    /// <summary>A Markdown fragment inlined verbatim, minus its <c># Title</c> line.</summary>
    private string Include(DocumentFile document, IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 1)
            return Problem(document, "an include region names exactly one file.");
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(document.Path)!, arguments[0]));
        if (!File.Exists(path))
            return Problem(document, $"included file '{arguments[0]}' does not exist.");
        var lines = File.ReadAllText(path).Replace("\r\n", "\n").Split('\n').ToList();
        var title = lines.FindIndex(line => line.StartsWith("# ", StringComparison.Ordinal));
        if (title >= 0)
            lines.RemoveAt(title);
        var text = string.Join('\n', lines).Trim('\n');
        // The fragment's relative links are written from its own folder; rebase them.
        var from = Path.GetDirectoryName(path)!;
        var to = Path.GetDirectoryName(document.Path)!;
        return System.Text.RegularExpressions.Regex.Replace(text, @"\]\((?<target>[^)\s#:]+)(?<rest>[^)]*)\)", match =>
        {
            var target = match.Groups["target"].Value;
            var rebased = Path.GetRelativePath(to, Path.GetFullPath(Path.Combine(from, target))).Replace('\\', '/');
            if (!rebased.StartsWith('.'))
                rebased = "./" + rebased;
            return $"]({rebased}{match.Groups["rest"].Value})";
        });
    }

    // --- component-index -------------------------------------------------------------------

    /// <summary>
    /// Pages of the component folder that are guides, linked on their own rather than listed as
    /// components (Theming documents <c>ConfigProvider</c>, but is not a component page). Same
    /// list as <c>ionicGuides</c> in docs/.vitepress/config.mts.
    /// </summary>
    private static readonly HashSet<string> GuidePages = new(StringComparer.Ordinal) { "index.md", "theming.md", "overlays.md" };

    private string ComponentIndex(DocumentFile document, IReadOnlyList<string> arguments)
    {
        var prefix = arguments.Count > 0 ? arguments[0] : "./";
        var asList = arguments.Count > 1 && arguments[1] == "list";
        var root = DocsGenerator.ComponentDocsDir + "/";
        var pages = _documents
            .Where(d => d.RelativePath.StartsWith(root, StringComparison.Ordinal)
                        && !d.RelativePath[root.Length..].Contains('/')
                        && !GuidePages.Contains(Path.GetFileName(d.Path))
                        && d.Text.Contains("<!-- miko:api", StringComparison.Ordinal))
            .OrderBy(d => d.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        string Components(DocumentFile page) => string.Join(", ", _pages
            .Where(p => p.Value == page && _catalog.Components.ContainsKey(p.Key))
            .Select(p => p.Key)
            .Order(StringComparer.Ordinal)
            .Select(Markdown.Code));

        string Link(DocumentFile page) => $"[{page.Title}]({prefix}{Path.GetFileName(page.Path)})";
        // A summary's own links are relative to its page, not to the index; keep only their text.
        string Summary(DocumentFile page) =>
            System.Text.RegularExpressions.Regex.Replace(DocsGenerator.Summary(page.Text), @"\[([^\]]+)\]\([^)]*\)", "$1");

        if (asList)
        {
            return string.Join("\n", pages.Select(page => $"- {Link(page)}: {Components(page)} — {Summary(page)}"));
        }
        return Markdown.Table(["Page", "Components", "Summary"],
            pages.Select(page => (IReadOnlyList<string>)[Link(page), Components(page), Summary(page)]));
    }
}
