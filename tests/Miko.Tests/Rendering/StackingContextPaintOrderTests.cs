using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// Paint order follows CSS stacking contexts, not just sibling order: a positioned box's
/// <c>z-index</c> is compared against boxes outside its parent, and only an ancestor that
/// establishes a stacking context confines it.
/// <para>
/// Surfaced by Ionic's <c>ion-fab</c> (issues/ion-fab.md problem 3): the fab (<c>z-index: 1000</c>)
/// lives inside <c>ion-content</c>, which is <c>position: relative</c> with <c>z-index: auto</c>.
/// A relative box with no z-index does NOT establish a stacking context, so the fab must out-paint
/// its uncle <c>ion-header</c> (<c>z-index: 10</c>). Sorting siblings only, the fab could never
/// beat the header — the half of the fab that hangs over the header was painted under it.
/// </para>
/// </summary>
public class StackingContextPaintOrderTests : IDisposable
{
    private const int W = 200;
    private const int H = 200;

    private readonly SKBitmap _bitmap = new(W, H);
    private readonly SKCanvas _canvas;

    private static readonly Color Red = Color.FromHex("ff0000");
    private static readonly Color Blue = Color.FromHex("0000ff");

    public StackingContextPaintOrderTests()
    {
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private static StyleRule Rule(string cls, Style style) => new()
    {
        Selector = new ClassSelector(cls),
        Style = style,
    };

    private void Render(Element root, params StyleRule[] rules)
    {
        _canvas.Clear(SKColors.White);
        var engine = new MikoEngine();
        engine.Initialize(root, [new StyleSheet { Rules = rules.ToList() }], _canvas, W, H);
        engine.Render(_canvas);
    }

    private bool IsBlueAt(int x, int y)
    {
        var p = _bitmap.GetPixel(x, y);
        return p.Blue > p.Red;
    }

    /// <summary>
    /// The ion-page shape: a banner followed by a content wrapper, with a badge inside the wrapper
    /// hanging up over the banner.
    /// <code>
    /// root
    ///  ├ banner   (relative, z-index 10, red, y 0..50)        — the "header"
    ///  └ wrapper  (relative, starts at y 50, no z-index)      — the "ion-content"
    ///      └ badge (absolute, z-index 999, blue, top:-25)     — the "fab", y 25..75
    /// </code>
    /// The badge overlaps the banner in y 25..50, and (30, 35) probes that overlap.
    /// </summary>
    private static (Element Root, StyleRule[] Rules) BuildHeaderAndFab(int? wrapperZIndex)
    {
        var badge = new DivElement { Class = "badge" };
        var wrapper = new DivElement { Class = "wrapper" };
        wrapper.AddChild(badge);
        var banner = new DivElement { Class = "banner" };
        var root = new DivElement { Class = "root" };
        root.AddChild(banner);
        root.AddChild(wrapper);

        var wrapperStyle = new Style { Position = Position.Relative, Width = Length.Px(W), Height = Length.Px(150) };
        if (wrapperZIndex is { } z) wrapperStyle.ZIndex = z;

        return (root,
        [
            Rule("root", new Style { Width = Length.Px(W), Height = Length.Px(H) }),
            Rule("banner", new Style
            {
                Position = Position.Relative,
                ZIndex = 10,
                Width = Length.Px(W),
                Height = Length.Px(50),
                BackgroundColor = Red,
            }),
            Rule("wrapper", wrapperStyle),
            Rule("badge", new Style
            {
                Position = Position.Absolute,
                ZIndex = 999,
                Top = Length.Px(-25),
                Left = Length.Px(0),
                Width = Length.Px(60),
                Height = Length.Px(50),
                BackgroundColor = Blue,
            }),
        ]);
    }

    [Fact]
    public void ZIndexedDescendant_OutPaintsAnUncle_ThroughAZIndexAutoParent()
    {
        var (root, rules) = BuildHeaderAndFab(wrapperZIndex: null);
        Render(root, rules);

        IsBlueAt(30, 35).ShouldBeTrue("the badge (z 999) must paint over the banner (z 10)");
        // Outside the overlap the banner is still visible on its own.
        IsBlueAt(150, 10).ShouldBeFalse();
    }

    [Fact]
    public void AZIndexedParent_ConfinesItsDescendantsZIndex()
    {
        // Give the wrapper z-index 0: now it DOES establish a stacking context, so the badge's 999
        // only orders it inside the wrapper. Wrapper (0) loses to banner (10), as CSS requires.
        var (root, rules) = BuildHeaderAndFab(wrapperZIndex: 0);
        Render(root, rules);

        IsBlueAt(30, 35).ShouldBeFalse("a z-indexed ancestor traps the descendant's z-index");
    }

    [Fact]
    public void HigherParentZIndex_LiftsTheWholeSubtree()
    {
        // Wrapper at 20 beats the banner's 10, so its subtree paints above regardless.
        var (root, rules) = BuildHeaderAndFab(wrapperZIndex: 20);
        Render(root, rules);

        IsBlueAt(30, 35).ShouldBeTrue();
    }

    [Fact]
    public void ZIndexedSiblings_StillPaintInZOrder()
    {
        // The pre-existing sibling contract must not regress: a later sibling with a lower z-index
        // paints below an earlier one with a higher z-index.
        var low = new DivElement { Class = "low" };
        var high = new DivElement { Class = "high" };
        var root = new DivElement { Class = "root" };
        root.AddChild(high);   // earlier in document order, but higher z
        root.AddChild(low);

        Render(root,
            Rule("root", new Style { Width = Length.Px(W), Height = Length.Px(H) }),
            Rule("high", new Style
            {
                Position = Position.Absolute,
                ZIndex = 5,
                Top = Length.Px(0), Left = Length.Px(0),
                Width = Length.Px(60), Height = Length.Px(60),
                BackgroundColor = Blue,
            }),
            Rule("low", new Style
            {
                Position = Position.Absolute,
                ZIndex = 1,
                Top = Length.Px(0), Left = Length.Px(0),
                Width = Length.Px(60), Height = Length.Px(60),
                BackgroundColor = Red,
            }));

        IsBlueAt(30, 30).ShouldBeTrue("z 5 beats z 1 even though it comes first in the DOM");
    }

    [Fact]
    public void EqualZIndex_KeepsDocumentOrder()
    {
        var first = new DivElement { Class = "first" };
        var second = new DivElement { Class = "second" };
        var root = new DivElement { Class = "root" };
        root.AddChild(first);
        root.AddChild(second);

        Render(root,
            Rule("root", new Style { Width = Length.Px(W), Height = Length.Px(H) }),
            Rule("first", new Style
            {
                Position = Position.Absolute,
                ZIndex = 3,
                Top = Length.Px(0), Left = Length.Px(0),
                Width = Length.Px(60), Height = Length.Px(60),
                BackgroundColor = Red,
            }),
            Rule("second", new Style
            {
                Position = Position.Absolute,
                ZIndex = 3,
                Top = Length.Px(0), Left = Length.Px(0),
                Width = Length.Px(60), Height = Length.Px(60),
                BackgroundColor = Blue,
            }));

        IsBlueAt(30, 30).ShouldBeTrue("same z-index → the later sibling wins");
    }

    [Fact]
    public void AClippingAncestor_StillClipsAZIndexedDescendant()
    {
        // Lifting a descendant out for z-ordering must not let it escape an ancestor's clip:
        // `overflow: hidden` still cuts it off, exactly as in CSS.
        var badge = new DivElement { Class = "badge" };
        var clipper = new DivElement { Class = "clipper" };
        clipper.AddChild(badge);
        var root = new DivElement { Class = "root" };
        root.AddChild(clipper);

        Render(root,
            Rule("root", new Style { Width = Length.Px(W), Height = Length.Px(H) }),
            Rule("clipper", new Style
            {
                Position = Position.Relative,
                Width = Length.Px(W),
                Height = Length.Px(40),
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            }),
            Rule("badge", new Style
            {
                Position = Position.Absolute,
                ZIndex = 999,
                Top = Length.Px(0), Left = Length.Px(0),
                Width = Length.Px(60), Height = Length.Px(120),
                BackgroundColor = Blue,
            }));

        IsBlueAt(30, 20).ShouldBeTrue("inside the clip it paints");
        IsBlueAt(30, 80).ShouldBeFalse("past the clip it is cut off");
    }

    [Fact]
    public void AnOpaqueAncestorsOpacity_StillAppliesToAZIndexedDescendant()
    {
        // opacity < 1 establishes a stacking context AND is applied as a layer; the descendant must
        // stay inside that layer rather than being hoisted out and painted at full strength.
        var badge = new DivElement { Class = "badge" };
        var faded = new DivElement { Class = "faded" };
        faded.AddChild(badge);
        var root = new DivElement { Class = "root" };
        root.AddChild(faded);

        Render(root,
            Rule("root", new Style { Width = Length.Px(W), Height = Length.Px(H) }),
            Rule("faded", new Style
            {
                Position = Position.Relative,
                Opacity = 0.5f,
                Width = Length.Px(W),
                Height = Length.Px(H),
            }),
            Rule("badge", new Style
            {
                Position = Position.Absolute,
                ZIndex = 999,
                Top = Length.Px(0), Left = Length.Px(0),
                Width = Length.Px(60), Height = Length.Px(60),
                BackgroundColor = Blue,
            }));

        // Half-strength blue over white: still bluest, but no longer saturated.
        var pixel = _bitmap.GetPixel(30, 30);
        pixel.Blue.ShouldBeGreaterThan(pixel.Red);
        pixel.Red.ShouldBeGreaterThan((byte)0, "the 50% layer opacity must still apply");
    }
}
