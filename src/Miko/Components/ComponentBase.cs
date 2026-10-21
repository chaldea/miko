using System.Diagnostics.CodeAnalysis;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Diagnostics;
using Miko.Events;
using Miko.Layout;
using Miko.Routing;

namespace Miko.Components;

public abstract class ComponentBase : IComponent
{
    public NavigationManager? NavigationManager { get; internal set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private Element? _rootElement;
    private bool _initialized;

    /// <summary>
    /// Set once the component has been disposed: the element it produced was discarded and no render
    /// claimed the instance again. A disposed component never renders again — see
    /// <see cref="StateHasChanged"/>.
    /// </summary>
    private bool _discarded;

    /// <summary>
    /// The nested components this component's last render produced, by identity. The next render
    /// reuses those whose identity is unchanged instead of recreating them (ISSUE-154 BUG-01).
    /// </summary>
    private ComponentRegistry? _children;

    /// <summary>The registry before last, emptied, kept so a re-render does not allocate a new one.</summary>
    private ComponentRegistry? _spareChildren;

    /// <summary>
    /// The nested component whose produced element is also this component's root — set when this
    /// component's markup is a single component (<c>IonAlert</c> renders an <c>IonPortal</c>).
    /// </summary>
    private ComponentBase? _rootChild;

    /// <summary>The reverse link of <see cref="_rootChild"/>: the component whose root is this component's element.</summary>
    private ComponentBase? _rootOwner;

    /// <summary>
    /// The root element's class list as this component's own <see cref="Build"/> left it, before an
    /// ancestor's build post-pass added to it (<c>IonItem</c> stamping <c>button-small</c> on a child
    /// button). The difference is carried over when the component later re-renders on its own.
    /// </summary>
    private string? _builtRootClass;

    /// <summary>The root the last run of <c>base.Build()</c> produced. See <see cref="AdoptProducedElement"/>.</summary>
    private Element? _lastBuiltRoot;

    /// <summary>See <see cref="AdoptProducedElement"/>.</summary>
    private Action? _adoptedDisposeCallback;

    /// <summary>
    /// The service provider that resolved this component's <see cref="InjectAttribute"/>
    /// properties. Captured from the ambient <see cref="ComponentServiceScope"/> on the
    /// first <see cref="Build"/> so it can be re-pushed during nested renders triggered by
    /// <see cref="StateHasChanged"/> (which runs outside the original ancestor's <c>Build</c>).
    /// </summary>
    private IServiceProvider? _services;
    private CascadingValueSource.Snapshot? _cascadingSnapshot;

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
    /// Invoked once, when the component instance is finally discarded — the element it produced
    /// was removed by a re-render that did not keep the instance (its identity changed, or its
    /// markup is no longer rendered), or the whole tree was replaced. Override to release resources
    /// or unsubscribe from events.
    ///
    /// <para>A parent re-render keeps a nested component whose identity is unchanged — the same
    /// call site, position and <c>@key</c> — so <see cref="OnInitialized"/> runs once per instance
    /// and subscriptions made there live until this method (ISSUE-154). Work started in
    /// <see cref="OnParametersSet"/>, which runs on every parent render, must not register again
    /// each time.</para>
    /// </summary>
    protected virtual void OnDispose() { }

    internal void DisposeInternal()
    {
        // 幂等：同一个实例可能同时出现在多条拆解路径上（旧根的拆解链与子树遍历）。
        if (_discarded) return;
        // 此后它的 StateHasChanged 必须失效（见 StateHasChanged 中的说明，ISSUE-121）。
        _discarded = true;
        OnDispose();
    }

    /// <summary>Whether a parent render may reuse this instance (it has not been disposed).</summary>
    internal bool CanBeReused => !_discarded;

    /// <summary>
    /// Called when a parent render reuses this instance, before it assigns the parameters again.
    /// Element event handlers written on the component (<see cref="TryAddHostEventListener"/>) are
    /// supplied anew by every parent render, so the previous set is dropped here.
    /// </summary>
    internal void BeginParentRender() => _hostEventListeners = null;

    public virtual Element Build()
    {
        // 分段探针（ISSUE-136）：默认关闭，关闭时只是一次布尔判断。
        // 嵌套组件的 Build 在父构建内部发生，由探针自行去重，只计最外层。
        var buildScope = FrameTimingDiagnostics.EnterBuild();
        try
        {
            return BuildCore();
        }
        finally
        {
            FrameTimingDiagnostics.ExitBuild(buildScope);
        }
    }

    private Element BuildCore()
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
        using var cascadingScope = CascadingValueSource.RestoreIfEmpty(_cascadingSnapshot);
        SetCascadingParameters();
        _cascadingSnapshot = CascadingValueSource.Capture();

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

        // The builder reuses the nested components of the previous render whose identity is
        // unchanged (ISSUE-154).
        RenderVersion++;
        var builder = new RenderTreeBuilder(_children, TakeSpareChildren());
        // Make this component's service provider ambient to its descendants. Nested components
        // produced by RenderTreeBuilder.OpenComponent<T>() construct via new T() and resolve
        // [Inject] from ComponentServiceScope.Current in their own Build() — which runs on the
        // same call stack while this using-block is open.
        using (ComponentServiceScope.Push(_services))
        {
            BuildRenderTree(builder);
        }
        var root = builder.Build();
        CommitRender(builder);
        _rootElement = _lastBuiltRoot = root;
        _builtRootClass = root.Class;
        StampRoot(root);
        AttachHostEventListeners(root);
        return root;
    }

