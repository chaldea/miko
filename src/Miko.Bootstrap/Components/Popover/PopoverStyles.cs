using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class PopoverToken
{
    public PopoverToken(Theme theme)
    {
        PopoverMaxWidth = Length.Px(276);
        PopoverBg = Color.White;
        PopoverBorderWidth = theme.BorderWidth;
        PopoverBorderColor = theme.BorderColor;
        PopoverBorderRadius = theme.BorderRadius;
        PopoverHeaderPaddingY = Length.Rem(0.5f);
        PopoverHeaderPaddingX = Length.Rem(1);
        PopoverHeaderBg = Color.FromRgb(240, 240, 240);
        PopoverBodyPaddingY = Length.Rem(1);
        PopoverBodyPaddingX = Length.Rem(1);
        PopoverBodyColor = theme.BodyColor;
    }

    public Length PopoverMaxWidth { get; set; }
    public Color PopoverBg { get; set; }
    public float PopoverBorderWidth { get; set; }
    public Color PopoverBorderColor { get; set; }
    public float PopoverBorderRadius { get; set; }
    public Length PopoverHeaderPaddingY { get; set; }
    public Length PopoverHeaderPaddingX { get; set; }
    public Color PopoverHeaderBg { get; set; }
    public Length PopoverBodyPaddingY { get; set; }
    public Length PopoverBodyPaddingX { get; set; }
    public Color PopoverBodyColor { get; set; }
}

internal static class PopoverStyles
{
    internal static CssObject GenStyle(PopoverToken t)
    {
        return new CssObject
        {
            [".popover"] = new()
            {
                ZIndex = 1070,
                Display = Display.Block,
                MaxWidth = t.PopoverMaxWidth,
                BackgroundColor = t.PopoverBg,
                Border = new Border(Length.Px(t.PopoverBorderWidth), BorderStyle.Solid, t.PopoverBorderColor),
                BorderRadius = t.PopoverBorderRadius
            },

            [".popover-header"] = new()
            {
                Padding = new Padding(t.PopoverHeaderPaddingY, t.PopoverHeaderPaddingX),
                MarginBottom = Length.Px(0),
                BackgroundColor = t.PopoverHeaderBg,
                BorderBottom = new BorderSide(Length.Px(t.PopoverBorderWidth), BorderStyle.Solid, t.PopoverBorderColor)
            },

            [".popover-body"] = new()
            {
                Padding = new Padding(t.PopoverBodyPaddingY, t.PopoverBodyPaddingX),
                Color = t.PopoverBodyColor
            }
        };
    }
}
