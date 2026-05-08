using Miko.Components;
using Miko.Core;

namespace Miko.Routing;

public class RouteView
{
    private readonly Router _router;
    private readonly NavigationManager _navigationManager;
    private readonly Type? _defaultLayout;

    public RouteView(Router router, NavigationManager navigationManager, Type? defaultLayout = null)
    {
        _router = router;
        _navigationManager = navigationManager;
        _defaultLayout = defaultLayout;
    }

    public Element Render(string path)
    {
        var componentType = _router.Resolve(path);
        if (componentType == null)
            throw new InvalidOperationException($"No route found for path: {path}");

        var component = (ComponentBase)Activator.CreateInstance(componentType)!;
        component.NavigationManager = _navigationManager;

        var content = component.Build();

        if (_defaultLayout != null)
        {
            var layout = (LayoutComponentBase)Activator.CreateInstance(_defaultLayout)!;
            layout.NavigationManager = _navigationManager;
            layout.Body = content;
            var s = layout.Build();
            return s;
        }

        return content;
    }
}
