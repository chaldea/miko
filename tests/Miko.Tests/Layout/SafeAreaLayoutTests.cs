using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// Verifies safe-area inset handling in <see cref="LayoutEngine.Layout"/>. When non-zero insets
/// are supplied (from a mobile host's system-bar insets), the root is laid out inside the
/// inset viewport rect — origin shifts to (left, top) and the available size shrinks by
/// left+right / top+bottom — so content is not occluded by the status bar / navigation bar.
/// Zero insets must reproduce the pre-safe-area behavior exactly.
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

    // A fixed-position child pinned to top:0 left:0 — resolves against the (inset) viewport block.
    private static StyleSheet FixedChildSheet() => new()
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
                Selector = new ClassSelector("pinned"),
                Style = new Style
                {
                    Position = Position.Fixed,
                    Top = Length.Px(0), Left = Length.Px(0),
                    Width = Length.Px(50), Height = Length.Px(50),
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

        // Default-parameter overload (no insets) must match zero insets exactly.
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
    public void NonZeroInsets_OffsetOriginAndShrinkSize()
    {
        const float top = 48f, bottom = 24f, left = 12f, right = 8f;
        var root = new DivElement { Class = "root" };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { FullSizeRootSheet() },
            ViewportW, ViewportH, new SafeAreaInsets(left, top, right, bottom));

        // Origin shifts into the safe area...
        layout.BoxModel.Content.Left.ShouldBe(left);
        layout.BoxModel.Content.Top.ShouldBe(top);
        // ...and 100% width/height resolve against the inset (available) size.
        layout.BoxModel.Content.Width.ShouldBe(ViewportW - left - right);
        layout.BoxModel.Content.Height.ShouldBe(ViewportH - top - bottom);
        // The content stays clear of the bottom/right system bars.
        layout.BoxModel.Content.Bottom.ShouldBe(ViewportH - bottom, 0.5f);
        layout.BoxModel.Content.Right.ShouldBe(ViewportW - right, 0.5f);
    }

    [Fact]
    public void TopInsetOnly_PushesContentBelowStatusBar()
    {
        const float top = 48f;
        var root = new DivElement { Class = "root" };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { FullSizeRootSheet() },
            ViewportW, ViewportH, new SafeAreaInsets(0, top, 0, 0));

        layout.BoxModel.Content.Top.ShouldBe(top);
        layout.BoxModel.Content.Width.ShouldBe(ViewportW);          // no horizontal inset
        layout.BoxModel.Content.Height.ShouldBe(ViewportH - top);   // only the top is reserved
    }

    [Fact]
    public void FixedChild_ResolvesAgainstInsetViewportBlock()
    {
        const float top = 48f, left = 12f;
        var root = new DivElement { Class = "root" };
        var pinned = new DivElement { Class = "pinned" };
        root.AddChild(pinned);

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { FixedChildSheet() },
            ViewportW, ViewportH, new SafeAreaInsets(left, top, 0, 0));

        var pinnedBox = FindByClass(layout, "pinned");
        pinnedBox.ShouldNotBeNull();
        // A fixed top:0 left:0 element pins to the safe-area corner, not the screen corner,
        // so it isn't drawn under the status bar.
        pinnedBox!.BoxModel.MarginBox.Top.ShouldBe(top);
        pinnedBox.BoxModel.MarginBox.Left.ShouldBe(left);
    }
}
