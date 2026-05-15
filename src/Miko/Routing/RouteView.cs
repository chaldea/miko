using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Core;

namespace Miko.Routing;

public class RouteView
{
    private readonly Router _router;
    private readonly NavigationManager _navigationManager;
    private readonly Type? _defaultLayout;
    private readonly IServiceProvider _serviceProvider;

    public RouteView(Router router, NavigationManager navigationManager, Type? defaultLayout, IServiceProvider serviceProvider)
    {
        _router = router;
        _navigationManager = navigationManager;
        _defaultLayout = defaultLayout;
        _serviceProvider = serviceProvider;
    }

    public Element Render(string path)
    {
        var componentType = _router.Resolve(path);
        if (componentType == null)
            throw new InvalidOperationException($"No route found for path: {path}");

        var component = CreateComponent(componentType);
        var content = component.Build();

        if (_defaultLayout != null)
        {
            var layout = (LayoutComponentBase)CreateComponent(_defaultLayout);
            layout.BodyElement = content;
            layout.Body = builder => { builder.AttachElement(content); };
            return layout.Build();
        }

        return content;
    }

    private ComponentBase CreateComponent(Type componentType)
    {
        var component = (ComponentBase)ActivatorUtilities.CreateInstance(_serviceProvider, componentType);
        InjectServices(component);
        component.NavigationManager = _navigationManager;
        return component;
    }

    private void InjectServices(ComponentBase component)
    {
        var properties = component.GetType().GetProperties(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (prop.GetCustomAttributes(typeof(InjectAttribute), true).Length > 0 && prop.CanWrite)
            {
                var service = _serviceProvider.GetService(prop.PropertyType);
                if (service != null)
                    prop.SetValue(component, service);
            }
        }
    }
}
