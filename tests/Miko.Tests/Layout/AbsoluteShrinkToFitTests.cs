using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// Absolutely-positioned elements with <c>width: auto</c> must shrink-to-fit their content (CSS
/// shrink-to-fit sizing), not stretch to the parent's available width. Surfaced by porting Ionic's
/// <c>ion-fab</c>, whose <c>width: fit-content</c> container drives its centering / edge positioning.
/// </summary>
public class AbsoluteShrinkToFitTests
{
    private readonly LayoutEngine _layoutEngine = new();

    private static StyleSheet Sheet(params StyleRule[] rules) => new() { Rules = rules.ToList() };

    private static StyleRule Rule(string className, Style style) => new()
    {
        Selector = new ClassSelector(className),
        Style = style,
    };

    [Fact]
    public void AbsoluteChild_WidthAuto_ShrinksToContentWidth()
    {
        // A wide block parent with an absolutely-positioned child (width auto) that contains a fixed
        // 56px box. The absolute child should size to the 56px content, not the parent's 800px.
        var inner = new DivElement { Class = "inner" };
        var abs = new DivElement { Class = "abs" };
        abs.AddChild(inner);
        var root = new DivElement { Class = "parent" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("parent", new Style { Display = Display.Block, Width = Length.Px(800) }),
            Rule("abs", new Style { Display = Display.Block, Position = Position.Absolute }),
            Rule("inner", new Style { Display = Display.Block, Width = Length.Px(56), Height = Length.Px(56) }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        var absBox = layoutRoot.Children[0];
        absBox.BoxModel.Content.Width.ShouldBe(56f, 0.5f);
    }

    [Fact]
    public void AbsoluteChild_ExplicitWidth_IsHonored()
    {
        // An absolute child with an explicit (non-auto) width keeps that width — the shrink-to-fit
        // path only applies to width:auto.
        var abs = new DivElement { Class = "abs" };
        var root = new DivElement { Class = "parent" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("parent", new Style { Display = Display.Block, Width = Length.Px(800) }),
            Rule("abs", new Style { Display = Display.Block, Position = Position.Absolute, Width = Length.Px(120) }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        layoutRoot.Children[0].BoxModel.Content.Width.ShouldBe(120f, 0.5f);
    }

    [Fact]
    public void AbsoluteChild_PercentWidth_ResolvesAgainstParent()
    {
        // A percentage width on an absolute child still resolves against the parent's content width
        // (the shrink-to-fit change must not break percentage sizing).
        var abs = new DivElement { Class = "abs" };
        var root = new DivElement { Class = "parent" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("parent", new Style { Display = Display.Block, Width = Length.Px(800) }),
            Rule("abs", new Style { Display = Display.Block, Position = Position.Absolute, Width = Length.Percent(50) }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        layoutRoot.Children[0].BoxModel.Content.Width.ShouldBe(400f, 0.5f);
    }
}
