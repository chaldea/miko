// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Common;
using Miko.Styling;

namespace MikoApp1.Razor
{
    internal class GlobalStyles
    {
        public static StyleSheet Create()
        {
            var styleSheet = new StyleSheet();

            //
            styleSheet.AddRule(Style.Class("layout")
                .Set(x => x.Display, Display.Flex)
                .Set(x => x.Width, Length.Percent(100))
                .Set(x => x.Height, Length.Percent(100)));

            styleSheet.AddRule(Style.Class("sidebar")
                .Set(x => x.Width, Length.Px(200))
                .Set(x => x.Height, Length.Percent(100))
                .Set(x => x.Padding, Length.Px(16))
                .Set(x => x.BackgroundColor, Color.FromRgb(52, 58, 64))
                .Set(x => x.Color, Color.White));

            styleSheet.AddRule(Style.Class("title")
                .Set(x => x.Color, Color.White));

            styleSheet.AddRule(Style.Class("main-content")
                .Set(x => x.FlexGrow, 1)
                .Set(x => x.Padding, Length.Px(16))
                .Set(x => x.OverflowY, Overflow.Scroll));

            styleSheet.AddRule(Style.Class("nav-item")
                .Set(x => x.Padding, Length.Px(10))
                .Set(x => x.MarginBottom, Length.Px(4))
                .Set(x => x.Color, Color.White));

            styleSheet.AddRule(Style.Class("icon-row")
                .Set(x => x.Display, Display.Flex)
                .Set(x => x.FlexDirection, FlexDirection.Row)
                .Set(x => x.FlexWrap, FlexWrap.Wrap)
                .Set(x => x.Gap, Length.Px(16))
                .Set(x => x.MarginBottom, Length.Px(20))
                .Set(x => x.AlignItems, AlignItems.FlexEnd));

            styleSheet.AddRule(Style.Class("icon-item")
                .Set(x => x.Display, Display.Flex)
                .Set(x => x.FlexDirection, FlexDirection.Column)
                .Set(x => x.AlignItems, AlignItems.Center)
                .Set(x => x.Width, Length.Px(80)));

            styleSheet.AddRule(Style.Class("icon-label")
                .Set(x => x.FontSize, Length.Px(12))
                .Set(x => x.Color, Color.FromRgb(108, 117, 125)));

            styleSheet.AddRule(Style.Class("icon-btn")
                .Set(x => x.Display, Display.Flex)
                .Set(x => x.FlexDirection, FlexDirection.Row)
                .Set(x => x.AlignItems, AlignItems.Center)
                .Set(x => x.Width, Length.Px(100)));

            styleSheet.AddRule(Style.Class("table-row-striped")
                .Set(x => x.BackgroundColor, Color.FromHex("f2f2f2")));

            return styleSheet;
        }
    }
}
