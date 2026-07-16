using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-breadcrumb</c> / <c>ion-breadcrumbs</c>. Ported from the Ionic source:
/// <c>breadcrumb.scss</c> / <c>.md.scss</c> / <c>.ios.scss</c> and
/// <c>breadcrumbs.scss</c> / <c>.md.scss</c> / <c>.ios.scss</c> (+ their <c>*.vars.scss</c>).
/// <para>
/// A breadcrumbs bar is a wrapping flex row. Each breadcrumb is a flex row holding a native
/// anchor/span and a trailing separator (<c>"/"</c> on md, a forward chevron on ios) that the
/// enclosing container strips from the last crumb. Rules are scoped by the active mode class
/// (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class BreadcrumbStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // ion-breadcrumbs — a wrapping flex row, vertically centered.
            [$".ion-breadcrumbs.{mode}"] = new()
            {
                Display = Display.Flex,
                FlexWrap = FlexWrap.Wrap,
                AlignItems = AlignItems.Center,
            },

            // ion-breadcrumb host — a flex row that does not grow/shrink, carrying the crumb color
            // and typography.
            [$".ion-breadcrumb.{mode}"] = new()
            {
                Display = Display.Flex,
                FlexGrow = 0,
                FlexShrink = 0,
                AlignItems = AlignItems.Center,
                Color = t.BreadcrumbColor,
                FontSize = Length.Px(t.BreadcrumbFontSize),
                FontWeight = FontWeight.Normal,
                LineHeight = Length.Number(1.5f),
            },

            // .breadcrumb-native — the clickable surface (anchor or span). Fills the host width,
            // padded per mode, takes the crumb color (Miko has no `inherit`, so we mirror it), and
            // (ios) has a rounded corner.
            [$".ion-breadcrumb.{mode} .breadcrumb-native"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                PaddingTop = Length.Px(t.BreadcrumbPaddingY),
                PaddingBottom = Length.Px(t.BreadcrumbPaddingY),
                PaddingLeft = Length.Px(t.BreadcrumbPaddingX),
                PaddingRight = Length.Px(t.BreadcrumbPaddingX),
                Color = t.BreadcrumbColor,
                TextDecoration = TextDecoration.None,
                BorderRadius = new BorderRadius(Length.Px(t.BreadcrumbBorderRadius)),
            },

            // Active crumb — a stronger color and (per mode) a heavier weight. Also recolor the
            // native surface (no `inherit` in Miko).
            [$".ion-breadcrumb.{mode}.breadcrumb-active"] = new()
            {
                Color = t.BreadcrumbColorActive,
                FontWeight = t.BreadcrumbActiveFontWeight,
            },
            [$".ion-breadcrumb.{mode}.breadcrumb-active .breadcrumb-native"] = new()
            {
                Color = t.BreadcrumbColorActive,
            },

            // Disabled crumb — dimmed and non-interactive.
            [$".ion-breadcrumb.{mode}.breadcrumb-disabled"] = new()
            {
                Opacity = 0.5f,
                PointerEvents = PointerEvents.None,
                Cursor = Cursor.Default,
            },

            // .breadcrumb-separator — the glyph between crumbs, in the neutral separator color with
            // side margins so it sits centered in the gap.
            [$".ion-breadcrumb.{mode} .breadcrumb-separator"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Color = t.BreadcrumbSeparatorColor,
                MarginLeft = Length.Px(t.BreadcrumbSeparatorMarginX),
                MarginRight = Length.Px(t.BreadcrumbSeparatorMarginX),
            },

            // Slotted icons take the crumb icon color/size.
            [$".ion-breadcrumb.{mode} .ion-icon"] = new()
            {
                Color = t.BreadcrumbIconColor,
                FontSize = Length.Px(t.BreadcrumbIconFontSize),
            },

            // The separator chevron (ios) uses the separator color, not the icon color.
            [$".ion-breadcrumb.{mode} .breadcrumb-separator .ion-icon"] = new()
            {
                Color = t.BreadcrumbSeparatorColor,
                FontSize = Length.Px(t.BreadcrumbIconFontSize),
            },

            // Active crumb recolors its slotted icons.
            [$".ion-breadcrumb.{mode}.breadcrumb-active .breadcrumb-native .ion-icon"] = new()
            {
                Color = t.BreadcrumbIconColorActive,
            },
        };

        // Named-color crumbs (Ionic in-breadcrumbs-color / ion-color): recolor the text to the base
        // palette color.
        AddColor(css, mode, "primary", t.Primary);
        AddColor(css, mode, "secondary", t.Secondary);
        AddColor(css, mode, "tertiary", t.Tertiary);
        AddColor(css, mode, "success", t.Success);
        AddColor(css, mode, "warning", t.Warning);
        AddColor(css, mode, "danger", t.Danger);
        AddColor(css, mode, "light", t.Light);
        AddColor(css, mode, "medium", t.Medium);
        AddColor(css, mode, "dark", t.Dark);

        return css;
    }

    private static void AddColor(CssObject css, string mode, string name, Color color)
    {
        css[$".ion-breadcrumb.{mode}.ion-color-{name}"] = new() { Color = color };
    }
}
