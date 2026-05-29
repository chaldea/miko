using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class TooltipToken
{
    public TooltipToken(Theme theme)
    {
        TooltipMaxWidth = Length.Px(200);
        TooltipPaddingX = Length.Rem(0.5f);
        TooltipPaddingY = Length.Rem(0.25f);
        TooltipFontSize = Length.Rem(0.875f);
        TooltipColor = Color.White;
        TooltipBg = Color.Black;
        TooltipBorderRadius = theme.BorderRadius;
        TooltipOpacity = 0.9f;
    }

    public Length TooltipMaxWidth { get; set; }
    public Length TooltipPaddingX { get; set; }
    public Length TooltipPaddingY { get; set; }
    public Length TooltipFontSize { get; set; }
    public Color TooltipColor { get; set; }
    public Color TooltipBg { get; set; }
    public float TooltipBorderRadius { get; set; }
    public float TooltipOpacity { get; set; }
}

internal static class TooltipStyles
{
    internal static CssObject GenStyle(TooltipToken t)
    {
        return new CssObject
        {
            [".tooltip"] = new()
            {
                ZIndex = 1080,
                Display = Display.Block,
                Opacity = 0
            },

            [".tooltip.show"] = new()
            {
                Opacity = t.TooltipOpacity
            },

            [".tooltip-inner"] = new()
            {
                MaxWidth = t.TooltipMaxWidth,
                Padding = new Padding(t.TooltipPaddingY, t.TooltipPaddingX),
                Color = t.TooltipColor,
                TextAlign = TextAlign.Center,
                BackgroundColor = t.TooltipBg,
                BorderRadius = t.TooltipBorderRadius
            }
        };
    }
}
