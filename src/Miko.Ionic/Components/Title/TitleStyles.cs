using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Stylesheet for <see cref="IonTitle"/>. Ported from Ionic's title base and mode styles.
/// </summary>
internal static class TitleStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        return new CssObject
        {
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
        };
    }
}
