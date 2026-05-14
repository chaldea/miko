using Miko.Common;
using Miko.Core.DomElements;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class ButtonStyles
{
    internal static void Apply(StyleSheet sheet, Theme t)
    {
        sheet.AddRule(Style.For<ButtonElement>()
            .Set(x => x.Display, Display.InlineBlock)
            .Set(x => x.Padding, new Padding(6, 12))
            .Set(x => x.Margin, new Margin(0, 8, 8, 0))
            .Set(x => x.FontSize, Length.Px(16))
            .Set(x => x.FontWeight, FontWeight.Normal)
            .Set(x => x.LineHeight, Length.Px(24))
            .Set(x => x.TextAlign, TextAlign.Center)
            .Set(x => x.TextDecoration, TextDecoration.None)
            .Set(x => x.VerticalAlign, VerticalAlign.Middle)
            .Set(x => x.Cursor, Cursor.Pointer)
            .Set(x => x.UserSelect, UserSelect.None)
            .Set(x => x.BackgroundColor, Color.Transparent)
            .Set(x => x.Border, new Border(1, BorderStyle.Solid, Color.Transparent))
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadius)));

        sheet.AddRule(Style.For<ButtonElement>().Disabled()
            .Set(x => x.Opacity, 0.65f)
            .Set(x => x.Cursor, Cursor.NotAllowed));

        var variants = new[]
        {
            ("btn-primary",   t.Primary,   Color.White,       DarkenColor(t.Primary, 0.85f),   DarkenColor(t.Primary, 0.80f)),
            ("btn-secondary", t.Secondary, Color.White,       DarkenColor(t.Secondary, 0.85f), DarkenColor(t.Secondary, 0.80f)),
            ("btn-success",   t.Success,   Color.White,       DarkenColor(t.Success, 0.85f),   DarkenColor(t.Success, 0.80f)),
            ("btn-danger",    t.Danger,    Color.White,       DarkenColor(t.Danger, 0.85f),    DarkenColor(t.Danger, 0.80f)),
            ("btn-warning",   t.Warning,   Color.FromHex("000"), DarkenColor(t.Warning, 0.85f), DarkenColor(t.Warning, 0.80f)),
            ("btn-info",      t.Info,      Color.FromHex("000"), DarkenColor(t.Info, 0.85f),   DarkenColor(t.Info, 0.80f)),
            ("btn-light",     t.Light,     Color.FromHex("000"), DarkenColor(t.Light, 0.90f),  DarkenColor(t.Light, 0.85f)),
            ("btn-dark",      t.Dark,      Color.White,       DarkenColor(t.Dark, 0.85f),      DarkenColor(t.Dark, 0.80f)),
        };

        foreach (var (cls, bg, fg, hoverBg, hoverBorder) in variants)
        {
            sheet.AddRule(Style.Class(cls)
                .Set(x => x.BackgroundColor, bg)
                .Set(x => x.Color, fg)
                .Set(x => x.BorderColor, bg));

            sheet.AddRule(Style.Class(cls).Hover()
                .Set(x => x.BackgroundColor, hoverBg)
                .Set(x => x.BorderColor, hoverBorder));

            sheet.AddRule(Style.Class(cls).Active()
                .Set(x => x.BackgroundColor, DarkenColor(bg, 0.80f))
                .Set(x => x.BorderColor, DarkenColor(bg, 0.75f)));
        }

        // Outline variants
        var outlineVariants = new[]
        {
            ("btn-outline-primary",   t.Primary),
            ("btn-outline-secondary", t.Secondary),
            ("btn-outline-success",   t.Success),
            ("btn-outline-danger",    t.Danger),
            ("btn-outline-warning",   t.Warning),
            ("btn-outline-info",      t.Info),
            ("btn-outline-light",     t.Light),
            ("btn-outline-dark",      t.Dark),
        };

        foreach (var (cls, color) in outlineVariants)
        {
            sheet.AddRule(Style.Class(cls)
                .Set(x => x.BackgroundColor, Color.Transparent)
                .Set(x => x.Color, color)
                .Set(x => x.BorderColor, color));

            sheet.AddRule(Style.Class(cls).Hover()
                .Set(x => x.BackgroundColor, color)
                .Set(x => x.Color, Color.White));
        }

        // Sizes
        sheet.AddRule(Style.Class("btn-sm")
            .Set(x => x.Padding, new Padding(4, 8))
            .Set(x => x.FontSize, Length.Px(14))
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadiusSm)));

        sheet.AddRule(Style.Class("btn-lg")
            .Set(x => x.Padding, new Padding(8, 16))
            .Set(x => x.FontSize, Length.Px(20))
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadiusLg)));

        sheet.AddRule(Style.Class("btn-link")
            .Set(x => x.BackgroundColor, Color.Transparent)
            .Set(x => x.Color, t.LinkColor)
            .Set(x => x.TextDecoration, TextDecoration.Underline)
            .Set(x => x.BorderColor, Color.Transparent));
    }

    private static Color DarkenColor(Color c, float factor) =>
        Color.FromRgb((byte)(c.R * factor), (byte)(c.G * factor), (byte)(c.B * factor));
}
