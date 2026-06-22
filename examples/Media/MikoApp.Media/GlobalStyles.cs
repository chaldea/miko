using Miko.Common;
using Miko.Styling;

namespace MikoApp.Media;

internal static class GlobalStyles
{
    public static StyleSheet Create()
    {
        var styleSheet = new StyleSheet();

        styleSheet.Add(new CssObject
        {
            [".app-root"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                BackgroundColor = new Color(0x1A, 0x1D, 0x21),
                Padding = new Padding(20),
            },

            [".app-header"] = new()
            {
                Color = Color.White,
                FontSize = Length.Px(22),
                MarginBottom = Length.Px(16),
            },

            [".loading"] = new()
            {
                Color = new Color(0xAD, 0xB5, 0xBD),
                Padding = new Padding(20),
            },

            // 视频英雄区：固定尺寸，圆角 + overflow:hidden 演示盒模型裁剪。
            [".hero"] = new()
            {
                Display = Display.Block,
                Width = Length.Px(480),
                Height = Length.Px(270),
                BorderRadius = Length.Px(12),
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
                MarginBottom = Length.Px(24),
                BackgroundColor = Color.Black,
            },

            // 缩略图网格：flex 换行。
            [".grid"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                FlexWrap = FlexWrap.Wrap,
            },

            // 卡片固定尺寸，确保图片加载前后布局稳定（占位图→真实图无重排）。
            [".card"] = new()
            {
                Width = Length.Px(160),
                MarginRight = Length.Px(16),
                MarginBottom = Length.Px(16),
                BackgroundColor = new Color(0x26, 0x2A, 0x2F),
                BorderRadius = Length.Px(8),
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
            },

            // <img> 显式定尺寸：加载前显示占位图，加载完成后填充。
            [".thumb"] = new()
            {
                Display = Display.Block,
                Width = Length.Px(160),
                Height = Length.Px(160),
                BackgroundColor = new Color(0xE9, 0xEC, 0xEF),
            },

            [".card-name"] = new()
            {
                Color = Color.White,
                FontSize = Length.Px(14),
                Padding = new Padding(8),
            },

            [".card-price"] = new()
            {
                Color = new Color(0xAD, 0xB5, 0xBD),
                FontSize = Length.Px(13),
                PaddingLeft = Length.Px(8),
                PaddingRight = Length.Px(8),
                PaddingBottom = Length.Px(10),
            },
        });

        return styleSheet;
    }
}
