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
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick",
                EventCallback.Factory.Create<MouseEventArgs>(this, Increment));
            builder.AddContent(2, $"Count: {_count}");
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
            builder.OpenElement(0, "div");
            builder.AddContent(1, $"Count: {Count}");
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
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "onclick",
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
            builder.OpenElement(0, "div");
            builder.AddContent(1, _open ? "OPEN" : "CLOSED");

            Button = new ForwardingButtonComponent { OnClick = Open };
            builder.OpenElement(2, "div");
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
            builder.OpenElement(0, "div");
            builder.CloseElement();
        }

        protected override void OnDispose() => DisposeCount++;
    }

    // A parent that re-renders and re-creates its nested child each render (like a page that
    // toggles _menuOpen and re-renders an IonMenu).
    private class ReRenderingParentComponent : ComponentBase
    {
        private int _tick;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, $"tick:{_tick}");
            builder.OpenComponent<DisposableChildComponent>(2);
            builder.CloseComponent();
            builder.CloseElement();
        }

        public void Bump()
        {
            _tick++;
            StateHasChanged();
        }
    }

    [Fact]
    public void StateHasChanged_DiscardedNestedComponent_IsDisposed()
    {
        DisposableChildComponent.DisposeCount = 0;

        var parent = new ReRenderingParentComponent();
        var element = parent.Build();
        // Give the root a parent so StateHasChanged takes the parent-swap branch.
        var grandparent = new DivElement();
        grandparent.AddChild(element);

        DisposableChildComponent.DisposeCount.ShouldBe(0);

        // Each re-render creates a fresh child and discards the previous one — the discarded
        // instance must be disposed exactly once per re-render.
        parent.Bump();
        DisposableChildComponent.DisposeCount.ShouldBe(1);

        parent.Bump();
        DisposableChildComponent.DisposeCount.ShouldBe(2);
    }

    [Fact]
    public void StateHasChanged_RootComponent_DiscardedNestedComponent_IsDisposed()
    {
        DisposableChildComponent.DisposeCount = 0;

        // No grandparent → StateHasChanged takes the root (ReplaceElementContent) branch.
        var parent = new ReRenderingParentComponent();
        parent.Build();

        parent.Bump();
        DisposableChildComponent.DisposeCount.ShouldBe(1);
    }
}
