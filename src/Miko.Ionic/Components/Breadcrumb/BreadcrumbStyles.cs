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

            // Slotted icons take the crumb icon color/size. Miko sizes icons via width/height
            // (Ionic uses font-size: 18px with a 1em box), so stamp the box explicitly — this
            // out-ranks the default .ion-icon.{mode} size from IconStyles.
            [$".ion-breadcrumb.{mode} .ion-icon"] = new()
            {
                Color = t.BreadcrumbIconColor,
                Width = Length.Px(t.BreadcrumbIconFontSize),
                Height = Length.Px(t.BreadcrumbIconFontSize),
            },

            // Slotted start/end icons keep an 8px gap to the label (Ionic
            // ::slotted(ion-icon[slot="start"|"end"]) margin-start/end).
            [$".ion-breadcrumb.{mode} .ion-slot-start .ion-icon"] = new()
            {
                MarginRight = Length.Px(t.BreadcrumbIconSlotMargin),
            },
            [$".ion-breadcrumb.{mode} .ion-slot-end .ion-icon"] = new()
            {
                MarginLeft = Length.Px(t.BreadcrumbIconSlotMargin),
            },

            // The separator chevron (ios) uses the separator color, not the icon color.
            [$".ion-breadcrumb.{mode} .breadcrumb-separator .ion-icon"] = new()
            {
                Color = t.BreadcrumbSeparatorColor,
            },

            // Collapsed crumb — the native content is hidden (Ionic
            // :host(.breadcrumb-collapsed) .breadcrumb-native { display: none }).
            [$".ion-breadcrumb.{mode}.breadcrumb-collapsed .breadcrumb-native"] = new()
            {
                Display = Display.None,
            },

            // Collapsed indicator — the small "…" button shown in place of the collapsed crumbs
            // (Ionic .breadcrumbs-collapsed-indicator: 32x18, side margins, tinted background).
            [$".ion-breadcrumb.{mode} .breadcrumbs-collapsed-indicator"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Px(t.BreadcrumbIndicatorWidth),
                Height = Length.Px(t.BreadcrumbIndicatorHeight),
                MarginLeft = Length.Px(t.BreadcrumbIndicatorMarginX),
                MarginRight = Length.Px(t.BreadcrumbIndicatorMarginX),
                BackgroundColor = t.BreadcrumbIndicatorBackground,
                Color = t.BreadcrumbIndicatorColor,
                BorderRadius = new BorderRadius(Length.Px(t.BreadcrumbIndicatorBorderRadius)),
                Cursor = Cursor.Pointer,
            },

            // The indicator's ellipsis icon (Ionic sizes it at 22px, nudged down 1px).
            [$".ion-breadcrumb.{mode} .breadcrumbs-collapsed-indicator .ion-icon"] = new()
            {
                Color = t.BreadcrumbIndicatorColor,
                Width = Length.Px(t.BreadcrumbIndicatorIconSize),
                Height = Length.Px(t.BreadcrumbIndicatorIconSize),
                MarginTop = Length.Px(1),
            },

            // Indicator hover feedback (md: opacity .7, ios: opacity .45).
            [$".ion-breadcrumb.{mode} .breadcrumbs-collapsed-indicator:hover"] = new()
            {
                Opacity = mode == "ios" ? 0.45f : 0.7f,
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
        // Crumb-level color (ion-breadcrumb[color]): the host takes the palette base; the native
        // surface must be recolored too (Miko has no `inherit`, and .breadcrumb-native carries an
        // explicit color that would otherwise win).
        css[$".ion-breadcrumb.{mode}.ion-color-{name}"] = new() { Color = color };
        css[$".ion-breadcrumb.{mode}.ion-color-{name} .breadcrumb-native"] = new() { Color = color };

        // Container color (ion-breadcrumbs[color]): every crumb — including the active one — and
        // its separator take the palette base (Ionic :host(.in-breadcrumbs-color) /
        // :host(.in-breadcrumbs-color.breadcrumb-active) / .breadcrumb-separator rules). These
        // selectors intentionally out-rank the .breadcrumb-active color rules above.
        css[$".ion-breadcrumbs.{mode}.ion-color-{name} .ion-breadcrumb.in-breadcrumbs-color"] = new() { Color = color };
        css[$".ion-breadcrumbs.{mode}.ion-color-{name} .ion-breadcrumb.in-breadcrumbs-color .breadcrumb-native"] = new() { Color = color };
        css[$".ion-breadcrumbs.{mode}.ion-color-{name} .ion-breadcrumb.in-breadcrumbs-color .breadcrumb-separator"] = new() { Color = color };
    }
}
