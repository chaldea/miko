using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Miko.Components;

namespace Miko.Routing;

public class Router
{
    private readonly List<RouteData> _routes = new();

    public IReadOnlyList<RouteData> Routes => _routes;

    public void MapRoute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] TComponent>(string template) where TComponent : class
    {
        _routes.Add(new RouteData
        {
            Template = template,
            ComponentType = typeof(TComponent)
        });
    }

    public void MapRoute(string template, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type componentType)
    {
        _routes.Add(new RouteData
        {
            Template = template,
            ComponentType = componentType
        });
    }

    [RequiresUnreferencedCode("Assembly scanning uses reflection and is not compatible with trimming. Use MapRoute<T>() for AOT-safe route registration.")]
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

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    public Type? Resolve(string path)
    {
        var route = _routes.FirstOrDefault(r =>
            string.Equals(r.Template, path, StringComparison.OrdinalIgnoreCase));
        return route?.ComponentType;
    }
}
