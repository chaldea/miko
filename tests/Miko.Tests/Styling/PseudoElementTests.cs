using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Styling;

public class PseudoElementTests
{
    [Fact]
    public void After_ShouldCreatePseudoElementRule()
    {
        var sheet = new StyleSheet();
        sheet.AddRule(Style.Class("box")
            .After()
            .Set(x => x.Content, "")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.Width, Length.Px(100))
            .Set(x => x.Height, Length.Px(100)));

        sheet.PseudoElementRules.Count.ShouldBe(1);
        sheet.PseudoElementRules[0].Type.ShouldBe(PseudoElementType.After);
        sheet.PseudoElementRules[0].Style.Content.ShouldBe("");
        sheet.PseudoElementRules[0].Style.Width.ShouldBe(Length.Px(100));
    }

    [Fact]
    public void Before_ShouldCreatePseudoElementRule()
    {
        var sheet = new StyleSheet();
        sheet.AddRule(Style.Class("box")
            .Before()
            .Set(x => x.Content, "Hello")
            .Set(x => x.Display, Display.InlineBlock));

        sheet.PseudoElementRules.Count.ShouldBe(1);
        sheet.PseudoElementRules[0].Type.ShouldBe(PseudoElementType.Before);
        sheet.PseudoElementRules[0].Style.Content.ShouldBe("Hello");
    }

    [Fact]
    public void After_ShouldMatchElementByClass()
    {
        var sheet = new StyleSheet();
        sheet.AddRule(Style.Class("box")
            .After()
            .Set(x => x.Content, "")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.Width, Length.Px(50))
            .Set(x => x.Height, Length.Px(50)));

        var rule = sheet.PseudoElementRules[0];
        var matchingElement = new DivElement { Class = "box" };
        var nonMatchingElement = new DivElement { Class = "other" };

        rule.Selector.Matches(matchingElement).ShouldBeTrue();
        rule.Selector.Matches(nonMatchingElement).ShouldBeFalse();
    }

    [Fact]
    public void Layout_ShouldInjectAfterPseudoElement()
    {
        var root = new DivElement { Class = "box" };
        var sheet = new StyleSheet();
        sheet.AddRule(Style.Class("box")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.Width, Length.Px(200)));
        sheet.AddRule(Style.Class("box")
            .After()
            .Set(x => x.Content, "")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.Width, Length.Px(100))
            .Set(x => x.Height, Length.Px(50)));

        var engine = new LayoutEngine();
        var layoutRoot = engine.Layout(root, [sheet], 800, 600);

        layoutRoot.Children.Count.ShouldBe(1);
        layoutRoot.Children[0].Element.ShouldBeOfType<PseudoElement>();
        layoutRoot.Children[0].ComputedStyle.Width.ShouldBe(Length.Px(100));
        layoutRoot.Children[0].ComputedStyle.Height.ShouldBe(Length.Px(50));
    }

    [Fact]
    public void Layout_ShouldInjectBeforePseudoElement()
    {
        var root = new DivElement { Class = "container" };
        var child = new SpanElement { TextContent = "Hello" };
        root.AddChild(child);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Class("container")
            .Set(x => x.Display, Display.Block));
        sheet.AddRule(Style.Class("container")
            .Before()
            .Set(x => x.Content, "Before")
            .Set(x => x.Display, Display.Block));

        var engine = new LayoutEngine();
        var layoutRoot = engine.Layout(root, [sheet], 800, 600);

        layoutRoot.Children.Count.ShouldBe(2);
        layoutRoot.Children[0].Element.ShouldBeOfType<PseudoElement>();
        layoutRoot.Children[0].Element.TextContent.ShouldBe("Before");
        layoutRoot.Children[1].Element.ShouldBe(child);
    }

    [Fact]
    public void Layout_ShouldInjectBothBeforeAndAfter()
    {
        var root = new DivElement { Class = "box" };
        var child = new SpanElement { TextContent = "Content" };
        root.AddChild(child);

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Class("box")
            .Set(x => x.Display, Display.Block));
        sheet.AddRule(Style.Class("box")
            .Before()
            .Set(x => x.Content, "<<")
            .Set(x => x.Display, Display.Inline));
        sheet.AddRule(Style.Class("box")
            .After()
            .Set(x => x.Content, ">>")
            .Set(x => x.Display, Display.Inline));

        var engine = new LayoutEngine();
        var layoutRoot = engine.Layout(root, [sheet], 800, 600);

        layoutRoot.Children.Count.ShouldBe(3);
        layoutRoot.Children[0].Element.TextContent.ShouldBe("<<");
        layoutRoot.Children[1].Element.ShouldBe(child);
        layoutRoot.Children[2].Element.TextContent.ShouldBe(">>");
    }

    [Fact]
    public void Layout_ShouldNotInjectWhenSelectorDoesNotMatch()
    {
        var root = new DivElement { Class = "other" };

        var sheet = new StyleSheet();
        sheet.AddRule(Style.Class("other")
            .Set(x => x.Display, Display.Block));
        sheet.AddRule(Style.Class("box")
            .After()
            .Set(x => x.Content, "")
            .Set(x => x.Display, Display.Block));

        var engine = new LayoutEngine();
        var layoutRoot = engine.Layout(root, [sheet], 800, 600);

        layoutRoot.Children.Count.ShouldBe(0);
    }

    [Fact]
    public void Style_Content_ShouldMerge()
    {
        var style1 = new Style();
        var style2 = new Style { Content = "hello" };

        style1.Merge(style2);

        style1.Content.ShouldBe("hello");
    }

    [Fact]
    public void Style_Content_ShouldNotOverrideExisting()
    {
        var style1 = new Style { Content = "first" };
        var style2 = new Style { Content = "second" };

        style1.Merge(style2);

        style1.Content.ShouldBe("first");
    }
}
