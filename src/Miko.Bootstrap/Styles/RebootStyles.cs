using Miko.Common;
using Miko.Core.DomElements;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class RebootStyles
{
    internal static void Apply(StyleSheet sheet, Theme t)
    {
        // headings
        sheet.AddRule(Style.For<H1Element>()
            .Set(x => x.FontSize, Length.Px(40))
            .Set(x => x.FontWeight, FontWeight.Medium)
            .Set(x => x.LineHeight, Length.Px(48))
            .Set(x => x.MarginTop, Length.Px(0))
            .Set(x => x.MarginBottom, Length.Px(8))
            .Set(x => x.Color, t.HeadingColor));

        sheet.AddRule(Style.For<H2Element>()
            .Set(x => x.FontSize, Length.Px(32))
            .Set(x => x.FontWeight, FontWeight.Medium)
            .Set(x => x.LineHeight, Length.Px(38))
            .Set(x => x.MarginTop, Length.Px(24))
            .Set(x => x.MarginBottom, Length.Px(8))
            .Set(x => x.Color, t.HeadingColor));

        sheet.AddRule(Style.For<H3Element>()
            .Set(x => x.FontSize, Length.Px(28))
            .Set(x => x.FontWeight, FontWeight.Medium)
            .Set(x => x.LineHeight, Length.Px(34))
            .Set(x => x.MarginTop, Length.Px(0))
            .Set(x => x.MarginBottom, Length.Px(8))
            .Set(x => x.Color, t.HeadingColor));

        sheet.AddRule(Style.For<H4Element>()
            .Set(x => x.FontSize, Length.Px(24))
            .Set(x => x.FontWeight, FontWeight.Medium)
            .Set(x => x.LineHeight, Length.Px(29))
            .Set(x => x.MarginTop, Length.Px(0))
            .Set(x => x.MarginBottom, Length.Px(8))
            .Set(x => x.Color, t.HeadingColor));

        sheet.AddRule(Style.For<H5Element>()
            .Set(x => x.FontSize, Length.Px(20))
            .Set(x => x.FontWeight, FontWeight.Medium)
            .Set(x => x.LineHeight, Length.Px(24))
            .Set(x => x.MarginTop, Length.Px(0))
            .Set(x => x.MarginBottom, Length.Px(8))
            .Set(x => x.Color, t.HeadingColor));

        sheet.AddRule(Style.For<H6Element>()
            .Set(x => x.FontSize, Length.Px(16))
            .Set(x => x.FontWeight, FontWeight.Medium)
            .Set(x => x.LineHeight, Length.Px(19))
            .Set(x => x.MarginTop, Length.Px(0))
            .Set(x => x.MarginBottom, Length.Px(8))
            .Set(x => x.Color, t.HeadingColor));

        // paragraph
        sheet.AddRule(Style.For<ParagraphElement>()
            .Set(x => x.MarginTop, Length.Px(0))
            .Set(x => x.MarginBottom, Length.Px(16)));

        // anchor
        sheet.AddRule(Style.For<AnchorElement>()
            .Set(x => x.Color, t.LinkColor)
            .Set(x => x.TextDecoration, TextDecoration.Underline));

        sheet.AddRule(Style.For<AnchorElement>().Hover()
            .Set(x => x.Color, t.LinkHoverColor));
    }
}
