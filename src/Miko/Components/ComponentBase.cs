using Miko.Core;
using Miko.Layout;
using Miko.Routing;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Miko.Components;

public abstract class ComponentBase : IComponent
{
    public NavigationManager? NavigationManager { get; internal set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private Element? _rootElement;
    private bool _initialized;

    /// <summary>
    /// The service provider that resolved this component's <see cref="InjectAttribute"/>
    /// properties. Captured from the ambient <see cref="ComponentServiceScope"/> on the
    /// first <see cref="Build"/> so it can be re-pushed during nested renders triggered by
    /// <see cref="StateHasChanged"/> (which runs outside the original ancestor's <c>Build</c>).
    /// </summary>
    private IServiceProvider? _services;

    protected virtual void BuildRenderTree(RenderTreeBuilder builder) { }

    /// <summary>
    /// Method invoked when the component is ready to start, having received its initial parameters.
    /// </summary>
    protected virtual void OnInitialized() { }

    /// <summary>
    /// Async method invoked when the component is ready to start.
    /// If this returns an incomplete Task, the component will re-render when the task completes.
    /// </summary>
    protected virtual Task OnInitializedAsync() => Task.CompletedTask;

    /// <summary>
    /// Method invoked when the component has received parameters from its parent.
    /// </summary>
    protected virtual void OnParametersSet() { }

    /// <summary>
    /// Async method invoked when the component has received parameters from its parent.
    /// If this returns an incomplete Task, the component will re-render when the task completes.
    /// </summary>
    protected virtual Task OnParametersSetAsync() => Task.CompletedTask;

    /// <summary>
    /// Invoked when the component instance is discarded — i.e. the element it produced is
    /// replaced or removed during a re-render. Override to release resources or unsubscribe
    /// from events (nested components are recreated each render, so subscriptions made in
    /// <see cref="OnInitialized"/>/<see cref="OnParametersSet"/> must be torn down here).
    /// </summary>
    protected virtual void OnDispose() { }

    internal void DisposeInternal() => OnDispose();

    public virtual Element Build()
    {
        // Resolve [Inject] services from the ambient ComponentServiceScope (pushed by RouteView
        // for the top-level page, then re-pushed by every ancestor while its BuildRenderTree
        // runs — see the using-block below). This makes [Inject] available on any component
        // instantiated through RenderTreeBuilder.OpenComponent<T>, not only routed pages.
        // Resolution happens once per component instance: subsequent rebuilds keep the same
        // injected values (matches Blazor's behaviour).
        if (!_initialized)
        {
            _services ??= ComponentServiceScope.Current;
            if (_services != null)
                InjectServices(this, _services);
        }

        // Resolve cascading parameters first, so they're available inside the lifecycle methods
        // below (matches Blazor, where cascading values arrive with the initial parameter set).
        // The ambient values are in scope because the providing CascadingValue<T>.Build is still
        // on the call stack (see CascadingValueSource).
        SetCascadingParameters();

        if (!_initialized)
        {
            OnInitialized();
            var initTask = OnInitializedAsync();
            TrackPendingTask(initTask);
            _initialized = true;
        }

        OnParametersSet();
        var paramsTask = OnParametersSetAsync();
        TrackPendingTask(paramsTask);

        var builder = new RenderTreeBuilder();
        // Make this component's service provider ambient to its descendants. Nested components
        // produced by RenderTreeBuilder.OpenComponent<T>() construct via new T() and resolve
        // [Inject] from ComponentServiceScope.Current in their own Build() — which runs on the
        // same call stack while this using-block is open.
        using (ComponentServiceScope.Push(_services))
        {
            BuildRenderTree(builder);
        }
        _rootElement = builder.Build();
        return _rootElement;
    }

    /// <summary>
    /// Populates a component's <see cref="InjectAttribute"/> properties (public or non-public)
    /// from the supplied service provider. Properties whose service is not registered are left
    /// untouched (mirrors Blazor, which throws — but here unresolved injections are tolerated to
    /// preserve the existing <see cref="Routing.RouteView"/> behaviour). Read-only properties are
    /// skipped, matching Blazor.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Inject properties are declared on the component type and preserved with it.")]
    private static void InjectServices(ComponentBase component, IServiceProvider serviceProvider)
    {
        var properties = component.GetType().GetProperties(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (!prop.CanWrite) continue;
            if (prop.GetCustomAttribute<InjectAttribute>() == null) continue;

            var service = serviceProvider.GetService(prop.PropertyType);
            if (service != null)
                prop.SetValue(component, service);
        }
    }

    /// <summary>
    /// Tracks an async lifecycle task. If the task is incomplete, schedules StateHasChanged
    /// to be called (on the render thread via SynchronizationContext) when it completes.
    /// </summary>
    private void TrackPendingTask(Task task)
    {
        if (task.IsCompleted)
        {
            // Fast path: task already completed (e.g. Task.CompletedTask or cached result).
            if (task.IsFaulted)
                HandleTaskException(task.Exception!);
            return;
        }

        // Slow path: task is running. Capture the current SynchronizationContext (which the
        // caller — MikoInteractionController.Initialize/Rebuild/DispatchWithSyncContext — has
        // installed before invoking Build()/OnInitializedAsync()). The continuation will be
        // posted back to that context (i.e. MikoDispatcher) so it runs on the render thread.
        var capturedCtx = SynchronizationContext.Current;

        _ = task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                HandleTaskException(t.Exception!);
                return;
            }

            if (capturedCtx != null)
            {
                // Post StateHasChanged back to the render thread via dispatcher.
                capturedCtx.Post(_ => StateHasChanged(), null);
            }
            else
            {
                // No sync context available (e.g. unit tests): call directly.
                StateHasChanged();
            }
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private static void HandleTaskException(AggregateException ex)
    {
        var flatten = ex.Flatten();
        Console.Error.WriteLine($"[ComponentBase] Unhandled exception in async lifecycle: {flatten.InnerException ?? flatten}");
    }

    /// <summary>
    /// Triggers a re-render. Invoked automatically after an event handler raised
    /// through an <see cref="EventCallback{T}"/> completes, so handlers usually do
    /// not need to call <see cref="StateHasChanged"/> themselves.
    /// </summary>
    internal void NotifyStateChanged() => StateHasChanged();

    protected void StateHasChanged()
    {
        if (_rootElement == null) return;

        if (_rootElement.Parent == null)
        {
            var newElement = BuildNew();
            // The old children are discarded by ReplaceElementContent — tear down the nested
            // components that produced them first.
            foreach (var oldChild in _rootElement.Children)
                DisposeSubtree(oldChild);
            ReplaceElementContent(_rootElement, newElement);
            return;
        }

        var parent = _rootElement.Parent;
        var index = parent.Children.IndexOf(_rootElement);
        if (index < 0) return;

        var oldElement = _rootElement;
        var rebuilt = Build();
        TransferLayoutBox(oldElement, rebuilt);
        parent.Children[index] = rebuilt;
        rebuilt.SetParent(parent);
        _rootElement = rebuilt;
        // The old subtree (and the nested component instances that produced it) is now
        // unreferenced — dispose those components so their event subscriptions are released.
        // Skip oldElement itself: this component instance persists and re-renders.
        foreach (var oldChild in oldElement.Children)
            DisposeSubtree(oldChild);
    }

    // Walks a discarded element subtree and invokes each element's component dispose callback.
    private static void DisposeSubtree(Element element)
    {
        element.DisposeCallback?.Invoke();
        foreach (var child in element.Children)
            DisposeSubtree(child);
    }

    private Element BuildNew()
    {
        SetCascadingParameters();
        var builder = new RenderTreeBuilder();
        // Re-push our captured provider so nested components rebuilt during StateHasChanged
        // still receive [Inject] services. StateHasChanged runs outside any ancestor's Build,
        // so without this push the ambient scope would be empty.
        using (ComponentServiceScope.Push(_services))
        {
            BuildRenderTree(builder);
        }
        return builder.Build();
    }

    /// <summary>
    /// Populates this component's <see cref="CascadingParameterAttribute"/> properties from the
    /// ambient <see cref="CascadingValueSource"/>. Mirrors how <c>[Inject]</c> is resolved by
    /// reflection in <see cref="RouteView"/>. Properties with no matching provider are left
    /// untouched (keep their default).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Cascading parameter properties are declared on the component type and preserved with it.")]
    private void SetCascadingParameters()
    {
        var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (!prop.CanWrite) continue;
            var attr = prop.GetCustomAttribute<CascadingParameterAttribute>();
            if (attr == null) continue;

            if (CascadingValueSource.TryResolve(prop.PropertyType, attr.Name, out var value))
                prop.SetValue(this, value);
        }
    }

    private static void ReplaceElementContent(Element target, Element source)
    {
        var oldChildren = new List<Element>(target.Children);
        target.Children.Clear();
        for (int i = 0; i < source.Children.Count; i++)
        {
            var newChild = source.Children[i];
            target.Children.Add(newChild);
            newChild.SetParent(target);

            if (i < oldChildren.Count)
                TransferLayoutBox(oldChildren[i], newChild);
        }
        target.Style = source.Style;
        target.TextContent = source.TextContent;
        target.Id = source.Id;
        target.Class = source.Class;
        target.IsDirty = true;
    }

    private static void TransferLayoutBox(Element oldElement, Element newElement)
    {
        if (oldElement.LayoutBox != null)
        {
            newElement.LayoutBox = new LayoutBox
            {
                Element = newElement,
                ComputedStyle = oldElement.LayoutBox.ComputedStyle,
                Children = oldElement.LayoutBox.Children
            };
        }

        int count = Math.Min(oldElement.Children.Count, newElement.Children.Count);
        for (int i = 0; i < count; i++)
            TransferLayoutBox(oldElement.Children[i], newElement.Children[i]);
    }
}
