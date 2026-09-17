using Miko.Common;
using Miko.Components;
using Miko.Core.DomElements;
using Miko.Events;
using Shouldly;

namespace Miko.Tests.Components;

public class RenderTreeBuilderTests
{
    /// <summary>
    /// `type` 的字符串到 <see cref="InputType"/> 的映射在这里独立存在一份（另两份是
    /// <see cref="InputElement.IsEditable"/> 与控制器的点击分支）。number/tel 曾漏在这里，
    /// 于是 `<input type="number">` 悄悄退化成 Text，数字软键盘永远唤不起来。
    /// </summary>
    [Theory]
    [InlineData("number", InputType.Number)]
    [InlineData("tel", InputType.Number)]
    [InlineData("password", InputType.Password)]
    [InlineData("search", InputType.Search)]
    [InlineData("checkbox", InputType.Checkbox)]
    [InlineData("text", InputType.Text)]
    [InlineData("date", InputType.Text)]
    public void TypeAttribute_MapsToInputType(string attribute, InputType expected)
    {
        var builder = new RenderTreeBuilder();
        var __e1 = builder.OpenElement<InputElement>();
        __e1.Type = global::Miko.Components.RenderTreeBuilder.ParseInputType(attribute);
        builder.CloseElement();

        var input = builder.Build().ShouldBeOfType<InputElement>();
        input.Type.ShouldBe(expected);
    }

    [Fact]
    public void OpenCloseElement_BuildsSingleElement()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement<DivElement>();
        builder.CloseElement();

