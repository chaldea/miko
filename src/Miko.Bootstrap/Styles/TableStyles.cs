using Miko.Common;
using Miko.Core.DomElements;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class TableStyles
{
    internal static void Apply(StyleSheet sheet, Theme t)
    {
        sheet.AddRule(Style.Class("table")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.Width, Length.Percent(100))
            .Set(x => x.MarginBottom, Length.Px(16))
            .Set(x => x.Color, t.BodyColor)
            .Set(x => x.BorderBottom, new BorderSide(t.BorderWidth, BorderStyle.Solid, t.BorderColor)));

        sheet.AddRule(Style.For<TheadElement>()
            .Set(x => x.Display, Display.Block)
            .Set(x => x.BorderBottom, new BorderSide(2, BorderStyle.Solid, t.BorderColor)));

        sheet.AddRule(Style.For<TbodyElement>()
            .Set(x => x.Display, Display.Block));

        sheet.AddRule(Style.For<TrElement>()
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Row)
            .Set(x => x.BorderBottom, new BorderSide(t.BorderWidth, BorderStyle.Solid, t.BorderColor)));

        sheet.AddRule(Style.For<ThElement>()
            .Set(x => x.Display, Display.Block)
            .Set(x => x.FlexGrow, 1)
            .Set(x => x.FlexShrink, 1)
            .Set(x => x.FlexBasis, Length.Px(0))
            .Set(x => x.Padding, new Padding(8))
            .Set(x => x.FontWeight, FontWeight.Bold)
            .Set(x => x.TextAlign, TextAlign.Left));

        sheet.AddRule(Style.For<TdElement>()
            .Set(x => x.Display, Display.Block)
            .Set(x => x.FlexGrow, 1)
            .Set(x => x.FlexShrink, 1)
            .Set(x => x.FlexBasis, Length.Px(0))
            .Set(x => x.Padding, new Padding(8)));

        sheet.AddRule(Style.Class("table-sm")
            .Set(x => x.FontSize, Length.Px(14)));

        sheet.AddRule(Style.Class("table-bordered")
            .Set(x => x.Border, new Border(t.BorderWidth, BorderStyle.Solid, t.BorderColor)));

        sheet.AddRule(Style.Class("table-dark")
            .Set(x => x.Color, Color.White)
            .Set(x => x.BackgroundColor, t.Dark)
            .Set(x => x.BorderColor, t.Gray700));

        var contextual = new[]
        {
            ("table-primary",   t.PrimaryBgSubtle,   t.PrimaryTextEmphasis),
            ("table-secondary", t.SecondaryBgSubtle, t.SecondaryTextEmphasis),
            ("table-success",   t.SuccessBgSubtle,   t.SuccessTextEmphasis),
            ("table-danger",    t.DangerBgSubtle,    t.DangerTextEmphasis),
            ("table-warning",   t.WarningBgSubtle,   t.WarningTextEmphasis),
            ("table-info",      t.InfoBgSubtle,      t.InfoTextEmphasis),
            ("table-light",     t.LightBgSubtle,     t.LightTextEmphasis),
        };

        foreach (var (cls, bg, fg) in contextual)
            sheet.AddRule(Style.Class(cls).Set(x => x.BackgroundColor, bg).Set(x => x.Color, fg));
    }
}
