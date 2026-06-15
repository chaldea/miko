using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// Hit-testing honors CSS <c>pointer-events</c>: a <c>none</c> element is transparent to taps
/// (they pass through to whatever is behind it), while its descendants stay hittable and an
/// <c>auto</c> descendant can override an inherited <c>none</c>. Underpins the sidemenu's
/// full-screen overlay host, which is <c>pointer-events:none</c> while the menu is closed so
/// the page below stays interactive (ISSUE-052 §3b).
/// </summary>
public class PointerEventsHitTestTests
{
    private const float W = 400f;
    private const float H = 300f;

    private static MikoEngine CreateEngine(Element root)
    {
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)W, (int)H));
        engine.Initialize(root, new List<StyleSheet>(), surface.Canvas, W, H);
        return engine;
    }

    private static DivElement FullCover(string id, PointerEvents? pe = null) => new()
    {
        Id = id,
        Style = new Style
        {
            Position = Position.Absolute,
            Top = Length.Px(0), Left = Length.Px(0),
            Width = Length.Percent(100), Height = Length.Percent(100),
            PointerEvents = pe,
        }
    };

    [Fact]
    public void PointerEventsNone_TapPassesThroughToSiblingBehind()
    {
        var page = FullCover("page");
        var overlay = FullCover("overlay", PointerEvents.None);

        var root = new DivElement
        {
            Style = new Style { Position = Position.Relative, Width = Length.Percent(100), Height = Length.Percent(100) },
        };
        root.AddChild(page);
        root.AddChild(overlay); // authored last → reverse-order hit-test checks it first

        var engine = CreateEngine(root);
        var hit = engine.HitTest(W / 2f, H / 2f);

        // The overlay is transparent to the tap, so the page behind it is hit.
        hit.ShouldBe(page);
    }

    [Fact]
    public void PointerEventsAuto_TapHitsTheTopElement()
    {
        var page = FullCover("page");
        var overlay = FullCover("overlay", PointerEvents.Auto);

        var root = new DivElement
        {
            Style = new Style { Position = Position.Relative, Width = Length.Percent(100), Height = Length.Percent(100) },
        };
        root.AddChild(page);
        root.AddChild(overlay);

        var engine = CreateEngine(root);
        var hit = engine.HitTest(W / 2f, H / 2f);

        hit.ShouldBe(overlay);
    }

    [Fact]
    public void Default_NoPointerEvents_TapHitsTheTopElement()
    {
        var page = FullCover("page");
        var overlay = FullCover("overlay"); // default = auto

        var root = new DivElement
        {
            Style = new Style { Position = Position.Relative, Width = Length.Percent(100), Height = Length.Percent(100) },
        };
        root.AddChild(page);
        root.AddChild(overlay);

        var engine = CreateEngine(root);
        var hit = engine.HitTest(W / 2f, H / 2f);

        hit.ShouldBe(overlay);
    }

    [Fact]
    public void PointerEventsNone_Parent_AutoChild_StillHitsChild()
    {
        // A none host with an auto child: the host passes through, but the child (which resets
        // pointer-events to auto) is still a hit target.
        var child = new DivElement
        {
            Id = "child",
            Style = new Style
            {
                Position = Position.Absolute,
                Top = Length.Px(0), Left = Length.Px(0),
                Width = Length.Px(100), Height = Length.Percent(100),
                PointerEvents = PointerEvents.Auto,
            }
        };
        var host = FullCover("host", PointerEvents.None);
        host.AddChild(child);

        var page = FullCover("page");

        var root = new DivElement
        {
            Style = new Style { Position = Position.Relative, Width = Length.Percent(100), Height = Length.Percent(100) },
        };
        root.AddChild(page);
        root.AddChild(host);

        var engine = CreateEngine(root);

        // Inside the auto child (x < 100) → hits the child despite the none host.
        engine.HitTest(50f, H / 2f).ShouldBe(child);
        // Outside the child (x > 100) → host is transparent → page behind is hit.
        engine.HitTest(200f, H / 2f).ShouldBe(page);
    }
}
