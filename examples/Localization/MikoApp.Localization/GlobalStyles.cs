using Miko.Common;
using Miko.Styling;

namespace MikoApp.Localization;

/// <summary>
/// Styles written direction-neutrally: logical properties (<c>*InlineStart</c> /
/// <c>*InlineEnd</c>) and the initial <c>text-align: start</c>, so the right-to-left languages
/// need no styles of their own.
/// </summary>
internal static class GlobalStyles
{
    private static readonly Color Accent = Color.FromRgb(13, 110, 253);
    private static readonly Color Muted = Color.FromRgb(108, 117, 125);
    private static readonly Color Border = Color.FromRgb(222, 226, 230);

    public static StyleSheet Create()
    {
        var styleSheet = new StyleSheet();

        styleSheet.Add(new CssObject
        {
            [".app"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                BackgroundColor = Color.FromRgb(248, 249, 250),
                Color = Color.FromRgb(33, 37, 41),
            },
            [".toolbar"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Gap = Length.Px(16),
                Padding = new Padding(Length.Px(12), Length.Px(20)),
                BackgroundColor = Color.White,
                BorderBottomWidth = Length.Px(1),
                BorderBottomStyle = BorderStyle.Solid,
                BorderBottomColor = Border,
            },
            [".app-title"] = new() { FontSize = Length.Px(18), FontWeight = FontWeight.Bold },
            [".nav"] = new() { Display = Display.Flex, Gap = Length.Px(8), FlexGrow = 1 },
            [".languages"] = new() { Display = Display.Flex, Gap = Length.Px(6) },
            [".nav-btn"] = new()
            {
                Padding = new Padding(Length.Px(6), Length.Px(12)),
                BorderWidth = Length.Px(0),
                BackgroundColor = Color.Transparent,
                Color = Accent,
                FontSize = Length.Px(14),
                Cursor = Cursor.Pointer,
            },
            [".lang-btn"] = new()
            {
                Padding = new Padding(Length.Px(5), Length.Px(10)),
                BorderWidth = Length.Px(1),
                BorderColor = Border,
                BorderRadius = new BorderRadius(Length.Px(14)),
                BackgroundColor = Color.White,
                FontSize = Length.Px(13),
                Cursor = Cursor.Pointer,
            },
            [".lang-btn-active"] = new()
            {
                BackgroundColor = Accent,
                BorderColor = Accent,
                Color = Color.White,
            },
            [".page"] = new()
            {
                FlexGrow = 1,
                OverflowY = Overflow.Auto,
                Padding = new Padding(Length.Px(24)),
            },
            [".content"] = new() { MaxWidth = Length.Px(720) },
            [".title"] = new() { FontSize = Length.Px(30), MarginTop = Length.Px(0), MarginBottom = Length.Px(8) },
            [".intro"] = new()
            {
                FontSize = Length.Px(15),
                LineHeight = Length.Number(1.6f),
                Color = Muted,
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(20),
            },
            [".card"] = new()
            {
                BackgroundColor = Color.White,
                Padding = new Padding(Length.Px(16)),
                MarginBottom = Length.Px(16),
                BorderRadius = new BorderRadius(Length.Px(8)),
                BorderWidth = Length.Px(1),
                BorderColor = Border,
                BorderStyle = BorderStyle.Solid,
                // The accent bar sits on the start side: left in LTR, right in RTL.
                BorderInlineStartWidth = Length.Px(4),
                BorderInlineStartColor = Accent,
            },
            [".card-title"] = new() { FontSize = Length.Px(17), MarginTop = Length.Px(0), MarginBottom = Length.Px(12) },
            [".row"] = new()
            {
                Display = Display.Flex,
                JustifyContent = JustifyContent.SpaceBetween,
                PaddingTop = Length.Px(6),
                PaddingBottom = Length.Px(6),
                BorderBottomWidth = Length.Px(1),
                BorderBottomStyle = BorderStyle.Solid,
                BorderBottomColor = Color.FromRgb(241, 243, 245),
            },
            [".label"] = new() { Color = Muted, FontSize = Length.Px(14) },
            [".value"] = new() { FontSize = Length.Px(14), FontWeight = FontWeight.SemiBold },
            [".note"] = new() { FontSize = Length.Px(14), MarginTop = Length.Px(12), MarginBottom = Length.Px(0) },
            [".script-name"] = new()
            {
                FontSize = Length.Px(12),
                Color = Muted,
                MarginBottom = Length.Px(6),
            },
            [".sample"] = new()
            {
                FontSize = Length.Px(20),
                LineHeight = Length.Number(1.5f),
                MarginTop = Length.Px(0),
                MarginBottom = Length.Px(0),
            },
        });

        return styleSheet;
    }
}
