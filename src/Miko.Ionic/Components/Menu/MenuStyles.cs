using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the sidemenu layout components (<c>ion-app</c>, <c>ion-menu</c>,
/// <c>ion-menu-backdrop</c>, <c>ion-menu-button</c>, <c>ion-buttons</c>). Ported from the
/// Ionic source: <c>app.scss</c>, <c>menu.scss</c> / <c>menu.md.scss</c> / <c>menu.ios.scss</c>,
/// <c>menu-button.scss</c>, <c>buttons.scss</c>.
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>) the host stamps onto its
/// root. The drawer (<c>.ion-menu-inner</c>) and backdrop are non-component child <c>div</c>s of
/// the moded <c>.ion-menu-host</c>, so they are scoped as descendants of the host's mode class
/// (their mode-specific values — width / shadow / border / backdrop color — then switch with it).
/// See <see cref="PageStyles"/> for the rationale.
/// </para>
/// </summary>
internal static class MenuStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        return new CssObject
        {
            // ion-app — the sidemenu shell and the positioned containing block for the
            // overlay menu + backdrop. Position:relative makes the absolutely positioned
            // menu pin to this box. No overflow: that would disable the z-index paint sort
            // that lifts the menu/backdrop above the page.
            [$".ion-app.{mode}"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Position = Position.Relative,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                BackgroundColor = t.AppBackground,
            },

            // ion-menu-host — full-screen overlay layer (relative to ion-app) that owns the
            // drawer + backdrop, mirroring Ionic's <ion-menu> Host. Always mounted so the
            // slide/fade can animate in both directions. Transparent; establishes the
            // containing block so the backdrop's 100% fills the screen.
            [$".ion-menu-host.{mode}"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                ZIndex = 1000,
            },

            // Closed & settled: transparent to taps so the page below stays interactive.
            [$".ion-menu-host.{mode}.ion-menu-host-idle"] = new()
            {
                PointerEvents = PointerEvents.None,
            },
            // Open or animating closed: interactive, so the backdrop catches the dim-area tap.
            [$".ion-menu-host.{mode}.ion-menu-host-open"] = new()
            {
                PointerEvents = PointerEvents.Auto,
            },
            [$".ion-menu-host.{mode}.ion-menu-host-closing"] = new()
            {
                PointerEvents = PointerEvents.Auto,
            },

            // ion-menu-inner — the drawer itself. Absolutely positioned within the host, full
            // height, a flex column so the inner ion-content (flex-grow / basis 0) resolves to
            // the drawer's height. MD lifts it with an elevation shadow; iOS uses a hairline
            // trailing border. Slides in/out by animating its leading/trailing offset — so it
            // animates both directions and, when closed (off-screen), its hit-box leaves the
            // viewport and never blocks the page.
            [$".ion-menu-host.{mode} .ion-menu-inner"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Width = Length.Px(t.MenuWidth),
                Height = Length.Percent(100),
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                BackgroundColor = t.MenuBackground,
                BoxShadow = t.MenuBoxShadow.Count > 0 ? t.MenuBoxShadow : null,
                BorderRight = t.MenuBorderWidth > 0
                    ? new BorderSide(Length.Px(t.MenuBorderWidth), BorderStyle.Solid, t.MenuBorderColor)
                    : new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
                ZIndex = 1001,
                // The drawer panel itself spans the full screen height (so its surface reaches
                // under the status/navigation bars), but its contents must clear those bars:
                // env(safe-area-inset-*) pads the top/bottom. The drawer is a slide-in overlay,
                // so unlike the host/backdrop it DOES opt into the inset for its content.
                PaddingTop = Length.SafeAreaInsetTop,
                PaddingBottom = Length.SafeAreaInsetBottom,
                Transitions = new List<Transition>
                {
                    Transition.For(x => x.Left).Duration(t.MenuAnimDuration).EaseOut(),
                    Transition.For(x => x.Right).Duration(t.MenuAnimDuration).EaseOut(),
                },
            },

            // Side="start" (leading): closed off-screen to the left, open at the left edge.
            [$".ion-menu-host.{mode} .ion-menu-inner-start"] = new()
            {
                Left = Length.Px(-t.MenuWidth),
                Right = Length.Auto,
            },
            [$".ion-menu-host.{mode} .ion-menu-inner-start.ion-menu-inner-open"] = new()
            {
                Left = Length.Px(0),
            },

            // Side="end" (trailing): closed off-screen to the right, open at the right edge.
            [$".ion-menu-host.{mode} .ion-menu-inner-end"] = new()
            {
                Left = Length.Auto,
                Right = Length.Px(-t.MenuWidth),
            },
            [$".ion-menu-host.{mode} .ion-menu-inner-end.ion-menu-inner-open"] = new()
            {
                Right = Length.Px(0),
            },

            // ion-menu-backdrop — full-cover dim layer filling the host, below the drawer
            // (z-index). display:none at rest; mounted while open/closing and animates opacity.
            [$".ion-menu-host.{mode} .ion-menu-backdrop"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                BackgroundColor = t.BackdropColor,
                Opacity = 0f,
                Display = Display.None,
                ZIndex = 1000,
                Cursor = Cursor.Pointer,
                Transitions = new List<Transition>
                {
                    Transition.For(x => x.Opacity).Duration(t.MenuAnimDuration).EaseOut(),
                },
            },

            // Mounted (present) while the menu is open or animating closed.
            [$".ion-menu-host.{mode} .ion-menu-backdrop-mounted"] = new()
            {
                Display = Display.Block,
            },

            // Faded in (open). BackdropColor already carries the ~.32 alpha.
            [$".ion-menu-host.{mode} .ion-menu-backdrop-open"] = new()
            {
                Opacity = 1f,
            },

            // ion-menu-button — the hamburger in the toolbar. Square tap target holding the
            // menu icon; auto width so it does not stretch the toolbar row.
            [$".ion-menu-button.{mode}"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Px(48),
                Height = Length.Px(48),
                Color = t.ToolbarColor,
                Cursor = Cursor.Pointer,
            },

            [$".ion-menu-button.{mode} .ion-icon"] = new()
            {
                Width = Length.Px(24),
                Height = Length.Px(24),
            },

            // ion-buttons — slot container in the toolbar. Auto width (no flex-grow) so the
            // grow-able title absorbs the remaining row width next to it.
            [$".ion-buttons.{mode}"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
            },
        };
    }
}
