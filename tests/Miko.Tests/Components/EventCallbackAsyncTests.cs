using Miko.Components;
using Miko.Core.DomElements;
using Miko.Events;
using Shouldly;

namespace Miko.Tests.Components;

public class EventCallbackAsyncTests
{
    private class AsyncCounterComponent : ComponentBase
    {
        public int Count { get; set; }
        public bool AsyncCompleted { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick",
                EventCallback.Factory.Create<MouseEventArgs>(this, IncrementAsync));
            builder.AddContent(2, $"Count: {Count}");
            builder.CloseElement();
        }

        private async Task IncrementAsync()
        {
            // Simulate async work
            await Task.Delay(10);
            Count++;
            AsyncCompleted = true;
        }
    }

    [Fact]
    public async Task AsyncEventCallback_ShouldCallStateHasChangedAfterCompletion()
    {
        var component = new AsyncCounterComponent();
        var element = component.Build();

        element.TextContent.ShouldBe("Count: 0");
        component.AsyncCompleted.ShouldBeFalse();

        // Simulate click - handler returns Task but doesn't block
        element.OnClick!.Invoke(new MouseEventArgs { Target = element });

        // Give the async operation time to complete
        await Task.Delay(50);

        // Component should have re-rendered automatically
        component.Count.ShouldBe(1);
        component.AsyncCompleted.ShouldBeTrue();
        element.TextContent.ShouldBe("Count: 1");
    }

    private class AsyncActionComponent : ComponentBase
    {
        public bool Executed { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick",
                EventCallback.Factory.Create<MouseEventArgs>(this, ExecuteAsync));
            builder.AddContent(2, Executed ? "Done" : "Click");
            builder.CloseElement();
        }

        private async Task ExecuteAsync()
        {
            await Task.Delay(10);
            Executed = true;
        }
    }

    [Fact]
    public async Task AsyncActionCallback_ShouldTriggerRerender()
    {
        var component = new AsyncActionComponent();
        var element = component.Build();

        element.TextContent.ShouldBe("Click");

        element.OnClick!.Invoke(new MouseEventArgs { Target = element });

        await Task.Delay(50);

        component.Executed.ShouldBeTrue();
        element.TextContent.ShouldBe("Done");
    }

    [Fact]
    public void SyncEventCallback_ShouldStillWork()
    {
        // Verify synchronous event callbacks still work after adding async support
        var component = new AutoUpdateCounterComponentPublic();
        var element = component.Build();

        element.TextContent.ShouldBe("Count: 0");

        element.OnClick!.Invoke(new MouseEventArgs { Target = element });

        element.TextContent.ShouldBe("Count: 1");
    }

    // A reusable child (like Bootstrap Button) that exposes a non-generic EventCallback
    // parameter and forwards its own @onclick to it. Mirrors the real Razor-generated pattern.
    private class ButtonChildComponent : ComponentBase
    {
        [Parameter] public EventCallback OnClick { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick",
                EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
            builder.CloseElement();
        }

        private Task HandleClick(MouseEventArgs _) => OnClick.InvokeAsync();
    }

    // A parent that owns state, renders the child, and binds the child's EventCallback to its
    // own async handler. When the child button fires, the parent's handler runs and — because
    // EventCallback.Factory.Create(parent, handler) bound the receiver to the parent — the
    // PARENT re-renders. This is exactly the ISSUE-060 search-button scenario.
    private class ParentWithAsyncChildCallback : ComponentBase
    {
        private string _status = "idle";
        public ButtonChildComponent? Child { get; private set; }
        public readonly TaskCompletionSource Gate = new();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, $"Status: {_status}");

            Child = new ButtonChildComponent
            {
                OnClick = EventCallback.Factory.Create(this, LoadAsync)
            };
            var childElement = Child.Build();
            builder.AttachElement(childElement);

            builder.CloseElement();
        }

        private async Task LoadAsync()
        {
            _status = "loading";
            await Gate.Task;
            _status = "loaded";
        }
    }

    [Fact]
    public async Task ParentEventCallback_AsyncChildClick_RerendersParent()
    {
        var parent = new ParentWithAsyncChildCallback();
        var element = parent.Build();
        element.TextContent.ShouldBe("Status: idle");

        // Simulate clicking the child button.
        parent.Child.ShouldNotBeNull();
        var childRoot = parent.Child!.Build();
        childRoot.OnClick.ShouldNotBeNull();
        childRoot.OnClick!.Invoke(new MouseEventArgs { Target = childRoot });

        // The handler set _status="loading" synchronously and the parent re-rendered.
        element.TextContent.ShouldBe("Status: loading");

        // Complete the async work; parent re-renders again with "loaded".
        parent.Gate.SetResult();
        await Task.Delay(50);
        element.TextContent.ShouldBe("Status: loaded");
    }
}

// Public version for cross-class testing
public class AutoUpdateCounterComponentPublic : ComponentBase
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
