using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class ProgressToken
{
    public ProgressToken(Theme theme)
    {
        ProgressHeight = Length.Rem(1);
        ProgressBg = theme.Gray200;
        ProgressBorderRadius = theme.BorderRadius;
        ProgressBarColor = Color.White;
        ProgressBarBg = theme.Primary;
    }

    public Length ProgressHeight { get; set; }
    public Color ProgressBg { get; set; }
    public float ProgressBorderRadius { get; set; }
    public Color ProgressBarColor { get; set; }
    public Color ProgressBarBg { get; set; }
}

internal static class ProgressStyles
{
    internal static CssObject GenStyle(ProgressToken t)
    {
        return new CssObject
        {
            [".progress"] = new()
            {
                Display = Display.Flex,
                Height = t.ProgressHeight,
                OverflowX = Overflow.Hidden,
                BackgroundColor = t.ProgressBg,
                BorderRadius = t.ProgressBorderRadius
            },

            [".progress-bar"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center,
                Color = t.ProgressBarColor,
                TextAlign = TextAlign.Center,
                BackgroundColor = t.ProgressBarBg
            }
        };
    }
}
