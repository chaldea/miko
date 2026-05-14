using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class AlertStyles
{
    internal static void Apply(StyleSheet sheet, Theme t)
    {
        sheet.AddRule(Style.Class("alert")
            .Set(x => x.Position, Position.Relative)
            .Set(x => x.Padding, new Padding(16))
            .Set(x => x.MarginBottom, Length.Px(16))
            .Set(x => x.Border, new Border(t.BorderWidth, BorderStyle.Solid, Color.Transparent))
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadius)));

        var variants = new[]
        {
            ("alert-primary",   t.PrimaryBgSubtle,   t.PrimaryTextEmphasis,   t.PrimaryBorderSubtle),
            ("alert-secondary", t.SecondaryBgSubtle, t.SecondaryTextEmphasis, t.SecondaryBorderSubtle),
            ("alert-success",   t.SuccessBgSubtle,   t.SuccessTextEmphasis,   t.SuccessBorderSubtle),
            ("alert-danger",    t.DangerBgSubtle,    t.DangerTextEmphasis,    t.DangerBorderSubtle),
            ("alert-warning",   t.WarningBgSubtle,   t.WarningTextEmphasis,   t.WarningBorderSubtle),
            ("alert-info",      t.InfoBgSubtle,      t.InfoTextEmphasis,      t.InfoBorderSubtle),
            ("alert-light",     t.LightBgSubtle,     t.LightTextEmphasis,     t.LightBorderSubtle),
            ("alert-dark",      t.DarkBgSubtle,      t.DarkTextEmphasis,      t.DarkBorderSubtle),
        };

        foreach (var (cls, bg, fg, border) in variants)
            sheet.AddRule(Style.Class(cls)
                .Set(x => x.BackgroundColor, bg)
                .Set(x => x.Color, fg)
                .Set(x => x.BorderColor, border));

        sheet.AddRule(Style.Class("alert-heading")
            .Set(x => x.Color, Color.Transparent)); // inherits

        sheet.AddRule(Style.Class("alert-link")
            .Set(x => x.FontWeight, FontWeight.Bold));
    }
}
