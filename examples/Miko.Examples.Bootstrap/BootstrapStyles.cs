using Miko.Styling;
using Miko.Styling.Selectors;
using Miko.Common;

namespace Miko.Examples.Bootstrap;

/// <summary>
/// Provides Bootstrap-inspired styles for Miko elements.
/// Based on Bootstrap 5 design system.
/// </summary>
public static class BootstrapStyles
{
    // Bootstrap 5 Color Palette
    public static class Colors
    {
        public static readonly Color Primary = Color.FromRgb(0, 123, 255);      // #007BFF
        public static readonly Color Secondary = Color.FromRgb(108, 117, 125);  // #6C757D
        public static readonly Color Success = Color.FromRgb(40, 167, 69);      // #28A745
        public static readonly Color Danger = Color.FromRgb(220, 53, 69);       // #DC3545
        public static readonly Color Warning = Color.FromRgb(255, 193, 7);      // #FFC107
        public static readonly Color Info = Color.FromRgb(23, 162, 184);        // #17A2B8
        public static readonly Color Light = Color.FromRgb(248, 249, 250);      // #F8F9FA
        public static readonly Color Dark = Color.FromRgb(52, 58, 64);          // #343A40

        // Text colors
        public static readonly Color TextWhite = Color.White;
        public static readonly Color TextDark = Color.FromRgb(33, 37, 41);      // #212529
    }

    /// <summary>
    /// Creates a complete Bootstrap-inspired stylesheet.
    /// </summary>
    public static StyleSheet CreateBootstrapStyleSheet()
    {
        var styleSheet = new StyleSheet();

        // Container styles
        styleSheet.AddRule(
            new ClassSelector("container"),
            new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                PaddingTop = Length.Px(40),
                PaddingRight = Length.Px(40),
                PaddingBottom = Length.Px(40),
                PaddingLeft = Length.Px(40),
                BackgroundColor = Color.White
            }
        );