    /// <summary>Records what a render produced: its nested components and the root-sharing link.</summary>
    private void CommitRender(RenderTreeBuilder builder)
    {
        CommitChildren(builder);
        _rootChild = builder.RootComponent;
        if (_rootChild is not null) _rootChild._rootOwner = this;
    }

    private ComponentRegistry? TakeSpareChildren()
    {
        var spare = _spareChildren;
        _spareChildren = null;
        return spare;
    }

    /// <summary>
    /// Makes the render's nested components the ones the next render claims from. The previous
    /// registry only holds instances this render did not keep — they leave with the discarded
    /// subtree — so it is emptied and kept for the render after next.
    /// </summary>
    private void CommitChildren(RenderTreeBuilder builder)
    {
        var previous = _children;
        _children = builder.Components;
        if (previous is not null && !ReferenceEquals(previous, _children))
        {
            previous.Clear();
            _spareChildren = previous;
        }
    }

    /// <summary>
    /// Links <paramref name="root"/> to the components that share it — the nested components whose
    /// produced element it is, this component, and the outer components whose root it already is —
    /// so that discarding the element disposes them.
    ///
    /// <para>The callback disposes only components whose root is <b>still</b> this element. A
    /// component that a later render reused has moved on to a new element by the time the old one is
    /// discarded, so it is skipped and keeps running (ISSUE-154). Each stamp replaces the previous
    /// callback, so a component that returns the same retained element from every build does not
    /// grow a chain of callbacks.</para>
    /// </summary>
    private void StampRoot(Element root)
    {
        // Common case: no other component shares the root.
        if (!SharesRoot(_rootChild, root) && !SharesRoot(_rootOwner, root))
        {
            root.DisposeCallback = () =>
            {
                if (ReferenceEquals(_rootElement, root)) DisposeInternal();
            };
            return;
        }

        var chain = new List<ComponentBase>(2);
        for (var inner = _rootChild; inner is not null && ReferenceEquals(inner._rootElement, root); inner = inner._rootChild)
            chain.Add(inner);
        chain.Reverse();
        chain.Add(this);
        for (var owner = _rootOwner; owner is not null && ReferenceEquals(owner._rootElement, root); owner = owner._rootOwner)
            chain.Add(owner);

        var components = chain.ToArray();
        root.DisposeCallback = () =>
        {
            foreach (var component in components)
            {
                if (ReferenceEquals(component._rootElement, root))
                    component.DisposeInternal();
            }
        };
    }

    private static bool SharesRoot(ComponentBase? component, Element root)
        => component is not null && ReferenceEquals(component._rootElement, root);

