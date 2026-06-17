using Miko.Common;
using Miko.Styling;

namespace MikoApp1.Razor;

internal class GlobalStyles
{
    public static StyleSheet Create()
    {
        var styleSheet = new StyleSheet();

        styleSheet.Add(new CssObject
        {
            [".layout"] = new()
            {
                Display = Display.Flex,
                Width = Length.Percent(100),
                Height = Length.Percent(100)
            },
            [".sidebar"] = new()
            {
                Width = Length.Px(200),
                Height = Length.Percent(100),
                Padding = new Padding(16),
                BackgroundColor = Color.FromRgb(52, 58, 64),
                Color = Color.White
            },
            [".title"] = new() { Color = Color.White },
            [".main-content"] = new()
            {
                FlexGrow = 1,
                Padding = new Padding(16),
                OverflowY = Overflow.Scroll
            },
            [".nav-item"] = new()
            {
                Padding = new Padding(10),
                MarginBottom = Length.Px(4),
                Color = Color.White
            },
            [".icon-row"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                FlexWrap = FlexWrap.Wrap,
                Gap = Length.Px(16),
                MarginBottom = Length.Px(20),
                AlignItems = AlignItems.FlexEnd
            },
            [".icon-item"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.Center,
                Width = Length.Px(80)
            },
            [".icon-label"] = new()
            {
                FontSize = Length.Px(12),
                Color = Color.FromRgb(108, 117, 125)
            },
            [".icon-btn"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                Width = Length.Px(100)
            },
            [".table-row-striped"] = new()
            {
                BackgroundColor = Color.FromHex("f2f2f2")
            }
        });

        return styleSheet;
    }
}
