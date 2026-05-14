using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class CardStyles
{
    internal static void Apply(StyleSheet sheet, Theme t)
    {
        sheet.AddRule(Style.Class("card")
            .Set(x => x.Position, Position.Relative)
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Column)
            .Set(x => x.MinWidth, Length.Px(0))
            .Set(x => x.BackgroundColor, t.BodyBg)
            .Set(x => x.Border, new Border(t.BorderWidth, BorderStyle.Solid, Color.FromRgba(0, 0, 0, 45)))
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadius)));

        sheet.AddRule(Style.Class("card-body")
            .Set(x => x.FlexGrow, 1)
            .Set(x => x.FlexShrink, 1)
            .Set(x => x.Padding, new Padding(16)));

        sheet.AddRule(Style.Class("card-title")
            .Set(x => x.MarginBottom, Length.Px(8)));

        sheet.AddRule(Style.Class("card-subtitle")
            .Set(x => x.MarginTop, Length.Px(-4))
            .Set(x => x.MarginBottom, Length.Px(0)));

        sheet.AddRule(Style.Class("card-header")
            .Set(x => x.Padding, new Padding(8, 16))
            .Set(x => x.BackgroundColor, Color.FromRgba(33, 37, 41, 8))
            .Set(x => x.BorderBottom, new BorderSide(t.BorderWidth, BorderStyle.Solid, Color.FromRgba(0, 0, 0, 45))));

        sheet.AddRule(Style.Class("card-footer")
            .Set(x => x.Padding, new Padding(8, 16))
            .Set(x => x.BackgroundColor, Color.FromRgba(33, 37, 41, 8))
            .Set(x => x.BorderTop, new BorderSide(t.BorderWidth, BorderStyle.Solid, Color.FromRgba(0, 0, 0, 45))));

        sheet.AddRule(Style.Class("card-img-top")
            .Set(x => x.Width, Length.Percent(100))
            .Set(x => x.BorderTopLeftRadius, Length.Px(t.BorderRadius - 1))
            .Set(x => x.BorderTopRightRadius, Length.Px(t.BorderRadius - 1)));

        sheet.AddRule(Style.Class("card-img-bottom")
            .Set(x => x.Width, Length.Percent(100))
            .Set(x => x.BorderBottomLeftRadius, Length.Px(t.BorderRadius - 1))
            .Set(x => x.BorderBottomRightRadius, Length.Px(t.BorderRadius - 1)));
    }
}
