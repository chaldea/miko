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
                Width = Length.Percent(100),
                MarginTop = Length.Px(t.CardMarginTop),
                MarginRight = Length.Px(t.CardMarginEnd),
                MarginBottom = Length.Px(t.CardMarginBottom),
                MarginLeft = Length.Px(t.CardMarginStart),
                BackgroundColor = t.CardBackground,
                Color = t.CardColor,
                FontSize = Length.Px(t.CardFontSize),
                LineHeight = t.CardLineHeight,
                BorderRadius = new BorderRadius(Length.Px(t.CardBorderRadius)),
                BoxShadow = t.CardBoxShadow.Count > 0 ? (StyleProperty<List<BoxShadow>>?)t.CardBoxShadow : null,
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
                // card-header.scss --color: inherit — the header follows the card, so a colored
                // card's contrast color reaches the titles inside it.
                Color = Css.Inherit,
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

        AddTitles(css, mode);

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
        // card-title.scss / card-subtitle.scss :host(.ion-color) { color: current-color(base) }.
        css[$".ion-card-title.{mode}.ion-color-{name}"] = new() { Color = background };
        css[$".ion-card-subtitle.{mode}.ion-color-{name}"] = new() { Color = background };
    }

    /// <summary>
    /// ion-card-title / ion-card-subtitle typography (card-title.{mode}.scss, card-subtitle.{mode}.scss
    /// and their vars). Without these rules both rendered like the card body — 14px, regular weight
    /// (ISSUE-153).
    /// </summary>
    private static void AddTitles(CssObject css, string mode)
    {
        var md = mode == "md";
        css[$".ion-card-title.{mode}"] = new()
        {
            Display = Display.Block,
            Position = Position.Relative,
            MarginTop = Length.Px(0),
            MarginBottom = Length.Px(0),
            PaddingTop = Length.Px(0),
            PaddingBottom = Length.Px(0),
            FontSize = Length.Px(md ? 20 : 28),                       // $card-{mode}-title-font-size
            FontWeight = md ? FontWeight.Medium : FontWeight.Bold,     // 500 / 700
            LineHeight = Length.Number(1.2f),
            Color = Color.FromHex(md ? "262626" : "000000"),          // text-color-step-150 / text-color
        };
        css[$".ion-card-subtitle.{mode}"] = new()
        {
            Display = Display.Block,
            Position = Position.Relative,
            MarginTop = Length.Px(0),
            MarginBottom = Length.Px(md ? 0 : 4),                      // $card-ios-subtitle-margin-bottom
            PaddingTop = Length.Px(0),
            PaddingBottom = Length.Px(0),
            FontSize = Length.Px(md ? 14 : 12),
            FontWeight = md ? FontWeight.Medium : FontWeight.Bold,
            TextTransform = md ? null : TextTransform.Uppercase,
            LetterSpacing = md ? null : Length.Px(0.4f),
            Color = Color.FromHex(md ? "737373" : "666666"),          // text-color-step-450 / -400
        };

        // In a colored card the titles follow its contrast color: the hosts carry Ionic's
        // ion-inherit-color class (core.scss .ion-color .ion-inherit-color { color: inherit }).
        css[$".ion-card.{mode}.ion-color .ion-card-title.{mode}:not(.ion-color)"] = new() { Color = Css.Inherit };
        css[$".ion-card.{mode}.ion-color .ion-card-subtitle.{mode}:not(.ion-color)"] = new() { Color = Css.Inherit };

        if (md)
        {
            // card-header.md.scss: stacked title / subtitle are 8px apart.
            css[$".ion-card-header.{mode} .ion-card-title.{mode}:not(:first-child)"] = new() { MarginTop = Length.Px(8) };
            css[$".ion-card-header.{mode} .ion-card-subtitle.{mode}:not(:first-child)"] = new() { MarginTop = Length.Px(8) };
        }
        else
        {
            // card-header.ios.scss :host(.card-header-translucent): a 90% opaque header. Miko has no
            // backdrop-filter, so the blur behind it is not reproduced.
            css[$".ion-card-header.{mode}.card-header-translucent"] = new()
            {
                BackgroundColor = Color.FromRgba(255, 255, 255, 0.9f),
            };
        }
    }
}
