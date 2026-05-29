using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class ListToken
{
    public ListToken(Theme theme)
    {
        ListGroupBorderColor = theme.BorderColor;
        ListGroupBorderWidth = theme.BorderWidth;
        ListGroupBorderRadius = theme.BorderRadius;
        ListGroupItemPaddingX = Length.Rem(1);
        ListGroupItemPaddingY = Length.Rem(0.5f);
        ListGroupColor = theme.BodyColor;
        ListGroupBg = Color.White;
        ListGroupActiveColor = Color.White;
        ListGroupActiveBg = theme.Primary;
        ListGroupActiveBorderColor = theme.Primary;
        ListGroupDisabledColor = theme.Gray600;
        ListGroupDisabledBg = Color.White;
    }

    public Color ListGroupBorderColor { get; set; }
    public float ListGroupBorderWidth { get; set; }
    public float ListGroupBorderRadius { get; set; }
    public Length ListGroupItemPaddingX { get; set; }
    public Length ListGroupItemPaddingY { get; set; }
    public Color ListGroupColor { get; set; }
    public Color ListGroupBg { get; set; }
    public Color ListGroupActiveColor { get; set; }
    public Color ListGroupActiveBg { get; set; }
    public Color ListGroupActiveBorderColor { get; set; }
    public Color ListGroupDisabledColor { get; set; }
    public Color ListGroupDisabledBg { get; set; }
}

internal static class ListStyles
{
    internal static CssObject GenStyle(ListToken t)
    {
        return new CssObject
        {
            [".list-group"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                PaddingLeft = Length.Px(0),
                MarginBottom = Length.Px(0),
                BorderRadius = t.ListGroupBorderRadius
            },

            [".list-group-item"] = new()
            {
                Position = Position.Relative,
                Display = Display.Block,
                Padding = new Padding(t.ListGroupItemPaddingY, t.ListGroupItemPaddingX),
                Color = t.ListGroupColor,
                BackgroundColor = t.ListGroupBg,
                Border = new Border(Length.Px(t.ListGroupBorderWidth), BorderStyle.Solid, t.ListGroupBorderColor),

                ["&:first-child"] = new()
                {
                    BorderTopLeftRadius = Length.Px(t.ListGroupBorderRadius),
                    BorderTopRightRadius = Length.Px(t.ListGroupBorderRadius)
                },

                ["&:last-child"] = new()
                {
                    BorderBottomLeftRadius = Length.Px(t.ListGroupBorderRadius),
                    BorderBottomRightRadius = Length.Px(t.ListGroupBorderRadius)
                }
            },

            [".list-group-item.active"] = new()
            {
                ZIndex = 2,
                Color = t.ListGroupActiveColor,
                BackgroundColor = t.ListGroupActiveBg,
                BorderColor = t.ListGroupActiveBorderColor
            },

            [".list-group-item.disabled"] = new()
            {
                Color = t.ListGroupDisabledColor,
                BackgroundColor = t.ListGroupDisabledBg
            },

            [".list-group-item + .list-group-item"] = new()
            {
                BorderTopWidth = Length.Px(0)
            },

            [".list-group-flush"] = new()
            {
                BorderRadius = 0,

                ["> .list-group-item"] = new()
                {
                    BorderRight = new BorderSide(0),
                    BorderLeft = new BorderSide(0),
                    BorderRadius = 0
                }
            }
        };
    }
}
