using System.Reflection;
using System.Text.RegularExpressions;
using Miko.Components;
using Miko.Ionic;
using Miko.Ionic.Components;

namespace Miko.DocGen;

public enum ParameterKind
{
    Property,
    Event,
    Slot,
}

public sealed record ComponentParameter(
    PropertyInfo Property,
    ParameterKind Kind,
    string TypeName,
    string? DefaultValue);

/// <summary>
/// The public Razor components of <c>Miko.Ionic</c>, discovered by reflection over the compiled
/// assembly — the exact API an app compiles against, including parameters declared in
/// <c>@code</c> blocks that no other source generator could see.
/// </summary>
public sealed class ComponentCatalog
{
    /// <summary>
    /// Public components an app never authors directly: overlay plumbing that <c>IonApp</c> and
    /// the overlay components render for themselves, and the panels <c>IonSelect</c> opens.
    /// They are left out of the coverage check; everything else must be documented.
    /// </summary>
    public static readonly IReadOnlySet<string> InfrastructureComponents = new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(IonOverlayHost),
        nameof(IonPortal),
        nameof(IonSelectModal),
        nameof(IonSelectPopover),
    };

    private readonly string _componentSourceRoot;
    private readonly Dictionary<Type, bool> _usesChildContent = new();

    public ComponentCatalog(string repoRoot)
    {
        _componentSourceRoot = Path.Combine(repoRoot, "src", "Miko.Ionic", "Components");
        Assembly = typeof(IonicComponentBase).Assembly;
        Components = Assembly.GetExportedTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                           && type.Namespace == typeof(IonicComponentBase).Namespace
                           && typeof(ComponentBase).IsAssignableFrom(type))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToDictionary(type => type.Name, StringComparer.Ordinal);
        Documentation = new XmlDocumentation(Assembly, typeof(ComponentBase).Assembly);
    }

    public Assembly Assembly { get; }

    public IReadOnlyDictionary<string, Type> Components { get; }

    public XmlDocumentation Documentation { get; }

    /// <summary>Looks up any public type of the library by simple name.</summary>
    public Type? FindType(string name) =>
        Assembly.GetExportedTypes().FirstOrDefault(type => type.Name == name)
        ?? typeof(ComponentBase).Assembly.GetExportedTypes().FirstOrDefault(type => type.Name == name);

    /// <summary>
    /// The component's own parameters, alphabetically. Parameters every component shares
    /// (<c>Class</c>, <c>Style</c>) are documented once instead of on every table; the inherited
    /// <c>ChildContent</c> is listed only where the component actually renders it.
    /// </summary>
    public IReadOnlyList<ComponentParameter> Parameters(Type component)
    {
        var defaults = DefaultValues(component);
        var result = new List<ComponentParameter>();
        foreach (var property in component.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<ParameterAttribute>() is null)
                continue;
            var declaring = property.DeclaringType!;
            if (declaring == typeof(IonicComponentBase))
                continue;
            if (declaring == typeof(ComponentBase))
            {
                if (property.Name != nameof(ComponentBase.ChildContent) || !UsesChildContent(component))
                    continue;
            }

            var kind = KindOf(property);
            defaults.TryGetValue(property.Name, out var value);
            result.Add(new ComponentParameter(property, kind, Formatting.TypeName(property), kind == ParameterKind.Property ? value : null));
        }

        return result
            .OrderBy(p => p.Property.Name == nameof(ComponentBase.ChildContent) ? 0 : 1)
            .ThenBy(p => p.Property.Name, StringComparer.Ordinal)
            .ToList();
    }

    public static ParameterKind KindOf(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (type.Namespace == typeof(RenderFragment).Namespace && type.Name.StartsWith(nameof(RenderFragment), StringComparison.Ordinal))
            return ParameterKind.Slot;
        if (type == typeof(EventCallback) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>)))
            return ParameterKind.Event;
        if (typeof(Delegate).IsAssignableFrom(type) && property.Name.StartsWith("On", StringComparison.Ordinal))
            return ParameterKind.Event;
        return ParameterKind.Property;
    }

    /// <summary>
    /// Default values read from two fresh instances. A value that differs between them (a
    /// generated id, say) has no fixed default and is reported as such.
    /// </summary>
    private static Dictionary<string, string?> DefaultValues(Type component)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        object? first, second;
        try
        {
            first = Activator.CreateInstance(component);
            second = Activator.CreateInstance(component);
        }
        catch
        {
            return values;
        }

        foreach (var property in component.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<ParameterAttribute>() is null || property.GetMethod is null)
                continue;
            try
            {
                var a = Formatting.Value(property.GetValue(first));
                var b = Formatting.Value(property.GetValue(second));
                values[property.Name] = a == b ? a : null;
            }
            catch
            {
                values[property.Name] = null;
            }
        }
        return values;
    }

    // ChildContent is declared once on ComponentBase, so reflection cannot tell whether a given
    // component renders it. The component's own source can: look for the identifier outside
    // comments.
    private static readonly Regex RazorComment = new(@"@\*.*?\*@", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex LineComment = new(@"//.*$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex ChildContentUse = new(@"\bChildContent\b", RegexOptions.Compiled);

    private bool UsesChildContent(Type component)
    {
        if (_usesChildContent.TryGetValue(component, out var uses))
            return uses;

        uses = true;
        var source = Directory.EnumerateFiles(_componentSourceRoot, component.Name + ".razor", SearchOption.AllDirectories).FirstOrDefault();
        if (source is not null)
        {
            var text = LineComment.Replace(RazorComment.Replace(File.ReadAllText(source), ""), "");
            uses = ChildContentUse.IsMatch(text);
        }
        _usesChildContent[component] = uses;
        return uses;
    }

    /// <summary>The <see cref="IonicTheme"/> properties holding a component token, by name.</summary>
    public static IReadOnlyDictionary<string, PropertyInfo> ThemeTokens { get; } =
        typeof(IonicTheme).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => typeof(IonicToken).IsAssignableFrom(property.PropertyType))
            .ToDictionary(property => property.Name, StringComparer.Ordinal);
}
