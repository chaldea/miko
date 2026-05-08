using System.Reflection;
using Miko.Components;

namespace Miko.Routing;

public class Router
{
    private readonly List<RouteData> _routes = new();

    public IReadOnlyList<RouteData> Routes => _routes;

    public void ScanAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                var routeAttributes = type.GetCustomAttributes<RouteAttribute>();
                foreach (var attr in routeAttributes)
                {
                    _routes.Add(new RouteData
                    {
                        Template = attr.Template,
                        ComponentType = type
                    });
                }
            }
        }
    }

    public Type? Resolve(string path)
    {
        var route = _routes.FirstOrDefault(r =>
            string.Equals(r.Template, path, StringComparison.OrdinalIgnoreCase));
        return route?.ComponentType;
    }
}
