using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class PaginationToken
{
    public PaginationToken(Theme theme)
    {
        PaginationPaddingX = Length.Rem(0.75f);
        PaginationPaddingY = Length.Rem(0.375f);
        PaginationColor = theme.Primary;
        PaginationBg = Color.White;
        PaginationBorderWidth = theme.BorderWidth;
        PaginationBorderColor = theme.Gray300;
        PaginationBorderRadius = theme.BorderRadius;
        PaginationHoverBg = theme.Gray200;
        PaginationHoverBorderColor = theme.Gray300;
        PaginationActiveColor = Color.White;
        PaginationActiveBg = theme.Primary;
        PaginationActiveBorderColor = theme.Primary;
        PaginationDisabledColor = theme.Gray600;
        PaginationDisabledBg = Color.White;
    }

    public Length PaginationPaddingX { get; set; }
    public Length PaginationPaddingY { get; set; }
    public Color PaginationColor { get; set; }
    public Color PaginationBg { get; set; }
    public float PaginationBorderWidth { get; set; }
    public Color PaginationBorderColor { get; set; }
    public float PaginationBorderRadius { get; set; }
    public Color PaginationHoverBg { get; set; }
    public Color PaginationHoverBorderColor { get; set; }
    public Color PaginationActiveColor { get; set; }
    public Color PaginationActiveBg { get; set; }
    public Color PaginationActiveBorderColor { get; set; }
    public Color PaginationDisabledColor { get; set; }
    public Color PaginationDisabledBg { get; set; }
}

internal static class PaginationStyles
{
    internal static CssObject GenStyle(PaginationToken t)
    {
        return new CssObject
        {
            [".pagination"] = new()
            {
                Display = Display.Flex,
                PaddingLeft = Length.Px(0)
            },

            [".page-link"] = new()
            {
                Position = Position.Relative,
                Display = Display.Block,
                Padding = new Padding(t.PaginationPaddingY, t.PaginationPaddingX),
                Color = t.PaginationColor,
                BackgroundColor = t.PaginationBg,
                Border = new Border(Length.Px(t.PaginationBorderWidth), BorderStyle.Solid, t.PaginationBorderColor)
            },

            [".page-link:hover"] = new()
            {
                ZIndex = 2,
                BackgroundColor = t.PaginationHoverBg,
                BorderColor = t.PaginationHoverBorderColor
            },

            [".page-link.active, .active > .page-link"] = new()
            {
                ZIndex = 3,
                Color = t.PaginationActiveColor,
                BackgroundColor = t.PaginationActiveBg,
                BorderColor = t.PaginationActiveBorderColor
            },

            [".page-link.disabled, .disabled > .page-link"] = new()
            {
                Color = t.PaginationDisabledColor,
                BackgroundColor = t.PaginationDisabledBg
            },

            [".page-item:first-child .page-link"] = new()
            {
                BorderTopLeftRadius = t.PaginationBorderRadius,
                BorderBottomLeftRadius = t.PaginationBorderRadius
            },

            [".page-item:last-child .page-link"] = new()
            {
                BorderTopRightRadius = t.PaginationBorderRadius,
                BorderBottomRightRadius = t.PaginationBorderRadius
            }
        };
    }
}
