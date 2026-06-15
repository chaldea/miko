using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// Verifies the layout invariants the Ionic sidemenu (overlay drawer) relies on after the
/// animation rework: the drawer and backdrop are absolute siblings of the page under
/// <c>ion-app</c>; the drawer slides on/off-screen by animating its leading/trailing offset
/// (so the closed drawer sits off-screen and its hit-box leaves the viewport), and the page
/// fills the app regardless. Mirrors <c>Miko.Ionic</c>'s structure without depending on the
/// component library.
/// </summary>
public class SideMenuOverlayLayoutTests
{
    private const float MenuWidth = 304f;
    private const float ViewportW = 390f;
    private const float ViewportH = 844f;

    private readonly LayoutEngine _layoutEngine = new();

    // ion-app: relative flex column filling the viewport.
    // ion-page: block child that fills the app (the overlay must not push it).
    // ion-menu / ion-menu-backdrop: absolute siblings of the page.
    // The drawer's leading/trailing offset is the open/closed state: start side uses Left
    // (closed -MenuWidth → open 0), end side uses Right.
    private static StyleSheet BuildSheet(string side, bool menuOpen)
    {
        bool start = side == "start";
        return new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new() { Selector = new ClassSelector("ion-app"), Style = new Style
                {
                    Display = Display.Flex, FlexDirection = FlexDirection.Column,
                    Position = Position.Relative,
                    Width = Length.Percent(100), Height = Length.Percent(100),
                } },
                new() { Selector = new ClassSelector("ion-page"), Style = new Style
                {
                    Display = Display.Block,
                    FlexGrow = 1, FlexShrink = 1, FlexBasis = Length.Px(0),
                    Width = Length.Percent(100),
                } },
                new() { Selector = new ClassSelector("ion-menu-backdrop"), Style = new Style
                {
                    Position = Position.Absolute,
                    Top = Length.Px(0), Left = Length.Px(0),
                    Width = Length.Percent(100), Height = Length.Percent(100),
                    Display = menuOpen ? Display.Block : Display.None,
                    ZIndex = 1000,
                } },
                new() { Selector = new ClassSelector("ion-menu"), Style = new Style
                {
                    Position = Position.Absolute,
                    Top = Length.Px(0),
                    // Closed: off-screen by MenuWidth on the anchored side. Open: flush at 0.
                    Left = start ? Length.Px(menuOpen ? 0 : -MenuWidth) : Length.Auto,
                    Right = start ? Length.Auto : Length.Px(menuOpen ? 0 : -MenuWidth),
                    Width = Length.Px(MenuWidth), Height = Length.Percent(100),
                    Display = Display.Flex, FlexDirection = FlexDirection.Column,
                    ZIndex = 1001,
                } },
            }
        };
    }

    private static (DivElement app, DivElement page, DivElement backdrop, DivElement menu) BuildDom()
    {
        var app = new DivElement { Class = "ion-app" };
        var page = new DivElement { Class = "ion-page" };
        var backdrop = new DivElement { Class = "ion-menu-backdrop" };
        var menu = new DivElement { Class = "ion-menu" };

        // Page first, overlay siblings last — the engine's reverse-document-order hit-test
        // depends on the overlay coming after the full-size page.
        app.AddChild(page);
        app.AddChild(backdrop);
        app.AddChild(menu);
        return (app, page, backdrop, menu);
    }

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
    public void OpenMenu_StartDrawer_PinnedLeft_FullSize()
    {
        var (app, _, _, _) = BuildDom();

        var layoutRoot = _layoutEngine.Layout(app, new List<StyleSheet> { BuildSheet("start", menuOpen: true) }, ViewportW, ViewportH);

        var menu = FindByClass(layoutRoot, "ion-menu");
        menu.ShouldNotBeNull();
        menu!.ComputedStyle.Position.ShouldBe(Position.Absolute);
        menu.BoxModel.MarginBox.Left.ShouldBe(0f);
        menu.BoxModel.MarginBox.Top.ShouldBe(0f);
        menu.BoxModel.Content.Width.ShouldBe(MenuWidth);
        menu.BoxModel.Content.Height.ShouldBe(ViewportH);
    }

    [Fact]
    public void ClosedMenu_StartDrawer_SlidOffScreenLeft()
    {
        var (app, _, _, _) = BuildDom();

        var layoutRoot = _layoutEngine.Layout(app, new List<StyleSheet> { BuildSheet("start", menuOpen: false) }, ViewportW, ViewportH);

        // Closed drawer's border box is entirely off the left edge (right edge <= 0), so its
        // hit-box has left the viewport and cannot capture page taps.
        var menu = FindByClass(layoutRoot, "ion-menu");
        menu.ShouldNotBeNull();
        menu!.BoxModel.MarginBox.Left.ShouldBe(-MenuWidth);
        menu.BoxModel.BorderBox.Right.ShouldBeLessThanOrEqualTo(0f);
    }

    [Fact]
    public void OpenMenu_EndDrawer_PinnedRight()
    {
        var (app, _, _, _) = BuildDom();

        var layoutRoot = _layoutEngine.Layout(app, new List<StyleSheet> { BuildSheet("end", menuOpen: true) }, ViewportW, ViewportH);

        var menu = FindByClass(layoutRoot, "ion-menu");
        menu.ShouldNotBeNull();
        // Open end drawer sits flush against the right edge of the viewport.
        menu!.BoxModel.MarginBox.Right.ShouldBe(ViewportW, 0.5f);
        menu.BoxModel.Content.Width.ShouldBe(MenuWidth);
    }

    [Fact]
    public void ClosedMenu_EndDrawer_SlidOffScreenRight()
    {
        var (app, _, _, _) = BuildDom();

        var layoutRoot = _layoutEngine.Layout(app, new List<StyleSheet> { BuildSheet("end", menuOpen: false) }, ViewportW, ViewportH);

        // Closed end drawer's left edge is at/after the right viewport edge → off-screen.
        var menu = FindByClass(layoutRoot, "ion-menu");
        menu.ShouldNotBeNull();
        menu!.BoxModel.BorderBox.Left.ShouldBeGreaterThanOrEqualTo(ViewportW - 0.5f);
    }

    [Fact]
    public void Menu_OverlayIsOutOfFlow_PageStillFillsTheApp()
    {
        var (app, _, _, _) = BuildDom();

        var layoutRoot = _layoutEngine.Layout(app, new List<StyleSheet> { BuildSheet("start", menuOpen: true) }, ViewportW, ViewportH);

        // The absolutely-positioned overlay siblings must not consume flow space: the page
        // fills the full viewport regardless of the open drawer.
        var page = FindByClass(layoutRoot, "ion-page");
        page.ShouldNotBeNull();
        page!.BoxModel.Content.Width.ShouldBe(ViewportW);
        page.BoxModel.Content.Height.ShouldBe(ViewportH);
    }

    [Fact]
    public void ClosedMenu_BackdropIsRemovedFromTheLayoutTree()
    {
        var (app, _, _, _) = BuildDom();

        var layoutRoot = _layoutEngine.Layout(app, new List<StyleSheet> { BuildSheet("start", menuOpen: false) }, ViewportW, ViewportH);

        // The backdrop is display:none when closed (so it cannot block page taps); the drawer
        // remains mounted (off-screen) so it can animate back in.
        FindByClass(layoutRoot, "ion-menu-backdrop").ShouldBeNull();
        FindByClass(layoutRoot, "ion-menu").ShouldNotBeNull();

        var page = FindByClass(layoutRoot, "ion-page");
        page.ShouldNotBeNull();
        page!.BoxModel.Content.Height.ShouldBe(ViewportH);
    }

    // The toolbar row that carries the menu button: a leading auto-width ion-buttons
    // (holding the hamburger) followed by a flex-grow ion-title. The title must absorb
    // the remaining row width next to the fixed-size buttons. Mirrors the
    // .toolbar-container (space-between) / .ion-buttons / .ion-title interplay.
    [Fact]
    public void Toolbar_LeadingButtons_AreAutoWidth_TitleFillsRemainingWidth()
    {
        const float buttonsWidth = 48f;
        var container = new DivElement { Class = "toolbar-container" };
        var buttons = new DivElement { Class = "ion-buttons" };
        var button = new DivElement { Class = "ion-menu-button" };
        var title = new DivElement { Class = "ion-title" };
        buttons.AddChild(button);
        container.AddChild(buttons);
        container.AddChild(title);

        var sheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new() { Selector = new ClassSelector("toolbar-container"), Style = new Style
                {
                    Display = Display.Flex, FlexDirection = FlexDirection.Row,
                    AlignItems = AlignItems.Center, JustifyContent = JustifyContent.SpaceBetween,
                    Width = Length.Percent(100), Height = Length.Px(56),
                } },
                new() { Selector = new ClassSelector("ion-buttons"), Style = new Style
                {
                    Display = Display.Flex, AlignItems = AlignItems.Center,
                } },
                new() { Selector = new ClassSelector("ion-menu-button"), Style = new Style
                {
                    Width = Length.Px(buttonsWidth), Height = Length.Px(48),
                } },
                new() { Selector = new ClassSelector("ion-title"), Style = new Style
                {
                    Display = Display.Flex, FlexGrow = 1,
                } },
            }
        };

        var layoutRoot = _layoutEngine.Layout(container, new List<StyleSheet> { sheet }, ViewportW, 56f);

        var buttonsBox = FindByClass(layoutRoot, "ion-buttons");
        var titleBox = FindByClass(layoutRoot, "ion-title");
        buttonsBox.ShouldNotBeNull();
        titleBox.ShouldNotBeNull();

        // Buttons are auto-width: shrink to the hamburger, pinned to the leading edge.
        buttonsBox!.BoxModel.Content.Width.ShouldBe(buttonsWidth);
        buttonsBox.BoxModel.MarginBox.Left.ShouldBe(0f);
        // Title (flex-grow) absorbs the rest of the row and sits after the buttons.
        titleBox!.BoxModel.MarginBox.Left.ShouldBe(buttonsWidth);
        titleBox.BoxModel.Content.Width.ShouldBe(ViewportW - buttonsWidth);
    }
}
