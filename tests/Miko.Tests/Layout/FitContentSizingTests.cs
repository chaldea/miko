using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// <c>width/height: fit-content</c> — a size that comes from the content, like <c>auto</c>, but is a
/// DEFINITE size: opposite insets never take it over, so an out-of-flow box stays shrink-wrapped and
/// its <c>margin: auto</c> has leftover space to center with.
/// <para>
/// Also covers the sibling defect this surfaced: when only ONE axis of an out-of-flow box is sized
/// by its insets, the other axis must keep the shrink-to-fit sizing the regular-flow pass gave it,
/// rather than silently filling the containing block.
/// </para>
/// <para>
/// Both come from Ionic's <c>ion-fab</c> (issues/ion-fab.md problem 1), whose host is
/// <c>position:absolute; width:fit-content; height:fit-content</c> and whose centering rules pin
/// both edges of an axis and hand the leftover to auto margins. With <c>auto</c> the host instead
/// stretched across the whole content area — parking its button in the corner and, at
/// <c>z-index:1000</c>, swallowing every pointer event aimed at the other fabs.
/// </para>
/// </summary>
public class FitContentSizingTests
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

    // Host (400×300, positioned) > abs (the box under test) > inner (a fixed 56×56 content box).
    private static (DivElement Host, DivElement Abs) BuildHost()
    {
        var inner = new DivElement { Class = "inner" };
        var abs = new DivElement { Class = "abs" };
        abs.AddChild(inner);
        var host = new DivElement { Class = "host" };
        host.AddChild(abs);
        return (host, abs);
    }

    private static StyleRule HostRule() => Rule("host", new Style
    {
        Position = Position.Relative,
        Width = Length.Px(400),
        Height = Length.Px(300),
    });

    private static StyleRule InnerRule() => Rule("inner", new Style
    {
        Width = Length.Px(56),
        Height = Length.Px(56),
    });

    // ---- fit-content is a definite size, not "auto" ----

    [Fact]
    public void FitContent_WithOppositeInsets_StaysShrinkWrapped()
    {
        // The distinguishing case: width:auto here would be solved from left+right and stretch to
        // 400; fit-content keeps the 56px content size.
        var (host, _) = BuildHost();

        var sheet = Sheet(
            HostRule(),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Width = Length.FitContent,
                Height = Length.FitContent,
                Left = Length.Px(0),
                Right = Length.Px(0),
                Top = Length.Px(0),
                Bottom = Length.Px(0),
            }),
            InnerRule());

        var layoutRoot = _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        box.BoxModel.Content.Width.ShouldBe(56f, 0.5f);
        box.BoxModel.Content.Height.ShouldBe(56f, 0.5f);
    }

    [Fact]
    public void FitContent_WithOppositeInsetsAndAutoMargins_CentersInTheContainingBlock()
    {
        // The ion-fab centering idiom: fit-content + all insets 0 + margin auto. The size stays at
        // the content, so the equation is over-constrained and the leftover splits evenly.
        var (host, _) = BuildHost();

        var sheet = Sheet(
            HostRule(),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Width = Length.FitContent,
                Height = Length.FitContent,
                Left = Length.Px(0),
                Right = Length.Px(0),
                Top = Length.Px(0),
                Bottom = Length.Px(0),
                Margin = new Margin(Length.Auto),
            }),
            InnerRule());

        var layoutRoot = _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        box.BoxModel.BorderBox.Left.ShouldBe((400f - 56f) / 2f, 0.5f);
        box.BoxModel.BorderBox.Top.ShouldBe((300f - 56f) / 2f, 0.5f);
        box.BoxModel.Content.Width.ShouldBe(56f, 0.5f);
        box.BoxModel.Content.Height.ShouldBe(56f, 0.5f);
    }

    [Fact]
    public void FitContent_CentersOnOneAxisWhileTheOtherIsPinned()
    {
        // The other reported fab: horizontal="end" vertical="center" — pinned to the right edge,
        // centered vertically. Both axes must stay 56px wide/tall.
        var (host, _) = BuildHost();

        var sheet = Sheet(
            HostRule(),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Width = Length.FitContent,
                Height = Length.FitContent,
                Right = Length.Px(10),
                Top = Length.Px(0),
                Bottom = Length.Px(0),
                MarginTop = Length.Auto,
                MarginBottom = Length.Auto,
            }),
            InnerRule());

        var layoutRoot = _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        box.BoxModel.Content.Width.ShouldBe(56f, 0.5f);
        box.BoxModel.BorderBox.Right.ShouldBe(400f - 10f, 0.5f);
        box.BoxModel.BorderBox.Top.ShouldBe((300f - 56f) / 2f, 0.5f);
    }

    [Fact]
    public void FitContent_InRegularFlow_ShrinksInsteadOfFillingTheParent()
    {
        // In-flow block-level boxes fill the parent when width:auto; fit-content must not.
        var inner = new DivElement { Class = "inner" };
        var block = new DivElement { Class = "block" };
        block.AddChild(inner);
        var host = new DivElement { Class = "host" };
        host.AddChild(block);

        var sheet = Sheet(
            HostRule(),
            Rule("block", new Style { Display = Display.Block, Width = Length.FitContent }),
            InnerRule());

        var layoutRoot = _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        FindByClass(layoutRoot, "block")!.BoxModel.Content.Width.ShouldBe(56f, 0.5f);
    }

    [Fact]
    public void FitContent_InRegularFlow_AutoMarginsCenterIt()
    {
        // `width:fit-content; margin:0 auto` centers in the parent — the leftover is real because
        // the box no longer fills.
        var inner = new DivElement { Class = "inner" };
        var block = new DivElement { Class = "block" };
        block.AddChild(inner);
        var host = new DivElement { Class = "host" };
        host.AddChild(block);

        var sheet = Sheet(
            HostRule(),
            Rule("block", new Style
            {
                Display = Display.Block,
                Width = Length.FitContent,
                MarginLeft = Length.Auto,
                MarginRight = Length.Auto,
            }),
            InnerRule());

        var layoutRoot = _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        FindByClass(layoutRoot, "block")!.BoxModel.BorderBox.Left.ShouldBe((400f - 56f) / 2f, 0.5f);
    }

    [Fact]
    public void FitContent_ReportsAsAuto_SoLayoutPathsTreatItAsContentSized()
    {
        // fit-content deliberately answers IsAuto — every layout algorithm's "measure the content"
        // branch keys off that, and only the out-of-flow sizing rules need the distinction.
        Length.FitContent.IsAuto.ShouldBeTrue();
        Length.FitContent.IsFitContent.ShouldBeTrue();
        Length.Auto.IsFitContent.ShouldBeFalse();
        Length.Px(10).IsFitContent.ShouldBeFalse();
        Length.FitContent.ToString().ShouldBe("fit-content");
    }

    // ---- the untouched axis keeps shrink-to-fit ----

    [Fact]
    public void InsetSizedHeight_LeavesTheWidthShrinkWrapped()
    {
        // width:auto with only ONE horizontal inset is shrink-to-fit. Resolving the height from
        // top+bottom must not quietly turn the width into "fill the containing block".
        var (host, _) = BuildHost();

        var sheet = Sheet(
            HostRule(),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(20),
                Top = Length.Px(0),
                Bottom = Length.Px(0),
            }),
            InnerRule());

        var layoutRoot = _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        box.BoxModel.Content.Width.ShouldBe(56f, 0.5f);   // not 400
        box.BoxModel.Content.Height.ShouldBe(300f, 0.5f); // solved from top+bottom
        box.BoxModel.BorderBox.Left.ShouldBe(20f, 0.5f);
    }

    [Fact]
    public void InsetSizedHeight_WithRightInset_AnchorsTheShrinkWrappedBoxToTheRightEdge()
    {
        // The exact `horizontal="end" vertical="center"` shape before fit-content entered: a
        // stretched width also drags the right-anchored box off the left edge (it was landing at a
        // negative x), so pin both the size and the position.
        var (host, _) = BuildHost();

        var sheet = Sheet(
            HostRule(),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Right = Length.Px(10),
                Top = Length.Px(0),
                Bottom = Length.Px(0),
            }),
            InnerRule());

        var layoutRoot = _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        var box = FindByClass(layoutRoot, "abs")!;
        box.BoxModel.Content.Width.ShouldBe(56f, 0.5f);
        box.BoxModel.BorderBox.Right.ShouldBe(400f - 10f, 0.5f);
        box.BoxModel.BorderBox.Left.ShouldBe(400f - 10f - 56f, 0.5f);
    }

    [Fact]
    public void InsetSizedHeight_KeepsAnExplicitPercentWidthResolvingAgainstTheContainingBlock()
    {
        // The shrink-to-fit relay must only apply to auto widths: a declared percentage still needs
        // the containing block as its basis.
        var (host, _) = BuildHost();

        var sheet = Sheet(
            HostRule(),
            Rule("abs", new Style
            {
                Position = Position.Absolute,
                Width = Length.Percent(25),
                Left = Length.Px(0),
                Top = Length.Px(0),
                Bottom = Length.Px(0),
            }),
            InnerRule());

        var layoutRoot = _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);

        FindByClass(layoutRoot, "abs")!.BoxModel.Content.Width.ShouldBe(100f, 0.5f);
    }
}
