using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for <c>ion-alert</c>. Ported from the Ionic source: <c>alert.scss</c> / <c>.md.scss</c> /
/// <c>.ios.scss</c> (+ their <c>*.vars.scss</c>).
/// <para>
/// A centered modal: a fixed full-screen host that centers a card (the wrapper). The card stacks a
/// head (title + sub-title), an optional message, an optional inputs group (text / radio /
/// checkbox), and a button group (a row; a column when there are more than two buttons). md
/// left-aligns the head and right-aligns uppercase buttons in a padded group; ios centers
/// everything and divides buttons with hairlines. Rules are scoped by the active mode class
/// (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/> for the mode-scoping rationale.
/// </para>
/// </summary>
internal static class AlertStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // Host — a fixed full-screen overlay that centers its card.
            [$".ion-alert.{mode}"] = new()
            {
                Position = Position.Fixed,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                ZIndex = 1000,
            },

            [$".ion-alert.{mode}.overlay-hidden"] = new()
            {
                Display = Display.None,
            },

            // Backdrop — the tappable dim layer.
            [$".ion-alert.{mode} .alert-backdrop"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                BackgroundColor = t.AlertBackdropColor,
                Opacity = t.AlertBackdropOpacity,
                Cursor = Cursor.Pointer,
            },

            // Wrapper — the centered card.
            [$".ion-alert.{mode} .alert-wrapper"] = new()
            {
                Position = Position.Relative,
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                MinWidth = Length.Px(t.AlertMinWidth),
                MaxWidth = Length.Px(t.AlertMaxWidth),
                BackgroundColor = t.AlertBackground,
                BorderRadius = new BorderRadius(Length.Px(t.AlertBorderRadius)),
                BoxShadow = t.AlertBoxShadow,
                ZIndex = 10,
            },

            // Head — the title/sub-title block.
            [$".ion-alert.{mode} .alert-head"] = new()
            {
                PaddingTop = Length.Px(t.AlertHeadPaddingY),
                PaddingBottom = Length.Px(t.AlertHeadPaddingY),
                PaddingLeft = Length.Px(t.AlertHeadPaddingX),
                PaddingRight = Length.Px(t.AlertHeadPaddingX),
                TextAlign = t.AlertHeadTextAlign,
            },

            [$".ion-alert.{mode} .alert-title"] = new()
            {
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(0),
                Color = t.AlertTitleColor,
                FontSize = Length.Px(t.AlertTitleFontSize),
                FontWeight = t.AlertTitleFontWeight,
            },

            [$".ion-alert.{mode} .alert-sub-title"] = new()
            {
                MarginTop = Length.Px(5),
                Color = t.AlertSubTitleColor,
                FontSize = Length.Px(t.AlertSubTitleFontSize),
                FontWeight = FontWeight.Normal,
            },

            // Message.
            [$".ion-alert.{mode} .alert-message"] = new()
            {
                PaddingTop = Length.Px(t.AlertMessagePaddingY),
                PaddingBottom = Length.Px(t.AlertMessagePaddingY),
                PaddingLeft = Length.Px(t.AlertMessagePaddingX),
                PaddingRight = Length.Px(t.AlertMessagePaddingX),
                Color = t.AlertMessageColor,
                FontSize = Length.Px(t.AlertMessageFontSize),
                TextAlign = t.AlertHeadTextAlign,
            },

            // Button group — a row; column when vertical (>2 buttons).
            [$".ion-alert.{mode} .alert-button-group"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                FlexWrap = FlexWrap.Wrap,
                Width = Length.Percent(100),
                PaddingTop = Length.Px(t.AlertButtonGroupPadding),
                PaddingBottom = Length.Px(t.AlertButtonGroupPadding),
                PaddingLeft = Length.Px(t.AlertButtonGroupPadding),
                PaddingRight = Length.Px(t.AlertButtonGroupPadding),
                JustifyContent = t.AlertButtonGroupJustify,
            },
            [$".ion-alert.{mode} .alert-button-group-vertical"] = new()
            {
                FlexDirection = FlexDirection.Column,
                FlexWrap = FlexWrap.Nowrap,
            },

            // Button.
            [$".ion-alert.{mode} .alert-button"] = new()
            {
                Position = Position.Relative,
                Display = Display.Block,
                MarginLeft = Length.Px(t.AlertButtonMarginX),
                PaddingTop = Length.Px(t.AlertButtonPadding),
                PaddingBottom = Length.Px(t.AlertButtonPadding),
                PaddingLeft = Length.Px(t.AlertButtonPadding),
                PaddingRight = Length.Px(t.AlertButtonPadding),
                BorderWidth = Length.Px(0),
                BorderRadius = new BorderRadius(Length.Px(t.AlertButtonBorderRadius)),
                BackgroundColor = Color.Transparent,
                Color = t.AlertButtonColor,
                FontSize = Length.Px(t.AlertButtonFontSize),
                FontWeight = t.AlertButtonFontWeight,
                TextTransform = t.AlertButtonTextTransform,
                Cursor = Cursor.Pointer,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            [$".ion-alert.{mode} .alert-button-inner"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            },

            // Inputs group.
            [$".ion-alert.{mode} .alert-input-group"] = new()
            {
                PaddingTop = Length.Px(t.AlertMessagePaddingY),
                PaddingBottom = Length.Px(t.AlertMessagePaddingY),
                PaddingLeft = Length.Px(t.AlertMessagePaddingX),
                PaddingRight = Length.Px(t.AlertMessagePaddingX),
            },
            [$".ion-alert.{mode} .alert-input"] = new()
            {
                Width = Length.Percent(100),
                PaddingTop = Length.Px(10),
                PaddingBottom = Length.Px(10),
                BorderWidth = Length.Px(0),
                BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, t.AlertListBorderColor),
                BackgroundColor = Color.Transparent,
                Color = t.AlertTitleColor,
                BoxSizing = BoxSizing.BorderBox,
            },

            // Radio / checkbox groups — bordered scroll regions.
            [$".ion-alert.{mode} .alert-radio-group"] = new()
            {
                Position = Position.Relative,
                BorderTop = new BorderSide(Length.Px(1), BorderStyle.Solid, t.AlertListBorderColor),
                BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, t.AlertListBorderColor),
                OverflowY = Overflow.Auto,
            },
            [$".ion-alert.{mode} .alert-checkbox-group"] = new()
            {
                Position = Position.Relative,
                BorderTop = new BorderSide(Length.Px(1), BorderStyle.Solid, t.AlertListBorderColor),
                BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, t.AlertListBorderColor),
                OverflowY = Overflow.Auto,
            },

            // Tappable radio/checkbox rows.
            [$".ion-alert.{mode} .alert-tappable"] = new()
            {
                Display = Display.Flex,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.AlertTappableHeight),
                BorderWidth = Length.Px(0),
                BackgroundColor = Color.Transparent,
                Cursor = Cursor.Pointer,
                TextAlign = TextAlign.Left,
            },
            [$".ion-alert.{mode} .alert-radio-label"] = new()
            {
                FlexGrow = 1,
                PaddingTop = Length.Px(13),
                PaddingBottom = Length.Px(13),
                PaddingLeft = Length.Px(52),
                PaddingRight = Length.Px(26),
                Color = t.AlertMessageColor,
                FontSize = Length.Px(16),
            },
            [$".ion-alert.{mode} .alert-checkbox-label"] = new()
            {
                FlexGrow = 1,
                PaddingTop = Length.Px(13),
                PaddingBottom = Length.Px(13),
                PaddingLeft = Length.Px(53),
                PaddingRight = Length.Px(26),
                Color = t.AlertMessageColor,
                FontSize = Length.Px(16),
            },

            // Radio unchecked circle.
            [$".ion-alert.{mode} .alert-radio-icon"] = new()
            {
                Position = Position.Relative,
                Display = Display.Block,
                Width = Length.Px(20),
                Height = Length.Px(20),
                BorderWidth = Length.Px(2),
                BorderStyle = BorderStyle.Solid,
                BorderColor = t.AlertControlBorderColorOff,
                BorderRadius = new BorderRadius(Length.Percent(50)),
            },
            // Radio checked accent.
            [$".ion-alert.{mode} .alert-radio-button-checked .alert-radio-icon"] = new()
            {
                BorderColor = t.AlertControlAccent,
            },
            [$".ion-alert.{mode} .alert-radio-button-checked .alert-radio-inner"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(3),
                Left = Length.Px(3),
                Width = Length.Px(10),
                Height = Length.Px(10),
                BackgroundColor = t.AlertControlAccent,
                BorderRadius = new BorderRadius(Length.Percent(50)),
            },

            // Checkbox unchecked square.
            [$".ion-alert.{mode} .alert-checkbox-icon"] = new()
            {
                Position = Position.Relative,
                Width = Length.Px(16),
                Height = Length.Px(16),
                BorderWidth = Length.Px(2),
                BorderStyle = BorderStyle.Solid,
                BorderColor = t.AlertControlBorderColorOff,
                BorderRadius = new BorderRadius(Length.Px(2)),
            },
            // Checkbox checked fill.
            [$".ion-alert.{mode} .alert-checkbox-button-checked .alert-checkbox-icon"] = new()
            {
                BorderColor = t.AlertControlAccent,
                BackgroundColor = t.AlertControlAccent,
            },

            // Disabled tappable rows.
            [$".ion-alert.{mode} .alert-radio-button-disabled"] = new()
            {
                Opacity = 0.5f,
                PointerEvents = PointerEvents.None,
            },
            [$".ion-alert.{mode} .alert-checkbox-button-disabled"] = new()
            {
                Opacity = 0.5f,
                PointerEvents = PointerEvents.None,
            },
            [$".ion-alert.{mode} .alert-input-disabled"] = new()
            {
                Opacity = 0.5f,
                PointerEvents = PointerEvents.None,
            },
        };

        // iOS divides stacked buttons with a hairline top border (md relies on margins instead).
        if (mode == "ios")
        {
            css[$".ion-alert.{mode} .alert-button-group .alert-button"] = new()
            {
                FlexGrow = 1,
                BorderTop = new BorderSide(Length.Px(0.55f), BorderStyle.Solid, t.AlertListBorderColor),
            };
        }

        return css;
    }
}
