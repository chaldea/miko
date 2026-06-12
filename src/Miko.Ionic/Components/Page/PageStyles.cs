using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the page-structure components (<c>ion-page</c>, <c>ion-header</c>,
/// <c>ion-toolbar</c>, <c>ion-title</c>, <c>ion-content</c>). Ported from the Ionic
/// Material Design source: <c>header.scss</c> / <c>header.md.scss</c>,
/// <c>toolbar.scss</c> / <c>toolbar.md.scss</c>, <c>title.scss</c> / <c>title.md.scss</c>,
/// <c>content.scss</c>.
/// </summary>
internal static class PageStyles
{
    internal static CssObject GenStyle(IonicTheme t)
    {
        return new CssObject
        {
            // ion-page — flex column shell: header on top, content fills the rest.
            // Grows to fill its flex parent (the tabs-inner content area) so percentage
            // heights are not resolved against a zero-height basis.
            [".ion-page"] = new()
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
            [".ion-header"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                BoxShadow = t.HeaderBoxShadow,
                ZIndex = 10,
            },

            // ion-toolbar — full-width bar holding the title.
            [".ion-toolbar"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                BackgroundColor = t.ToolbarBackground,
                Color = t.ToolbarColor,
            },

            [".ion-toolbar .toolbar-container"] = new()
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

            // ion-title — grows to fill the toolbar; MD typography.
            [".ion-title"] = new()
            {
                Display = Display.Flex,
                FlexGrow = 1,
                AlignItems = AlignItems.Center,
                PaddingLeft = Length.Px(t.TitlePaddingX),
                PaddingRight = Length.Px(t.TitlePaddingX),
                FontSize = Length.Px(t.TitleFontSize),
                FontWeight = t.TitleFontWeight,
                Color = t.ToolbarColor,
            },

            [".ion-title .toolbar-title"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                WhiteSpace = WhiteSpace.Nowrap,
                OverflowX = Overflow.Hidden,
            },

            // ion-content — region filling the remaining page height below the header.
            [".ion-content"] = new()
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

            [".ion-content .inner-scroll"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                OverflowY = Overflow.Auto,
            },
        };
    }
}
