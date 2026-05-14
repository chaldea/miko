using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class GridStyles
{
    internal static void Apply(StyleSheet sheet, Theme t)
    {
        sheet.AddRule(Style.Class("container")
            .Set(x => x.Width, Length.Percent(100))
            .Set(x => x.Padding, new Padding(24))
            .Set(x => x.MarginRight, Length.Auto)
            .Set(x => x.MarginLeft, Length.Auto));

        sheet.AddRule(Style.Class("container-fluid")
            .Set(x => x.Width, Length.Percent(100))
            .Set(x => x.PaddingRight, Length.Px(12))
            .Set(x => x.PaddingLeft, Length.Px(12)));

        sheet.AddRule(Style.Class("row")
            .Set(x => x.Display, Display.Flex)
            .Set(x => x.FlexWrap, FlexWrap.Wrap)
            .Set(x => x.Gap, Length.Px(8))
            .Set(x => x.MarginBottom, Length.Px(16)));

        sheet.AddRule(Style.Class("col")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.FlexGrow, 1)
            .Set(x => x.FlexShrink, 1)
            .Set(x => x.FlexBasis, Length.Px(0))
            .Set(x => x.PaddingRight, Length.Px(12))
            .Set(x => x.PaddingLeft, Length.Px(12)));

        sheet.AddRule(Style.Class("col-auto")
            .Set(x => x.Display, Display.Block)
            .Set(x => x.FlexGrow, 0)
            .Set(x => x.FlexShrink, 0)
            .Set(x => x.Width, Length.Auto)
            .Set(x => x.PaddingRight, Length.Px(12))
            .Set(x => x.PaddingLeft, Length.Px(12)));

        for (int i = 1; i <= 12; i++)
        {
            float pct = (float)i / 12 * 100;
            sheet.AddRule(Style.Class($"col-{i}")
                .Set(x => x.Display, Display.Block)
                .Set(x => x.FlexGrow, 0)
                .Set(x => x.FlexShrink, 0)
                .Set(x => x.Width, Length.Percent(pct))
                .Set(x => x.PaddingRight, Length.Px(12))
                .Set(x => x.PaddingLeft, Length.Px(12)));
        }

        // Responsive containers
        var breakpoints = new[] { ("sm", 576f, 540f), ("md", 768f, 720f), ("lg", 992f, 960f), ("xl", 1200f, 1140f), ("xxl", 1400f, 1320f) };
        foreach (var (name, minW, maxW) in breakpoints)
        {
            sheet.AddMediaRule(MediaCondition.MinWidth(minW), Style.Class("container")
                .Set(x => x.MaxWidth, Length.Px(maxW)));

            for (int i = 1; i <= 12; i++)
            {
                float pct = (float)i / 12 * 100;
                sheet.AddMediaRule(MediaCondition.MinWidth(minW), Style.Class($"col-{name}-{i}")
                    .Set(x => x.Display, Display.Block)
                    .Set(x => x.FlexGrow, 0)
                    .Set(x => x.FlexShrink, 0)
                    .Set(x => x.Width, Length.Percent(pct))
                    .Set(x => x.PaddingRight, Length.Px(12))
                    .Set(x => x.PaddingLeft, Length.Px(12)));
            }
        }
    }
}
