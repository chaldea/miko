using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Shouldly;

namespace Miko.Tests.Components;

public class StateHasChangedTests
{
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
}
