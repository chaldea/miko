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
