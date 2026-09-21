// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Common;
using Miko.Ionic;

namespace Anime;

internal static class AppTheme
{
    public static IonicTheme Create() => new()
    {
        Palette = new()
        {
            Primary = Color.FromHex("#00b5e5"),
            BackgroundColor = Color.White,
            TextColor = Color.FromHex("#292b30"),
            Medium = Color.FromHex("#94979e")
        },
        Button = new()
        {
            SolidBackground = Color.FromHex("#00b5e5"),
            SolidColor = Color.White,
            TextColor = Color.FromHex("#00b5e5")
        },
        Tab = new()
        {
            BarHeight = 58,
            BarColor = Color.FromHex("#85878d"),
            BarColorSelected = Color.FromHex("#00b5e5"),
            ButtonFontSize = 11,
            ButtonIconSize = 24
        },
        Segment = new()
        {
            ButtonColor = Color.FromHex("#93969b"),
            ButtonCheckedColor = Color.FromHex("#00b5e5"),
            IndicatorColor = Color.FromHex("#00b5e5"),
            ButtonFontSize = 13,
            ButtonMinHeight = 40,
            ButtonMinWidth = 54,
            ButtonPaddingX = 12
        },
        Searchbar = new()
        {
            InputHeight = Length.Px(36),
            InputMinHeight = 36,
            InputFontSize = 13,
            InputBackground = Color.FromHex("#f6f6f7"),
            InputBorderRadius = 20,
            InputBoxShadow = []
        },
        Title = new()
        {
            FontSize = 15,
            TextAlign = TextAlign.Center
        },
        Toolbar = new() { MinHeight = 50 }
    };
}
