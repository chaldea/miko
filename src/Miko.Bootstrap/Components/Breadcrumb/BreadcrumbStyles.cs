using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class BreadcrumbToken
{
    public BreadcrumbToken(Theme theme)
    {
        BreadcrumbPaddingX = Length.Px(0);
        BreadcrumbPaddingY = Length.Px(0);
        BreadcrumbMarginBottom = Length.Rem(1);
        BreadcrumbFontSize = Length.Rem(1);
        BreadcrumbBorderRadius = theme.BorderRadius;
        BreadcrumbDividerColor = theme.Gray600;
        BreadcrumbItemPaddingX = Length.Rem(0.5f);
        BreadcrumbActiveColor = theme.Gray600;
    }

    public Length BreadcrumbPaddingX { get; set; }
    public Length BreadcrumbPaddingY { get; set; }
    public Length BreadcrumbMarginBottom { get; set; }
    public Length BreadcrumbFontSize { get; set; }
    public Length BreadcrumbBorderRadius { get; set; }
    public Color BreadcrumbDividerColor { get; set; }
    public Length BreadcrumbItemPaddingX { get; set; }
    public Color BreadcrumbActiveColor { get; set; }
}

internal static class BreadcrumbStyles
{
    internal static CssObject GenStyle(BreadcrumbToken t)
    {
        return new CssObject
        {
            [".breadcrumb"] = new()
            {
                Display = Display.Flex,
                FlexWrap = FlexWrap.Wrap,
                Padding = new Padding(t.BreadcrumbPaddingY, t.BreadcrumbPaddingX),
                MarginBottom = t.BreadcrumbMarginBottom,
                FontSize = t.BreadcrumbFontSize,
                // NOTE: list-style: none not supported
                BorderRadius = t.BreadcrumbBorderRadius
            },

            [".breadcrumb-item.active"] = new()
            {
                Color = t.BreadcrumbActiveColor
            },

            [".breadcrumb-item + .breadcrumb-item"] = new()
            {
                PaddingLeft = t.BreadcrumbItemPaddingX,

                ["&::before"] = new()
                {
                    // NOTE: float: left not supported
                    PaddingRight = t.BreadcrumbItemPaddingX,
                    Color = t.BreadcrumbDividerColor,
                    Content = "/"
                }
            }
        };
    }
}
