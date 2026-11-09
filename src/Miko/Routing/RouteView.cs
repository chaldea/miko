using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Core;

namespace Miko.Routing;

public class RouteView
{
    private readonly Router _router;
    private readonly NavigationManager _navigationManager;

    [DynamicallyAccessedMembers(Components.ComponentTypeMembers.Activation)]
    private readonly Type? _defaultLayout;

    private readonly IServiceProvider _serviceProvider;

    public RouteView(
        Router router,
        NavigationManager navigationManager,
        [DynamicallyAccessedMembers(Components.ComponentTypeMembers.Activation)] Type? defaultLayout,
        IServiceProvider serviceProvider)
    {
        _router = router;
        _navigationManager = navigationManager;
        _defaultLayout = defaultLayout;
        _serviceProvider = serviceProvider;
    }

    public Element Render(string path)
    {
        var match = _router.Match(path)
            ?? throw new InvalidOperationException($"No route found for path: {path}");

        // Push our service provider as the ambient render scope so any descendant component
        // built via RenderTreeBuilder.OpenComponent<T> (which uses new T()) can still resolve
        // its [Inject] properties — see ComponentServiceScope. ComponentBase.Build itself also
        // re-pushes this provider for its own children, but we open the scope here so that the
        // page's first build sees a non-empty stack.
        using var _ = Components.ComponentServiceScope.Push(_serviceProvider);

        var component = CreateComponent(match.ComponentType);
        // Before the first Build, so OnInitialized / OnParametersSet already see the values.
        SetRouteParameters(component, match.ComponentType, match);

        if (_defaultLayout != null)
        {
            var layout = (LayoutComponentBase)CreateComponent(_defaultLayout);
            // The page is built where the layout places its body — inside the layout's render,
            // not before it — so it receives the values the layout cascades, as in Blazor. Built
            // up front, a page never saw the IonOverlayContext of the layout's IonApp, and every
            // overlay it declared rendered inline instead of in the app's overlay layer
            // (ISSUE-153 problem 1).
            //
            // The page content (possibly a transparent multi-root FragmentElement) stays in the
            // DOM as the page's stable root, but the layout engine skips it (display:contents),
            // so no wrapper element disturbs the layout.
            layout.SetBody(component);
            var rendered = layout.Build();
            // 装配完就放开对页面内容根元素的引用：内容此后由已建成的元素树自己持有，页面重渲染走
            // StateHasChanged 就地替换。留着则等于攥住第 0 代，其 SupersededBy 前向链会把之后每一代
            // 连同各自的 ComputedStyle 全部钉在内存里（ISSUE-142 复审：反复点
            // IonSegmentButton 每次泄漏约 730 KB，强制压缩回收也放不掉）。布局仍记着页面组件，
            // 以便自己重渲染时交出页面的当前根（ISSUE-157）。
            layout.ReleaseBody();
            return rendered;
        }

        // No layout: the page is the engine's root. A multi-root page is a transparent
        // FragmentElement; the layout engine treats a root-level fragment as the (permitted)
        // auto-created wrapper, so it can be returned as-is — keeping it as the component's
        // stable root so StateHasChanged can re-render it in place.
        return component.Build();
    }

    private ComponentBase CreateComponent(
        [DynamicallyAccessedMembers(Components.ComponentTypeMembers.Activation)] Type componentType)
    {
        var component = (ComponentBase)ActivatorUtilities.CreateInstance(_serviceProvider, componentType);
        InjectServices(component, componentType, _serviceProvider);
        component.NavigationManager = _navigationManager;
        return component;
    }

    /// <summary>
    /// Populates the page component's <see cref="InjectAttribute"/> properties. The property set
    /// is resolved once per component type by <see cref="ComponentParameterCache"/> rather than by
    /// reflecting on every navigation (ISSUE-136). Unresolved services are tolerated, as before.
    /// </summary>
    private static void InjectServices(
        ComponentBase component,
        [DynamicallyAccessedMembers(Components.ComponentTypeMembers.Parameters)] Type componentType,
        IServiceProvider serviceProvider)
    {
        var injected = ComponentParameterCache.GetInjectedProperties(componentType);
        for (int i = 0; i < injected.Length; i++)
        {
            var service = serviceProvider.GetService(injected[i].Property.PropertyType);
            if (service != null)
                injected[i].SetValue(component, service);
        }
    }

    /// <summary>
    /// Assigns the route values to the page's <see cref="ParameterAttribute"/> properties
    /// (ISSUE-164), matched by name case-insensitively as in Blazor. The property set comes from
    /// <see cref="ComponentParameterCache"/>, built once per type.
    ///
    /// <para>As in Blazor, nothing is converted: the constraint decides the value's type
    /// (<c>{id:int}</c> → <see cref="int"/>, unconstrained and catch-all → <see cref="string"/>) and
    /// it must be assignable to the property (<see cref="int"/> also to <c>int?</c>). A mismatch or
    /// a route parameter without a property is a wiring error and throws with the fix in the
    /// message, instead of leaving the property silently at its default. Absent optional
    /// parameters are not assigned, so the property keeps its initializer.</para>
    /// </summary>
    private static void SetRouteParameters(
        ComponentBase component,
        [DynamicallyAccessedMembers(Components.ComponentTypeMembers.Parameters)] Type componentType,
        RouteMatch match)
    {
        if (match.Parameters.Count == 0)
            return;

        var properties = ComponentParameterCache.GetParameters(componentType);
        foreach (var (name, value) in match.Parameters)
        {
            var setter = FindParameter(properties, name)
                ?? throw new InvalidOperationException(
                    $"Route '{match.Route.Template}' has a parameter '{name}', but {componentType.FullName} " +
                    $"has no [Parameter] property named '{name}'.");

            var propertyType = setter.Property.PropertyType;
            if (value != null && !propertyType.IsInstanceOfType(value))
            {
                throw new InvalidOperationException(
                    $"Route '{match.Route.Template}' supplies parameter '{name}' as {value.GetType().Name}, " +
                    $"which cannot be assigned to {componentType.FullName}.{setter.Property.Name} ({propertyType.Name}). " +
                    "Give the parameter a constraint that produces the property's type (for example {id:int}) " +
                    "or change the property's type.");
            }

            setter.SetValue(component, value);
        }
    }

    private static ComponentParameterCache.ParameterSetter? FindParameter(
        ComponentParameterCache.ParameterSetter[] properties, string name)
    {
        for (int i = 0; i < properties.Length; i++)
        {
            if (string.Equals(properties[i].Property.Name, name, StringComparison.OrdinalIgnoreCase))
                return properties[i];
        }
        return null;
    }
}
