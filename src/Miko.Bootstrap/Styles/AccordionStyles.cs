using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class AccordionStyles
{
    internal static void Apply(StyleSheet sheet, Theme t)
    {
        sheet.AddRule(Style.Class("accordion")
            .Set(x => x.Border, new Border(t.BorderWidth, BorderStyle.Solid, Color.FromHex("dee2e6")))
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadius)));

        sheet.AddRule(Style.Class("accordion-item")
            .Set(x => x.BorderBottom, new BorderSide(t.BorderWidth, BorderStyle.Solid, Color.FromHex("dee2e6"))));

        sheet.AddRule(Style.Class("accordion-header")
            .Set(x => x.MarginBottom, Length.Px(0))
            .Set(x => x.Padding, new Padding(0)));

        sheet.AddRule(Style.Class("accordion-button")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.AlignItems, AlignItems.Center)
            .Set(x => x.Width, Length.Percent(100))
            .Set(x => x.Padding, new Padding(16, 20))
            .Set(x => x.FontSize, Length.Px(16))
            .Set(x => x.FontWeight, FontWeight.Normal)
            .Set(x => x.TextAlign, TextAlign.Left)
            .Set(x => x.Border, new Border(Length.Px(0), BorderStyle.None, Color.Transparent))
            .Set(x => x.BorderRadius, new BorderRadius(Length.Px(0)))
            .Set(x => x.Cursor, Cursor.Pointer));

        sheet.AddRule(Style.Class("accordion-icon")
            .Set(x => x.MarginLeft, Length.Auto)
            .Set(x => x.FontSize, Length.Px(12)));

        sheet.AddRule(Style.Class("accordion-collapse")
            .Set(x => x.OverflowY, Overflow.Hidden));

        sheet.AddRule(Style.Class("accordion-body")
            .Set(x => x.Padding, new Padding(16, 20)));
    }
}
