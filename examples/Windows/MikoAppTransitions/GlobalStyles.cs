using Miko.Common;
using Miko.Styling;

namespace MikoAppTransitions;

internal static class GlobalStyles
{
    public static StyleSheet Create()
    {
        var styleSheet = new StyleSheet();

        styleSheet.Add(new CssObject
        {
            [".demo-content"] = new()
            {
                Padding = new Padding(20),
            },

            // 每种转场效果一个主题色的详情页头图，让图层滑动/淡入在视觉上清晰可辨。
            [".hero"] = new()
            {
                Padding = new Padding(36, 20),
                Color = Color.FromHex("#ffffff"),
                BackgroundColor = Color.FromHex("#5260ff"),
            },
            [".hero-ios"] = new() { BackgroundColor = Color.FromHex("#5260ff") },
            [".hero-slide"] = new() { BackgroundColor = Color.FromHex("#2dd36f") },
            [".hero-fade"] = new() { BackgroundColor = Color.FromHex("#eb445a") },
            [".hero-modal"] = new() { BackgroundColor = Color.FromHex("#7044ff") },
            [".hero-none"] = new() { BackgroundColor = Color.FromHex("#92949c") },

            [".hero-title"] = new()
            {
                FontSize = Length.Px(24),
                FontWeight = FontWeight.Bold,
            },
            [".hero-sub"] = new()
            {
                FontSize = Length.Px(14),
                MarginTop = Length.Px(8),
            },
        });

        return styleSheet;
    }
}
