using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>Styles for <c>ion-select</c> and <c>ion-select-option</c>.</summary>
internal static class SelectStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            [$".ion-select.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.SelectMinHeight),
                Color = t.SelectTextColor,
                FontSize = Length.Px(t.SelectFontSize),
            },

            [$".ion-select.{mode}.select-disabled"] = new()
            {
                Opacity = 0.4f,
                PointerEvents = PointerEvents.None,
            },

            [$".ion-select.{mode} .select-wrapper"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.SelectMinHeight),
                Cursor = Cursor.Pointer,
            },

            [$".ion-select.{mode} .select-wrapper-inner"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.SelectMinHeight),
                PaddingTop = Length.Px(t.SelectPaddingTop),
                PaddingRight = Length.Px(t.SelectPaddingEnd),
                PaddingBottom = Length.Px(t.SelectPaddingBottom),
                PaddingLeft = Length.Px(t.SelectPaddingStart),
                BoxSizing = BoxSizing.BorderBox,
            },

            [$".ion-select.{mode} .label-text-wrapper"] = new()
            {
                Display = Display.Block,
                Color = t.SelectLabelColor,
                FontSize = Length.Px(12),
                MarginBottom = Length.Px(2),
            },

            [$".ion-select.{mode} .native-wrapper"] = new()
            {
                Display = Display.Flex,
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Percent(0),
                Position = Position.Relative,
                AlignItems = AlignItems.Center,
                MinWidth = Length.Px(0),
            },

            [$".ion-select.{mode} .select-native"] = new()
            {
                Width = Length.Percent(100),
                MinHeight = Length.Px(t.SelectMinHeight),
                BackgroundColor = t.SelectBackground,
                Color = t.SelectTextColor,
                BorderWidth = Length.Px(0),
                Opacity = 0f,
                Position = Position.Absolute,
                Left = Length.Px(0),
                Right = Length.Px(0),
                Top = Length.Px(0),
                Bottom = Length.Px(0),
                Cursor = Cursor.Pointer,
                ZIndex = 1,
            },

            [$".ion-select.{mode} .select-text"] = new()
            {
                Color = t.SelectTextColor,
                WhiteSpace = WhiteSpace.Nowrap,
                Overflow = Overflow.Hidden,
            },

            [$".ion-select.{mode} .select-placeholder"] = new()
            {
                Color = t.SelectPlaceholderColor,
            },

            [$".ion-select.{mode} .select-icon"] = new()
            {
                Width = Length.Px(20),
                Height = Length.Px(20),
                MarginLeft = Length.Px(8),
                FlexShrink = 0,
            },

            [$".ion-select.{mode} .select-highlight"] = new()
            {
                Height = Length.Px(2),
                BackgroundColor = t.SelectHighlightColor,
                Width = Length.Percent(100),
            },

            [$".ion-select.{mode}.select-fill-solid .select-wrapper-inner"] = new()
            {
                BackgroundColor = mode == "md" ? new Color(0, 0, 0, 10) : t.SelectBackground,
                BorderRadius = new BorderRadius(Length.Px(t.SelectBorderRadius)),
                PaddingLeft = Length.Px(12),
                PaddingRight = Length.Px(12),
            },

            [$".ion-select.{mode}.select-fill-outline .select-wrapper-inner"] = new()
            {
                BorderWidth = Length.Px(1),
                BorderStyle = BorderStyle.Solid,
                BorderColor = t.SelectBorderColor,
                BorderRadius = new BorderRadius(Length.Px(t.SelectBorderRadius)),
                PaddingLeft = Length.Px(12),
                PaddingRight = Length.Px(12),
            },

            [$".ion-select.{mode}.select-shape-round .select-wrapper-inner"] = new()
            {
                BorderRadius = new BorderRadius(Length.Px(t.SelectRoundBorderRadius)),
            },

            [$".ion-select.{mode} .select-bottom"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                PaddingTop = Length.Px(4),
                FontSize = Length.Px(12),
            },

            [$".ion-select.{mode} .helper-text"] = new()
            {
                Color = t.SelectHelperColor,
            },

            [$".ion-select.{mode} .error-text"] = new()
            {
                Color = t.SelectErrorColor,
            },

            [$".ion-select-option.{mode}"] = new()
            {
                Display = Display.Block,
                PaddingTop = Length.Px(8),
                PaddingRight = Length.Px(12),
                PaddingBottom = Length.Px(8),
                PaddingLeft = Length.Px(12),
                Color = t.SelectTextColor,
            },

            [$".ion-select-option.{mode}.select-option-disabled"] = new()
            {
                Opacity = 0.4f,
                PointerEvents = PointerEvents.None,
            },
        };

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
        css[$".ion-select.{mode}.ion-color-{name}"] = new()
        {
            Color = color,
        };
        css[$".ion-select.{mode}.ion-color-{name} .select-highlight"] = new()
        {
            BackgroundColor = color,
        };
        css[$".ion-select.{mode}.ion-color-{name} .select-icon"] = new()
        {
            Color = color,
        };
    }
}
