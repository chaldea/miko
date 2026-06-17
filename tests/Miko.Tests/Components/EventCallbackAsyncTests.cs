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
