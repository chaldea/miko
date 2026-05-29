using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class DropdownToken
{
    public DropdownToken(Theme theme)
    {
        DropdownMinWidth = Length.Rem(10);
        DropdownPaddingY = Length.Rem(0.5f);
        DropdownBg = Color.White;
        DropdownBorderColor = theme.BorderColor;
        DropdownBorderWidth = theme.BorderWidth;
        DropdownBorderRadius = theme.BorderRadius;
        DropdownItemPaddingX = Length.Rem(1);
        DropdownItemPaddingY = Length.Rem(0.25f);
        DropdownLinkColor = theme.BodyColor;
        DropdownLinkHoverBg = theme.Gray200;
        DropdownLinkActiveColor = Color.White;
        DropdownLinkActiveBg = theme.Primary;
        DropdownDividerBg = theme.Gray200;
    }

    public Length DropdownMinWidth { get; set; }
    public Length DropdownPaddingY { get; set; }
    public Color DropdownBg { get; set; }
    public Color DropdownBorderColor { get; set; }
    public float DropdownBorderWidth { get; set; }
    public float DropdownBorderRadius { get; set; }
    public Length DropdownItemPaddingX { get; set; }
    public Length DropdownItemPaddingY { get; set; }
    public Color DropdownLinkColor { get; set; }
    public Color DropdownLinkHoverBg { get; set; }
    public Color DropdownLinkActiveColor { get; set; }
    public Color DropdownLinkActiveBg { get; set; }
    public Color DropdownDividerBg { get; set; }
}

internal static class DropdownStyles
{
    internal static CssObject GenStyle(DropdownToken t)
    {
        return new CssObject
        {
            [".dropdown"] = new()
            {
                Position = Position.Relative
            },

            [".dropdown-menu"] = new()
            {
                Position = Position.Absolute,
                ZIndex = 1000,
                Display = Display.None,
                MinWidth = t.DropdownMinWidth,
                Padding = new Padding(t.DropdownPaddingY, Length.Px(0)),
                BackgroundColor = t.DropdownBg,
                Border = new Border(Length.Px(t.DropdownBorderWidth), BorderStyle.Solid, t.DropdownBorderColor),
                BorderRadius = t.DropdownBorderRadius
            },

            [".dropdown-menu.show"] = new()
            {
                Display = Display.Block
            },

            [".dropdown-item"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                Padding = new Padding(t.DropdownItemPaddingY, t.DropdownItemPaddingX),
                Color = t.DropdownLinkColor,
                BackgroundColor = Color.Transparent,
                Border = new Border(Length.Px(0), BorderStyle.None, Color.Transparent)
            },

            [".dropdown-item:hover, .dropdown-item:focus"] = new()
            {
                BackgroundColor = t.DropdownLinkHoverBg
            },

            [".dropdown-item.active, .dropdown-item:active"] = new()
            {
                Color = t.DropdownLinkActiveColor,
                BackgroundColor = t.DropdownLinkActiveBg
            },

            [".dropdown-divider"] = new()
            {
                Height = Length.Px(0),
                Margin = new Margin(Length.Rem(0.5f), Length.Px(0)),
                OverflowX = Overflow.Hidden,
                BorderTop = new BorderSide(Length.Px(1), BorderStyle.Solid, t.DropdownDividerBg)
            }
        };
    }
}
