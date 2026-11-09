using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using Miko.Components;

namespace Miko.Routing;

/// <summary>
/// Maps route templates (Blazor syntax, see <see cref="RouteTemplate"/>) to page components.
///
/// <para>Templates are parsed when registered, so an invalid one throws there. A path is matched
/// against the routes in Blazor's precedence order — <c>/user/new</c> beats <c>/user/{id:int}</c>,
/// which beats <c>/user/{name}</c>, which beats <c>/user/{*rest}</c> — regardless of the order
/// they were registered in; routes of equal precedence keep registration order.</para>
/// </summary>
public class Router
{
    private readonly List<RouteData> _routes = new();
    private readonly List<RouteTemplate> _templates = new();

    // Route indices in match order; rebuilt on the first match after a registration.
    private int[]? _matchOrder;

    /// <summary>The registered routes, in registration order.</summary>
    public IReadOnlyList<RouteData> Routes => _routes;

    public void MapRoute<[DynamicallyAccessedMembers(Components.ComponentTypeMembers.Activation)] TComponent>(string template) where TComponent : class
        => Add(template, typeof(TComponent));

    public void MapRoute(string template, [DynamicallyAccessedMembers(Components.ComponentTypeMembers.Activation)] Type componentType)
        => Add(template, componentType);

    [RequiresUnreferencedCode("Assembly scanning uses reflection and is not compatible with trimming. Use MapRoute<T>() for AOT-safe route registration.")]
    public void ScanAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var attr in type.GetCustomAttributes<RouteAttribute>())
                    Add(attr.Template, type);
            }
        }
    }

    /// <summary>
    /// Finds the route for <paramref name="path"/> and the values of its parameters, or null when
    /// no route matches. The query string and fragment are ignored.
    /// </summary>
    public RouteMatch? Match(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var order = _matchOrder ??= BuildMatchOrder();
        var segments = RouteTemplate.SplitPath(path);
        foreach (var index in order)
        {
            var values = _templates[index].Match(segments);
            if (values != null)
                return new RouteMatch(_routes[index], values);
        }
        return null;
    }

    [return: DynamicallyAccessedMembers(Components.ComponentTypeMembers.Activation)]
    public Type? Resolve(string path) => Match(path)?.ComponentType;

    /// <summary>
    /// The page's identity, used as its scroll-snapshot key (ISSUE-144): the component type, so a
    /// page registered at <c>/</c> and <c>/home</c> keeps one snapshot. A route with parameters
    /// adds their values — <c>/user/1</c> and <c>/user/2</c> are the same component showing
    /// different content, and must not restore each other's scroll position (ISSUE-164). Null
    /// when no route matches (the engine then keys by path).
    /// </summary>
    internal string? ResolvePageKey(string path)
    {
        var match = Match(path);
        if (match == null)
            return null;

        var key = match.ComponentType.FullName ?? match.ComponentType.Name;
        if (match.Parameters.Count == 0)
            return key;

        var builder = new StringBuilder(key);
        foreach (var (name, value) in match.Parameters.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append('|').Append(name.ToLowerInvariant()).Append('=')
                .Append(Convert.ToString(value, CultureInfo.InvariantCulture));
        }
        return builder.ToString();
    }

    private void Add(string template, [DynamicallyAccessedMembers(Components.ComponentTypeMembers.Activation)] Type componentType)
    {
        var parsed = RouteTemplate.Parse(template);
        _routes.Add(new RouteData { Template = template, ComponentType = componentType });
        _templates.Add(parsed);
        _matchOrder = null;
    }

    private int[] BuildMatchOrder()
    {
        var order = new int[_routes.Count];
        for (int i = 0; i < order.Length; i++)
            order[i] = i;

        Array.Sort(order, (a, b) =>
        {
            var result = RouteTemplate.ComparePrecedence(_templates[a], _templates[b]);
            return result != 0 ? result : a.CompareTo(b);
        });
        return order;
    }
}
