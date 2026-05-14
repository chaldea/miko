using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class ListGroupStyles
{
    internal static void Apply(StyleSheet sheet, Theme t)
    {
        sheet.AddRule(Style.Class("list-group")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Column)
            .Set(x => x.PaddingLeft, Length.Px(0))
            .Set(x => x.MarginBottom, Length.Px(0))
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadius)));

        sheet.AddRule(Style.Class("list-group-item")
            .Set(x => x.Position, Position.Relative)
            .Set(x => x.Display, Display.Block)
            .Set(x => x.Padding, new Padding(8, 16))
            .Set(x => x.Color, t.BodyColor)
            .Set(x => x.BackgroundColor, t.BodyBg)
            .Set(x => x.Border, new Border(t.BorderWidth, BorderStyle.Solid, t.BorderColor))
            .Set(x => x.MarginTop, Length.Px(-1)));

        sheet.AddRule(Style.Class("list-group-item").Hover()
            .Set(x => x.BackgroundColor, t.TertiaryBg));

        sheet.AddRule(Style.Class("list-group-item-active")
            .Set(x => x.Color, Color.White)
            .Set(x => x.BackgroundColor, t.Primary)
            .Set(x => x.BorderColor, t.Primary));

        sheet.AddRule(Style.Class("list-group-item-disabled")
            .Set(x => x.Color, t.SecondaryColor)
            .Set(x => x.BackgroundColor, t.BodyBg));

        sheet.AddRule(Style.Class("list-group-flush")
            .Set(x => x.BorderRadius, new BorderRadius(0)));

        var variants = new[]
        {
            ("list-group-item-primary",   t.PrimaryBgSubtle,   t.PrimaryTextEmphasis,   t.PrimaryBorderSubtle),
            ("list-group-item-secondary", t.SecondaryBgSubtle, t.SecondaryTextEmphasis, t.SecondaryBorderSubtle),
            ("list-group-item-success",   t.SuccessBgSubtle,   t.SuccessTextEmphasis,   t.SuccessBorderSubtle),
            ("list-group-item-danger",    t.DangerBgSubtle,    t.DangerTextEmphasis,    t.DangerBorderSubtle),
            ("list-group-item-warning",   t.WarningBgSubtle,   t.WarningTextEmphasis,   t.WarningBorderSubtle),
            ("list-group-item-info",      t.InfoBgSubtle,      t.InfoTextEmphasis,      t.InfoBorderSubtle),
            ("list-group-item-light",     t.LightBgSubtle,     t.LightTextEmphasis,     t.LightBorderSubtle),
            ("list-group-item-dark",      t.DarkBgSubtle,      t.DarkTextEmphasis,      t.DarkBorderSubtle),
        };

        foreach (var (cls, bg, fg, border) in variants)
            sheet.AddRule(Style.Class(cls)
                .Set(x => x.BackgroundColor, bg)
                .Set(x => x.Color, fg)
                .Set(x => x.BorderColor, border));
    }
}
