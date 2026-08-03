using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// Out-of-flow boxes (<c>position: absolute</c> / <c>fixed</c>) whose size is determined by opposite
/// insets rather than by content: with <c>width: auto</c> and both <c>left</c> and <c>right</c> set,
/// CSS solves <c>left + width + right = containing block width</c> (likewise for the block axis).
/// Also covers <c>fixed</c> resolving against the viewport rather than the nearest positioned
/// ancestor. Surfaced by the full-screen overlay idiom
/// <c>position: fixed; top/right/bottom/left: 0</c>, which used to collapse to 0×0 (ISSUE-112).
/// </summary>
public class InsetSizedPositioningTests
{
    private const float ViewportW = 800f;
    private const float ViewportH = 600f;

    private readonly LayoutEngine _layoutEngine = new();

    private static StyleSheet Sheet(params StyleRule[] rules) => new() { Rules = rules.ToList() };

    private static StyleRule Rule(string className, Style style) => new()
    {
        Selector = new ClassSelector(className),
        Style = style,
    };

    private static LayoutBox? FindByClass(LayoutBox box, string cls)
    {
        if (box.Element.Class == cls) return box;
        foreach (var child in box.Children)
        {
            var found = FindByClass(child, cls);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>The exact repro from ISSUE-112: a full-screen fixed overlay inside a 500×500 root.</summary>
    [Fact]
    public void FixedOverlay_AllInsetsZero_FillsTheViewport()
    {
        var overlay = new DivElement { Class = "action-sheet" };
        var root = new DivElement { Class = "root" };
        root.AddChild(overlay);

        var sheet = Sheet(
            Rule("root", new Style
            {
                BoxSizing = BoxSizing.BorderBox,
                Width = Length.Px(500),
                Height = Length.Px(500),
            }),
            Rule("action-sheet", new Style
            {
                BoxSizing = BoxSizing.BorderBox,
                Position = Position.Fixed,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                ZIndex = 1000,
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "action-sheet")!;
        box.ShouldNotBeNull();
        // The overlay covers the whole viewport — not the 500×500 root, and not 0×0.
        box.BoxModel.BorderBox.Left.ShouldBe(0f, 0.5f);
        box.BoxModel.BorderBox.Top.ShouldBe(0f, 0.5f);
        box.BoxModel.Content.Width.ShouldBe(ViewportW, 0.5f);
        box.BoxModel.Content.Height.ShouldBe(ViewportH, 0.5f);
    }

    [Fact]
    public void AbsoluteBox_LeftAndRight_WidthSolvedFromContainingBlock()
    {
        // Absolute child of a positioned 400×300 host: left:20 + right:30 leaves 350 of width.
        var abs = new DivElement { Class = "abs" };
        var host = new DivElement { Class = "host" };
        host.AddChild(abs);

        var sheet = Sheet(
            Rule("host", new Style
            {
                Position = Position.Relative,
                Width = Length.Px(400),
                Height = Length.Px(300),
            }),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(20),
                Right = Length.Px(30),
                Top = Length.Px(10),
                Height = Length.Px(50),
            }));

        var layoutRoot = _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        box.BoxModel.Content.Width.ShouldBe(350f, 0.5f);
        box.BoxModel.BorderBox.Left.ShouldBe(20f, 0.5f);
        box.BoxModel.BorderBox.Right.ShouldBe(370f, 0.5f);
        // The untouched axis keeps its explicit height.
        box.BoxModel.Content.Height.ShouldBe(50f, 0.5f);
    }

    [Fact]
    public void AbsoluteBox_TopAndBottom_HeightSolvedFromContainingBlock()
    {
        var abs = new DivElement { Class = "abs" };
        var host = new DivElement { Class = "host" };
        host.AddChild(abs);

        var sheet = Sheet(
            Rule("host", new Style
            {
                Position = Position.Relative,
                Width = Length.Px(400),
                Height = Length.Px(300),
            }),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Top = Length.Px(40),
                Bottom = Length.Px(60),
                Left = Length.Px(0),
                Width = Length.Px(100),
            }));

        var layoutRoot = _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        // 300 - 40 - 60 = 200
        box.BoxModel.Content.Height.ShouldBe(200f, 0.5f);
        box.BoxModel.BorderBox.Top.ShouldBe(40f, 0.5f);
        box.BoxModel.BorderBox.Bottom.ShouldBe(240f, 0.5f);
        box.BoxModel.Content.Width.ShouldBe(100f, 0.5f);
    }