        builder.Build().ShouldBeOfType<DivElement>();
    }

    [Fact]
    public void MultipleTopLevelElements_AreWrappedInTransparentFragment_NotOverwritten()
    {
        // 多个顶层元素（如 <video/> 后跟条件块）必须全部保留，承载进一个透明 FragmentElement，
        // 而不是后者覆盖前者（ISSUE-062 问题2：video 标签丢失）。容器透明化是 ISSUE-066 问题1：
        // 不再用不透明 <div> 包裹，以免破坏样式布局。
        var builder = new RenderTreeBuilder();
        builder.OpenElement<VideoElement>();
        builder.CloseElement();
        builder.OpenElement<DivElement>();
        builder.CloseElement();

        var root = builder.Build();

        var wrapper = root.ShouldBeOfType<FragmentElement>();
        wrapper.Children.Count.ShouldBe(2);
        wrapper.Children[0].ShouldBeOfType<VideoElement>();
        wrapper.Children[1].ShouldBeOfType<DivElement>();
    }

    [Fact]
    public void ThreeTopLevelElements_AllPreservedInOrder()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement<VideoElement>();
        builder.CloseElement();
        builder.OpenElement<SpanElement>();
        builder.CloseElement();
        builder.OpenElement<ParagraphElement>();
        builder.CloseElement();

        var wrapper = builder.Build().ShouldBeOfType<FragmentElement>();
        wrapper.Children.Count.ShouldBe(3);
        wrapper.Children[0].ShouldBeOfType<VideoElement>();
        wrapper.Children[1].ShouldBeOfType<SpanElement>();
        wrapper.Children[2].ShouldBeOfType<ParagraphElement>();
    }

    [Fact]
    public void AddAttribute_Class_SetsClassProperty()
    {
        var builder = new RenderTreeBuilder();
        var __e2 = builder.OpenElement<ButtonElement>();
        __e2.Class = "btn-primary";
        builder.CloseElement();

        builder.Build().Class.ShouldBe("btn-primary");
    }

    [Fact]
    public void AddContent_SetsTextContent()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement<ButtonElement>();
        builder.AddContent("Click me");
        builder.CloseElement();

        builder.Build().TextContent.ShouldBe("Click me");
    }

    [Fact]
    public void AddContent_MultipleFragments_ConcatenatesTextContent()
    {
        // Razor compiles `Clicked @_count times` into three AddContent calls.
        var builder = new RenderTreeBuilder();
        builder.OpenElement<ButtonElement>();
        builder.AddContent("Clicked ");
        builder.AddContent(2, 5);
        builder.AddContent(" times");
        builder.CloseElement();

        builder.Build().TextContent.ShouldBe("Clicked 5 times");
    }

    [Fact]
    public void AddContent_WhitespaceOnlyFragmentBetweenValues_IsPreserved()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement<SpanElement>();
        builder.AddContent("a");
        builder.AddContent(" ");
        builder.AddContent("b");
        builder.CloseElement();

        builder.Build().TextContent.ShouldBe("a b");
    }

    [Fact]
    public void NestedElements_BuildsParentChildRelationship()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement<DivElement>();
        builder.OpenElement<ButtonElement>();
        builder.CloseElement();
        builder.OpenElement<ButtonElement>();
        builder.CloseElement();
        builder.CloseElement();

        var root = builder.Build();
        root.ShouldBeOfType<DivElement>();
        root.Children.Count.ShouldBe(2);
        root.Children[0].ShouldBeOfType<ButtonElement>();
    }

    [Fact]
    public void Build_WithUnclosedElement_Throws()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement<DivElement>();

        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_EmptyBuilder_ReturnsEmptyTransparentFragment()
    {
        // A component that renders nothing (e.g. a transparent CascadingValue with null
        // ChildContent) yields an empty FragmentElement rather than throwing.
        var builder = new RenderTreeBuilder();

        var root = builder.Build().ShouldBeOfType<FragmentElement>();
        root.Children.ShouldBeEmpty();
    }

    [Fact]
    public void UnknownTagName_Throws()
    {
        var builder = new RenderTreeBuilder();

        // 名字驱动的回退路径仍在服役（AddMarkupContent 的运行时 HTML 解析要用），
        // 未知标签必须照旧抛错。
        Should.Throw<InvalidOperationException>(() => builder.OpenElement(0, "unknown-tag"));
    }

    // -----------------------------------------------------------------
    // 强类型构建 API（ISSUE-136）
    //
    // 这些用例针对的是 Razor 编译器现在发射的那套调用：OpenElement<T>() 返回实例、
    // 属性直接赋值、事件写进强类型槽位。语义必须与旧的「序号 + 字符串名」路径完全一致
    // ——尤其是下面逐条覆盖的那些有 issue 背书的边角规则。
    // -----------------------------------------------------------------

    [Fact]
    public void TypedOpenElement_ReturnsInstance_AndAttachesOnClose()
    {
        var builder = new RenderTreeBuilder();

        var div = builder.OpenElement<DivElement>();
        div.Class = "outer";
        var span = builder.OpenElement<SpanElement>();
        span.Id = "inner";
        builder.CloseElement();
        builder.CloseElement();

        var root = builder.Build().ShouldBeOfType<DivElement>();
        root.ShouldBeSameAs(div);
        root.Class.ShouldBe("outer");
        root.Children.ShouldHaveSingleItem().ShouldBeSameAs(span);
        span.Id.ShouldBe("inner");
        span.Parent.ShouldBeSameAs(root);
    }

    [Fact]
    public void TypedOpenElement_MultipleRoots_AreWrappedInTransparentFragment()
    {
        // 多根经 AttachToTree 并入一个透明片段，而不是后者覆盖前者。
        var builder = new RenderTreeBuilder();

        builder.OpenElement<DivElement>();
        builder.CloseElement();
        builder.OpenElement<SpanElement>();
        builder.CloseElement();

        var root = builder.Build().ShouldBeOfType<FragmentElement>();
        root.Children.Count.ShouldBe(2);
        root.Children[0].ShouldBeOfType<DivElement>();
        root.Children[1].ShouldBeOfType<SpanElement>();
    }

    [Fact]
    public void TypedAddContent_MergesAdjacentText_ButNotAcrossChildElements()
    {
        // 与旧路径同款的 TextNode 语义（ISSUE-086）：相邻文本合并，被子元素分隔的各自成节点。
        var builder = new RenderTreeBuilder();

        builder.OpenElement<ParagraphElement>();
        builder.AddContent("a");
        builder.AddContent("b");
        builder.OpenElement<SpanElement>();
        builder.CloseElement();
        builder.AddContent("c");
        builder.CloseElement();

        var p = builder.Build().ShouldBeOfType<ParagraphElement>();
        p.Children.Count.ShouldBe(3);
        p.Children[0].ShouldBeOfType<TextNode>().Text.ShouldBe("ab");
        p.Children[1].ShouldBeOfType<SpanElement>();
        p.Children[2].ShouldBeOfType<TextNode>().Text.ShouldBe("c");
    }

    [Fact]
    public void TypedAddContent_DecodesHtmlEntities()
    {
        var builder = new RenderTreeBuilder();

        builder.OpenElement<DivElement>();
        builder.AddContent("a&amp;b");
        builder.CloseElement();

        builder.Build().TextContent.ShouldBe("a&b");
    }

    [Fact]
    public void ToHandler_BindsCallbackToTypedSlot()
    {
        var builder = new RenderTreeBuilder();
        var clicks = 0;

        var button = builder.OpenElement<ButtonElement>();
        button.OnClick = RenderTreeBuilder.ToHandler(
            EventCallback.Factory.Create<MouseEventArgs>(this, _ => clicks++));
        builder.CloseElement();

        button.OnClick.ShouldNotBeNull();
        button.OnClick!.Invoke(new MouseEventArgs { Target = button });
        clicks.ShouldBe(1);
    }

    [Fact]
    public void ToHandler_WithoutDelegate_LeavesSlotUnset()
    {
        // 没有委托的回调不该装上一个什么都不做的包装器——旧路径也是直接跳过。
        RenderTreeBuilder.ToHandler(default(EventCallback<MouseEventArgs>)).ShouldBeNull();
    }

    [Fact]
    public void SetInputValue_NullValue_DoesNotClearUserText()
    {
        // ISSUE-121：value 属性缺席（表达式求值为 null）时不得动元素当前值，
        // 否则祖先重渲染会清空用户正在输入的内容。
        var input = new InputElement();
        input.InsertText("typed");

        RenderTreeBuilder.SetInputValue(input, null);

        input.Value.ShouldBe("typed");
    }

    [Fact]
    public void SetInputValue_RangeInput_KeepsNumericValueInStep()
    {
        var input = new InputElement { Type = InputType.Range };

        RenderTreeBuilder.SetInputValue(input, "42");

        input.Value.ShouldBe("42");
        input.NumericValue.ShouldBe(42f);
    }

    [Fact]
    public void SetTextAreaValue_NullValue_DoesNotClearUserText()
    {
        var textArea = new TextAreaElement();
        textArea.InsertText("typed");

        RenderTreeBuilder.SetTextAreaValue(textArea, null);

        textArea.Value.ShouldBe("typed");
    }

    [Theory]
    [InlineData("checkbox", InputType.Checkbox)]
    [InlineData("radio", InputType.Radio)]
    [InlineData("password", InputType.Password)]
    [InlineData("range", InputType.Range)]
    [InlineData("search", InputType.Search)]
    [InlineData("number", InputType.Number)]
    // number/tel 都要数字键盘，按纯文本编辑。
    [InlineData("tel", InputType.Number)]
    [InlineData("text", InputType.Text)]
    [InlineData("something-else", InputType.Text)]
    [InlineData(null, InputType.Text)]
    public void ParseInputType_MatchesLegacySwitch(string? value, InputType expected)
        => RenderTreeBuilder.ParseInputType(value).ShouldBe(expected);

    [Theory]
    // HTML 布尔属性：存在即为真，只有显式的 "false" 为假。
    [InlineData("false", false)]
    [InlineData("FALSE", false)]
    [InlineData("true", true)]
    [InlineData("", true)]
    [InlineData("autoplay", true)]
    [InlineData(null, true)]
    public void ParseBooleanAttribute_MatchesLegacySwitch(string? value, bool expected)
        => RenderTreeBuilder.ParseBooleanAttribute(value).ShouldBe(expected);

    [Fact]
    public void SetStyle_NonStyleValue_IsIgnored()
    {
        // 旧的属性 switch 只匹配 Styling.Style，其它值（如 CSS 字符串）落不到任何 case
        // 而被静默丢弃。object? 重载保留这一行为，避免既有组件编译失败。
        var builder = new RenderTreeBuilder();
        var div = builder.OpenElement<DivElement>();

        RenderTreeBuilder.SetStyle(div, (object?)"width: 10px");

        div.Style.ShouldBeNull();
    }

    [Fact]
    public void TypedOpenComponent_ReturnsInstance_AndAttachesProducedElement()
    {
        var builder = new RenderTreeBuilder();

        var component = builder.OpenComponent<LabelComponent>();
        component.Text = "hello";
        builder.CloseComponent();

        var root = builder.Build().ShouldBeOfType<SpanElement>();
        root.TextContent.ShouldBe("hello");
    }

    [Fact]
    public void TypedCloseComponent_ChainsComponentDisposeOntoProducedElement()
    {
        var builder = new RenderTreeBuilder();

        var component = builder.OpenComponent<LabelComponent>();
        component.Text = "x";
        builder.CloseComponent();

        var root = builder.Build();
        root.DisposeCallback.ShouldNotBeNull();
        root.DisposeCallback!.Invoke();
        component.Disposed.ShouldBeTrue();
    }

    [Fact]
    public void TypedOpenElement_TextArea_RecyclesChildTextIntoValue()
    {
        // HTML 中 textarea 的初始文本写在标签内容里，CloseElement 要把它回收进 Value。
        var builder = new RenderTreeBuilder();

        builder.OpenElement<TextAreaElement>();
        builder.AddContent("initial");
        builder.CloseElement();

        var textArea = builder.Build().ShouldBeOfType<TextAreaElement>();
        textArea.Value.ShouldBe("initial");
        textArea.Children.ShouldBeEmpty();
    }

    private sealed class LabelComponent : ComponentBase
    {
        [Parameter] public string? Text { get; set; }

        public bool Disposed { get; private set; }

        protected override void OnDispose() => Disposed = true;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<SpanElement>();
            builder.AddContent(Text);
            builder.CloseElement();
        }
    }
}
