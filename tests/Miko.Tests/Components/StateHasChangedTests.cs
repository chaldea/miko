using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Shouldly;

namespace Miko.Tests.Components;

public class StateHasChangedTests
{
    // Mirrors the compiler output for `<button @onclick="Increment">Count: @_count</button>`:
    // the handler only mutates state and never calls StateHasChanged itself.
    private class AutoUpdateCounterComponent : ComponentBase
    {
        private int _count;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var __e1 = builder.OpenElement<ButtonElement>();
            __e1.OnClick = global::Miko.Components.RenderTreeBuilder.ToHandler(
                EventCallback.Factory.Create<MouseEventArgs>(this, Increment));
            builder.AddContent($"Count: {_count}");
            builder.CloseElement();
        }

        private void Increment() => _count++;
    }

    [Fact]
    public void EventCallback_AfterHandler_AutoUpdatesWithoutManualStateHasChanged()
    {
        var component = new AutoUpdateCounterComponent();
        var element = component.Build();
        element.TextContent.ShouldBe("Count: 0");

        // Simulate a click dispatching to the wired-up handler.
        element.OnClick!.Invoke(new MouseEventArgs { Target = element });

        element.TextContent.ShouldBe("Count: 1");
    }

    private class CounterComponent : ComponentBase
    {
        public int Count { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            builder.AddContent($"Count: {Count}");
            builder.CloseElement();
        }

        public void Increment()
        {
            Count++;
            StateHasChanged();
        }
    }

    [Fact]
    public void StateHasChanged_RootComponent_WithoutParent_ShouldUpdateElement()
    {
        var component = new CounterComponent();
        var element = component.Build();

        // 根组件没有 parent
        element.Parent.ShouldBeNull();
        element.TextContent.ShouldBe("Count: 0");

        // 调用 StateHasChanged 应该更新元素内容
        component.Increment();

        element.TextContent.ShouldBe("Count: 1");
    }

    [Fact]
    public void StateHasChanged_RootComponent_ShouldPreserveElementReference()
    {
        var component = new CounterComponent();
        var element = component.Build();
        var originalRef = element;

        component.Increment();

        // 引擎持有的引用不应改变
        element.ShouldBeSameAs(originalRef);
    }

    [Fact]
    public void StateHasChanged_ChildComponent_WithParent_ShouldReplaceInParent()
    {
        var component = new CounterComponent();
        var element = component.Build();

        // 模拟有 parent 的情况
        var parent = new DivElement();
        parent.AddChild(element);

        component.Increment();

        parent.Children.Count.ShouldBe(1);
        parent.Children[0].TextContent.ShouldBe("Count: 1");
    }

    // A reusable button-like child whose @onclick handler lives in the child and merely
    // forwards to a parent-supplied Action — exactly like IonMenuButton (ISSUE-052).
    private class ForwardingButtonComponent : ComponentBase
    {
        [Parameter] public Action? OnClick { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var __e3 = builder.OpenElement<DivElement>();
            __e3.OnClick = global::Miko.Components.RenderTreeBuilder.ToHandler(
                EventCallback.Factory.Create<MouseEventArgs>(this, () => OnClick?.Invoke()));
            builder.CloseElement();
        }
    }

    // A parent that owns toggle state AND the conditionally-rendered content, and embeds
    // the forwarding child — exactly like HomePage owning _menuOpen, the IonMenu drawer,
    // and the IonMenuButton. The parent's callback must call StateHasChanged() itself,
    // because the child's EventCallback only re-renders the child, not the parent.
    private class ToggleHostComponent : ComponentBase
    {
        private bool _open;
        public ForwardingButtonComponent? Button { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            builder.AddContent(1, _open ? "OPEN" : "CLOSED");

            Button = new ForwardingButtonComponent { OnClick = Open };
            builder.OpenElement<DivElement>();
            var childElement = Button.Build();
            builder.AttachElement(childElement);
            builder.CloseElement();

            builder.CloseElement();
        }

        private void Open()
        {
            _open = true;
            StateHasChanged();
        }
    }

    [Fact]
    public void StateHasChanged_ParentOwnsState_ChildClickForwardsToParent_ParentRerenders()
    {
        var host = new ToggleHostComponent();
        var element = host.Build();
        element.TextContent.ShouldBe("CLOSED");

        // Simulate clicking the child button: it forwards to the parent's Open(), which
        // flips the parent state and calls StateHasChanged() to re-render the parent.
        host.Button.ShouldNotBeNull();
        var childRoot = host.Button!.Build();
        childRoot.OnClick.ShouldNotBeNull();
        childRoot.OnClick!.Invoke(new MouseEventArgs { Target = childRoot });

        // The parent's rendered content reflects the new state.
        element.TextContent.ShouldBe("OPEN");
    }

    // A nested child rendered via OpenComponent/CloseComponent (the real component path), so
    // its produced element carries a DisposeCallback. Counts OnDispose invocations.
    private class DisposableChildComponent : ComponentBase
    {
        public static int DisposeCount;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            builder.CloseElement();
        }

        protected override void OnDispose() => DisposeCount++;
    }

    // A parent that re-renders and renders its nested child conditionally (like a page that
    // toggles _menuOpen and re-renders an IonMenu).
    private class ReRenderingParentComponent : ComponentBase
    {
        private int _tick;
        public bool ShowChild { get; set; } = true;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            builder.AddContent($"tick:{_tick}");
            if (ShowChild)
            {
                builder.OpenComponent<DisposableChildComponent>(1, null);
                builder.CloseComponent();
            }
            builder.CloseElement();
        }

        public void Bump()
        {
            _tick++;
            StateHasChanged();
        }
    }

    // ISSUE-154: a re-render keeps the nested instance whose identity is unchanged, so it is not
    // disposed; it is disposed exactly once, when a re-render stops rendering it.
    [Fact]
    public void StateHasChanged_NestedComponent_IsKeptAcrossRerenders_AndDisposedOnceWhenDropped()
    {
        DisposableChildComponent.DisposeCount = 0;

        var parent = new ReRenderingParentComponent();
        var element = parent.Build();
        // Give the root a parent so StateHasChanged takes the parent-swap branch.
        var grandparent = new DivElement();
        grandparent.AddChild(element);

        parent.Bump();
        parent.Bump();
        DisposableChildComponent.DisposeCount.ShouldBe(0);

        parent.ShowChild = false;
        parent.Bump();
        DisposableChildComponent.DisposeCount.ShouldBe(1);

        parent.Bump();
        DisposableChildComponent.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public void StateHasChanged_RootComponent_NestedComponent_IsKept_AndDisposedOnceWhenDropped()
    {
        DisposableChildComponent.DisposeCount = 0;

        // No grandparent → StateHasChanged takes the root (in-place) branch.
        var parent = new ReRenderingParentComponent();
        parent.Build();

        parent.Bump();
        DisposableChildComponent.DisposeCount.ShouldBe(0);

        parent.ShowChild = false;
        parent.Bump();
        DisposableChildComponent.DisposeCount.ShouldBe(1);
    }

    // A page that renders two top-level elements (a FragmentElement root) and owns state, like
    // a multi-root Razor page using a Layout (ISSUE-066 问题1).
    private class MultiRootCounterPage : ComponentBase
    {
        private int _count;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            builder.AddContent($"a:{_count}");
            builder.CloseElement();
            builder.OpenElement<DivElement>();
            builder.AddContent($"b:{_count}");
            builder.CloseElement();
        }

        public void Increment()
        {
            _count++;
            StateHasChanged();
        }
    }

    [Fact]
    public void StateHasChanged_MultiRootPage_WithoutParent_UpdatesInPlace()
    {
        // No-layout case: the fragment root has no parent → ReplaceElementContent branch.
        var page = new MultiRootCounterPage();
        var root = page.Build();

        root.ShouldBeOfType<FragmentElement>();
        root.Parent.ShouldBeNull();
        root.Children[0].TextContent.ShouldBe("a:0");
        root.Children[1].TextContent.ShouldBe("b:0");

        page.Increment();

        // The engine holds the same fragment reference; its children are updated in place.
        root.Children[0].TextContent.ShouldBe("a:1");
        root.Children[1].TextContent.ShouldBe("b:1");
    }

    [Fact]
    public void StateHasChanged_MultiRootPage_WithParent_ReplacesFragmentInParent()
    {
        // Layout case: the fragment root is a child of the layout's <div class="root">.
        var page = new MultiRootCounterPage();
        var root = page.Build();
        var layoutRoot = new DivElement { Class = "root" };
        layoutRoot.AddChild(root);

        layoutRoot.Children.Count.ShouldBe(1);

        page.Increment();

        // A single fragment child remains (replaced, not duplicated) and reflects the new state.
        layoutRoot.Children.Count.ShouldBe(1);
        var fragment = layoutRoot.Children[0].ShouldBeOfType<FragmentElement>();
        fragment.Children[0].TextContent.ShouldBe("a:1");
        fragment.Children[1].TextContent.ShouldBe("b:1");
    }

    /// <summary>Records its disposals; renders one div.</summary>
    private sealed class DisposalProbe : ComponentBase
    {
        internal static int Disposed;

        protected override void OnDispose() => Disposed++;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            builder.CloseElement();
        }
    }

    /// <summary>
    /// A component whose markup starts with a component: the probe produces its root. With
    /// <see cref="Keyed"/> the probe carries a key that changes on every render, so it is a new
    /// identity each time.
    /// </summary>
    private sealed class RootIsComponent : ComponentBase
    {
        private int _renders;
        internal bool Keyed { get; set; }

        internal void Refresh() => StateHasChanged();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<DisposalProbe>(1, Keyed ? _renders++ : null);
            builder.CloseComponent();
        }
    }

    /// <summary>
    /// ISSUE-153: a self re-render disposed the nested components of the old root's children but
    /// not the one that produced the old root itself. A self-dismissed IonAlert (root: IonPortal)
    /// therefore stayed registered in IonApp's overlay layer. ISSUE-154: the instance producing the
    /// root is kept while its identity holds; when it changes, the replaced one is disposed once.
    /// </summary>
    [Fact]
    public void SelfRerender_DisposesTheComponentThatProducedTheOldRoot()
    {
        DisposalProbe.Disposed = 0;
        var component = new RootIsComponent { Keyed = true };
        var host = new DivElement { Children = { component.Build() } };

        component.Refresh();
        DisposalProbe.Disposed.ShouldBe(1);

        component.Refresh();
        DisposalProbe.Disposed.ShouldBe(2, "each replaced instance is disposed exactly once");
        host.Children.Count.ShouldBe(1);
    }

    [Fact]
    public void SelfRerender_KeepsTheComponentThatProducesTheRoot()
    {
        DisposalProbe.Disposed = 0;
        var component = new RootIsComponent();
        var host = new DivElement { Children = { component.Build() } };

        component.Refresh();
        component.Refresh();

        DisposalProbe.Disposed.ShouldBe(0);
        host.Children.Count.ShouldBe(1);

        // The probe still shares the root: discarding it disposes the probe exactly once.
        host.Children[0].DisposeComponentSubtree();
        DisposalProbe.Disposed.ShouldBe(1);
    }
}
