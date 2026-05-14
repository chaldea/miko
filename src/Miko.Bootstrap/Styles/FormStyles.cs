using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class FormStyles
{
    internal static void Apply(StyleSheet sheet, Theme t)
    {
        sheet.AddRule(Style.Class("form-control")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.Width, Length.Percent(100))
            .Set(x => x.Padding, new Padding(6, 12))
            .Set(x => x.FontSize, Length.Px(16))
            .Set(x => x.FontWeight, FontWeight.Normal)
            .Set(x => x.LineHeight, Length.Px(24))
            .Set(x => x.Color, t.BodyColor)
            .Set(x => x.BackgroundColor, t.BodyBg)
            .Set(x => x.Border, new Border(t.BorderWidth, BorderStyle.Solid, t.BorderColor))
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadius)));

        sheet.AddRule(Style.Class("form-control").Focus()
            .Set(x => x.BorderColor, Color.FromHex("86b7fe")));

        sheet.AddRule(Style.Class("form-control").Disabled()
            .Set(x => x.BackgroundColor, t.SecondaryBg)
            .Set(x => x.Color, t.SecondaryColor));

        sheet.AddRule(Style.Class("form-control-sm")
            .Set(x => x.Padding, new Padding(4, 8))
            .Set(x => x.FontSize, Length.Px(14))
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadiusSm)));

        sheet.AddRule(Style.Class("form-control-lg")
            .Set(x => x.Padding, new Padding(8, 16))
            .Set(x => x.FontSize, Length.Px(20))
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadiusLg)));

        sheet.AddRule(Style.Class("form-select")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.Width, Length.Percent(100))
            .Set(x => x.PaddingTop, Length.Px(6))
            .Set(x => x.PaddingRight, Length.Px(36))
            .Set(x => x.PaddingBottom, Length.Px(6))
            .Set(x => x.PaddingLeft, Length.Px(12))
            .Set(x => x.FontSize, Length.Px(16))
            .Set(x => x.Color, t.BodyColor)
            .Set(x => x.BackgroundColor, t.BodyBg)
            .Set(x => x.Border, new Border(t.BorderWidth, BorderStyle.Solid, t.BorderColor))
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadius)));

        sheet.AddRule(Style.Class("form-select-sm")
            .Set(x => x.PaddingTop, Length.Px(4))
            .Set(x => x.PaddingRight, Length.Px(32))
            .Set(x => x.PaddingBottom, Length.Px(4))
            .Set(x => x.PaddingLeft, Length.Px(8))
            .Set(x => x.FontSize, Length.Px(14))
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadiusSm)));

        sheet.AddRule(Style.Class("form-select-lg")
            .Set(x => x.PaddingTop, Length.Px(8))
            .Set(x => x.PaddingRight, Length.Px(40))
            .Set(x => x.PaddingBottom, Length.Px(8))
            .Set(x => x.PaddingLeft, Length.Px(16))
            .Set(x => x.FontSize, Length.Px(20))
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadiusLg)));

        sheet.AddRule(Style.Class("form-label")
            .Set(x => x.Display, Display.InlineBlock)
            .Set(x => x.MarginBottom, Length.Px(8))
            .Set(x => x.FontSize, Length.Px(16))
            .Set(x => x.Color, t.BodyColor));

        sheet.AddRule(Style.Class("form-text")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.MarginTop, Length.Px(4))
            .Set(x => x.FontSize, Length.Px(14))
            .Set(x => x.Color, t.SecondaryColor));

        sheet.AddRule(Style.Class("form-check")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Row)
            .Set(x => x.AlignItems, AlignItems.Center)
            .Set(x => x.MarginBottom, Length.Px(8))
            .Set(x => x.PaddingLeft, Length.Px(0)));

        sheet.AddRule(Style.Class("form-check-label")
            .Set(x => x.Display, Display.InlineBlock)
            .Set(x => x.MarginLeft, Length.Px(8))
            .Set(x => x.FontSize, Length.Px(16))
            .Set(x => x.Color, t.BodyColor));

        sheet.AddRule(Style.Class("input-group")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Row)
            .Set(x => x.AlignItems, AlignItems.Stretch)
            .Set(x => x.Width, Length.Percent(100)));

        sheet.AddRule(Style.Class("input-group-text")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.AlignItems, AlignItems.Center)
            .Set(x => x.Padding, new Padding(6, 12))
            .Set(x => x.FontSize, Length.Px(16))
            .Set(x => x.Color, t.BodyColor)
            .Set(x => x.TextAlign, TextAlign.Center)
            .Set(x => x.BackgroundColor, t.SecondaryBg)
            .Set(x => x.Border, new Border(t.BorderWidth, BorderStyle.Solid, t.BorderColor))
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadius)));

        sheet.AddRule(Style.Class("is-valid")
            .Set(x => x.BorderColor, t.FormValidBorderColor));

        sheet.AddRule(Style.Class("is-invalid")
            .Set(x => x.BorderColor, t.FormInvalidBorderColor));

        sheet.AddRule(Style.Class("valid-feedback")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.MarginTop, Length.Px(4))
            .Set(x => x.FontSize, Length.Px(14))
            .Set(x => x.Color, t.FormValidColor));

        sheet.AddRule(Style.Class("invalid-feedback")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.MarginTop, Length.Px(4))
            .Set(x => x.FontSize, Length.Px(14))
            .Set(x => x.Color, t.FormInvalidColor));

        sheet.AddRule(Style.Class("form-range")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.Width, Length.Percent(100))
            .Set(x => x.Height, Length.Px(24)));
    }
}
