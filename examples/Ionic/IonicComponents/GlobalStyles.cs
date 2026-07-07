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
            ["p"] = new()
            {
                LineHeight = Px(32),
            },
            [".playground-container"] = new()
            {
                Border = new Border(1, BorderStyle.Solid, "#dee3ea"),
                BorderRadius = new BorderRadius(Rem(0.4f)),
                Height = Px(200)
            },
            [".container-row"] = new ()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Height = Percent(100),
            },
            [".container-column"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.Stretch,
                JustifyContent = JustifyContent.Center,
                Height = Percent(100),
            }
        });

        return styleSheet;
    }
}
