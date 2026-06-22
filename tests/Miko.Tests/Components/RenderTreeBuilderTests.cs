using Miko.Components;
using Miko.Core.DomElements;
using Shouldly;

namespace Miko.Tests.Components;

public class RenderTreeBuilderTests
{
    [Fact]
    public void OpenCloseElement_BuildsSingleElement()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "div");
        builder.CloseElement();

        builder.Build().ShouldBeOfType<DivElement>();
    }

    [Fact]
    public void MultipleTopLevelElements_AreWrappedInDiv_NotOverwritten()
    {
        // 多个顶层元素（如 <video/> 后跟条件块）必须全部保留，包裹进一个 div，
        // 而不是后者覆盖前者（ISSUE-062 问题2：video 标签丢失）。
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "video");
        builder.CloseElement();
        builder.OpenElement(1, "div");
        builder.CloseElement();

        var root = builder.Build();

        var wrapper = root.ShouldBeOfType<DivElement>();
        wrapper.Children.Count.ShouldBe(2);
        wrapper.Children[0].ShouldBeOfType<VideoElement>();
        wrapper.Children[1].ShouldBeOfType<DivElement>();
    }

    [Fact]
    public void ThreeTopLevelElements_AllPreservedInOrder()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "video");
        builder.CloseElement();
        builder.OpenElement(1, "span");
        builder.CloseElement();
        builder.OpenElement(2, "p");
        builder.CloseElement();

        var wrapper = builder.Build().ShouldBeOfType<DivElement>();
        wrapper.Children.Count.ShouldBe(3);
        wrapper.Children[0].ShouldBeOfType<VideoElement>();
        wrapper.Children[1].ShouldBeOfType<SpanElement>();
        wrapper.Children[2].ShouldBeOfType<ParagraphElement>();
    }

    [Fact]
    public void AddAttribute_Class_SetsClassProperty()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "class", "btn-primary");
        builder.CloseElement();

        builder.Build().Class.ShouldBe("btn-primary");
    }

    [Fact]
    public void AddContent_SetsTextContent()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "button");
        builder.AddContent(1, "Click me");
        builder.CloseElement();

        builder.Build().TextContent.ShouldBe("Click me");
    }

    [Fact]
    public void AddContent_MultipleFragments_ConcatenatesTextContent()
    {
        // Razor compiles `Clicked @_count times` into three AddContent calls.
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "button");
        builder.AddContent(1, "Clicked ");
        builder.AddContent(2, 5);
        builder.AddContent(3, " times");
        builder.CloseElement();

        builder.Build().TextContent.ShouldBe("Clicked 5 times");
    }

    [Fact]
    public void AddContent_WhitespaceOnlyFragmentBetweenValues_IsPreserved()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "span");
        builder.AddContent(1, "a");
        builder.AddContent(2, " ");
        builder.AddContent(3, "b");
        builder.CloseElement();

        builder.Build().TextContent.ShouldBe("a b");
    }

    [Fact]
    public void NestedElements_BuildsParentChildRelationship()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "div");
        builder.OpenElement(1, "button");
        builder.CloseElement();
        builder.OpenElement(2, "button");
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
        builder.OpenElement(0, "div");

        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_EmptyBuilder_Throws()
    {
        var builder = new RenderTreeBuilder();

        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void UnknownTagName_Throws()
    {
        var builder = new RenderTreeBuilder();

        Should.Throw<InvalidOperationException>(() => builder.OpenElement(0, "unknown-tag"));
    }
}
