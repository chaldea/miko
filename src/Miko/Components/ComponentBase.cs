using Miko.Core;
using Miko.Layout;
using Miko.Routing;

namespace Miko.Components;

public abstract class ComponentBase : IComponent
{
    public NavigationManager? NavigationManager { get; internal set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private Element? _rootElement;
    private bool _initialized;
    private readonly List<Task> _pendingTasks = new();

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
        BuildRenderTree(builder);
        _rootElement = builder.Build();
        return _rootElement;
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

        // Slow path: task is running. Schedule a continuation to re-render when it completes.
        // The continuation runs on the render thread (via the installed SynchronizationContext).
        _pendingTasks.Add(task);
        _ = task.ContinueWith(t =>
        {
            _pendingTasks.Remove(t);
            if (t.IsFaulted)
                HandleTaskException(t.Exception!);
            else
                StateHasChanged();
        }, TaskScheduler.Current);
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
        var builder = new RenderTreeBuilder();
        BuildRenderTree(builder);
        return builder.Build();
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