    /// <summary>
    /// Called by <see cref="RenderTreeBuilder.CloseComponent"/> with the element <see cref="Build"/>
    /// returned. Normally that is the root <see cref="Build"/> just produced and stamped, and only the
    /// class list the component built is recorded. A <see cref="Build"/> override that returns an
    /// element without going through <c>base.Build()</c> (one that returns another component's
    /// element) is adopted here instead, so it is still disposed with that element.
    /// </summary>
    internal void AdoptProducedElement(Element element)
    {
        _builtRootClass = element.Class;
        if (ReferenceEquals(_lastBuiltRoot, element)) return;

        _rootElement = element;
        // Compose over whatever callback the element carries — unless it is still the one composed
        // last time (re-composing would grow a chain on a retained element).
        if (ReferenceEquals(element.DisposeCallback, _adoptedDisposeCallback)) return;
        var inner = element.DisposeCallback;
        _adoptedDisposeCallback = () =>
        {
            inner?.Invoke();
            if (ReferenceEquals(_rootElement, element)) DisposeInternal();
        };
        element.DisposeCallback = _adoptedDisposeCallback;
    }

    /// <summary>
    /// Element event handlers written on this component that match none of its parameters
    /// (<c>&lt;IonChip @onclick="…"&gt;</c>). Listening on the element the component renders mirrors
    /// a listener on an Ionic host element. Kept on the instance and re-attached on every
    /// <see cref="Build"/>, because a self re-render produces a new root (ISSUE-153 problem 5).
    /// </summary>
    private List<(string EventType, Delegate Handler)>? _hostEventListeners;

    internal bool TryAddHostEventListener(string attributeName, object? value)
    {
        if (!attributeName.StartsWith("on", StringComparison.Ordinal)) return false;
        var eventType = attributeName.Substring(2);

        Delegate? handler = value switch
        {
            EventCallback<MouseEventArgs> c => (MikoEventHandler<MouseEventArgs>)(a => _ = c.InvokeAsync(a)),
            EventCallback<PointerEventArgs> c => (MikoEventHandler<PointerEventArgs>)(a => _ = c.InvokeAsync(a)),
            EventCallback<FocusEventArgs> c => (MikoEventHandler<FocusEventArgs>)(a => _ = c.InvokeAsync(a)),
            EventCallback<ChangeEventArgs> c => (MikoEventHandler<ChangeEventArgs>)(a => _ = c.InvokeAsync(a)),
            EventCallback<ScrollEventArgs> c => (MikoEventHandler<ScrollEventArgs>)(a => _ = c.InvokeAsync(a)),
            EventCallback<KeyboardEventArgs> c => (MikoEventHandler<KeyboardEventArgs>)(a => _ = c.InvokeAsync(a)),
            EventCallback<InputEventArgs> c => (MikoEventHandler<InputEventArgs>)(a => _ = c.InvokeAsync(a)),
            _ => null,
        };
        if (handler is null) return false;

        (_hostEventListeners ??= new()).Add((eventType, handler));
        return true;
    }

    private void AttachHostEventListeners(Element root)
    {
        if (_hostEventListeners is null) return;
        foreach (var (eventType, handler) in _hostEventListeners)
        {
            // A component may return the same retained element from every build; add once.
            if (!root.HasEventListener(eventType, handler))
                root.AddEventListener(eventType, handler);
        }
    }

