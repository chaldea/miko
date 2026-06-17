using Miko.Components;
using Miko.Core.DomElements;
using Shouldly;

namespace Miko.Tests.Components;

public class ComponentLifecycleAsyncTests
{
    private class AsyncInitComponent : ComponentBase
    {
        public string Status { get; set; } = "Initial";
        public bool InitCompleted { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, $"Status: {Status}");
            builder.CloseElement();
        }

        protected override async Task OnInitializedAsync()
        {
            await Task.Delay(10);
            Status = "Loaded";
            InitCompleted = true;
        }
    }

    [Fact]
    public async Task OnInitializedAsync_ShouldTriggerRerender()
    {
        var component = new AsyncInitComponent();
        var element = component.Build();

        // First render shows initial state
        element.TextContent.ShouldBe("Status: Initial");
        component.InitCompleted.ShouldBeFalse();

        // Give async init time to complete
        await Task.Delay(50);

        // Component should have re-rendered with loaded state
        component.Status.ShouldBe("Loaded");
        component.InitCompleted.ShouldBeTrue();
        element.TextContent.ShouldBe("Status: Loaded");
    }

    private class AsyncParamsComponent : ComponentBase
    {
        [Parameter] public string? Input { get; set; }
        public string? ProcessedInput { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, $"Processed: {ProcessedInput ?? "None"}");
            builder.CloseElement();
        }

        protected override async Task OnParametersSetAsync()
        {
            await Task.Delay(10);
            ProcessedInput = Input?.ToUpper();
        }
    }

    [Fact]
    public async Task OnParametersSetAsync_ShouldTriggerRerender()
    {
        var component = new AsyncParamsComponent { Input = "test" };
        var element = component.Build();

        // First render happens before async completes
        element.TextContent.ShouldBe("Processed: None");

        await Task.Delay(50);

        // After async completes, should have processed the input
        component.ProcessedInput.ShouldBe("TEST");
        element.TextContent.ShouldBe("Processed: TEST");
    }

    private class SyncLifecycleComponent : ComponentBase
    {
        public bool InitCalled { get; set; }
        public bool ParamsSetCalled { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.CloseElement();
        }

        protected override void OnInitialized()
        {
            InitCalled = true;
        }

        protected override void OnParametersSet()
        {
            ParamsSetCalled = true;
        }
    }

    [Fact]
    public void SyncLifecycleMethods_ShouldStillWork()
    {
        var component = new SyncLifecycleComponent();
        component.Build();

        component.InitCalled.ShouldBeTrue();
        component.ParamsSetCalled.ShouldBeTrue();
    }

    private class MixedLifecycleComponent : ComponentBase
    {
        public int SyncCount { get; set; }
        public int AsyncCount { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, $"Sync: {SyncCount}, Async: {AsyncCount}");
            builder.CloseElement();
        }

        protected override void OnInitialized()
        {
            SyncCount++;
        }

        protected override async Task OnInitializedAsync()
        {
            await Task.Delay(10);
            AsyncCount++;
        }
    }

    [Fact]
    public async Task MixedLifecycle_BothSyncAndAsyncShouldExecute()
    {
        var component = new MixedLifecycleComponent();
        var element = component.Build();

        // Sync method executes immediately
        component.SyncCount.ShouldBe(1);
        component.AsyncCount.ShouldBe(0);

        await Task.Delay(50);

        // Async method completes and triggers re-render
        component.SyncCount.ShouldBe(1);
        component.AsyncCount.ShouldBe(1);
        element.TextContent.ShouldBe("Sync: 1, Async: 1");
    }
}
