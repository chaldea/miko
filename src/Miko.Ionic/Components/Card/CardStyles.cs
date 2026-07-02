using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>Styles for <c>ion-card</c>, <c>ion-card-header</c>, and <c>ion-card-content</c>.</summary>
internal static class CardStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            [$".ion-card.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                MarginTop = Length.Px(t.CardMarginTop),
                MarginRight = Length.Px(t.CardMarginEnd),
                MarginBottom = Length.Px(t.CardMarginBottom),
                MarginLeft = Length.Px(t.CardMarginStart),
                BackgroundColor = t.CardBackground,
                Color = t.CardColor,
                FontSize = Length.Px(t.CardFontSize),
                LineHeight = t.CardLineHeight,
                BorderRadius = new BorderRadius(Length.Px(t.CardBorderRadius)),
                BoxShadow = t.CardBoxShadow.Count > 0 ? t.CardBoxShadow : null,
                Overflow = Overflow.Hidden,
            },

            [$".ion-card.{mode}.card-disabled"] = new()
            {
                Opacity = 0.3f,
                Cursor = Cursor.Default,
                PointerEvents = PointerEvents.None,
            },

            [$".ion-card.{mode} .card-native"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                MinHeight = Length.Px(0),
                PaddingTop = Length.Px(0),
                PaddingRight = Length.Px(0),
                PaddingBottom = Length.Px(0),
                PaddingLeft = Length.Px(0),
                MarginTop = Length.Px(0),
                MarginRight = Length.Px(0),
                MarginBottom = Length.Px(0),
                MarginLeft = Length.Px(0),
                BackgroundColor = t.CardBackground,
                Color = t.CardColor,
                BorderWidth = Length.Px(0),
                TextDecoration = TextDecoration.None,
                Cursor = Cursor.Pointer,
            },

            [$".ion-card-header.{mode}"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                FlexDirection = mode == "ios" ? FlexDirection.ColumnReverse : FlexDirection.Column,
                PaddingTop = Length.Px(t.CardHeaderPaddingTop),
                PaddingRight = Length.Px(t.CardHeaderPaddingEnd),
                PaddingBottom = Length.Px(t.CardHeaderPaddingBottom),
                PaddingLeft = Length.Px(t.CardHeaderPaddingStart),
                BackgroundColor = Color.Transparent,
                Color = t.CardColor,
            },

            [$".ion-card-content.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                PaddingTop = Length.Px(t.CardContentPaddingTop),
                PaddingRight = Length.Px(t.CardContentPaddingEnd),
                PaddingBottom = Length.Px(t.CardContentPaddingBottom),
                PaddingLeft = Length.Px(t.CardContentPaddingStart),
                FontSize = Length.Px(t.CardContentFontSize),
                LineHeight = t.CardContentLineHeight,
            },

            [$".ion-card-header.{mode} + .ion-card-content.{mode}"] = new()
            {
                PaddingTop = Length.Px(0),
            },
        };

        AddColor(css, mode, "primary", t.Primary, Color.White);
        AddColor(css, mode, "secondary", t.Secondary, Color.White);
        AddColor(css, mode, "tertiary", t.Tertiary, Color.White);
        AddColor(css, mode, "success", t.Success, Color.Black);
        AddColor(css, mode, "warning", t.Warning, Color.Black);
        AddColor(css, mode, "danger", t.Danger, Color.White);
        AddColor(css, mode, "light", t.Light, Color.Black);
        AddColor(css, mode, "medium", t.Medium, Color.White);
        AddColor(css, mode, "dark", t.Dark, Color.White);

        return css;
    }

    private static void AddColor(CssObject css, string mode, string name, Color background, Color color)
    {
        css[$".ion-card.{mode}.ion-color-{name}"] = new()
        {
            BackgroundColor = background,
            Color = color,
        };
        css[$".ion-card-header.{mode}.ion-color-{name}"] = new()
        {
            BackgroundColor = background,
            Color = color,
        };
    }
}
