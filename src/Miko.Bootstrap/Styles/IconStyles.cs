using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class IconStyles
{
    internal static void Apply(StyleSheet sheet, Theme t)
    {
        sheet.AddRule(Style.Class("bi")
            .Set(x => x.Display, Display.InlineBlock)
            .Set(x => x.FontFamily, "bootstrap-icons")
            .Set(x => x.FontStyle, FontStyle.Normal)
            .Set(x => x.FontWeight, FontWeight.Normal)
            .Set(x => x.FontSize, Length.Px(16))
            .Set(x => x.LineHeight, Length.Px(1)));

        sheet.AddRule(Style.Class("icon-sm")
            .Set(x => x.FontSize, Length.Px(16)));

        sheet.AddRule(Style.Class("icon-md")
            .Set(x => x.FontSize, Length.Px(24)));

        sheet.AddRule(Style.Class("icon-lg")
            .Set(x => x.FontSize, Length.Px(32)));

        sheet.AddRule(Style.Class("icon-xl")
            .Set(x => x.FontSize, Length.Px(48)));

        sheet.AddRule(Style.Class("icon-primary")
            .Set(x => x.Color, t.Primary));

        sheet.AddRule(Style.Class("icon-secondary")
            .Set(x => x.Color, t.Secondary));

        sheet.AddRule(Style.Class("icon-success")
            .Set(x => x.Color, t.Success));

        sheet.AddRule(Style.Class("icon-danger")
            .Set(x => x.Color, t.Danger));

        sheet.AddRule(Style.Class("icon-warning")
            .Set(x => x.Color, t.Warning));

        sheet.AddRule(Style.Class("icon-info")
            .Set(x => x.Color, t.Info));

        sheet.AddRule(Style.Class("btn-icon")
            .Set(x => x.FontSize, Length.Px(16))
            .Set(x => x.MarginRight, Length.Px(6)));
    }
}
