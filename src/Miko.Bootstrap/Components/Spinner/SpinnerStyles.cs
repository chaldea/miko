using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class SpinnerToken
{
    public SpinnerToken(Theme theme)
    {
        SpinnerWidth = Length.Rem(2);
        SpinnerHeight = Length.Rem(2);
        SpinnerBorderWidth = Length.Rem(0.25f);
        SpinnerSmWidth = Length.Rem(1);
        SpinnerSmHeight = Length.Rem(1);
    }

    public Length SpinnerWidth { get; set; }
    public Length SpinnerHeight { get; set; }
    public Length SpinnerBorderWidth { get; set; }
    public Length SpinnerSmWidth { get; set; }
    public Length SpinnerSmHeight { get; set; }
}

internal static class SpinnerStyles
{
    internal static CssObject GenStyle(SpinnerToken t)
    {
        return new CssObject
        {
            [".spinner-border"] = new()
            {
                Display = Display.InlineBlock,
                Width = t.SpinnerWidth,
                Height = t.SpinnerHeight,
                Border = new Border(t.SpinnerBorderWidth, BorderStyle.Solid, Color.Transparent),
                // NOTE: border-right-color: transparent for spinning effect; currentColor border not fully supported
                BorderRadius = 800f
            },

            [".spinner-border-sm"] = new()
            {
                Width = t.SpinnerSmWidth,
                Height = t.SpinnerSmHeight
            },

            [".spinner-grow"] = new()
            {
                Display = Display.InlineBlock,
                Width = t.SpinnerWidth,
                Height = t.SpinnerHeight,
                BorderRadius = 800f,
                Opacity = 0
            },

            [".spinner-grow-sm"] = new()
            {
                Width = t.SpinnerSmWidth,
                Height = t.SpinnerSmHeight
            }
        };
    }
}
