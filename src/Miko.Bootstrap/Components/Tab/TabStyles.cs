using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class TabToken
{
    public TabToken(Theme theme)
    {
        NavLinkPaddingX = Length.Rem(1);
        NavLinkPaddingY = Length.Rem(0.5f);
        NavTabsBorderWidth = theme.BorderWidth;
        NavTabsBorderColor = theme.Gray300;
        NavTabsBorderRadius = theme.BorderRadius;
        NavTabsLinkActiveColor = theme.BodyColor;
        NavTabsLinkActiveBg = Color.White;
        NavTabsLinkActiveBorderColor = theme.Gray300;
        NavLinkColor = theme.Primary;
    }

    public Length NavLinkPaddingX { get; set; }
    public Length NavLinkPaddingY { get; set; }
    public float NavTabsBorderWidth { get; set; }
    public Color NavTabsBorderColor { get; set; }
    public float NavTabsBorderRadius { get; set; }
    public Color NavTabsLinkActiveColor { get; set; }
    public Color NavTabsLinkActiveBg { get; set; }
    public Color NavTabsLinkActiveBorderColor { get; set; }
    public Color NavLinkColor { get; set; }
}

internal static class TabStyles
{
    internal static CssObject GenStyle(TabToken t)
    {
        return new CssObject
        {
            [".nav"] = new()
            {
                Display = Display.Flex,
                FlexWrap = FlexWrap.Wrap,
                PaddingLeft = Length.Px(0),
                MarginBottom = Length.Px(0)
            },

            [".nav-link"] = new()
            {
                Display = Display.Block,
                Padding = new Padding(t.NavLinkPaddingY, t.NavLinkPaddingX),
                Color = t.NavLinkColor,
                TextDecoration = TextDecoration.None,
                Border = new Border(Length.Px(0), BorderStyle.None, Color.Transparent)
            },

            [".nav-link:hover"] = new()
            {
                Color = t.NavLinkColor
            },

            [".nav-link.active"] = new()
            {
                Color = t.NavTabsLinkActiveColor
            },

            [".nav-tabs"] = new()
            {
                BorderBottom = new BorderSide(Length.Px(t.NavTabsBorderWidth), BorderStyle.Solid, t.NavTabsBorderColor)
            },

            [".nav-tabs .nav-link"] = new()
            {
                MarginBottom = Length.Px(-t.NavTabsBorderWidth),
                Border = new Border(Length.Px(t.NavTabsBorderWidth), BorderStyle.Solid, Color.Transparent),
                BorderTopLeftRadius = Length.Px(t.NavTabsBorderRadius),
                BorderTopRightRadius = Length.Px(t.NavTabsBorderRadius)
            },

            [".nav-tabs .nav-link.active"] = new()
            {
                Color = t.NavTabsLinkActiveColor,
                BackgroundColor = t.NavTabsLinkActiveBg,
                BorderColor = t.NavTabsLinkActiveBorderColor
            },

            [".tab-content > .tab-pane"] = new()
            {
                Display = Display.None
            },

            [".tab-content > .active"] = new()
            {
                Display = Display.Block
            }
        };
    }
}
