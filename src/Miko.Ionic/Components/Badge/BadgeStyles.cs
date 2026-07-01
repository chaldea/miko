using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>Styles for <c>ion-badge</c>.</summary>
internal static class BadgeStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            [$".ion-badge.{mode}"] = new()
            {
                Display = Display.InlineBlock,
                MinWidth = Length.Px(t.BadgeMinWidth),
                PaddingTop = Length.Px(t.BadgePaddingTop),
                PaddingRight = Length.Px(t.BadgePaddingEnd),
                PaddingBottom = Length.Px(t.BadgePaddingBottom),
                PaddingLeft = Length.Px(t.BadgePaddingStart),
                BackgroundColor = t.BadgeBackground,
                Color = t.BadgeColor,
                FontSize = Length.Px(t.BadgeFontSize),
                FontWeight = FontWeight.Bold,
                LineHeight = Length.Number(1),
                TextAlign = TextAlign.Center,
                WhiteSpace = WhiteSpace.Nowrap,
                VerticalAlign = VerticalAlign.Baseline,
                BorderRadius = new BorderRadius(Length.Px(t.BadgeBorderRadius)),
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
        css[$".ion-badge.{mode}.ion-color-{name}"] = new()
        {
            BackgroundColor = background,
            Color = color,
        };
    }
}
