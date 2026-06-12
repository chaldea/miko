using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the tab layout components (<c>ion-tabs</c>, <c>ion-tab-bar</c>,
/// <c>ion-tab-button</c>). Ported from the Ionic Material Design source:
/// <c>tabs.scss</c>, <c>tab-bar.scss</c> / <c>tab-bar.md.scss</c>,
/// <c>tab-button.scss</c> / <c>tab-button.md.scss</c>.
/// </summary>
internal static class TabStyles
{
    internal static CssObject GenStyle(IonicTheme t)
    {
        return new CssObject
        {
            // ion-tabs — flex column container that fills its parent.
            [".ion-tabs"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            },

            // tabs-inner — the routed content area; grows to push the tab bar to the edge.
            [".tabs-inner"] = new()
            {
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Px(0),
            },

            // ion-tab-bar — the bar holding the tab buttons.
            [".ion-tab-bar"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
                Height = Length.Px(t.TabBarHeight),
                BackgroundColor = t.TabBarBackground,
                Color = t.TabBarColor,
                TextAlign = TextAlign.Center,
            },

            // slot="bottom" (default): border on top.
            [".ion-tab-bar-bottom"] = new()
            {
                BorderTop = new BorderSide(Length.Px(t.TabBarBorderWidth), BorderStyle.Solid, t.TabBarBorderColor),
            },

            // slot="top": border on the bottom instead.
            [".ion-tab-bar-top"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(t.TabBarBorderWidth), BorderStyle.Solid, t.TabBarBorderColor),
            },

            // ion-tab-button — each button: icon stacked above label, vertically centered.
            [".ion-tab-button"] = new()
            {
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Px(0),
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Height = Length.Percent(100),
                MaxWidth = Length.Px(t.TabButtonMaxWidth),
                PaddingLeft = Length.Px(t.TabButtonPaddingX),
                PaddingRight = Length.Px(t.TabButtonPaddingX),
                Color = t.TabBarColor,
                FontSize = Length.Px(t.TabButtonFontSize),
                TextDecoration = TextDecoration.None,
            },

            // Selected tab button takes the selected (primary) color.
            [".ion-tab-button.tab-selected"] = new()
            {
                Color = t.TabBarColorSelected,
            },
        };
    }
}