    [Fact]
    public void InsetSizedBox_BorderBox_DeductsPaddingAndBorder()
    {
        // The inset equation solves the margin-box size; padding/border/margin come out of it,
        // so the content box shrinks accordingly (border-box sizing).
        var abs = new DivElement { Class = "abs" };
        var host = new DivElement { Class = "host" };
        host.AddChild(abs);

        var sheet = Sheet(
            Rule("host", new Style
            {
                Position = Position.Relative,
                Width = Length.Px(400),
                Height = Length.Px(300),
            }),
            Rule("abs", new Style
            {
                BoxSizing = BoxSizing.BorderBox,
                Position = Position.Absolute,
                Left = Length.Px(0),
                Right = Length.Px(0),
                Top = Length.Px(0),
                Bottom = Length.Px(0),
                Padding = new Padding(Length.Px(10)),
                Margin = new Margin(Length.Px(5)),
                BorderWidth = Length.Px(2),
            }));

        var layoutRoot = _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        // 400 - (5+5 margin) - (2+2 border) - (10+10 padding) = 366
        box.BoxModel.Content.Width.ShouldBe(366f, 0.5f);
        // 300 - 10 - 4 - 20 = 266
        box.BoxModel.Content.Height.ShouldBe(266f, 0.5f);
        // The margin box exactly spans the containing block.
        box.BoxModel.MarginBox.Left.ShouldBe(0f, 0.5f);
        box.BoxModel.MarginBox.Right.ShouldBe(400f, 0.5f);
    }

