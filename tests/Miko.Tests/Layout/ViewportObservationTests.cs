using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-151: the engine tells <see cref="IViewportObserver"/> elements where they can be seen, after
/// every layout and scroll and before the frame is painted, and lays the tree out again when an
/// observer changes it — the hook virtualized lists are built on.
/// </summary>
public class ViewportObservationTests : IDisposable
{
    private const int W = 400;
    private const int H = 300;

    private readonly SKSurface _surface = SKSurface.Create(new SKImageInfo(W, H));

    public void Dispose() => _surface.Dispose();

    private sealed class ProbeElement : DivElement, IViewportObserver
    {
        public List<ViewportObservation> Calls { get; } = new();
        public Func<ViewportObservation, bool>? OnChange { get; set; }

        public bool OnViewportChanged(in ViewportObservation observation)
        {
            Calls.Add(observation);
            return OnChange?.Invoke(observation) ?? false;
        }
    }

    private static DivElement Scroller(float height, params Element[] children)
    {
        var scroller = new DivElement
        {
            Class = "scroller",
            Style = new Style { Height = Length.Px(height), OverflowY = Overflow.Auto },
        };
        foreach (var child in children) scroller.AddChild(child);
        return scroller;
    }

    private static DivElement Block(float height) => new() { Style = new Style { Height = Length.Px(height) } };

    private MikoEngine Start(Element root)
    {
        var engine = new MikoEngineBuilder().Build();
        engine.Initialize(root, new List<StyleSheet>(), _surface.Canvas, W, H);
        return engine;
    }

    [Fact]
    public void Should_ReportTheScrolledViewport_InLayoutCoordinates()
    {
        var probe = new ProbeElement { Style = new Style { Height = Length.Px(2000) } };
        var root = Scroller(H, probe);
        var engine = Start(root);

        var first = probe.Calls.ShouldHaveSingleItem();
        first.VisibleRect.Top.ShouldBe(0f);
        first.VisibleRect.Height.ShouldBe(H);
        first.ScrollContainer.ShouldBeSameAs(root.LayoutBox);

        engine.ScrollBy(10, 10, 0, 500).ShouldBeTrue();
        engine.Render(_surface.Canvas);

        var scrolled = probe.Calls[^1];
        scrolled.VisibleRect.Top.ShouldBe(500f);
        scrolled.VisibleRect.Bottom.ShouldBe(800f);
        scrolled.LayoutRoot.ShouldBeSameAs(first.LayoutRoot, "scrolling does not lay the tree out again");
    }

    [Fact]
    public void Should_IntersectNestedScrollContainers()
    {
        // outer (300px tall): a 100px header, then inner (400px tall) holding the probe.
        var probe = new ProbeElement { Style = new Style { Height = Length.Px(3000) } };
        var inner = Scroller(400, probe);
        inner.Class = "inner";
        var outer = Scroller(H, Block(100), inner, Block(500));
        var engine = Start(outer);

        outer.LayoutBox!.ScrollTop = 50;
        inner.LayoutBox!.ScrollTop = 200;
        engine.InvalidateElement(outer);
        engine.Render(_surface.Canvas);

        // On screen the inner box spans y = 100 − 50 = 50 … 300 (the outer clip); its content is drawn
        // 200 + 50 px higher than laid out, so the visible part of the content is y = 300 … 550.
        var seen = probe.Calls[^1];
        seen.VisibleRect.Top.ShouldBe(300f, 0.01f);
        seen.VisibleRect.Bottom.ShouldBe(550f, 0.01f);
        seen.ScrollContainer.ShouldBeSameAs(inner.LayoutBox, "the nearest vertical scroller");
    }

    [Fact]
    public void Should_IgnoreTheScrollOfContainersAboveAFixedAncestor()
    {
        var probe = new ProbeElement { Style = new Style { Height = Length.Px(100) } };
        var overlay = new DivElement
        {
            Style = new Style { Position = Position.Fixed, Top = Length.Px(0), Left = Length.Px(0), Width = Length.Px(W), Height = Length.Px(H) },
        };
        overlay.AddChild(probe);
        var root = Scroller(H, Block(2000), overlay);
        var engine = Start(root);

        root.LayoutBox!.ScrollTop = 700;
        engine.InvalidateElement(root);
        engine.Render(_surface.Canvas);

        var seen = probe.Calls[^1];
        seen.VisibleRect.Top.ShouldBe(0f);
        seen.VisibleRect.Height.ShouldBe(H);
        seen.ScrollContainer.ShouldBeNull("a fixed box does not scroll with its ancestors");
    }

    [Fact]
    public void Should_NotNotify_AnObserverUnderADisplayNoneAncestor()
    {
        var probe = new ProbeElement { Style = new Style { Height = Length.Px(100) } };
        var hidden = new DivElement { Style = new Style { Display = Display.None } };
        hidden.AddChild(probe);
        var engine = Start(Scroller(H, hidden));

        probe.Calls.ShouldBeEmpty();

        hidden.Style = new Style { Display = Display.Block };
        engine.Render(_surface.Canvas);
        probe.Calls.Count.ShouldBe(1, "shown: notified with the layout that shows it");
    }

    [Fact]
    public void Should_LayOutAgainBeforePainting_WhenAnObserverChangesTheTree()
    {
        var probe = new ProbeElement { Style = new Style { Height = Length.Px(100) } };
        var root = Scroller(H, probe);
        var added = Block(40);
        probe.OnChange = _ =>
        {
            if (added.Parent == null) probe.AddChild(added);
            return false;
        };

        var engine = Start(root);

        probe.Calls.Count.ShouldBe(2, "notified again with the layout that contains the change");
        probe.Calls[1].LayoutRoot.ShouldNotBeSameAs(probe.Calls[0].LayoutRoot);
        added.LayoutBox.ShouldNotBeNull();
        engine.GetCurrentLayout().ShouldBeSameAs(probe.Calls[1].LayoutRoot,
            "the frame is painted from the layout the observer last saw");
        engine.HasPendingVisualWork.ShouldBeFalse();
    }

    [Fact]
    public void Should_StopAfterABoundedNumberOfPasses_WhenAnObserverNeverSettles()
    {
        var probe = new ProbeElement { Style = new Style { Height = Length.Px(100) } };
        probe.OnChange = _ =>
        {
            probe.AddChild(Block(1));
            return false;
        };

        var engine = Start(Scroller(H, probe));

        probe.Calls.Count.ShouldBeLessThanOrEqualTo(10);
        engine.HasPendingRenderWork.ShouldBeTrue("the next frame carries on");
    }
}