        // Row (flex container)
        styleSheet.AddRule(
            new ClassSelector("row"),
            new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                MarginBottom = Length.Px(20)
            }
        );

        // Base button styles
        styleSheet.AddRule(
            new TagSelector("button"),
            new Style
            {
                Display = Display.InlineBlock,
                Padding = new Padding(6, 12),
                Margin = new Margin(0, 10, 10, 0),
                FontSize = 14,
                FontWeight = FontWeight.Normal,
                Border = new Border(1, BorderStyle.Solid, Color.Transparent),
                BorderRadius = new BorderRadius(4),
            }
        );

        // Button color variants
        AddButtonVariant(styleSheet, "btn-primary", Colors.Primary, Colors.TextWhite);
        AddButtonVariant(styleSheet, "btn-secondary", Colors.Secondary, Colors.TextWhite);
        AddButtonVariant(styleSheet, "btn-success", Colors.Success, Colors.TextWhite);
        AddButtonVariant(styleSheet, "btn-danger", Colors.Danger, Colors.TextWhite);
        AddButtonVariant(styleSheet, "btn-warning", Colors.Warning, Colors.TextDark);
        AddButtonVariant(styleSheet, "btn-info", Colors.Info, Colors.TextWhite);
        AddButtonVariant(styleSheet, "btn-light", Colors.Light, Colors.TextDark);
        AddButtonVariant(styleSheet, "btn-dark", Colors.Dark, Colors.TextWhite);

        // Button hover
        AddButtonHover(styleSheet, "btn-primary", Color.FromHex("0069d9"), Color.FromHex("0062cc"));
        AddButtonHover(styleSheet, "btn-secondary", Color.FromHex("5a6268"), Color.FromHex("545b62"));
        AddButtonHover(styleSheet, "btn-success", Color.FromHex("218838"), Color.FromHex("1e7e34"));
        AddButtonHover(styleSheet, "btn-danger", Color.FromHex("c82333"), Color.FromHex("bd2130"));
        AddButtonHover(styleSheet, "btn-warning", Color.FromHex("e0a800"), Color.FromHex("d39e00"));
        AddButtonHover(styleSheet, "btn-info", Color.FromHex("138496"), Color.FromHex("117a8b"));
        AddButtonHover(styleSheet, "btn-light", Color.FromHex("e2e6ea"), Color.FromHex("dae0e5"));
        AddButtonHover(styleSheet, "btn-dark", Color.FromHex("23272b"), Color.FromHex("1d2124"));

        // Outline button variants
        AddOutlineButtonVariant(styleSheet, "btn-outline-primary", Colors.Primary);
        AddOutlineButtonVariant(styleSheet, "btn-outline-secondary", Colors.Secondary);
        AddOutlineButtonVariant(styleSheet, "btn-outline-success", Colors.Success);
        AddOutlineButtonVariant(styleSheet, "btn-outline-danger", Colors.Danger);
        AddOutlineButtonVariant(styleSheet, "btn-outline-warning", Colors.Warning);
        AddOutlineButtonVariant(styleSheet, "btn-outline-info", Colors.Info);
        AddOutlineButtonVariant(styleSheet, "btn-outline-dark", Colors.Dark);

        // Button sizes
        styleSheet.AddRule(
            new ClassSelector("btn-sm"),
            new Style
            {
                PaddingTop = Length.Px(4),
                PaddingRight = Length.Px(8),
                PaddingBottom = Length.Px(4),
                PaddingLeft = Length.Px(8),
                FontSize = Length.Px(12)
            }
        );

        styleSheet.AddRule(
            new ClassSelector("btn-lg"),
            new Style
            {
                PaddingTop = Length.Px(10),
                PaddingRight = Length.Px(16),
                PaddingBottom = Length.Px(10),
                PaddingLeft = Length.Px(16),
                FontSize = Length.Px(16)
            }
        );

        // Heading styles
        styleSheet.AddRule(
            new TagSelector("h1"),
            new Style
            {
                FontSize = Length.Px(32),
                FontWeight = FontWeight.Bold,
                MarginBottom = Length.Px(20),
                Color = Colors.Dark
            }
        );

        styleSheet.AddRule(
            new TagSelector("h2"),
            new Style
            {
                FontSize = Length.Px(24),
                FontWeight = FontWeight.Bold,
                MarginTop = Length.Px(20),
                MarginBottom = Length.Px(15),
                Color = Colors.Dark
            }
        );

        // Form control styles
        AddFormControlStyles(styleSheet);

        // List styles
        AddListStyles(styleSheet);

        // Table styles
        AddTableStyles(styleSheet);

        return styleSheet;
    }

    private static void AddFormControlStyles(StyleSheet styleSheet)
    {
        // Base form-control style (text inputs)
        styleSheet.AddRule(
            new ClassSelector("form-control"),
            new Style
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                Padding = new Padding(6, 12),
                FontSize = Length.Px(14),
                FontWeight = FontWeight.Normal,
                Color = Colors.TextDark,
                BackgroundColor = Color.White,
                Border = new Border(1, BorderStyle.Solid, Color.FromHex("ced4da")),
                BorderRadius = new BorderRadius(6)
            }
        );

        // Form control focus state
        styleSheet.AddRule(
            new CompoundSelector(new ClassSelector("form-control"), new FocusSelector()),
            new Style
            {
                Color = Color.FromHex("212529"),
                BackgroundColor = Color.White,
                BorderColor = Color.FromHex("86b7fe"),
            }
        );

        // Form control sizes
        styleSheet.AddRule(
            new ClassSelector("form-control-lg"),
            new Style
            {
                PaddingTop = Length.Px(8),
                PaddingRight = Length.Px(16),
                PaddingBottom = Length.Px(8),
                PaddingLeft = Length.Px(16),
                FontSize = Length.Px(18),
                BorderRadius = new BorderRadius(6)
            }
        );

        styleSheet.AddRule(
            new ClassSelector("form-control-sm"),
            new Style
            {
                PaddingTop = Length.Px(4),
                PaddingRight = Length.Px(8),
                PaddingBottom = Length.Px(4),
                PaddingLeft = Length.Px(8),
                FontSize = Length.Px(12),
                BorderRadius = new BorderRadius(3)
            }
        );

        // Form select (dropdown)
        styleSheet.AddRule(
            new ClassSelector("form-select"),
            new Style
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                PaddingTop = Length.Px(6),
                PaddingRight = Length.Px(36),
                PaddingBottom = Length.Px(6),
                PaddingLeft = Length.Px(12),
                FontSize = Length.Px(14),
                Color = Colors.TextDark,
                BackgroundColor = Color.White,
                Border = new Border(1, BorderStyle.Solid, Color.FromHex("ced4da")),
                BorderRadius = new BorderRadius(4)
            }
        );

        styleSheet.AddRule(
            new ClassSelector("form-select-lg"),
            new Style
            {
                PaddingTop = Length.Px(8),
                PaddingRight = Length.Px(40),
                PaddingBottom = Length.Px(8),
                PaddingLeft = Length.Px(16),
                FontSize = Length.Px(18),
                BorderRadius = new BorderRadius(6)
            }
        );

        styleSheet.AddRule(
            new ClassSelector("form-select-sm"),
            new Style
            {
                PaddingTop = Length.Px(4),
                PaddingRight = Length.Px(32),
                PaddingBottom = Length.Px(4),
                PaddingLeft = Length.Px(8),
                FontSize = Length.Px(12),
                BorderRadius = new BorderRadius(3)
            }
        );

        // Form label
        styleSheet.AddRule(
            new ClassSelector("form-label"),
            new Style
            {
                Display = Display.InlineBlock,
                MarginBottom = Length.Px(8),
                FontSize = Length.Px(14),
                FontWeight = FontWeight.Normal,
                Color = Colors.TextDark
            }
        );

        // Form text (help text)
        styleSheet.AddRule(
            new ClassSelector("form-text"),
            new Style
            {
                Display = Display.Block,
                MarginTop = Length.Px(4),
                FontSize = Length.Px(12),
                Color = Colors.Secondary
            }
        );

        // Form check (container for checkbox/radio)
        styleSheet.AddRule(
            new ClassSelector("form-check"),
            new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                MarginBottom = Length.Px(8),
                PaddingLeft = Length.Px(0)
            }
        );

        // Form check label
        styleSheet.AddRule(
            new ClassSelector("form-check-label"),
            new Style
            {
                Display = Display.InlineBlock,
                MarginLeft = Length.Px(8),
                FontSize = Length.Px(14),
                Color = Colors.TextDark
            }
        );

        // Margin bottom utility (for form groups)
        styleSheet.AddRule(
            new ClassSelector("mb-3"),
            new Style
            {
                MarginBottom = Length.Px(16)
            }
        );

        styleSheet.AddRule(
            new ClassSelector("mb-4"),
            new Style
            {
                MarginBottom = Length.Px(24)
            }
        );

        // Input validation states
        styleSheet.AddRule(
            new ClassSelector("is-valid"),
            new Style
            {
                BorderColor = Colors.Success
            }
        );

        styleSheet.AddRule(
            new ClassSelector("is-invalid"),
            new Style
            {
                BorderColor = Colors.Danger
            }
        );

        // Valid/Invalid feedback text
        styleSheet.AddRule(
            new ClassSelector("valid-feedback"),
            new Style
            {
                Display = Display.Block,
                MarginTop = Length.Px(4),
                FontSize = Length.Px(12),
                Color = Colors.Success
            }
        );

        styleSheet.AddRule(
            new ClassSelector("invalid-feedback"),
            new Style
            {
                Display = Display.Block,
                MarginTop = Length.Px(4),
                FontSize = Length.Px(12),
                Color = Colors.Danger
            }
        );

        // Input group
        styleSheet.AddRule(
            new ClassSelector("input-group"),
            new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Stretch,
                Width = Length.Percent(100)
            }
        );

        styleSheet.AddRule(
            new ClassSelector("input-group-control"),
            new Style
            {
                Display = Display.Flex,
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = 0
            }
        );

        styleSheet.AddRule(
            new ClassSelector("input-group-text"),
            new Style
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Padding = new Padding(6, 12),
                FontSize = Length.Px(14),
                Color = Colors.TextDark,
                TextAlign = TextAlign.Center,
                BackgroundColor = Color.FromHex("e9ecef"),
                Border = new Border(1, BorderStyle.Solid, Color.FromHex("ced4da")),
                BorderRadius = new BorderRadius(6)
            }
        );

        // Range input style
        styleSheet.AddRule(
            new ClassSelector("form-range"),
            new Style
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                Height = Length.Px(24)
            }
        );

        // Disabled state
        styleSheet.AddRule(
            new ClassSelector("disabled"),
            new Style
            {
                BackgroundColor = Color.FromHex("e9ecef"),
                Color = Colors.Secondary
            }
        );
    }

    private static void AddListStyles(StyleSheet styleSheet)
    {
        // Column layout helper
        styleSheet.AddRule(
            new ClassSelector("col"),
            new Style
            {
                Display = Display.Block,
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = 0,
                PaddingRight = Length.Px(15),
                PaddingLeft = Length.Px(15)
            }
        );

        // List group container
        styleSheet.AddRule(
            new ClassSelector("list-group"),
            new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                PaddingLeft = Length.Px(0),
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(20),
                BorderRadius = new BorderRadius(6)
            }
        );

        // List group item
        styleSheet.AddRule(
            new ClassSelector("list-group-item"),
            new Style
            {
                Display = Display.Block,
                PaddingTop = Length.Px(12),
                PaddingRight = Length.Px(16),
                PaddingBottom = Length.Px(12),
                PaddingLeft = Length.Px(16),
                Color = Colors.TextDark,
                BackgroundColor = Color.White,
                Border = new Border(1, BorderStyle.Solid, Color.FromHex("dee2e6")),
                MarginTop = Length.Px(-1)  // Collapse borders
            }
        );

        // Active list group item
        styleSheet.AddRule(
            new ClassSelector("list-group-item-active"),
            new Style
            {
                Color = Color.White,
                BackgroundColor = Colors.Primary,
                BorderColor = Colors.Primary
            }
        );

        // Disabled list group item
        styleSheet.AddRule(
            new ClassSelector("list-group-item-disabled"),
            new Style
            {
                Color = Colors.Secondary,
                BackgroundColor = Color.White
            }
        );

        // Flush list group (no borders on sides, no rounded corners)
        styleSheet.AddRule(
            new ClassSelector("list-group-flush"),
            new Style
            {
                BorderRadius = new BorderRadius(0)
            }
        );

        // Numbered list group
        styleSheet.AddRule(
            new ClassSelector("list-group-numbered"),
            new Style
            {
                PaddingLeft = Length.Px(40)
            }
        );

        // Contextual list group item variants
        AddListGroupItemVariant(styleSheet, "list-group-item-primary",
            Color.FromHex("cfe2ff"), Color.FromHex("084298"), Color.FromHex("b6d4fe"));
        AddListGroupItemVariant(styleSheet, "list-group-item-secondary",
            Color.FromHex("e2e3e5"), Color.FromHex("41464b"), Color.FromHex("c4c8cb"));
        AddListGroupItemVariant(styleSheet, "list-group-item-success",
            Color.FromHex("d1e7dd"), Color.FromHex("0f5132"), Color.FromHex("badbcc"));
        AddListGroupItemVariant(styleSheet, "list-group-item-danger",
            Color.FromHex("f8d7da"), Color.FromHex("842029"), Color.FromHex("f5c2c7"));
        AddListGroupItemVariant(styleSheet, "list-group-item-warning",
            Color.FromHex("fff3cd"), Color.FromHex("664d03"), Color.FromHex("ffecb5"));
        AddListGroupItemVariant(styleSheet, "list-group-item-info",
            Color.FromHex("cff4fc"), Color.FromHex("055160"), Color.FromHex("b6effb"));
        AddListGroupItemVariant(styleSheet, "list-group-item-light",
            Color.FromHex("fefefe"), Color.FromHex("636464"), Color.FromHex("fdfdfe"));
        AddListGroupItemVariant(styleSheet, "list-group-item-dark",
            Color.FromHex("d3d3d4"), Color.FromHex("141619"), Color.FromHex("bababc"));
    }

    private static void AddListGroupItemVariant(StyleSheet styleSheet, string className, Color bgColor, Color textColor, Color borderColor)
    {
        styleSheet.AddRule(
            new ClassSelector(className),
            new Style
            {
                BackgroundColor = bgColor,
                Color = textColor,
                BorderColor = borderColor
            }
        );
    }

    private static void AddButtonVariant(StyleSheet styleSheet, string className, Color bgColor, Color textColor)
    {
        styleSheet.AddRule(
            new ClassSelector(className),
            new Style
            {
                BackgroundColor = bgColor,
                Color = textColor,
                BorderColor = bgColor
            }
        );
    }

    private static void AddButtonHover(StyleSheet styleSheet, string className, Color bgColor, Color borderColor)
    {
        styleSheet.AddRule(
            new CompoundSelector(new ClassSelector(className), new HoverSelector()),
            new Style()
            {
                BackgroundColor = bgColor,
                BorderColor = borderColor
            });
    }

    private static void AddOutlineButtonVariant(StyleSheet styleSheet, string className, Color color)
    {
        styleSheet.AddRule(
            new ClassSelector(className),
            new Style
            {
                BackgroundColor = Color.Transparent,
                Color = color,
                BorderColor = color
            }
        );
    }

    private static void AddTableStyles(StyleSheet styleSheet)
    {
        // Base table style
        styleSheet.AddRule(
            new ClassSelector("table"),
            new Style
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                MarginBottom = Length.Px(16),
                Color = Colors.TextDark,
                BorderWidth = Length.Px(0)
            }
        );

        // Table row
        styleSheet.AddRule(
            new ClassSelector("table-row"),
            new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                BorderWidth = Length.Px(1),
                BorderStyle = BorderStyle.Solid,
                BorderColor = Color.FromHex("dee2e6")
            }
        );

        // Table header row
        styleSheet.AddRule(
            new ClassSelector("table-header"),
            new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                BorderWidth = Length.Px(2),
                BorderStyle = BorderStyle.Solid,
                BorderColor = Color.FromHex("212529")
            }
        );

        // Table cell (th and td)
        styleSheet.AddRule(
            new ClassSelector("table-cell"),
            new Style
            {
                Display = Display.Block,
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = 0,
                PaddingTop = Length.Px(8),
                PaddingRight = Length.Px(8),
                PaddingBottom = Length.Px(8),
                PaddingLeft = Length.Px(8),
                TextAlign = TextAlign.Left
            }
        );

        // Table header cell
        styleSheet.AddRule(
            new ClassSelector("table-header-cell"),
            new Style
            {
                Display = Display.Block,
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = 0,
                PaddingTop = Length.Px(8),
                PaddingRight = Length.Px(8),
                PaddingBottom = Length.Px(8),
                PaddingLeft = Length.Px(8),
                FontWeight = FontWeight.Bold,
                TextAlign = TextAlign.Left
            }
        );

        // Striped table rows
        styleSheet.AddRule(
            new ClassSelector("table-striped"),
            new Style
            {
                BackgroundColor = Color.FromHex("f8f9fa")
            }
        );

        // Bordered table
        styleSheet.AddRule(
            new ClassSelector("table-bordered"),
            new Style
            {
                BorderWidth = Length.Px(1),
                BorderStyle = BorderStyle.Solid,
                BorderColor = Color.FromHex("dee2e6")
            }
        );

        styleSheet.AddRule(
            new ClassSelector("table-bordered-cell"),
            new Style
            {
                BorderWidth = Length.Px(1),
                BorderStyle = BorderStyle.Solid,
                BorderColor = Color.FromHex("dee2e6")
            }
        );

        // Borderless table
        styleSheet.AddRule(
            new ClassSelector("table-borderless"),
            new Style
            {
                BorderWidth = Length.Px(0),
                BorderStyle = BorderStyle.None
            }
        );

        styleSheet.AddRule(
            new ClassSelector("table-borderless-row"),
            new Style
            {
                BorderWidth = Length.Px(0),
                BorderStyle = BorderStyle.None
            }
        );

        // Hoverable table row
        styleSheet.AddRule(
            new CompoundSelector(new ClassSelector("table-hover-row"), new HoverSelector()),
            new Style
            {
                BackgroundColor = Color.FromHex("f5f5f5")
            }
        );

        // Small table
        styleSheet.AddRule(
            new ClassSelector("table-sm"),
            new Style
            {
                PaddingTop = Length.Px(4),
                PaddingRight = Length.Px(4),
                PaddingBottom = Length.Px(4),
                PaddingLeft = Length.Px(4)
            }
        );

        // Table contextual variants
        AddTableVariant(styleSheet, "table-primary", Color.FromHex("cfe2ff"), Color.FromHex("084298"));
        AddTableVariant(styleSheet, "table-secondary", Color.FromHex("e2e3e5"), Color.FromHex("41464b"));
        AddTableVariant(styleSheet, "table-success", Color.FromHex("d1e7dd"), Color.FromHex("0f5132"));
        AddTableVariant(styleSheet, "table-danger", Color.FromHex("f8d7da"), Color.FromHex("842029"));
        AddTableVariant(styleSheet, "table-warning", Color.FromHex("fff3cd"), Color.FromHex("664d03"));
        AddTableVariant(styleSheet, "table-info", Color.FromHex("cff4fc"), Color.FromHex("055160"));
        AddTableVariant(styleSheet, "table-light", Color.FromHex("fcfcfd"), Color.FromHex("636464"));
        AddTableVariant(styleSheet, "table-dark", Color.FromHex("212529"), Color.White);

        // Dark table
        styleSheet.AddRule(
            new ClassSelector("table-dark-header"),
            new Style
            {
                BackgroundColor = Color.FromHex("212529"),
                Color = Color.White,
                BorderColor = Color.FromHex("373b3e")
            }
        );

        // Active row
        styleSheet.AddRule(
            new ClassSelector("table-active"),
            new Style
            {
                BackgroundColor = Color.FromHex("e9ecef")
            }
        );

        // Caption
        styleSheet.AddRule(
            new ClassSelector("table-caption"),
            new Style
            {
                Display = Display.Block,
                PaddingTop = Length.Px(8),
                PaddingBottom = Length.Px(8),
                Color = Colors.Secondary,
                TextAlign = TextAlign.Left
            }
        );

        // Caption on top
        styleSheet.AddRule(
            new ClassSelector("caption-top"),
            new Style
            {
                MarginBottom = Length.Px(8)
            }
        );
    }

    private static void AddTableVariant(StyleSheet styleSheet, string className, Color bgColor, Color textColor)
    {
        styleSheet.AddRule(
            new ClassSelector(className),
            new Style
            {
                BackgroundColor = bgColor,
                Color = textColor
            }
        );
    }
}
