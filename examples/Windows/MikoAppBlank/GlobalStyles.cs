using Miko.Common;
using Miko.Styling;

namespace MikoAppBlank;

internal static class GlobalStyles
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
            [".main-content"] = new()
            {
                FlexGrow = 1,
                Display = Display.Flex
            },
            [".home"] = new()
            {
                FlexGrow = 1,
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
                Padding = new Padding(24)
            },
            [".home-title"] = new()
            {
                FontSize = Length.Px(32),
                FontWeight = FontWeight.Bold,
                Color = Color.FromRgb(33, 37, 41),
                MarginBottom = Length.Px(8)
            },
            [".home-subtitle"] = new()
            {
                FontSize = Length.Px(16),
                Color = Color.FromRgb(108, 117, 125),
                MarginBottom = Length.Px(24)
            },
            [".counter-btn"] = new()
            {
                Padding = new Padding(10, 20),
                BackgroundColor = Color.FromRgb(13, 110, 253),
                Color = Color.White,
                FontSize = Length.Px(14),
                BorderRadius = new BorderRadius(Length.Px(6)),
                Cursor = Cursor.Pointer
            }
        });

        return styleSheet;
    }
}
