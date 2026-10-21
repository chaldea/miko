using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Shouldly;

namespace Miko.Tests.Components;

/// <summary>
/// ISSUE-154 BUG-02：没有父节点的组件（引擎的根）重渲染时保留根元素、把新内容搬进去。
/// 混合文本必须保持原有顺序；根元素自己的属性、事件与控件值必须完整迁移到保留的根上。
/// </summary>
public class RootRerenderTests
{
    private sealed class Root : ComponentBase
    {
        public Action<RenderTreeBuilder> Render { get; set; } = _ => { };
        public void Refresh() => StateHasChanged();
        protected override void BuildRenderTree(RenderTreeBuilder builder) => Render(builder);
    }

    [Fact]
    public void Should_keep_interleaved_text_order_after_rerender()
    {
        var count = 0;
        var root = new Root
        {
            Render = b =>
            {
                b.OpenElement<DivElement>();
                b.AddContent($"before{count}");
                b.OpenElement<SpanElement>();
                b.AddContent("middle");
                b.CloseElement();
                b.AddContent($"after{count}");
                b.CloseElement();
            },
        };
        var element = root.Build();
        element.Parent.ShouldBeNull();

        count = 1;
        root.Refresh();
        root.Refresh();

        element.Children.Count.ShouldBe(3);
        element.Children[0].ShouldBeOfType<TextNode>().Text.ShouldBe("before1");
        element.Children[1].ShouldBeOfType<SpanElement>().TextContent.ShouldBe("middle");
        element.Children[2].ShouldBeOfType<TextNode>().Text.ShouldBe("after1");
        element.Children.ShouldAllBe(c => ReferenceEquals(c.Parent, element));
    }

    [Fact]
    public void Should_migrate_root_attributes_and_event_handlers()
    {
        var renders = 0;
        var clicked = -1;
        var disabled = false;
        var root = new Root
        {
            Render = b =>
            {
                var render = ++renders;
                var anchor = b.OpenElement<AnchorElement>();
                anchor.Id = $"id-{render}";
                anchor.Class = $"class-{render}";
                anchor.Href = $"/page/{render}";
                anchor.OnClick = _ => clicked = render;
                RenderTreeBuilder.SetDisabled(anchor, disabled);
                b.AddContent("link");
                b.CloseElement();
            },
        };
        var element = (AnchorElement)root.Build();

        disabled = true;
        root.Refresh();

        element.Id.ShouldBe("id-2");
        element.Class.ShouldBe("class-2");
        element.Href.ShouldBe("/page/2");
        element.HasState(ElementState.Disabled).ShouldBeTrue();
        element.OnClick!.Invoke(new MouseEventArgs { Target = element });
        clicked.ShouldBe(2);

        disabled = false;
        root.Refresh();
        element.HasState(ElementState.Disabled).ShouldBeFalse("a state the new render no longer declares is cleared");
    }

    [Fact]
    public void Should_migrate_event_listeners_without_keeping_the_old_ones()
    {
        var renders = 0;
        var calls = new List<int>();
        var root = new Root
        {
            Render = b =>
            {
                var render = ++renders;
                var div = b.OpenElement<DivElement>();
                div.AddEventListener<MouseEventArgs>(EventTypes.Click, _ => calls.Add(render));
                b.CloseElement();
            },
        };
        var element = root.Build();

        root.Refresh();
        new EventDispatcher().Dispatch(element, EventTypes.Click, new MouseEventArgs { Target = element });

        calls.ShouldBe(new[] { 2 });
    }

    [Fact]
    public void Should_migrate_a_declared_control_value_and_keep_undeclared_user_text()
    {
        string? declared = "first";
        var root = new Root
        {
            Render = b =>
            {
                var input = b.OpenElement<InputElement>();
                RenderTreeBuilder.SetInputValue(input, declared);
                input.Placeholder = declared ?? "none";
                b.CloseElement();
            },
        };
        var element = (InputElement)root.Build();
        element.Value.ShouldBe("first");

        declared = "second";
        root.Refresh();
        element.Value.ShouldBe("second");

        // 用户键入后，新渲染没有声明值：键入的文本与光标留在保留的根上（ISSUE-121 的语义）。
        element.MoveCursorToEnd();
        element.InsertText("!");
        declared = null;
        root.Refresh();
        element.Value.ShouldBe("second!");
        element.CursorPosition.ShouldBe("second!".Length);
        element.Placeholder.ShouldBe("none");
    }

    [Fact]
    public void Should_place_a_single_new_root_inside_a_preserved_fragment()
    {
        var multi = true;
        var root = new Root
        {
            Render = b =>
            {
                b.OpenElement<DivElement>();
                b.AddContent("a");
                b.CloseElement();
                if (multi)
                {
                    b.OpenElement<DivElement>();
                    b.AddContent("b");
                    b.CloseElement();
                }
            },
        };
        var fragment = root.Build().ShouldBeOfType<FragmentElement>();

        multi = false;
        root.Refresh();

        // 片段对布局透明，单个新根放进保留的片段里，而不是把它的子节点倒进片段。
        fragment.Children.Count.ShouldBe(1);
        fragment.Children[0].ShouldBeOfType<DivElement>().TextContent.ShouldBe("a");

        multi = true;
        root.Refresh();
        fragment.Children.Select(c => c.TextContent).ShouldBe(new[] { "a", "b" });
    }
}