    [Fact]
    public void InsetSizedBox_ChildrenLayOutAgainstTheResolvedSize()
    {
        // A percentage-sized child must resolve against the inset-derived size, and it must be
        // positioned inside the final (post-offset) box — the subtree is relaid out, then moved.
        var child = new DivElement { Class = "child" };
        var overlay = new DivElement { Class = "overlay" };
        overlay.AddChild(child);
        var root = new DivElement { Class = "root" };
        root.AddChild(overlay);

        var sheet = Sheet(
            Rule("root", new Style { Width = Length.Px(500), Height = Length.Px(500) }),
            Rule("overlay", new Style
            {
                Position = Position.Fixed,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
            }),
            Rule("child", new Style { Width = Length.Percent(50), Height = Length.Percent(25) }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var childBox = FindByClass(layoutRoot, "child")!;
        childBox.BoxModel.Content.Width.ShouldBe(ViewportW / 2f, 0.5f);
        childBox.BoxModel.Content.Height.ShouldBe(ViewportH / 4f, 0.5f);
        // Sits at the overlay's origin, i.e. the viewport origin.
        childBox.BoxModel.BorderBox.Left.ShouldBe(0f, 0.5f);
        childBox.BoxModel.BorderBox.Top.ShouldBe(0f, 0.5f);
    }

    [Fact]
    public void FixedBox_IgnoresPositionedAncestor_AndUsesTheViewport()
    {
        // A relative ancestor is the containing block for `absolute` descendants but never for
        // `fixed` ones, which always resolve against the viewport.
        var fixedBox = new DivElement { Class = "fixed" };
        var absBox = new DivElement { Class = "abs" };
        var host = new DivElement { Class = "host" };
        host.AddChild(absBox);
        host.AddChild(fixedBox);
        var root = new DivElement { Class = "root" };
        root.AddChild(host);

        var sheet = Sheet(
            Rule("root", new Style { Width = Length.Px(500), Height = Length.Px(500) }),
            Rule("host", new Style
            {
                Position = Position.Relative,
                Margin = new Margin(Length.Px(100)),
                Width = Length.Px(200),
                Height = Length.Px(200),
            }),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Top = Length.Px(0), Right = Length.Px(0),
                Bottom = Length.Px(0), Left = Length.Px(0),
            }),
            Rule("fixed", new Style
            {
                Position = Position.Fixed,
                Top = Length.Px(0), Right = Length.Px(0),
                Bottom = Length.Px(0), Left = Length.Px(0),
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        // absolute → the relative host's padding box (200×200 at 100,100).
        var abs = FindByClass(layoutRoot, "abs")!;
        abs.BoxModel.Content.Width.ShouldBe(200f, 0.5f);
        abs.BoxModel.Content.Height.ShouldBe(200f, 0.5f);
        abs.BoxModel.BorderBox.Left.ShouldBe(100f, 0.5f);
        abs.BoxModel.BorderBox.Top.ShouldBe(100f, 0.5f);

        // fixed → the viewport, unaffected by the same positioned ancestor.
        var fix = FindByClass(layoutRoot, "fixed")!;
        fix.BoxModel.Content.Width.ShouldBe(ViewportW, 0.5f);
        fix.BoxModel.Content.Height.ShouldBe(ViewportH, 0.5f);
        fix.BoxModel.BorderBox.Left.ShouldBe(0f, 0.5f);
        fix.BoxModel.BorderBox.Top.ShouldBe(0f, 0.5f);
    }

    [Fact]
    public void FixedBox_WithOffsetInsets_PinsToTheViewportEdges()
    {
        // A bottom sheet: pinned to the bottom of the viewport, full width, 200px tall.
        var sheetBox = new DivElement { Class = "bottom-sheet" };
        var root = new DivElement { Class = "root" };
        root.AddChild(sheetBox);

        var sheet = Sheet(
            Rule("root", new Style { Width = Length.Px(500), Height = Length.Px(500) }),
            Rule("bottom-sheet", new Style
            {
                Position = Position.Fixed,
                Left = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Height = Length.Px(200),
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "bottom-sheet")!;
        box.BoxModel.Content.Width.ShouldBe(ViewportW, 0.5f);
        box.BoxModel.Content.Height.ShouldBe(200f, 0.5f);
        box.BoxModel.BorderBox.Bottom.ShouldBe(ViewportH, 0.5f);
        box.BoxModel.BorderBox.Top.ShouldBe(ViewportH - 200f, 0.5f);
    }

    [Fact]
    public void SingleInset_StillShrinksToFit()
    {
        // Only one inset per axis → no over-constraint, so the box keeps CSS shrink-to-fit sizing
        // (the AbsoluteShrinkToFitTests contract must not regress).
        var inner = new DivElement { Class = "inner" };
        var abs = new DivElement { Class = "abs" };
        abs.AddChild(inner);
        var root = new DivElement { Class = "root" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("root", new Style { Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300) }),
            Rule("abs", new Style { Position = Position.Absolute, Left = Length.Px(10), Top = Length.Px(10) }),
            Rule("inner", new Style { Width = Length.Px(56), Height = Length.Px(56) }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        box.BoxModel.Content.Width.ShouldBe(56f, 0.5f);
        box.BoxModel.Content.Height.ShouldBe(56f, 0.5f);
    }

    [Fact]
    public void ExplicitSize_WinsOverOppositeInsets()
    {
        // Over-constrained with an explicit width: CSS ignores `right` (in LTR) and keeps the
        // declared width anchored at `left`.
        var abs = new DivElement { Class = "abs" };
        var root = new DivElement { Class = "root" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("root", new Style { Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300) }),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(10),
                Right = Length.Px(10),
                Width = Length.Px(120),
                Top = Length.Px(0),
                Bottom = Length.Px(0),
                Height = Length.Px(80),
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        box.BoxModel.Content.Width.ShouldBe(120f, 0.5f);
        box.BoxModel.Content.Height.ShouldBe(80f, 0.5f);
        box.BoxModel.BorderBox.Left.ShouldBe(10f, 0.5f);
    }

    [Fact]
    public void InsetSizedBox_InsideFlexParent_UsesTheContainingBlockNotTheFlexLine()
    {
        // Out-of-flow boxes are not flex items; the overlay must still resolve against the
        // positioned host, independent of the flex container's own content box.
        var overlay = new DivElement { Class = "overlay" };
        var item = new DivElement { Class = "item" };
        var host = new DivElement { Class = "host" };
        host.AddChild(item);
        host.AddChild(overlay);

        var sheet = Sheet(
            Rule("host", new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                Position = Position.Relative,
                Width = Length.Px(400),
                Height = Length.Px(300),
            }),
            Rule("item", new Style { Width = Length.Px(100), Height = Length.Px(100) }),
            Rule("overlay", new Style
            {
                Position = Position.Absolute,
                Top = Length.Px(0), Right = Length.Px(0),
                Bottom = Length.Px(0), Left = Length.Px(0),
            }));

        var layoutRoot = _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "overlay")!;
        box.BoxModel.Content.Width.ShouldBe(400f, 0.5f);
        box.BoxModel.Content.Height.ShouldBe(300f, 0.5f);
        // The in-flow sibling is unaffected by the out-of-flow overlay.
        FindByClass(layoutRoot, "item")!.BoxModel.Content.Width.ShouldBe(100f, 0.5f);
    }

    [Fact]
    public void NestedAbsolute_UsesTheInsetSizedAncestorAsItsContainingBlock()
    {
        // An absolute box nested in an inset-sized fixed overlay: the overlay is resized and moved
        // before its padding box becomes the child's containing block, so the inner box must span
        // the resolved overlay, not the pre-relayout 0×0 one.
        var inner = new DivElement { Class = "inner" };
        var overlay = new DivElement { Class = "overlay" };
        overlay.AddChild(inner);
        var root = new DivElement { Class = "root" };
        root.AddChild(overlay);

        var sheet = Sheet(
            Rule("root", new Style { Width = Length.Px(500), Height = Length.Px(500) }),
            Rule("overlay", new Style
            {
                Position = Position.Fixed,
                Top = Length.Px(0), Right = Length.Px(0),
                Bottom = Length.Px(0), Left = Length.Px(0),
            }),
            Rule("inner", new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(10), Right = Length.Px(10),
                Top = Length.Px(20), Bottom = Length.Px(20),
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var innerBox = FindByClass(layoutRoot, "inner")!;
        innerBox.BoxModel.Content.Width.ShouldBe(ViewportW - 20f, 0.5f);
        innerBox.BoxModel.Content.Height.ShouldBe(ViewportH - 40f, 0.5f);
        innerBox.BoxModel.BorderBox.Left.ShouldBe(10f, 0.5f);
        innerBox.BoxModel.BorderBox.Top.ShouldBe(20f, 0.5f);
    }

    // ---- auto margins on out-of-flow boxes (CSS 10.3.7 / 10.6.4) ----
    // When both insets AND the size are non-auto, the inset equation is over-constrained and the
    // leftover space goes to the axis's auto margins: both auto → split evenly (the idiomatic
    // absolute-centering trick), one auto → that side absorbs it all.

    [Fact]
    public void AutoMargins_BothSides_CenterTheBoxInTheContainingBlock()
    {
        var abs = new DivElement { Class = "abs" };
        var root = new DivElement { Class = "root" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("root", new Style { Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300) }),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(0), Right = Length.Px(0),
                Top = Length.Px(0), Bottom = Length.Px(0),
                Width = Length.Px(200), Height = Length.Px(100),
                Margin = new Margin(Length.Auto),
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        // Explicit size is kept; the leftover 200×200 is split evenly on both axes.
        box.BoxModel.Content.Width.ShouldBe(200f, 0.5f);
        box.BoxModel.Content.Height.ShouldBe(100f, 0.5f);
        box.BoxModel.BorderBox.Left.ShouldBe(100f, 0.5f);
        box.BoxModel.BorderBox.Right.ShouldBe(300f, 0.5f);
        box.BoxModel.BorderBox.Top.ShouldBe(100f, 0.5f);
        box.BoxModel.BorderBox.Bottom.ShouldBe(200f, 0.5f);
    }

    [Fact]
    public void AutoMargins_CenterAgainstTheViewport_ForFixedBoxes()
    {
        // The classic centered modal: fixed + all insets 0 + explicit size + margin auto.
        // A positioned ancestor must not shift it — fixed centers on the viewport.
        var modal = new DivElement { Class = "modal" };
        var host = new DivElement { Class = "host" };
        host.AddChild(modal);
        var root = new DivElement { Class = "root" };
        root.AddChild(host);

        var sheet = Sheet(
            Rule("root", new Style { Width = Length.Px(500), Height = Length.Px(500) }),
            Rule("host", new Style
            {
                Position = Position.Relative,
                Margin = new Margin(Length.Px(100)),
                Width = Length.Px(200), Height = Length.Px(200),
            }),
            Rule("modal", new Style
            {
                Position = Position.Fixed,
                Top = Length.Px(0), Right = Length.Px(0),
                Bottom = Length.Px(0), Left = Length.Px(0),
                Width = Length.Px(300), Height = Length.Px(200),
                Margin = new Margin(Length.Auto),
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "modal")!;
        box.BoxModel.Content.Width.ShouldBe(300f, 0.5f);
        box.BoxModel.Content.Height.ShouldBe(200f, 0.5f);
        box.BoxModel.BorderBox.Left.ShouldBe((ViewportW - 300f) / 2f, 0.5f);
        box.BoxModel.BorderBox.Top.ShouldBe((ViewportH - 200f) / 2f, 0.5f);
    }

    [Fact]
    public void AutoMargin_OnOneSideOnly_AbsorbsAllTheLeftoverSpace()
    {
        var abs = new DivElement { Class = "abs" };
        var root = new DivElement { Class = "root" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("root", new Style { Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300) }),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(0), Right = Length.Px(0),
                Top = Length.Px(0), Bottom = Length.Px(0),
                Width = Length.Px(100), Height = Length.Px(80),
                // margin-left auto → pushed to the right edge; margin-top auto → pushed to the bottom.
                MarginLeft = Length.Auto,
                MarginRight = Length.Px(0),
                MarginTop = Length.Auto,
                MarginBottom = Length.Px(0),
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        box.BoxModel.BorderBox.Right.ShouldBe(400f, 0.5f);
        box.BoxModel.BorderBox.Left.ShouldBe(300f, 0.5f);
        box.BoxModel.BorderBox.Bottom.ShouldBe(300f, 0.5f);
        box.BoxModel.BorderBox.Top.ShouldBe(220f, 0.5f);
    }

    [Fact]
    public void AutoMargins_AccountForTheNonAutoSideAndTheInsets()
    {
        // Leftover = cb - left - right - borderBoxWidth - fixed margin, and only the auto side
        // takes it. 400 - 20 - 30 - 100 - 50 = 200 goes to the auto margin-left.
        var abs = new DivElement { Class = "abs" };
        var root = new DivElement { Class = "root" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("root", new Style { Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300) }),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(20), Right = Length.Px(30),
                Top = Length.Px(0),
                Width = Length.Px(100), Height = Length.Px(50),
                MarginLeft = Length.Auto,
                MarginRight = Length.Px(50),
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        // margin box starts at left:20, then 200 of auto margin, so the border box starts at 220.
        box.BoxModel.BorderBox.Left.ShouldBe(220f, 0.5f);
        box.BoxModel.BorderBox.Right.ShouldBe(320f, 0.5f);
        // Right edge + the fixed 50 margin lands exactly on the `right:30` inset line.
        (box.BoxModel.BorderBox.Right + 50f).ShouldBe(400f - 30f, 0.5f);
    }

    [Fact]
    public void AutoMargins_WithBorderBoxSizing_CenterOnTheBorderBox()
    {
        // Centering must use the border box, so padding/border do not skew the offset.
        var abs = new DivElement { Class = "abs" };
        var root = new DivElement { Class = "root" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("root", new Style { Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300) }),
            Rule("abs", new Style
            {
                BoxSizing = BoxSizing.BorderBox,
                Position = Position.Absolute,
                Left = Length.Px(0), Right = Length.Px(0),
                Top = Length.Px(0), Bottom = Length.Px(0),
                Width = Length.Px(200), Height = Length.Px(100),
                Padding = new Padding(Length.Px(10)),
                BorderWidth = Length.Px(5),
                Margin = new Margin(Length.Auto),
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        // border-box:200 wide → content 200-2*5-2*10 = 170.
        box.BoxModel.Content.Width.ShouldBe(170f, 0.5f);
        box.BoxModel.BorderBox.Width.ShouldBe(200f, 0.5f);
        box.BoxModel.BorderBox.Left.ShouldBe(100f, 0.5f);
        box.BoxModel.BorderBox.Top.ShouldBe(100f, 0.5f);
    }

    [Fact]
    public void AutoMargins_WithoutOppositeInsets_DoNotShiftTheBox()
    {
        // Only `left` is set → the equation is not over-constrained, so there is no leftover space
        // for the auto margins to absorb; the box stays anchored at `left`.
        var abs = new DivElement { Class = "abs" };
        var root = new DivElement { Class = "root" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("root", new Style { Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300) }),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(30),
                Top = Length.Px(40),
                Width = Length.Px(100), Height = Length.Px(50),
                Margin = new Margin(Length.Auto),
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        box.BoxModel.BorderBox.Left.ShouldBe(30f, 0.5f);
        box.BoxModel.BorderBox.Top.ShouldBe(40f, 0.5f);
        box.BoxModel.Content.Width.ShouldBe(100f, 0.5f);
    }

    [Fact]
    public void AutoMargins_WithAutoSize_StayZero_InsetsStillDrive()
    {
        // width:auto + both insets → the size comes from the equation (no leftover), so the auto
        // margins must not steal space and shrink the box.
        var abs = new DivElement { Class = "abs" };
        var root = new DivElement { Class = "root" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("root", new Style { Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300) }),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(0), Right = Length.Px(0),
                Top = Length.Px(0), Bottom = Length.Px(0),
                Margin = new Margin(Length.Auto),
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        box.BoxModel.Content.Width.ShouldBe(400f, 0.5f);
        box.BoxModel.Content.Height.ShouldBe(300f, 0.5f);
        box.BoxModel.BorderBox.Left.ShouldBe(0f, 0.5f);
        box.BoxModel.BorderBox.Top.ShouldBe(0f, 0.5f);
    }

    [Fact]
    public void AutoMargins_WithOversizedBox_ClampToZero()
    {
        // Negative leftover: the box is wider than the space between the insets. CSS treats the
        // auto margins as 0 rather than pulling the box back.
        var abs = new DivElement { Class = "abs" };
        var root = new DivElement { Class = "root" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("root", new Style { Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300) }),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(0), Right = Length.Px(0),
                Top = Length.Px(0), Bottom = Length.Px(0),
                Width = Length.Px(600), Height = Length.Px(50),
                Margin = new Margin(Length.Auto),
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        box.BoxModel.BorderBox.Left.ShouldBe(0f, 0.5f);
        box.BoxModel.Content.Width.ShouldBe(600f, 0.5f);
    }

    [Fact]
    public void AutoMargins_InsideAFlexParent_CenterOnTheContainingBlock()
    {
        // Out-of-flow boxes are not flex items, so the flex container's own auto-margin handling
        // must not interfere: centering is against the positioned host.
        var overlay = new DivElement { Class = "overlay" };
        var item = new DivElement { Class = "item" };
        var host = new DivElement { Class = "host" };
        host.AddChild(item);
        host.AddChild(overlay);

        var sheet = Sheet(
            Rule("host", new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                Position = Position.Relative,
                Width = Length.Px(400), Height = Length.Px(300),
            }),
            Rule("item", new Style { Width = Length.Px(100), Height = Length.Px(100) }),
            Rule("overlay", new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(0), Right = Length.Px(0),
                Top = Length.Px(0), Bottom = Length.Px(0),
                Width = Length.Px(200), Height = Length.Px(100),
                Margin = new Margin(Length.Auto),
            }));

        var layoutRoot = _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "overlay")!;
        box.BoxModel.BorderBox.Left.ShouldBe(100f, 0.5f);
        box.BoxModel.BorderBox.Top.ShouldBe(100f, 0.5f);
        FindByClass(layoutRoot, "item")!.BoxModel.Content.Width.ShouldBe(100f, 0.5f);
    }

    [Fact]
    public void AutoMargins_CenterOnlyTheDeclaredAxis()
    {
        // margin: 0 auto → horizontal centering only; the block axis stays anchored at `top`.
        var abs = new DivElement { Class = "abs" };
        var root = new DivElement { Class = "root" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("root", new Style { Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300) }),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(0), Right = Length.Px(0),
                Top = Length.Px(0), Bottom = Length.Px(0),
                Width = Length.Px(200), Height = Length.Px(100),
                MarginLeft = Length.Auto,
                MarginRight = Length.Auto,
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(0),
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        box.BoxModel.BorderBox.Left.ShouldBe(100f, 0.5f);
        box.BoxModel.BorderBox.Top.ShouldBe(0f, 0.5f);
    }

    [Fact]
    public void AutoMargins_CenteredBox_ChildrenFollowTheOffset()
    {
        // The subtree must move with the box: a child sits at the centered origin, not the
        // pre-offset one.
        var inner = new DivElement { Class = "inner" };
        var abs = new DivElement { Class = "abs" };
        abs.AddChild(inner);
        var root = new DivElement { Class = "root" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("root", new Style { Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300) }),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(0), Right = Length.Px(0),
                Top = Length.Px(0), Bottom = Length.Px(0),
                Width = Length.Px(200), Height = Length.Px(100),
                Margin = new Margin(Length.Auto),
            }),
            Rule("inner", new Style { Width = Length.Percent(50), Height = Length.Px(20) }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var innerBox = FindByClass(layoutRoot, "inner")!;
        innerBox.BoxModel.Content.Width.ShouldBe(100f, 0.5f);
        innerBox.BoxModel.BorderBox.Left.ShouldBe(100f, 0.5f);
        innerBox.BoxModel.BorderBox.Top.ShouldBe(100f, 0.5f);
    }

    [Fact]
    public void PercentInsets_ResolveAgainstTheContainingBlock()
    {
        var abs = new DivElement { Class = "abs" };
        var root = new DivElement { Class = "root" };
        root.AddChild(abs);

        var sheet = Sheet(
            Rule("root", new Style { Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300) }),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Left = Length.Percent(10),
                Right = Length.Percent(10),
                Top = Length.Percent(10),
                Bottom = Length.Percent(10),
            }));

        var layoutRoot = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        // 400 - 40 - 40 = 320 ; 300 - 30 - 30 = 240
        box.BoxModel.Content.Width.ShouldBe(320f, 0.5f);
        box.BoxModel.Content.Height.ShouldBe(240f, 0.5f);
        box.BoxModel.BorderBox.Left.ShouldBe(40f, 0.5f);
        box.BoxModel.BorderBox.Top.ShouldBe(30f, 0.5f);
    }
}
