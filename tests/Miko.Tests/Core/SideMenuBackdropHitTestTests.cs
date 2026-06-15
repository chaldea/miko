using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// Hit-test invariants for the animated sidemenu (ISSUE-052 §2/§3). Structure is
/// <c>ion-app → [ ion-page, ion-menu-backdrop, ion-menu ]</c> — the page first, then the
/// overlay siblings, so the reverse-document-order hit-test reaches the overlay before the
/// full-size page. The open drawer/backdrop catch taps; the closed (off-screen) drawer and
/// the closed (display:none) backdrop must NOT, so the page stays interactive.
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

    // app(relative, fills) → [ page(block, fills), backdrop(absolute, fills), drawer(absolute,
    // MenuWidth) ]. pageFirst controls whether the page is authored before the overlay
    // siblings: hit-testing is reverse-document-order, so the overlay must come AFTER the
    // full-size page to be reachable. menuOpen=false slides the drawer off-screen (Left
    // -MenuWidth) and hides the backdrop (display:none).
    private static (DivElement app, DivElement page, DivElement backdrop, DivElement drawer) Build(
        bool pageFirst = true, bool menuOpen = true)
    {
        var page = new DivElement
        {
            Class = "ion-page",
            Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Percent(100) }
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

        var drawer = new DivElement
        {
            Class = "ion-menu",
            Style = new Style
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(menuOpen ? 0 : -MenuWidth),
                Width = Length.Px(MenuWidth), Height = Length.Percent(100),
                ZIndex = 1001,
            }
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
        if (pageFirst)
        {
            app.AddChild(backdrop);
            app.AddChild(drawer);
        }
        else
        {
            // Broken order: overlay before page (page ends up last → wins the hit-test).
            app.Children.Clear();
            app.AddChild(backdrop);
            app.AddChild(drawer);
            app.AddChild(page);
        }

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

    // Regression for ISSUE-052 §2: if the overlay is authored BEFORE the full-size page,
    // the reverse-document-order hit-test reaches the page first and the dim area never hits
    // the backdrop (menu cannot be closed by tapping outside). The overlay must come last.
    [Fact]
    public void Open_Click_OnDimArea_OverlayBeforePage_DoesNotReachBackdrop()
    {
        var (app, _, backdrop, _) = Build(pageFirst: false);
        var engine = CreateEngine(app);

        var hit = engine.HitTest(MenuWidth + 40f, H / 2f);

        hit.ShouldNotBe(backdrop);
        hit?.Class.ShouldBe("ion-page");
    }

    // ISSUE-052 §3: when closed, the off-screen drawer and display:none backdrop must not
    // capture taps anywhere — every point hits the page, so the app is fully interactive.
    [Fact]
    public void Closed_Click_InLeftStrip_HitsPage_NotOffScreenDrawer()
    {
        var (app, page, _, drawer) = Build(menuOpen: false);
        var engine = CreateEngine(app);

        // A point in the left strip where the OPEN drawer would sit (x < MenuWidth).
        var hit = engine.HitTest(MenuWidth / 2f, H / 2f);

        hit.ShouldBe(page);
        hit.ShouldNotBe(drawer);
    }

    [Fact]
    public void Closed_Click_Anywhere_HitsPage_NotHiddenBackdrop()
    {
        var (app, page, backdrop, _) = Build(menuOpen: false);
        var engine = CreateEngine(app);

        var hit = engine.HitTest(MenuWidth + 40f, H / 2f);

        hit.ShouldBe(page);
        hit.ShouldNotBe(backdrop);
    }
}
