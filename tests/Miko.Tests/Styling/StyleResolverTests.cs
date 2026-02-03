using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

public class StyleResolverTests
{
    [Fact]
    public void Resolve_WithNoStyles_ShouldReturnDefaultComputedStyle()
    {
        var resolver = new StyleResolver();
        var element = new DivElement();
        var styleSheets = new List<StyleSheet>();

        var computed = resolver.Resolve(element, styleSheets);

        computed.ShouldNotBeNull();
        computed.Display.ShouldBe(Display.Block); // Default for div
    }

    [Fact]
    public void Resolve_WithMatchingClassSelector_ShouldApplyStyle()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "container" };

        var styleSheet = new StyleSheet();
        styleSheet.AddRule(
            new ClassSelector(".container"),
            new Style { BackgroundColor = Color.Red }
        );

        var computed = resolver.Resolve(element, new List<StyleSheet> { styleSheet });

        computed.BackgroundColor.ShouldBe(Color.Red);
    }

    [Fact]
    public void Resolve_WithMultipleMatchingRules_ShouldApplyBySpecificity()
    {
        var resolver = new StyleResolver();
        var element = new DivElement
        {
            Id = "main",
            Class = "container"
        };

        var styleSheet = new StyleSheet();
        styleSheet.AddRule(
            new TagSelector("div"),
            new Style { BackgroundColor = Color.Red }
        );
        styleSheet.AddRule(
            new ClassSelector(".container"),
            new Style { BackgroundColor = Color.Green }
        );
        styleSheet.AddRule(
            new IdSelector("#main"),
            new Style { BackgroundColor = Color.Blue }
        );

        var computed = resolver.Resolve(element, new List<StyleSheet> { styleSheet });

        // ID selector has highest specificity
        computed.BackgroundColor.ShouldBe(Color.Blue);
    }

    [Fact]
    public void Resolve_WithInlineStyle_ShouldOverrideStyleSheet()
    {
        var resolver = new StyleResolver();
        var element = new DivElement
        {
            Class = "container",
            Style = new Style { BackgroundColor = Color.Yellow }
        };

        var styleSheet = new StyleSheet();
        styleSheet.AddRule(
            new ClassSelector(".container"),
            new Style { BackgroundColor = Color.Red }
        );

        var computed = resolver.Resolve(element, new List<StyleSheet> { styleSheet });

        // Inline style has highest priority
        computed.BackgroundColor.ShouldBe(Color.Yellow);
    }

    [Fact]
    public void Resolve_ForH1Element_ShouldApplyDefaultStyles()
    {
        var resolver = new StyleResolver();
        var element = new H1Element { TextContent = "Title" };

        var computed = resolver.Resolve(element, new List<StyleSheet>());

        computed.Display.ShouldBe(Display.Block);
        computed.FontSize.Value.ShouldBe(32);
        computed.FontWeight.ShouldBe(FontWeight.Bold);
        computed.MarginTop.Value.ShouldBe(21);      // Updated to browser default
        computed.MarginBottom.Value.ShouldBe(21);   // Updated to browser default
    }

    [Fact]
    public void Resolve_ForButtonElement_ShouldApplyDefaultStyles()
    {
        var resolver = new StyleResolver();
        var element = new ButtonElement { TextContent = "Click" };

        var computed = resolver.Resolve(element, new List<StyleSheet>());

        computed.Display.ShouldBe(Display.InlineBlock);
        computed.PaddingTop.Value.ShouldBe(2);      // Updated to browser default
        computed.PaddingRight.Value.ShouldBe(6);    // Updated to browser default
        computed.PaddingBottom.Value.ShouldBe(2);   // Updated to browser default
        computed.PaddingLeft.Value.ShouldBe(6);     // Updated to browser default
        computed.BorderTopWidth.Value.ShouldBe(2);     // Updated to browser default
        computed.BorderTopStyle.ShouldBe(BorderStyle.Solid);
    }

    [Fact]
    public void Resolve_WithInheritedProperties_ShouldInheritFromParent()
    {
        var resolver = new StyleResolver();

        // Create parent with custom font
        var parent = new DivElement
        {
            Style = new Style
            {
                FontFamily = "Arial",
                FontSize = Length.Px(20),
                Color = Color.Red
            }
        };

        // Create child
        var child = new SpanElement();
        parent.AddChild(child);

        // Resolve parent style first and set up LayoutBox
        var parentComputed = resolver.Resolve(parent, new List<StyleSheet>());
        parent.LayoutBox = new LayoutBox
        {
            Element = parent,
            ComputedStyle = parentComputed
        };

        // Resolve child style
        var childComputed = resolver.Resolve(child, new List<StyleSheet>());

        // Child should inherit text properties
        childComputed.FontFamily.ShouldBe("Arial");
        childComputed.FontSize.Value.ShouldBe(20);
        childComputed.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void Resolve_WithNonMatchingSelector_ShouldNotApplyStyle()
    {
        var resolver = new StyleResolver();
        var element = new DivElement { Class = "primary" };

        var styleSheet = new StyleSheet();
        styleSheet.AddRule(
            new ClassSelector(".container"),
            new Style { BackgroundColor = Color.Red }
        );

        var computed = resolver.Resolve(element, new List<StyleSheet> { styleSheet });

        computed.BackgroundColor.ShouldBe(Color.Transparent); // Default
    }

    [Fact]
    public void Resolve_ShouldInheritLineHeight()
    {
        var resolver = new StyleResolver();

        var parent = new DivElement
        {
            Style = new Style { LineHeight = Length.Px(24) }
        };

        var child = new SpanElement();
        parent.AddChild(child);

        var parentComputed = resolver.Resolve(parent, new List<StyleSheet>());
        parent.LayoutBox = new LayoutBox
        {
            Element = parent,
            ComputedStyle = parentComputed
        };

        var childComputed = resolver.Resolve(child, new List<StyleSheet>());

        childComputed.LineHeight.Value.ShouldBe(24);
    }

    [Fact]
    public void Resolve_ForParagraph_ShouldApply1emMargin()
    {
        var resolver = new StyleResolver();
        var element = new ParagraphElement();

        var computed = resolver.Resolve(element, new List<StyleSheet>());

        computed.Display.ShouldBe(Display.Block);
        computed.MarginTop.Value.ShouldBe(16);
        computed.MarginBottom.Value.ShouldBe(16);
        computed.MarginLeft.Value.ShouldBe(0);
        computed.MarginRight.Value.ShouldBe(0);
    }

    [Fact]
    public void Resolve_ForImgElement_ShouldApplyInlineDisplayAndNoBorder()
    {
        var resolver = new StyleResolver();
        var element = new ImageElement();

        var computed = resolver.Resolve(element, new List<StyleSheet>());

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
        var resolver = new StyleResolver();
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

        var computed = resolver.Resolve(element, new List<StyleSheet>());

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
        var resolver = new StyleResolver();
        var element = new DivElement();

        var computed = resolver.Resolve(element, new List<StyleSheet>());

        // LineHeight = 0 means "automatic" line-height calculation
        computed.LineHeight.Value.ShouldBe(0);
        computed.LineHeight.Unit.ShouldBe(LengthUnit.Px);
    }
}
