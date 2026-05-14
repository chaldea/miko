using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class NavStyles
{
    internal static void Apply(StyleSheet sheet, Theme t)
    {
        sheet.AddRule(Style.Class("nav")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexWrap, FlexWrap.Wrap)
            .Set(x => x.PaddingLeft, Length.Px(0))
            .Set(x => x.MarginBottom, Length.Px(0)));

        sheet.AddRule(Style.Class("nav-link")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.Padding, new Padding(8, 16))
            .Set(x => x.Color, t.LinkColor)
            .Set(x => x.TextDecoration, TextDecoration.None));

        sheet.AddRule(Style.Class("nav-link").Hover()
            .Set(x => x.Color, t.LinkHoverColor));

        sheet.AddRule(Style.Class("nav-link").Disabled()
            .Set(x => x.Color, t.SecondaryColor)
            .Set(x => x.Cursor, Cursor.Default));

        // Tabs
        sheet.AddRule(Style.Class("nav-tabs")
            .Set(x => x.BorderBottom, new BorderSide(t.BorderWidth, BorderStyle.Solid, t.BorderColor)));

        // Pills
        sheet.AddRule(Style.Class("nav-pills")
            .Set(x => x.Gap, Length.Px(4)));

        sheet.AddRule(Style.Class("nav-pills").Where(e => e.HasClass("active"))
            .Set(x => x.BackgroundColor, t.Primary)
            .Set(x => x.Color, Color.White)
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadius)));

        // Navbar
        sheet.AddRule(Style.Class("navbar")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Row)
            .Set(x => x.AlignItems, AlignItems.Center)
            .Set(x => x.Padding, new Padding(8, 16)));

        sheet.AddRule(Style.Class("navbar-brand")
            .Set(x => x.Display, Display.InlineBlock)
            .Set(x => x.PaddingTop, Length.Px(4))
            .Set(x => x.PaddingBottom, Length.Px(4))
            .Set(x => x.MarginRight, Length.Px(16))
            .Set(x => x.FontSize, Length.Px(20))
            .Set(x => x.FontWeight, FontWeight.Bold)
            .Set(x => x.TextDecoration, TextDecoration.None));

        sheet.AddRule(Style.Class("navbar-nav")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexDirection, FlexDirection.Row)
            .Set(x => x.PaddingLeft, Length.Px(0))
            .Set(x => x.MarginBottom, Length.Px(0)));

        sheet.AddRule(Style.Class("navbar-dark")
            .Set(x => x.BackgroundColor, t.Dark));

        sheet.AddRule(Style.Class("navbar-light")
            .Set(x => x.BackgroundColor, t.Light));

        sheet.AddRule(Style.Class("bg-dark")
            .Set(x => x.BackgroundColor, t.Dark));

        sheet.AddRule(Style.Class("bg-light")
            .Set(x => x.BackgroundColor, t.Light));
    }
}
