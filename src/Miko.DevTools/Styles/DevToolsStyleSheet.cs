using Miko.Common;
using Miko.Styling;
using Miko.Styling.Selectors;

namespace Miko.DevTools.Styles;

internal static class DevToolsStyleSheet
{
    private static readonly Color BgDark = new(36, 36, 36);
    private static readonly Color BgMedium = new(45, 45, 45);
    private static readonly Color BgLight = new(60, 60, 60);
    private static readonly Color BgSelected = new(38, 79, 120);
    private static readonly Color TextPrimary = new(212, 212, 212);
    private static readonly Color TextSecondary = new(250, 250, 250);
    private static readonly Color TextTag = new(86, 156, 214);
    private static readonly Color TextAttr = new(156, 220, 254);
    private static readonly Color TextString = new(206, 145, 120);
    private static readonly Color TextValue = new(181, 206, 168);
    private static readonly Color BorderColor = new(70, 70, 70);
    private static readonly Color TabActiveBorder = new(0, 122, 204);

    public static StyleSheet Create()
    {
        var sheet = new StyleSheet();

        sheet.AddRule(new ClassSelector("devtools-root"), new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Column,
            Width = Length.Percent(100),
            Height = Length.Percent(100),
            BackgroundColor = BgDark,
            Color = TextPrimary,
            FontSize = Length.Px(12)
        });

