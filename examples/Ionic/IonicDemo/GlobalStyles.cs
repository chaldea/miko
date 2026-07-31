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
            },

            [".component-detail"] = new()
            {
                PaddingBottom = Px(0),
                MarginBottom = Px(26),

                [".component-description"] = new()
                {
                    Color = (Color)"#3e4a58",
                    FontSize = Rem(1.125f),
                    LineHeight = Number(1.4f),
                    WhiteSpace = WhiteSpace.Normal,
                    PaddingBottom = Px(16),
                }
            }
        });
        return styleSheet;
    }
}