    /// <summary>
    /// Populates a component's <see cref="InjectAttribute"/> properties (public or non-public)
    /// from the supplied service provider. Properties whose service is not registered are left
    /// untouched (mirrors Blazor, which throws — but here unresolved injections are tolerated to
    /// preserve the existing <see cref="Routing.RouteView"/> behaviour). Read-only properties are
    /// skipped, matching Blazor.
    ///
    /// <para>The property set is resolved once per component type by
    /// <see cref="ComponentParameterCache"/> rather than by reflecting on every call.</para>
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "GetType() is statically unannotated, but a component can only ever be " +
                        "instantiated through RenderTreeBuilder.OpenComponent<T>/Router.MapRoute<T>, " +
                        "both of which declare ComponentTypeMembers.Activation — so the concrete " +
                        "type's parameter properties are preserved (ISSUE-140).")]
    private static void InjectServices(ComponentBase component, IServiceProvider serviceProvider)
    {
        var injected = ComponentParameterCache.GetInjectedProperties(component.GetType());
        for (int i = 0; i < injected.Length; i++)
        {
            var service = serviceProvider.GetService(injected[i].Property.PropertyType);
            if (service != null)
                injected[i].SetValue(component, service);
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
        if (_rootElement == null || _discarded) return;
        // While an event is being handled the render waits until the handler's synchronous part is
        // done, so a parent rendered meanwhile does not overwrite values its child has yet to report
        // (see RenderBatch, ISSUE-154).
        if (RenderBatch.TryDefer(this)) return;
        RenderNow();
    }

    /// <summary>
    /// Incremented on every render of this component — by its parent or by itself. A deferred render
    /// (<see cref="RenderBatch"/>) is dropped when the component has rendered since it was requested.
    /// </summary>
    internal int RenderVersion { get; private set; }

    /// <summary>Performs a <see cref="StateHasChanged"/> render immediately.</summary>
    internal void RenderNow()
    {
        if (_rootElement == null) return;

        // 已被丢弃的组件不得再渲染（ISSUE-121）。组件实例身份不变时会被父级重渲染复用
        // （ISSUE-154），只有真正出局（被拆解）的实例才会走到这里。
        //
        // 若放任它渲染：它会在自己那棵<b>已脱离 DOM</b>的旧树上重建，并把 SupersededBy 转发
        // 指针改写为指向那棵孤树。控制器随后经转发链拿到的就是看不见的元素，编辑落在其上——
        // 值仍经绑定流回页面（故内容看着正常），但光标停在原地不再跟随（现场问题 1）。
        if (_discarded) return;

        if (_rootElement.Parent == null)
        {
            RerenderInPlace();
            return;
        }

        var parent = _rootElement.Parent;
        var index = parent.Children.IndexOf(_rootElement);
        if (index < 0) return;

        var oldElement = _rootElement;
        var externalClasses = ExternalRootClasses(oldElement);
        var rebuilt = Build();
        AdoptProducedElement(rebuilt);
        AddClasses(rebuilt, externalClasses);

        if (ReferenceEquals(rebuilt, oldElement))
        {
            // 每次都交回同一个保留元素的组件（MikoPlayer）：结构没有替换，但内容可能变了。
            rebuilt.MarkDirty();
            rebuilt.BumpMutationVersion();
            return;
        }

        TransferLayoutBox(oldElement, rebuilt);
        TransferRuntimeState(oldElement, rebuilt);
        // ElementCollection 的索引器会自动设置父引用、下发引擎归属并记账这次结构替换
        // （ISSUE-129）——缺了记账，下一帧的布局缓存判定会认为「无事发生」而复用旧布局。
        parent.Children[index] = rebuilt;

        // 以本组件的产出为根的外层组件（本组件是其标记的唯一内容）的根随之换成新元素：否则它们
        // 之后的 StateHasChanged 会在已脱离的旧根上重建，拆解时也会认错自己的根。
        for (var owner = _rootOwner; owner is not null && ReferenceEquals(owner._rootElement, oldElement); owner = owner._rootOwner)
            owner._rootElement = rebuilt;
        StampRoot(rebuilt);

        // 旧子树出局：其中被本次渲染复用的组件已换到新元素上，拆解回调会跳过它们；只有本次
        // 渲染丢弃的组件被拆解（包括以本组件根元素为产出的那个——ISSUE-153：自行关闭的
        // IonAlert 的旧 IonPortal 若不拆解就一直留在 IonApp 的弹层里）。
        oldElement.DisposeComponentSubtree();
    }

    /// <summary>
    /// Re-renders a component whose root element has no parent — the engine's root, or a component
    /// built on its own. Whoever holds that root (the engine) keeps holding it, so the root element is
    /// preserved and the new render's content and state are moved into it.
    ///
    /// <para>The new children are moved in exactly as built, interleaved text included (ISSUE-154
    /// BUG-02: the old copy re-assigned <c>TextContent</c>, which merged every text child into one
    /// leading node, so <c>before&lt;span/&gt;after</c> became <c>beforeafter&lt;span/&gt;</c>). The
    /// root's own state — attributes, inline style, event handlers and listeners, state flags and
    /// control values — is migrated in full (<see cref="Element.AdoptStateFrom"/>).</para>
    ///
    /// <para>A root can only be preserved as the same element type. When a multi-root render (a
    /// transparent <see cref="FragmentElement"/>) becomes a single element, the element is placed in
    /// the preserved fragment, which the layout engine skips. Any other change of root type keeps the
    /// old element as the container of the new content.</para>
    /// </summary>
    private void RerenderInPlace()
    {
        var root = _rootElement!;
        var oldInner = InnerRootChain(root);
        var externalClasses = ExternalRootClasses(root);

        var built = BuildNew();
        var builder = _lastBuildNewBuilder;
        _lastBuildNewBuilder = null;

        var sameType = built.GetType() == root.GetType();
        var nestInFragment = !sameType && root is FragmentElement;
        var content = nestInFragment ? new FragmentElement { Children = { built } } : built;

        // Carry focus / caret state onto the incoming children before the old ones are dropped
        // (ISSUE-121). The root element itself survives, so only its children are paired up.
        foreach (var (oldChild, newChild) in PairChildren(root, content))
            TransferRuntimeState(oldChild, newChild);
        if (sameType) built.CopyInteractionStateFrom(root);

        var oldChildren = root.Children.ToArray();
        var pairs = PairChildren(root, content);
        root.Children.Clear();
        root.Children.AddRange(content.Children.ToArray());
        foreach (var (oldChild, newChild) in pairs)
            TransferLayoutBox(oldChild, newChild);

        if (!nestInFragment)
        {
            root.AdoptStateFrom(built);
            _builtRootClass = root.Class;
            AddClasses(root, externalClasses);
            // The built root itself is discarded; an @ref on it must follow the surviving root.
            builder?.RebindRootCaptures(built, root);
        }

        // The nested components that produced the built root now produce the preserved one; the
        // ones the previous render had there and this render dropped are disposed — the preserved
        // root is not discarded, so its dispose callback will not reach them.
        var newInner = nestInFragment ? null : builder?.RootComponent;
        for (var inner = newInner; inner is not null && ReferenceEquals(inner._rootElement, built); inner = inner._rootChild)
            inner._rootElement = root;
        _rootChild = newInner;
        if (newInner is not null) newInner._rootOwner = this;
        var kept = InnerRootChain(root);
        foreach (var previous in oldInner)
        {
            if (ReferenceEquals(previous._rootElement, root) && !kept.Contains(previous))
                previous.DisposeInternal();
        }
        StampRoot(root);

        // The old children are discarded: tear down the nested components this render did not keep.
        foreach (var oldChild in oldChildren)
            oldChild.DisposeComponentSubtree();
    }

    /// <summary>The nested components that share <paramref name="root"/> with this component, innermost last.</summary>
    private List<ComponentBase> InnerRootChain(Element root)
    {
        var chain = new List<ComponentBase>();
        for (var inner = _rootChild; inner is not null && ReferenceEquals(inner._rootElement, root); inner = inner._rootChild)
            chain.Add(inner);
        return chain;
    }

    /// <summary>
    /// Class names on <paramref name="root"/> that this component's own build did not put there — an
    /// ancestor's build post-pass added them (<c>IonItem</c> marks a child <c>IonButton</c>
    /// <c>button-small</c>, an outer component adds its theme scope). A component re-rendering on its
    /// own does not run those ancestors again, so the names are carried over to the new root.
    /// </summary>
    private string[]? ExternalRootClasses(Element root)
    {
        var current = root.Class;
        if (string.IsNullOrWhiteSpace(current) || current == _builtRootClass) return null;

        var built = _builtRootClass?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        List<string>? external = null;
        foreach (var name in current.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Array.IndexOf(built, name) < 0) (external ??= new()).Add(name);
        }
        return external?.ToArray();
    }

    private static void AddClasses(Element element, string[]? names)
    {
        if (names is null) return;
        foreach (var name in names)
        {
            if (!element.HasClass(name))
                element.Class = string.IsNullOrWhiteSpace(element.Class) ? name : $"{element.Class} {name}";
        }
    }

    /// <summary>
    /// Builds the replacement subtree used by <see cref="StateHasChanged"/> when the root
    /// element has no parent. Derived components can augment the emitted root in the same way
    /// as <see cref="Build"/>.
    /// </summary>
    protected virtual Element BuildNew()
    {
        // 见 Build()：同款分段探针，StateHasChanged 走的是这条路径。
        var buildScope = FrameTimingDiagnostics.EnterBuild();
        try
        {
            return BuildNewCore();
        }
        finally
        {
            FrameTimingDiagnostics.ExitBuild(buildScope);
        }
    }

    /// <summary>The builder of the last <see cref="BuildNew"/>, kept for its <c>@ref</c> captures and root component.</summary>
    private RenderTreeBuilder? _lastBuildNewBuilder;

    private Element BuildNewCore()
    {
        using var cascadingScope = CascadingValueSource.RestoreIfEmpty(_cascadingSnapshot);
        SetCascadingParameters();
        RenderVersion++;
        var builder = new RenderTreeBuilder(_children, TakeSpareChildren());
        _lastBuildNewBuilder = builder;
        // Re-push our captured provider so nested components rebuilt during StateHasChanged
        // still receive [Inject] services. StateHasChanged runs outside any ancestor's Build,
        // so without this push the ambient scope would be empty.
        using (ComponentServiceScope.Push(_services))
        {
            BuildRenderTree(builder);
        }
        var built = builder.Build();
        CommitChildren(builder);
        return built;
    }

    /// <summary>
    /// Populates this component's <see cref="CascadingParameterAttribute"/> properties from the
    /// ambient <see cref="CascadingValueSource"/>. Mirrors how <c>[Inject]</c> is resolved by
    /// reflection in <see cref="RouteView"/>. Properties with no matching provider are left
    /// untouched (keep their default).
    ///
    /// <para>Runs on <em>every</em> render (both <see cref="Build"/> and <see cref="BuildNew"/>),
    /// so the property set is resolved once per component type by
    /// <see cref="ComponentParameterCache"/> instead of re-reflecting each time (ISSUE-136).</para>
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "GetType() is statically unannotated, but a component can only ever be " +
                        "instantiated through RenderTreeBuilder.OpenComponent<T>/Router.MapRoute<T>, " +
                        "both of which declare ComponentTypeMembers.Activation — so the concrete " +
                        "type's parameter properties are preserved (ISSUE-140).")]
    private void SetCascadingParameters()
    {
        var cascading = ComponentParameterCache.GetCascadingParameters(GetType());
        for (int i = 0; i < cascading.Length; i++)
        {
            ref readonly var setter = ref cascading[i];
            if (CascadingValueSource.TryResolve(setter.Property.PropertyType, setter.Name, out var value))
                setter.SetValue(this, value);
        }
    }

    /// <summary>
    /// Pairs the children of a discarded element with those of its replacement, for carrying runtime
    /// state across a re-render. Children with an <c>@key</c> pair by key — so the state follows a
    /// list item when items are inserted, removed or reordered (ISSUE-154) — and the remaining
    /// children pair by position, as before. Pairs are listed in the replacement's child order.
    /// </summary>
    private static List<(Element Old, Element New)> PairChildren(Element oldElement, Element newElement)
    {
        var oldChildren = oldElement.Children;
        var newChildren = newElement.Children;
        var pairs = new List<(Element, Element)>(Math.Min(oldChildren.Count, newChildren.Count));

        if (!HasKeyedChild(oldElement) && !HasKeyedChild(newElement))
        {
            int count = Math.Min(oldChildren.Count, newChildren.Count);
            for (int i = 0; i < count; i++) pairs.Add((oldChildren[i], newChildren[i]));
            return pairs;
        }

        Dictionary<object, Element>? oldByKey = null;
        foreach (var child in oldChildren)
        {
            if (child.Key is { } key) (oldByKey ??= new()).TryAdd(key, child);
        }

        int nextUnkeyed = 0;
        foreach (var newChild in newChildren)
        {
            if (newChild.Key is { } key)
            {
                if (oldByKey is not null && oldByKey.TryGetValue(key, out var match)) pairs.Add((match, newChild));
                continue;
            }

            while (nextUnkeyed < oldChildren.Count && oldChildren[nextUnkeyed].Key is not null) nextUnkeyed++;
            if (nextUnkeyed < oldChildren.Count) pairs.Add((oldChildren[nextUnkeyed++], newChild));
        }
        return pairs;
    }

    private static bool HasKeyedChild(Element element)
    {
        foreach (var child in element.Children)
        {
            if (child.Key is not null) return true;
        }
        return false;
    }

    private static void TransferLayoutBox(Element oldElement, Element newElement)
    {
        LayoutBox? transferred = null;
        if (oldElement.LayoutBox != null)
        {
            transferred = new LayoutBox
            {
                Element = newElement,
                ComputedStyle = oldElement.LayoutBox.ComputedStyle,
            };
            newElement.LayoutBox = transferred;
        }

        // 子 LayoutBox 必须重新挂上本次配对产出的新盒，绝不能沿用 oldElement.LayoutBox.Children。
        //
        // 旧写法直接 `Children = oldElement.LayoutBox.Children`，于是新盒握着的是**上一代**的子盒，
        // 而子盒的 .Element 指向上一代元素、后者又经 SupersededBy 指向下一代……每次重渲染就多套一层。
        // 拖动滑块时每个 mousemove 都重渲染，于是整条历史全部可达：内存随拖动持续上涨、G2 暴涨，
        // 且 GC 完全无法回收（弱引用实测 30 步后 30 代全存活）。
        //
        // 未配对到的子元素（新树更长、或新出现的键）此处无盒可继承，交由下一次布局正常生成。
        if (!HasKeyedChild(oldElement) && !HasKeyedChild(newElement))
        {
            // 热路径（无 @key）：按位置配对，不分配。
            int count = Math.Min(oldElement.Children.Count, newElement.Children.Count);
            for (int i = 0; i < count; i++)
                TransferLayoutBox(oldElement.Children[i], newElement.Children[i]);
            if (transferred != null)
            {
                for (int i = 0; i < count; i++)
                {
                    var childBox = newElement.Children[i].LayoutBox;
                    if (childBox != null) transferred.Children.Add(childBox);
                }
            }
        }
        else
        {
            var pairs = PairChildren(oldElement, newElement);
            foreach (var (oldChild, newChild) in pairs)
                TransferLayoutBox(oldChild, newChild);
            if (transferred != null)
            {
                foreach (var (_, newChild) in pairs)
                {
                    if (newChild.LayoutBox is { } childBox) transferred.Children.Add(childBox);
                }
            }
        }

        if (transferred != null)
        {
            // 伪元素盒（::before/::after）由布局阶段合成，在 DOM 里没有对应子元素可配对，
            // 因此必须显式带过来——否则 CaptureTransitionableStyles 读不到它们的旧计算样式，
            // 伪元素上的 transition 会在重渲染后丢掉起始值。它们各自持有当次布局新建的
            // PseudoElement，不牵连上一代 DOM，搬运是安全的。
            foreach (var childBox in oldElement.LayoutBox!.Children)
            {
                if (childBox.Element is PseudoElement) transferred.Children.Add(childBox);
            }
        }
    }

    /// <summary>
    /// 把交互运行时状态从被丢弃的旧子树迁移到刚构建的新子树上（ISSUE-121）。
    ///
    /// <para>重渲染产出的是<b>全新</b>的元素实例，但焦点、文本光标位置这类状态活在实例上，
    /// 而不像 <c>Value</c> 那样每次都由组件参数重新写入。于是任何触发重渲染的事件处理器
    /// （<c>@onclick</c>、<c>@oninput</c>）都会把这些状态连同旧实例一起丢掉：<c>IonInput</c>
    /// 里点击 label 会重建整棵子树，新的 <c>&lt;input&gt;</c> 没有 Focus 状态，光标便不再绘制；
    /// 输入时同理，光标位置回到 0 而不跟随文本。</para>
    ///
    /// <para>同时在旧实例上留下 <see cref="Element.SupersededBy"/> 转发指针：控制器按引用
    /// 缓存了焦点元素，键盘输入需要经它找回在场的新实例。</para>
    ///
    /// <para>与 <see cref="TransferLayoutBox"/> 同款的逐层配对（带 <c>@key</c> 的按键、其余按位置，
    /// 见 <see cref="PairChildren"/>），并要求标签一致才配对——形状变化（如出现清除按钮）时不至于
    /// 把状态错搭到别的元素上。</para>
    /// </summary>
    private static void TransferRuntimeState(Element oldElement, Element newElement)
    {
        if (oldElement.TagName != newElement.TagName) return;

        oldElement.SupersededBy = newElement;
        newElement.CopyInteractionStateFrom(oldElement);

        if (!HasKeyedChild(oldElement) && !HasKeyedChild(newElement))
        {
            int count = Math.Min(oldElement.Children.Count, newElement.Children.Count);
            for (int i = 0; i < count; i++)
                TransferRuntimeState(oldElement.Children[i], newElement.Children[i]);
            return;
        }

        foreach (var (oldChild, newChild) in PairChildren(oldElement, newElement))
            TransferRuntimeState(oldChild, newChild);
    }
}
