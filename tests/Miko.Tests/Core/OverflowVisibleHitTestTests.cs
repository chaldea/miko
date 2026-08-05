using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// Hit testing must follow CSS painting, not box containment: an <c>overflow: visible</c> box does
/// not clip its descendants, so content that spills outside it stays clickable. A clipping box
/// (<c>overflow</c> other than visible, or one that has been scrolled) does swallow everything
/// outside it — that pruning must survive.
/// <para>
/// Surfaced by Ionic's <c>ion-fab</c> (issues/ion-fab.md problem 2): the fab host is
/// <c>fit-content</c>, i.e. only as tall as its main button, so an expanded <c>ion-fab-list</c>
/// lies entirely outside it. Every list button was unclickable because the traversal stopped at the
/// host before ever reaching them.
/// </para>
/// </summary>
public class OverflowVisibleHitTestTests : IDisposable
{
    private const float ViewportW = 400f;
    private const float ViewportH = 400f;

    private readonly SKBitmap _bitmap = new((int)ViewportW, (int)ViewportH);
    private readonly SKCanvas _canvas;

    public OverflowVisibleHitTestTests()
    {
        _canvas = new SKCanvas(_bitmap);
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

    private MikoEngine Build(Element root, params StyleRule[] rules)
    {
        var engine = new MikoEngine();
        engine.Initialize(root, [new StyleSheet { Rules = rules.ToList() }], _canvas, ViewportW, ViewportH);
        return engine;
    }

    // root(400×400) > host(50×20, positioned) > child(absolute, 50×50 at y 40) — the child sits
    // wholly below the host, exactly like a fab-list under a fit-content fab host.
    private static (DivElement Root, DivElement Host, DivElement Child) BuildTree()
    {
        var child = new DivElement { Class = "child" };
        var host = new DivElement { Class = "host" };
        host.AddChild(child);
        var root = new DivElement { Class = "root" };
        root.AddChild(host);
        return (root, host, child);
    }

    private static StyleRule RootRule() => Rule("root",
        new Style { Width = Length.Px(ViewportW), Height = Length.Px(ViewportH) });

    private static StyleRule ChildRule() => Rule("child", new Style
    {
        Position = Position.Absolute,
        Left = Length.Px(0),
        Top = Length.Px(40),
        Width = Length.Px(50),
        Height = Length.Px(50),
    });

    private static StyleRule HostRule(Overflow overflow) => Rule("host", new Style
    {
        Position = Position.Relative,
        Width = Length.Px(50),
        Height = Length.Px(20),
        OverflowX = overflow,
        OverflowY = overflow,
    });

    [Fact]
    public void ChildOutsideAnOverflowVisibleParent_IsHittable()
    {
        var (root, _, child) = BuildTree();
        var engine = Build(root, RootRule(), HostRule(Overflow.Visible), ChildRule());

        // (10, 60) is inside the child but 40px below the host's 20px-tall box.
        engine.HitTest(10, 60).ShouldBe(child);
    }

    [Fact]
    public void ChildOutsideAnOverflowHiddenParent_IsNotHittable()
    {
        var (root, _, _) = BuildTree();
        var engine = Build(root, RootRule(), HostRule(Overflow.Hidden), ChildRule());

        // The clipping parent swallows it — the tap resolves to whatever is behind.
        var hit = engine.HitTest(10, 60);
        hit.ShouldNotBeNull();
        hit.HasClass("child").ShouldBeFalse();
        hit.HasClass("root").ShouldBeTrue();
    }

    [Fact]
    public void TheOverflowVisibleParentItself_IsNotHitOutsideItsOwnBox()
    {
        // Descending past the box must not make the box itself a target for points outside it —
        // only its overflowing descendants are reachable there.
        var (root, _, _) = BuildTree();
        var engine = Build(root, RootRule(), HostRule(Overflow.Visible), ChildRule());

        // (60, 60): outside the host AND outside the child (which is only 50px wide).
        var hit = engine.HitTest(60, 60);
        hit.ShouldNotBeNull();
        hit.HasClass("host").ShouldBeFalse();
        hit.HasClass("root").ShouldBeTrue();
    }

    [Fact]
    public void PointInsideTheParent_StillResolvesToTheParent()
    {
        var (root, host, _) = BuildTree();
        var engine = Build(root, RootRule(), HostRule(Overflow.Visible), ChildRule());

        engine.HitTest(10, 10).ShouldBe(host);
    }

    [Fact]
    public void OverlappingOverflowingChild_WinsOverTheParent()
    {
        // Painting order still decides when both are candidates: the later/deeper box wins.
        var child = new DivElement { Class = "child" };
        var host = new DivElement { Class = "host" };
        host.AddChild(child);
        var root = new DivElement { Class = "root" };
        root.AddChild(host);

        var engine = Build(root,
            RootRule(),
            HostRule(Overflow.Visible),
            Rule("child", new Style
            {
                Position = Position.Absolute,
                Left = Length.Px(0),
                Top = Length.Px(10),   // starts inside the host, spills below it
                Width = Length.Px(50),
                Height = Length.Px(50),
            }));

        engine.HitTest(10, 15).ShouldBe(child);   // overlap region
        engine.HitTest(10, 55).ShouldBe(child);   // overflowing region
    }
}
