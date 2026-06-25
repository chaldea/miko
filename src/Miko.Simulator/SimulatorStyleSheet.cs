using Miko.Common;
using Miko.Styling;
using Miko.Styling.Selectors;

namespace Miko.Simulator;

/// <summary>
/// 模拟器右侧设置面板的样式表。面板本身用 Miko 引擎布局/渲染，
/// 因此其外观完全由这张样式表驱动（与 DevTools 面板同理）。
/// </summary>
internal static class SimulatorStyleSheet
{
    private static readonly Color BgDark = new(32, 33, 36);
    private static readonly Color BgMedium = new(45, 46, 50);
    private static readonly Color BgLight = new(60, 62, 67);
    private static readonly Color BgSelected = new(38, 79, 120);
    private static readonly Color Accent = new(0, 122, 204);
    private static readonly Color TextPrimary = new(222, 223, 226);
    private static readonly Color TextSecondary = new(150, 152, 158);
    private static readonly Color BorderColor = new(58, 59, 64);

    public static StyleSheet Create()
    {
        var sheet = new StyleSheet();

        sheet.AddRule(new ClassSelector("sim-panel"), new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Column,
            Width = Length.Percent(100),
            Height = Length.Percent(100),
            BackgroundColor = BgDark,
            Color = TextPrimary,
            FontSize = Length.Px(13),
        });

        sheet.AddRule(new ClassSelector("sim-header"), new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Column,
            PaddingTop = Length.Px(14),
            PaddingBottom = Length.Px(14),
            PaddingLeft = Length.Px(16),
            PaddingRight = Length.Px(16),
            BackgroundColor = BgMedium,
            BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, BorderColor),
        });

        sheet.AddRule(new ClassSelector("sim-title"), new Style
        {
            FontSize = Length.Px(15),
            FontWeight = FontWeight.Bold,
            Color = TextPrimary,
        });

        sheet.AddRule(new ClassSelector("sim-subtitle"), new Style
        {
            FontSize = Length.Px(11),
            Color = TextSecondary,
            MarginTop = Length.Px(2),
        });

        sheet.AddRule(new ClassSelector("sim-body"), new Style
        {
            FlexGrow = 1,
            MinHeight = Length.Px(0),
            OverflowY = Overflow.Scroll,
            PaddingTop = Length.Px(12),
            PaddingBottom = Length.Px(12),
            PaddingLeft = Length.Px(16),
            PaddingRight = Length.Px(16),
        });

        sheet.AddRule(new ClassSelector("sim-section"), new Style
        {
            MarginBottom = Length.Px(18),
        });

        sheet.AddRule(new ClassSelector("sim-section-label"), new Style
        {
            FontSize = Length.Px(11),
            FontWeight = FontWeight.Bold,
            Color = TextSecondary,
            MarginBottom = Length.Px(8),
        });

        // 设备列表项
        sheet.AddRule(new ClassSelector("sim-device"), new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Column,
            PaddingTop = Length.Px(8),
            PaddingBottom = Length.Px(8),
            PaddingLeft = Length.Px(10),
            PaddingRight = Length.Px(10),
            MarginBottom = Length.Px(4),
            BorderRadius = Length.Px(6),
            BackgroundColor = BgMedium,
            Cursor = Cursor.Pointer,
        });

        sheet.AddRule(new ClassSelector("sim-device-active"), new Style
        {
            BackgroundColor = BgSelected,
            BorderLeft = new BorderSide(Length.Px(3), BorderStyle.Solid, Accent),
        });

        sheet.AddRule(new ClassSelector("sim-device-name"), new Style
        {
            FontSize = Length.Px(13),
            Color = TextPrimary,
        });

        sheet.AddRule(new ClassSelector("sim-device-spec"), new Style
        {
            FontSize = Length.Px(10),
            Color = TextSecondary,
            MarginTop = Length.Px(2),
        });

        // 通用按钮行（朝向、安全区开关等）
        sheet.AddRule(new ClassSelector("sim-btn-row"), new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Row,
            Gap = Length.Px(8),
        });

        sheet.AddRule(new ClassSelector("sim-btn"), new Style
        {
            FlexGrow = 1,
            PaddingTop = Length.Px(8),
            PaddingBottom = Length.Px(8),
            BackgroundColor = BgMedium,
            Color = TextPrimary,
            TextAlign = TextAlign.Center,
            BorderRadius = Length.Px(6),
            Cursor = Cursor.Pointer,
        });

        sheet.AddRule(new ClassSelector("sim-btn-active"), new Style
        {
            BackgroundColor = Accent,
            Color = new Color(255, 255, 255),
        });

        // 只读信息行
        sheet.AddRule(new ClassSelector("sim-info-row"), new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Row,
            JustifyContent = JustifyContent.SpaceBetween,
            PaddingTop = Length.Px(4),
            PaddingBottom = Length.Px(4),
        });

        sheet.AddRule(new ClassSelector("sim-info-key"), new Style
        {
            Color = TextSecondary,
            FontSize = Length.Px(12),
        });

        sheet.AddRule(new ClassSelector("sim-info-val"), new Style
        {
            Color = TextPrimary,
            FontSize = Length.Px(12),
        });

        return sheet;
    }
}
