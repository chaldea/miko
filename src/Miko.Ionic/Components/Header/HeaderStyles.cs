using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Stylesheet for <see cref="IonHeader"/>. Ported from Ionic's header base and mode styles.
/// </summary>
internal static class HeaderStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        return new CssObject
        {
            // The header is a positioned layer so its shadow/border stays above page content.
            [$".ion-header.{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                Width = Length.Percent(100),
                BoxShadow = t.HeaderBoxShadow.Count > 0
                    ? (StyleProperty<List<BoxShadow>>?)t.HeaderBoxShadow
                    : null,
                BorderBottom = t.HeaderBorderWidth > 0
                    ? new BorderSide(Length.Px(t.HeaderBorderWidth), BorderStyle.Solid, t.HeaderBorderColor)
                    : new BorderSide(Length.Px(0), BorderStyle.None, Color.Transparent),
                ZIndex = 10,
            },

            // Only the first toolbar in a header is under the system status bar.
            [$".ion-header.{mode} .ion-toolbar:first-of-type"] = new()
            {
                PaddingTop = Length.SafeAreaInsetTop,
            },
        };
    }
}