        sheet.AddRule(new ClassSelector("devtools-toolbar"), new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Row,
            Height = Length.Px(34),
            BackgroundColor = BgMedium,
            BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, BorderColor),
            AlignItems = AlignItems.Center,
            PaddingLeft = Length.Px(8)
        });

        sheet.AddRule(new ClassSelector("devtools-tab"), new Style
        {
            PaddingLeft = Length.Px(12),
            PaddingRight = Length.Px(12),
            PaddingTop = Length.Px(8),
            PaddingBottom = Length.Px(8),
            Color = TextSecondary,
            Cursor = Cursor.Pointer
        });

        sheet.AddRule(new ClassSelector("devtools-tab-active"), new Style
        {
            Color = TextPrimary,
            BorderBottom = new BorderSide(Length.Px(2), BorderStyle.Solid, TabActiveBorder)
        });

        sheet.AddRule(new ClassSelector("devtools-content"), new Style
        {
            Flex = 1,
            Display = Display.Flex,
            OverflowY = Overflow.Hidden
        });

        // Elements panel
        sheet.AddRule(new ClassSelector("elements-panel"), new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Row,
            Width = Length.Percent(100),
            Height = Length.Percent(100)
        });

        sheet.AddRule(new ClassSelector("dom-tree-panel"), new Style
        {
            FlexGrow = 1,
            OverflowY = Overflow.Scroll,
            PaddingTop = Length.Px(4),
            PaddingBottom = Length.Px(4),
            PaddingLeft = Length.Px(4),
            PaddingRight = Length.Px(4),
            BorderRight = new BorderSide(Length.Px(1), BorderStyle.Solid, BorderColor)
        });

        sheet.AddRule(new ClassSelector("style-panel"), new Style
        {
            Width = Length.Px(280),
            OverflowY = Overflow.Scroll,
            PaddingTop = Length.Px(8),
            PaddingBottom = Length.Px(8),
            PaddingLeft = Length.Px(8),
            PaddingRight = Length.Px(8)
        });

        // DOM tree nodes
        sheet.AddRule(new ClassSelector("tree-node"), new Style
        {
            PaddingTop = Length.Px(1),
            PaddingBottom = Length.Px(1),
            PaddingLeft = Length.Px(4),
            PaddingRight = Length.Px(4),
            Cursor = Cursor.Pointer
        });

        sheet.AddRule(new ClassSelector("tree-node-selected"), new Style
        {
            BackgroundColor = BgSelected
        });

        sheet.AddRule(new ClassSelector("tree-node-tag"), new Style
        {
            Color = TextTag
        });

        sheet.AddRule(new ClassSelector("tree-node-attr"), new Style
        {
            Color = TextAttr
        });

        sheet.AddRule(new ClassSelector("tree-node-string"), new Style
        {
            Color = TextString
        });

        sheet.AddRule(new ClassSelector("tree-node-text"), new Style
        {
            Color = TextSecondary,
            FontStyle = FontStyle.Italic
        });

        sheet.AddRule(new ClassSelector("tree-toggle"), new Style
        {
            Color = TextSecondary,
            PaddingRight = Length.Px(4),
            Display = Display.InlineBlock,
            Width = Length.Px(12)
        });

        // Style inspector
        sheet.AddRule(new ClassSelector("style-section-title"), new Style
        {
            Color = TextPrimary,
            FontWeight = FontWeight.Bold,
            PaddingTop = Length.Px(4),
            PaddingBottom = Length.Px(4),
            BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, BorderColor),
            MarginBottom = Length.Px(4)
        });

        sheet.AddRule(new ClassSelector("style-row"), new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Row,
            PaddingTop = Length.Px(1),
            PaddingBottom = Length.Px(1)
        });

        sheet.AddRule(new ClassSelector("style-prop"), new Style
        {
            Color = TextAttr,
            Width = Length.Px(120),
            FlexShrink = 0
        });

        sheet.AddRule(new ClassSelector("style-value"), new Style
        {
            Color = TextValue,
            FlexGrow = 1
        });

        // Box model visualizer
        sheet.AddRule(new ClassSelector("box-model"), new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Column,
            AlignItems = AlignItems.Center,
            PaddingTop = Length.Px(8),
            PaddingBottom = Length.Px(8)
        });

        sheet.AddRule(new ClassSelector("box-margin"), new Style
        {
            BackgroundColor = new Color(246, 178, 107, 80),
            PaddingTop = Length.Px(12),
            PaddingBottom = Length.Px(12),
            PaddingLeft = Length.Px(16),
            PaddingRight = Length.Px(16),
            Width = Length.Percent(100)
        });

        sheet.AddRule(new ClassSelector("box-border"), new Style
        {
            BackgroundColor = new Color(253, 216, 53, 80),
            PaddingTop = Length.Px(12),
            PaddingBottom = Length.Px(12),
            PaddingLeft = Length.Px(16),
            PaddingRight = Length.Px(16)
        });

        sheet.AddRule(new ClassSelector("box-padding"), new Style
        {
            BackgroundColor = new Color(129, 199, 132, 80),
            PaddingTop = Length.Px(12),
            PaddingBottom = Length.Px(12),
            PaddingLeft = Length.Px(16),
            PaddingRight = Length.Px(16)
        });

        sheet.AddRule(new ClassSelector("box-content"), new Style
        {
            BackgroundColor = new Color(100, 150, 255, 80),
            PaddingTop = Length.Px(8),
            PaddingBottom = Length.Px(8),
            PaddingLeft = Length.Px(12),
            PaddingRight = Length.Px(12),
            Color = TextPrimary
        });

        sheet.AddRule(new ClassSelector("box-label"), new Style
        {
            Color = TextSecondary,
            FontSize = Length.Px(10)
        });

        sheet.AddRule(new ClassSelector("box-value"), new Style
        {
            Color = TextPrimary,
            FontSize = Length.Px(11)
        });

        // Console panel
        sheet.AddRule(new ClassSelector("console-panel"), new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Column,
            Width = Length.Percent(100),
            Height = Length.Percent(100)
        });

        sheet.AddRule(new ClassSelector("console-filter-bar"), new Style
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Row,
            AlignItems = AlignItems.Center,
            Height = Length.Px(30),
            FlexGrow = 0,
            FlexShrink = 0,
            FlexBasis = Length.Px(30),
            PaddingLeft = Length.Px(8),
            PaddingRight = Length.Px(8),
            BackgroundColor = BgMedium,
            BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, BorderColor),
            Gap = Length.Px(8)
        });

        sheet.AddRule(new ClassSelector("console-filter-label"), new Style
        {
            Color = TextSecondary,
            FontSize = Length.Px(11)
        });

        sheet.AddRule(new ClassSelector("console-filter-btn"), new Style
        {
            PaddingLeft = Length.Px(6),
            PaddingRight = Length.Px(6),
            PaddingTop = Length.Px(2),
            PaddingBottom = Length.Px(2),
            Color = TextSecondary,
            Cursor = Cursor.Pointer,
            BorderRadius = Length.Px(3)
        });

        sheet.AddRule(new ClassSelector("console-filter-btn-active"), new Style
        {
            BackgroundColor = BgSelected,
            Color = TextPrimary
        });

        sheet.AddRule(new ClassSelector("console-output"), new Style
        {
            FlexGrow = 1,
            MinHeight = Length.Px(0),
            OverflowY = Overflow.Scroll,
            PaddingTop = Length.Px(4),
            PaddingBottom = Length.Px(4),
            PaddingLeft = Length.Px(8),
            PaddingRight = Length.Px(8)
        });

        sheet.AddRule(new ClassSelector("console-entry"), new Style
        {
            PaddingTop = Length.Px(2),
            PaddingBottom = Length.Px(2),
            BorderBottom = new BorderSide(Length.Px(1), BorderStyle.Solid, new Color(50, 50, 50)),
            FontSize = Length.Px(11)
        });

        sheet.AddRule(new ClassSelector("console-entry-trace"), new Style { Color = new Color(120, 120, 120) });
        sheet.AddRule(new ClassSelector("console-entry-debug"), new Style { Color = new Color(150, 150, 150) });
        sheet.AddRule(new ClassSelector("console-entry-info"), new Style { Color = TextPrimary });
        sheet.AddRule(new ClassSelector("console-entry-warning"), new Style { Color = new Color(255, 204, 0) });
        sheet.AddRule(new ClassSelector("console-entry-error"), new Style { Color = new Color(255, 85, 85) });
        sheet.AddRule(new ClassSelector("console-entry-critical"), new Style
        {
            Color = new Color(255, 255, 255),
            BackgroundColor = new Color(200, 0, 0)
        });

        sheet.AddRule(new ClassSelector("console-timestamp"), new Style
        {
            Color = TextSecondary,
            PaddingRight = Length.Px(8),
            FontSize = Length.Px(10)
        });

        sheet.AddRule(new ClassSelector("console-category"), new Style
        {
            Color = TextAttr,
            PaddingRight = Length.Px(8),
            FontSize = Length.Px(10)
        });

        sheet.AddRule(new ClassSelector("console-empty"), new Style
        {
            Color = TextSecondary,
            FontStyle = FontStyle.Italic,
            PaddingTop = Length.Px(16),
            PaddingLeft = Length.Px(8)
        });

        return sheet;
    }
}
