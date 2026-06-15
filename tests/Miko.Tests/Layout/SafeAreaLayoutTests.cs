using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// Verifies safe-area handling in <see cref="LayoutEngine.Layout"/>. Unlike the original
/// (ISSUE-052) design that shrank the whole viewport to the inset rect, the layout viewport
/// now stays full-screen (origin 0,0; full width/height). Safe-area insets are exposed through
/// CSS <c>env(safe-area-inset-*)</c> length components that *content* opts into (via padding),
/// so full-screen overlays still cover the entire screen (see ISSUE-054).
/// </summary>
public class SafeAreaLayoutTests
{
    private const float ViewportW = 390f;
    private const float ViewportH = 844f;

    private readonly LayoutEngine _layoutEngine = new();

    // A root that fills 100% × 100% of its containing block.
    private static StyleSheet FullSizeRootSheet() => new()
    {
        Rules = new List<StyleRule>
        {
            new()
            {
                Selector = new ClassSelector("root"),
                Style = new Style
                {
                    Display = Display.Block,
                    Width = Length.Percent(100),
                    Height = Length.Percent(100),
                },
            },
        }
    };

    // A root that pads its content into the safe area via env(safe-area-inset-*).
    // box-sizing:border-box keeps the border box at viewport size while the content box insets.
    private static StyleSheet SafeAreaPaddedSheet() => new()
    {
        Rules = new List<StyleRule>
        {
            new()
            {
                Selector = new ClassSelector("root"),
                Style = new Style
                {
                    Display = Display.Block,
                    BoxSizing = BoxSizing.BorderBox,
                    Width = Length.Percent(100),
                    Height = Length.Percent(100),
                    PaddingTop = Length.SafeAreaInsetTop,
                    PaddingBottom = Length.SafeAreaInsetBottom,
                    PaddingLeft = Length.SafeAreaInsetLeft,
                    PaddingRight = Length.SafeAreaInsetRight,
                },
            },
        }
    };

    // A full-screen absolutely-positioned overlay (like the menu backdrop): no env() padding.
    private static StyleSheet OverlaySheet() => new()
    {
        Rules = new List<StyleRule>
        {
            new()
            {
                Selector = new ClassSelector("root"),
                Style = new Style
                {
                    Display = Display.Block, Position = Position.Relative,
                    Width = Length.Percent(100), Height = Length.Percent(100),
                },
            },
            new()
            {
                Selector = new ClassSelector("backdrop"),
                Style = new Style
                {
                    Position = Position.Absolute,
                    Top = Length.Px(0), Left = Length.Px(0),
                    Width = Length.Percent(100), Height = Length.Percent(100),
                },
            },
        }
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

    [Fact]
    public void ZeroInsets_MatchesViewport()
    {
        var root = new DivElement { Class = "root" };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { FullSizeRootSheet() },
            ViewportW, ViewportH, SafeAreaInsets.Zero);

        layout.BoxModel.Content.Left.ShouldBe(0f);
        layout.BoxModel.Content.Top.ShouldBe(0f);
        layout.BoxModel.Content.Width.ShouldBe(ViewportW);
        layout.BoxModel.Content.Height.ShouldBe(ViewportH);
    }

    [Fact]
    public void DefaultOverload_EqualsZeroInsets()
    {
        var root = new DivElement { Class = "root" };

        // No insets argument → SafeAreaInsets default(struct) == Zero.
        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { FullSizeRootSheet() },
            ViewportW, ViewportH);

        layout.BoxModel.Content.Left.ShouldBe(0f);
        layout.BoxModel.Content.Top.ShouldBe(0f);
        layout.BoxModel.Content.Width.ShouldBe(ViewportW);
        layout.BoxModel.Content.Height.ShouldBe(ViewportH);
    }

    [Fact]
    public void NonZeroInsets_RootStillFillsFullViewport()
    {
        // A full-size root WITHOUT env() padding must still fill the entire screen even when
        // insets are non-zero — the viewport is not shrunk. This is what lets overlays cover
        // the whole screen.
        const float top = 48f, bottom = 24f, left = 12f, right = 8f;
        var root = new DivElement { Class = "root" };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { FullSizeRootSheet() },
            ViewportW, ViewportH, new SafeAreaInsets(left, top, right, bottom));

        layout.BoxModel.Content.Left.ShouldBe(0f);
        layout.BoxModel.Content.Top.ShouldBe(0f);
        layout.BoxModel.Content.Width.ShouldBe(ViewportW);
        layout.BoxModel.Content.Height.ShouldBe(ViewportH);
    }

    [Fact]
    public void EnvPadding_InsetsContentBox()
    {
        // A root that opts into env(safe-area-inset-*) padding: its border box still fills the
        // viewport, but its content box is inset by the resolved insets.
        const float top = 48f, bottom = 24f, left = 12f, right = 8f;
        var root = new DivElement { Class = "root" };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { SafeAreaPaddedSheet() },
            ViewportW, ViewportH, new SafeAreaInsets(left, top, right, bottom));

        // Border box covers the whole screen...
        layout.BoxModel.BorderBox.Top.ShouldBe(0f);
        layout.BoxModel.BorderBox.Height.ShouldBe(ViewportH);
        layout.BoxModel.BorderBox.Width.ShouldBe(ViewportW);
        // ...while the content box is padded into the safe area.
        layout.BoxModel.Content.Top.ShouldBe(top);
        layout.BoxModel.Content.Left.ShouldBe(left);
        layout.BoxModel.Content.Bottom.ShouldBe(ViewportH - bottom, 0.5f);
        layout.BoxModel.Content.Right.ShouldBe(ViewportW - right, 0.5f);
    }

    [Fact]
    public void EnvPadding_TopInsetOnly()
    {
        const float top = 48f;
        var root = new DivElement { Class = "root" };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { SafeAreaPaddedSheet() },
            ViewportW, ViewportH, new SafeAreaInsets(0, top, 0, 0));

        layout.BoxModel.Content.Top.ShouldBe(top);          // only the top is reserved
        layout.BoxModel.Content.Left.ShouldBe(0f);
        layout.BoxModel.Content.Height.ShouldBe(ViewportH - top);
        layout.BoxModel.Content.Width.ShouldBe(ViewportW);  // no horizontal inset
    }

    [Fact]
    public void Overlay_CoversFullScreen_DespiteInsets()
    {
        // The regression ISSUE-054 fixes: a full-screen overlay (menu backdrop) must cover the
        // entire screen even with non-zero insets, so the dim layer reaches under the system bars.
        const float top = 48f, bottom = 24f;
        var root = new DivElement { Class = "root" };
        var backdrop = new DivElement { Class = "backdrop" };
        root.AddChild(backdrop);

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { OverlaySheet() },
            ViewportW, ViewportH, new SafeAreaInsets(0, top, 0, bottom));

        var backdropBox = FindByClass(layout, "backdrop");
        backdropBox.ShouldNotBeNull();
        backdropBox!.BoxModel.BorderBox.Top.ShouldBe(0f);
        backdropBox.BoxModel.BorderBox.Left.ShouldBe(0f);
        backdropBox.BoxModel.BorderBox.Width.ShouldBe(ViewportW);
        backdropBox.BoxModel.BorderBox.Height.ShouldBe(ViewportH);
    }
}
