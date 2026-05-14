using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class BadgeStyles
{
    internal static void Apply(StyleSheet sheet, Theme t)
    {
        sheet.AddRule(Style.Class("badge")
            .Set(x => x.Display, Display.InlineBlock)
            .Set(x => x.Padding, new Padding(4, 8))
            .Set(x => x.FontSize, Length.Px(12))
            .Set(x => x.FontWeight, FontWeight.Bold)
            .Set(x => x.LineHeight, Length.Px(16))
            .Set(x => x.TextAlign, TextAlign.Center)
            .Set(x => x.WhiteSpace, WhiteSpace.Nowrap)
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadius)));

        var variants = new[]
        {
            ("text-bg-primary",   t.Primary,   Color.White),
            ("text-bg-secondary", t.Secondary, Color.White),
            ("text-bg-success",   t.Success,   Color.White),
            ("text-bg-danger",    t.Danger,    Color.White),
            ("text-bg-warning",   t.Warning,   Color.Black),
            ("text-bg-info",      t.Info,      Color.Black),
            ("text-bg-light",     t.Light,     Color.Black),
            ("text-bg-dark",      t.Dark,      Color.White),
        };

        foreach (var (cls, bg, fg) in variants)
            sheet.AddRule(Style.Class(cls).Set(x => x.BackgroundColor, bg).Set(x => x.Color, fg));

        sheet.AddRule(Style.Class("rounded-pill")
            .Set(x => x.BorderRadius, new BorderRadius(t.BorderRadiusPill)));
    }
}
