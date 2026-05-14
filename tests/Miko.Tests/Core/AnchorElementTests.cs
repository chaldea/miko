using Miko.Common;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Core;

public class AnchorElementTests
{
    [Fact]
    public void TagName_ShouldBeA()
    {
        var anchor = new AnchorElement();
        anchor.TagName.ShouldBe("a");
    }

    [Fact]
    public void Href_ShouldBeSettable()
    {
        var anchor = new AnchorElement { Href = "https://example.com" };
        anchor.Href.ShouldBe("https://example.com");
    }

    [Fact]
    public void Target_ShouldBeSettable()
    {
        var anchor = new AnchorElement { Target = "_blank" };
        anchor.Target.ShouldBe("_blank");
    }

    [Fact]
    public void Rel_ShouldBeSettable()
    {
        var anchor = new AnchorElement { Rel = "noopener noreferrer" };
        anchor.Rel.ShouldBe("noopener noreferrer");
    }

    [Fact]
    public void DefaultStyle_ShouldBeInline()
    {
        var anchor = new AnchorElement { TextContent = "Link" };
        var resolver = new StyleResolver();
        var style = resolver.Resolve(anchor, new List<StyleSheet>());

        style.Display.ShouldBe(Display.Inline);
    }

    [Fact]
    public void DefaultStyle_ShouldHaveBlueColor()
    {
        var anchor = new AnchorElement { TextContent = "Link" };
        var resolver = new StyleResolver();
        var style = resolver.Resolve(anchor, new List<StyleSheet>());

        style.Color.ShouldBe(new Color(0, 0, 238));
    }

    [Fact]
    public void DefaultStyle_ShouldHaveUnderline()
    {
        var anchor = new AnchorElement { TextContent = "Link" };
        var resolver = new StyleResolver();
        var style = resolver.Resolve(anchor, new List<StyleSheet>());

        style.TextDecoration.ShouldBe(TextDecoration.Underline);
    }

    [Fact]
    public void CanContainChildren()
    {
        var anchor = new AnchorElement { Href = "https://example.com" };
        var span = new SpanElement { TextContent = "Click here" };

        anchor.AddChild(span);

        anchor.Children.Count.ShouldBe(1);
        anchor.Children[0].ShouldBe(span);
    }

    [Fact]
    public void InlineStyle_ShouldOverrideDefaultColor()
    {
        var anchor = new AnchorElement
        {
            TextContent = "Link",
            Style = new Style { Color = Color.Red }
        };
        var resolver = new StyleResolver();
        var style = resolver.Resolve(anchor, new List<StyleSheet>());

        style.Color.ShouldBe(Color.Red);
    }
}
