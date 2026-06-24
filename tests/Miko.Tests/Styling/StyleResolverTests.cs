using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Styling;

public class StyleResolverTests
{
    [Fact]
    public void Resolve_WithNoStyles_ShouldReturnDefaultComputedStyle()
    {
        var computed = new StyleResolver().Resolve(new DivElement(), []);

        computed.ShouldNotBeNull();
        computed.Display.ShouldBe(Display.Block);
    }

    [Fact]
    public void Resolve_WithMatchingClassSelector_ShouldApplyStyle()
    {
        var element = new DivElement { Class = "container" };
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("container").Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void Resolve_WithMultipleMatchingRules_ShouldApplyBySpecificity()
    {
        var element = new DivElement { Id = "main", Class = "container" };
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.For<DivElement>().Set(x => x.BackgroundColor, Color.Red));
        styleSheet.AddRule(Style.Class("container").Set(x => x.BackgroundColor, Color.Green));
        styleSheet.AddRule(Style.Id("main").Set(x => x.BackgroundColor, Color.Blue));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Blue);
    }

    [Fact]
    public void Resolve_WithInlineStyle_ShouldOverrideStyleSheet()
    {
        var element = new DivElement
        {
            Class = "container",
            Style = new Style { BackgroundColor = Color.Yellow }
        };
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("container").Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Yellow);
    }

    [Fact]
    public void Resolve_ForH1Element_ShouldApplyDefaultStyles()
    {
        var computed = new StyleResolver().Resolve(new H1Element { TextContent = "Title" }, []);

        computed.Display.ShouldBe(Display.Block);
        computed.FontSize.Value.ShouldBe(32);
        computed.FontWeight.ShouldBe(FontWeight.Bold);
        computed.MarginTop.Value.ShouldBe(21);
        computed.MarginBottom.Value.ShouldBe(21);
    }

    [Fact]
    public void Resolve_ForButtonElement_ShouldApplyDefaultStyles()
    {
        var computed = new StyleResolver().Resolve(new ButtonElement { TextContent = "Click" }, []);

        computed.Display.ShouldBe(Display.InlineBlock);
        computed.PaddingTop.Value.ShouldBe(2);
        computed.PaddingRight.Value.ShouldBe(6);
        computed.PaddingBottom.Value.ShouldBe(2);
        computed.PaddingLeft.Value.ShouldBe(6);
        computed.BorderTopWidth.Value.ShouldBe(2);
        computed.BorderTopStyle.ShouldBe(BorderStyle.Solid);
        // Browsers center button text by default (UA stylesheet text-align: center).
        computed.TextAlign.ShouldBe(TextAlign.Center);
    }

    [Fact]
    public void Resolve_ForButtonElement_CenterTextAlign_BeatsInheritedLeft()
    {
        // A button inside a left-aligned parent must still center its own text — the UA default
        // (text-align: center) is applied before inheritance, so it wins over the inherited Left.
        var resolver = new StyleResolver();
        var parent = new DivElement
        {
            Style = new Style { TextAlign = TextAlign.Left }
        };
        var button = new ButtonElement { TextContent = "Click" };
        parent.AddChild(button);

        var parentComputed = resolver.Resolve(parent, []);
        parent.LayoutBox = new LayoutBox { Element = parent, ComputedStyle = parentComputed };

        var buttonComputed = resolver.Resolve(button, []);
        buttonComputed.TextAlign.ShouldBe(TextAlign.Center);
    }

    [Fact]
    public void Resolve_ForButtonElement_ExplicitTextAlign_OverridesUaDefault()
    {
        // An author rule should still override the UA default (??= leaves an explicit value).
        var styleSheet = new StyleSheet();
        styleSheet.Rules.Add(new StyleRule
        {
            Selector = new Miko.Styling.Selectors.TagSelector("button"),
            Style = new Style { TextAlign = TextAlign.Left }
        });

        var computed = new StyleResolver().Resolve(new ButtonElement { TextContent = "Click" }, [styleSheet]);

        computed.TextAlign.ShouldBe(TextAlign.Left);
    }

    [Fact]
    public void Resolve_WithInheritedProperties_ShouldInheritFromParent()
    {
        var resolver = new StyleResolver();
        var parent = new DivElement
        {
            Style = new Style { FontFamily = "Arial", FontSize = Length.Px(20), Color = Color.Red }
        };
        var child = new SpanElement();
        parent.AddChild(child);

        var parentComputed = resolver.Resolve(parent, []);
        parent.LayoutBox = new LayoutBox { Element = parent, ComputedStyle = parentComputed };

        var childComputed = resolver.Resolve(child, []);
        childComputed.FontFamily.ShouldBe("Arial");
        childComputed.FontSize.Value.ShouldBe(20);
        childComputed.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void Resolve_WithNonMatchingSelector_ShouldNotApplyStyle()
    {
        var element = new DivElement { Class = "primary" };
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(Style.Class("container").Set(x => x.BackgroundColor, Color.Red));

        new StyleResolver().Resolve(element, [styleSheet]).BackgroundColor.ShouldBe(Color.Transparent);
    }

    [Fact]
    public void Resolve_ShouldInheritLineHeight()
    {
        var resolver = new StyleResolver();
        var parent = new DivElement { Style = new Style { LineHeight = Length.Px(24) } };
        var child = new SpanElement();
        parent.AddChild(child);

        var parentComputed = resolver.Resolve(parent, []);
        parent.LayoutBox = new LayoutBox { Element = parent, ComputedStyle = parentComputed };

        resolver.Resolve(child, []).LineHeight.Value.ShouldBe(24);
    }

    [Fact]
    public void Resolve_ForParagraph_ShouldApply1emMargin()
    {
        var computed = new StyleResolver().Resolve(new ParagraphElement(), []);

        computed.Display.ShouldBe(Display.Block);
        computed.MarginTop.Value.ShouldBe(16);
        computed.MarginBottom.Value.ShouldBe(16);
        computed.MarginLeft.Value.ShouldBe(0);
        computed.MarginRight.Value.ShouldBe(0);
    }

    [Fact]
    public void Resolve_ForImgElement_ShouldApplyInlineDisplayAndNoBorder()
    {
        var computed = new StyleResolver().Resolve(new ImageElement(), []);

        computed.Display.ShouldBe(Display.Inline);
        computed.BorderTopWidth.Value.ShouldBe(0);
        computed.BorderTopStyle.ShouldBe(BorderStyle.None);
    }

    [Theory]
    [InlineData("h1", 32, 21)]
    [InlineData("h2", 24, 20)]
    [InlineData("h3", 19, 19)]
    [InlineData("h4", 16, 21)]
    [InlineData("h5", 13, 22)]
    [InlineData("h6", 11, 26)]
    public void Resolve_ForHeadingElements_ShouldApplyBrowserDefaults(string tag, int fontSize, int margin)
    {
        Miko.Core.Element element = tag switch
        {
            "h1" => new H1Element(),
            "h2" => new H2Element(),
            "h3" => new H3Element(),
            "h4" => new H4Element(),
            "h5" => new H5Element(),
            "h6" => new H6Element(),
            _ => throw new ArgumentException("Invalid tag")
        };

        var computed = new StyleResolver().Resolve(element, []);

        computed.Display.ShouldBe(Display.Block);
        computed.FontSize.Value.ShouldBe(fontSize);
        computed.FontWeight.ShouldBe(FontWeight.Bold);
        computed.MarginTop.Value.ShouldBe(margin);
        computed.MarginBottom.Value.ShouldBe(margin);
        computed.MarginLeft.Value.ShouldBe(0);
        computed.MarginRight.Value.ShouldBe(0);
    }

    [Fact]
    public void Resolve_DefaultLineHeight_ShouldBeZero()
    {
        var computed = new StyleResolver().Resolve(new DivElement(), []);

        computed.LineHeight.Value.ShouldBe(0);
        computed.LineHeight.Unit.ShouldBe(LengthUnit.Px);
    }
}
