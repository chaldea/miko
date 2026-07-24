using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the tab layout components (<c>ion-tabs</c>, <c>ion-tab-bar</c>,
/// <c>ion-tab-button</c>). Ported from the Ionic source:
/// <c>tabs.scss</c>, <c>tab-bar.scss</c> / <c>tab-bar.md.scss</c> / <c>tab-bar.ios.scss</c>,
/// <c>tab-button.scss</c> / <c>tab-button.md.scss</c> / <c>tab-button.ios.scss</c>.
/// <para>
/// Rules are scoped by the active mode class (<c>md</c> / <c>ios</c>); see
/// <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class TabStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        // Distance of the badge overlay from the top of the tab button, for the default icon-top
        // layout. md sits it a touch lower than ios (tab-button.md.scss: 8px; tab-button.ios.scss: 4px).
        var badgeTop = mode == "ios" ? 4f : 8f;

        // tab-button.md.scss drops the badge to normal weight and leaves line-height at the base
        // (1); tab-button.ios.scss keeps the base bold weight but pins line-height to 16px.
        var badgeFontWeight = mode == "ios" ? FontWeight.Bold : FontWeight.Normal;
        var badgeLineHeight = mode == "ios" ? Length.Px(16) : Length.Number(1);

        var css = new CssObject
        {
            // ion-tabs — flex column container that fills its parent.
            [$".ion-tabs.{mode}"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            },

            // tabs-inner — the routed content area; grows to push the tab bar to the edge.
            // A flex column so its child page fills the available height. OverflowY:Hidden
            // makes it adopt the flex-assigned height as its content height (so the grow
            // child page is sized correctly) instead of collapsing to a zero basis.
            [$".ion-tabs.{mode} .tabs-inner"] = new()
            {
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Px(0),
                Width = Length.Percent(100),
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                OverflowY = Overflow.Hidden,
            },

            // ion-tab-bar — the bar holding the tab buttons. On mobile the bar sits at the bottom
            // edge under the system navigation bar / home indicator; env(safe-area-inset-bottom)
            // pads it so the buttons clear that band while the bar background fills behind it.
            // Zero-inset platforms (desktop) resolve the env() length to 0 (no-op).
            [$".ion-tab-bar.{mode}"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
                Height = Length.Px(t.TabBarHeight),
                BackgroundColor = t.TabBarBackground,
                Color = t.TabBarColor,
                TextAlign = TextAlign.Center,
                PaddingBottom = Length.SafeAreaInsetBottom,
                PaddingLeft = Length.SafeAreaInsetLeft,
                PaddingRight = Length.SafeAreaInsetRight,
                BoxSizing = BoxSizing.ContentBox,
            },

            // slot="bottom" (default): border on top.
            [$".ion-tab-bar.{mode}.ion-tab-bar-bottom"] = new()
            {
                BorderTop = new BorderSide(Length.Px(t.TabBarBorderWidth), BorderStyle.Solid, t.TabBarBorderColor),
            },

            // slot="top": border on the bottom instead.
            [$".ion-tab-bar.{mode}.ion-tab-bar-top"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(t.TabBarBorderWidth), BorderStyle.Solid, t.TabBarBorderColor),
            },

            // ion-tab-button — each button: icon stacked above label, vertically centered.
            // Position:relative so it is the containing block for the absolutely-positioned badge
            // overlay below (tab-button.scss :host + .button-native are relative in Ionic).
            [$".ion-tab-button.{mode}"] = new()
            {
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Px(0),
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Position = Position.Relative,
                Height = Length.Percent(100),
                MaxWidth = Length.Px(t.TabButtonMaxWidth),
                PaddingLeft = Length.Px(t.TabButtonPaddingX),
                PaddingRight = Length.Px(t.TabButtonPaddingX),
                Color = t.TabBarColor,
                FontSize = Length.Px(t.TabButtonFontSize),
                TextDecoration = TextDecoration.None,
            },

            // Selected tab button takes the selected (primary) color.
            [$".ion-tab-button.{mode}.tab-selected"] = new()
            {
                Color = t.TabBarColorSelected,
            },

            // Badge overlay — floats over the top-right of the icon rather than flowing under the
            // label. tab-button.scss ::slotted(ion-badge): absolute + border-box + z-index 1; left
            // is a percent+px calc so it tracks the button's horizontal center as the bar flexes.
            // The mode sheets then shrink the badge for the tab bar (tab-button.md.scss /
            // tab-button.ios.scss): md uses an 8px font, 3/2/2/2 padding, min-width 12, radius 8 and
            // normal weight; ios uses a 12px font with 1/6 padding and a 16px line-height.
            [$".ion-tab-button.{mode} .ion-badge"] = new()
            {
                BoxSizing = BoxSizing.BorderBox,
                Position = Position.Absolute,
                Top = Length.Px(badgeTop),
                Left = Length.Percent(50) + Length.Px(6),
                ZIndex = 1,

                BorderRadius = new BorderRadius(Length.Px(t.TabButtonBadgeBorderRadius)),
                PaddingTop = Length.Px(t.TabButtonBadgePaddingTop),
                PaddingRight = Length.Px(t.TabButtonBadgePaddingEnd),
                PaddingBottom = Length.Px(t.TabButtonBadgePaddingBottom),
                PaddingLeft = Length.Px(t.TabButtonBadgePaddingStart),
                MinWidth = Length.Px(t.TabButtonBadgeMinWidth),
                FontSize = Length.Px(t.TabButtonBadgeFontSize),
                FontWeight = badgeFontWeight,
                LineHeight = badgeLineHeight,
            },
        };

        // ::slotted(ion-badge:empty) — md only: an empty badge collapses to an 8x8 dot instead of
        // disappearing, overriding the base `.ion-badge:empty { display: none }` (tab-button.md.scss).
        // On ios there is no such override, so an empty badge stays hidden.
        if (mode == "md")
        {
            css[$".ion-tab-button.{mode} .ion-badge:empty"] = new()
            {
                Display = Display.Block,
                MinWidth = Length.Px(t.TabButtonBadgeSizeEmpty),
                Height = Length.Px(t.TabButtonBadgeSizeEmpty),
            };
        }

        return css;
    }
}
