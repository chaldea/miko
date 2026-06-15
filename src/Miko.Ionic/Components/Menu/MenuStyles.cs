using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the sidemenu layout components (<c>ion-app</c>, <c>ion-menu</c>,
/// <c>ion-menu-backdrop</c>, <c>ion-menu-button</c>, <c>ion-buttons</c>). Ported from the
/// Ionic source: <c>app.scss</c>, <c>menu.scss</c> / <c>menu.md.scss</c> / <c>menu.ios.scss</c>,
/// <c>menu-button.scss</c>, <c>buttons.scss</c>.
/// </summary>
internal static class MenuStyles
{
    internal static CssObject GenStyle(IonicTheme t)
    {
        return new CssObject
        {
            // ion-app — the sidemenu shell and the positioned containing block for the
            // overlay menu + backdrop. Position:relative makes the absolutely positioned
            // menu pin to this box. No overflow: that would disable the z-index paint sort
            // that lifts the menu/backdrop above the page.
            [".ion-app"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Position = Position.Relative,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                BackgroundColor = t.AppBackground,
            },

            // ion-menu — the drawer. Absolutely positioned (relative to ion-app), full
            // height, a flex column so the inner ion-content (flex-grow / basis 0) resolves
            // to the drawer's height. MD lifts it with an elevation shadow; iOS uses a
            // hairline trailing border. It is always mounted by the page and slides in/out
            // by animating its leading/trailing offset — so it animates both directions and,
            // when closed (off-screen), its hit-box is off-screen and never blocks the page.
            [".ion-menu"] = new()
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
                Transitions = new List<Transition>
                {
                    Transition.For(x => x.Left).Duration(t.MenuAnimDuration).EaseOut(),
                    Transition.For(x => x.Right).Duration(t.MenuAnimDuration).EaseOut(),
                },
            },

            // Side="start" (leading): closed off-screen to the left, open at the left edge.
            [".ion-menu-start"] = new()
            {
                Left = Length.Px(-t.MenuWidth),
                Right = Length.Auto,
            },
            [".ion-menu-start.ion-menu-open"] = new()
            {
                Left = Length.Px(0),
            },

            // Side="end" (trailing): closed off-screen to the right, open at the right edge.
            [".ion-menu-end"] = new()
            {
                Left = Length.Auto,
                Right = Length.Px(-t.MenuWidth),
            },
            [".ion-menu-end.ion-menu-open"] = new()
            {
                Right = Length.Px(0),
            },

            // ion-menu-backdrop — full-cover dim layer behind an open overlay menu. Painted
            // over the page (z-index) below the drawer. display:none at rest (so it does not
            // block page taps); the page mounts it while open/closing and animates opacity.
            [".ion-menu-backdrop"] = new()
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
            [".ion-menu-backdrop-mounted"] = new()
            {
                Display = Display.Block,
            },

            // Faded in (open). BackdropColor already carries the ~.32 alpha.
            [".ion-menu-backdrop-open"] = new()
            {
                Opacity = 1f,
            },

            // ion-menu-button — the hamburger in the toolbar. Square tap target holding the
            // menu icon; auto width so it does not stretch the toolbar row.
            [".ion-menu-button"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Px(48),
                Height = Length.Px(48),
                Color = t.ToolbarColor,
                Cursor = Cursor.Pointer,
            },

            [".ion-menu-button .ion-icon"] = new()
            {
                Width = Length.Px(24),
                Height = Length.Px(24),
            },

            // ion-buttons — slot container in the toolbar. Auto width (no flex-grow) so the
            // grow-able title absorbs the remaining row width next to it.
            [".ion-buttons"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
            },
        };
    }
}
