using Miko.Common;
using Miko.Styling;

namespace IonicDemo;

internal class GlobalStyles
{
    public static StyleSheet Create()
    {
        var styleSheet = new StyleSheet();
        styleSheet.Add(new CssObject()
        {
            [".component-icon"] = new()
            {
                BorderRadius = Percent(50),
                Padding = Px(7),
                Height = Px(18),
                Width = Px(18),
                MarginTop = Px(5),
                MarginBottom = Px(5),
            },

            [".component-icon-primary"] = new()
            {
                BackgroundColor = (Color)"#0054e9",
                Color = (Color)"#fff",
            }
        });
        return styleSheet;
    }
}
