using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// Hit-test invariants for the nested-host sidemenu (ISSUE-052 §3b). Structure is
/// <c>ion-app → [ ion-page, ion-menu-host → ( ion-menu-inner, ion-menu-backdrop ) ]</c> — the
/// page first, the full-screen host last so reverse-document-order hit-test reaches it first.
/// When open the host is <c>pointer-events:auto</c> so the dim area hits the backdrop and the
/// drawer hits the inner; when closed the host is <c>pointer-events:none</c> so every tap
/// passes through to the page (the drawer is off-screen and the backdrop is display:none).
/// </summary>
public class SideMenuBackdropHitTestTests
{
    private const float W = 390f;
    private const float H = 844f;
    private const float MenuWidth = 304f;

    private static MikoEngine CreateEngine(Element root)
    {
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)W, (int)H));
        engine.Initialize(root, new List<StyleSheet>(), surface.Canvas, W, H);
        return engine;
    }

    private static (DivElement app, DivElement page, DivElement backdrop, DivElement drawer) Build(bool menuOpen = true)
    {
        var page = new DivElement
        {
            Class = "ion-page",
            Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Percent(100) }
        };

        var drawer = new DivElement
        {
            Class = "ion-menu-inner",
            Style = new Style
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(menuOpen ? 0 : -MenuWidth),
                Width = Length.Px(MenuWidth), Height = Length.Percent(100),
                ZIndex = 1001,
            }
        };

        var backdrop = new DivElement
        {
            Class = "ion-menu-backdrop",
            Style = new Style
            {
                Position = Position.Absolute,
                Top = Length.Px(0), Left = Length.Px(0),
                Width = Length.Percent(100), Height = Length.Percent(100),
                Display = menuOpen ? Display.Block : Display.None,
                ZIndex = 1000,
            }
        };

        // Full-screen host: interactive when open, pointer-events:none when closed so taps
        // pass through to the page below.
        var host = new DivElement
        {
            Class = "ion-menu-host",
            Style = new Style
            {
                Position = Position.Absolute,
                Top = Length.Px(0), Left = Length.Px(0),
                Width = Length.Percent(100), Height = Length.Percent(100),
                PointerEvents = menuOpen ? PointerEvents.Auto : PointerEvents.None,
                ZIndex = 1000,
            },
            // Backdrop authored before drawer so reverse-order hit-test checks the drawer
            // first (drawer taps hit the drawer; dim-area taps fall through to the backdrop).
            Children = { backdrop, drawer }
        };

        var app = new DivElement
        {
            Class = "ion-app",
            Style = new Style
            {
                Display = Display.Flex, FlexDirection = FlexDirection.Column,
                Position = Position.Relative,
                Width = Length.Percent(100), Height = Length.Percent(100),
            },
        };
        app.AddChild(page);
        app.AddChild(host);

        return (app, page, backdrop, drawer);
    }

    [Fact]
    public void Open_Click_OnDimArea_BesideDrawer_HitsBackdrop()
    {
        var (app, _, backdrop, _) = Build();
        var engine = CreateEngine(app);

        var hit = engine.HitTest(MenuWidth + 40f, H / 2f);

        hit.ShouldBe(backdrop);
    }

    [Fact]
    public void Open_Click_OnDrawer_HitsDrawer_NotBackdrop()
    {
        var (app, _, _, drawer) = Build();
        var engine = CreateEngine(app);

        var hit = engine.HitTest(MenuWidth / 2f, H / 2f);

        hit.ShouldBe(drawer);
    }

    [Fact]
    public void Open_Click_OnDimArea_FiresBackdropOnClickHandler()
    {
        var (app, _, backdrop, _) = Build();
        backdrop.OnClick = _ => backdrop.Id = "clicked";

        var engine = CreateEngine(app);
        var hit = engine.HitTest(MenuWidth + 40f, H / 2f);
        hit.ShouldBe(backdrop);

        hit!.OnClick!.Invoke(new MouseEventArgs { Target = hit });

        backdrop.Id.ShouldBe("clicked");
    }

    // ISSUE-052 §3b: when closed the host is pointer-events:none, so taps pass through to the
    // page everywhere — including the left strip where the open drawer would sit, and the dim
    // area where the (now display:none) backdrop would be. The app stays fully interactive.
    [Fact]
    public void Closed_Click_InLeftStrip_PassesThroughHostToPage()
    {
        var (app, page, _, drawer) = Build(menuOpen: false);
        var engine = CreateEngine(app);

        var hit = engine.HitTest(MenuWidth / 2f, H / 2f);

        hit.ShouldBe(page);
        hit.ShouldNotBe(drawer);
    }

    [Fact]
    public void Closed_Click_InDimArea_PassesThroughHostToPage()
    {
        var (app, page, backdrop, _) = Build(menuOpen: false);
        var engine = CreateEngine(app);

        var hit = engine.HitTest(MenuWidth + 40f, H / 2f);

        hit.ShouldBe(page);
        hit.ShouldNotBe(backdrop);
    }
}
