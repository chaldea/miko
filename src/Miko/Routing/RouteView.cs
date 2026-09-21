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
        var componentType = _router.Resolve(path);
        if (componentType == null)
            throw new InvalidOperationException($"No route found for path: {path}");

        // Push our service provider as the ambient render scope so any descendant component
        // built via RenderTreeBuilder.OpenComponent<T> (which uses new T()) can still resolve
        // its [Inject] properties — see ComponentServiceScope. ComponentBase.Build itself also
        // re-pushes this provider for its own children, but we open the scope here so that the
        // page's first build sees a non-empty stack.
        using var _ = Components.ComponentServiceScope.Push(_serviceProvider);

        var component = CreateComponent(componentType);
        var content = component.Build();

        if (_defaultLayout != null)
        {
            var layout = (LayoutComponentBase)CreateComponent(_defaultLayout);
            // The page content (possibly a transparent multi-root FragmentElement) is placed into
            // the layout's body. The fragment stays in the DOM as the page's stable root, but the
            // layout engine skips it (display:contents), so no wrapper element disturbs the layout.
            layout.BodyElement = content;
            layout.Body = builder => { builder.AttachElement(content); };
            var rendered = layout.Build();
            // 装配完就放开对页面内容的引用（BodyElement 与捕获了它的 Body 闭包）：内容此后由
            // 已建成的元素树自己持有，页面重渲染走 StateHasChanged 就地替换，不会再回头
            // 问布局要内容。留着则等于攥住第 0 代，其 SupersededBy 前向链会把之后每一代
            // 连同各自的 ComputedStyle 全部钉在内存里（ISSUE-142 复审：反复点
            // IonSegmentButton 每次泄漏约 730 KB，强制压缩回收也放不掉）。
            layout.ReleaseBody();
            return rendered;
        }

        // No layout: the page is the engine's root. A multi-root page is a transparent
        // FragmentElement; the layout engine treats a root-level fragment as the (permitted)
        // auto-created wrapper, so it can be returned as-is — keeping it as the component's
        // stable root so StateHasChanged can re-render it in place.
        return content;
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
}
