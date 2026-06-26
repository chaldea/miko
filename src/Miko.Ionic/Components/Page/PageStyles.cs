using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the page-structure components (<c>ion-page</c>, <c>ion-header</c>,
/// <c>ion-toolbar</c>, <c>ion-title</c>, <c>ion-content</c>). Ported from the Ionic
/// source: <c>header.scss</c> / <c>header.md.scss</c> / <c>header.ios.scss</c>,
/// <c>toolbar.scss</c> / <c>toolbar.md.scss</c> / <c>toolbar.ios.scss</c>,
/// <c>title.scss</c> / <c>title.md.scss</c> / <c>title.ios.scss</c>, <c>content.scss</c>.
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>) that each component
/// stamps onto its root element, so a single stylesheet carries both modes and switching the
/// root class alone re-styles the tree. <paramref name="mode"/> is woven into each selector;
/// the mode-specific values come from <paramref name="t"/> (a per-mode <see cref="IonicTheme"/>).
/// </para>
/// </summary>
internal static class PageStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        return new CssObject
        {
            // ion-page — flex column shell: header on top, content fills the rest.
            // Grows to fill its flex parent (the tabs-inner content area) so percentage
            // heights are not resolved against a zero-height basis.
            [$".ion-page.{mode}"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            },

            // ion-header — block band above the content (order: -1 in Ionic).
            // MD mode renders an elevation shadow; iOS mode a hairline bottom border.
            // position:relative + z-index lift the header (and its shadow) above the
            // content that follows it in normal flow.
            [$".ion-header.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                Width = Length.Percent(100),
                BoxShadow = t.HeaderBoxShadow.Count > 0 ? t.HeaderBoxShadow : null,
                BorderBottom = t.HeaderBorderWidth > 0
                    ? new BorderSide(Length.Px(t.HeaderBorderWidth), BorderStyle.Solid, t.HeaderBorderColor)
                    : new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
                ZIndex = 10,
            },

            // ion-toolbar — full-width bar holding the title. Every toolbar clears a side notch
            // via env(safe-area-inset-left/right) (Ionic's toolbar.scss :host), but NOT the top:
            // only the FIRST toolbar in a header sits under the status bar, so its top inset is
            // applied by the header rule below — matching Ionic's
            // `ion-header ion-toolbar:first-of-type { padding-top: safe-area-top }`.
            // On desktop / zero-inset platforms these env() lengths resolve to 0 (no-op).
            [$".ion-toolbar.{mode}"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                BackgroundColor = t.ToolbarBackground,
                Color = t.ToolbarColor,
                PaddingLeft = Length.SafeAreaInsetLeft,
                PaddingRight = Length.SafeAreaInsetRight,
            },

            // Only the first toolbar inside a header gets the top safe-area inset (it's the one
            // under the system status bar). Subsequent toolbars in the same header keep zero top
            // padding. Ported from header.scss: `ion-header ion-toolbar:first-of-type`.
            [$".ion-header.{mode} .ion-toolbar:first-of-type"] = new()
            {
                PaddingTop = Length.SafeAreaInsetTop,
            },

            [$".ion-toolbar.{mode} .toolbar-container"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceBetween,
                Width = Length.Percent(100),
                // Ionic uses --min-height: 56px; the flex layout honors explicit height,
                // so set it directly to give the toolbar its fixed band height.
                Height = Length.Px(t.ToolbarMinHeight),
                MinHeight = Length.Px(t.ToolbarMinHeight),
            },

            // ion-title — grows to fill the toolbar. MD left-aligns; iOS centers.
            [$".ion-title.{mode}"] = new()
            {
                Display = Display.Flex,
                FlexGrow = 1,
                AlignItems = AlignItems.Center,
                JustifyContent = t.TitleTextAlign == TextAlign.Center
                    ? JustifyContent.Center
                    : JustifyContent.FlexStart,
                PaddingLeft = Length.Px(t.TitlePaddingX),
                PaddingRight = Length.Px(t.TitlePaddingX),
                FontSize = Length.Px(t.TitleFontSize),
                FontWeight = t.TitleFontWeight,
                Color = t.ToolbarColor,
            },

            [$".ion-title.{mode} .toolbar-title"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                WhiteSpace = WhiteSpace.Nowrap,
                OverflowX = Overflow.Hidden,
                TextAlign = t.TitleTextAlign,
            },

            // ion-content — region filling the remaining page height below the header.
            [$".ion-content.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Px(0),
                Width = Length.Percent(100),
                BackgroundColor = t.ContentBackground,
                Color = t.ContentColor,
                OverflowY = Overflow.Auto,
            },

            [$".ion-content.{mode} .inner-scroll"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                OverflowY = Overflow.Auto,
            },
        };
    }
}
