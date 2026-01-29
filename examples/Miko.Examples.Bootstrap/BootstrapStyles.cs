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

        return styleSheet;
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
}
