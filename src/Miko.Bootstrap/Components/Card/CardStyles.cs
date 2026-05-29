using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class CardToken
{
    public CardToken(Theme theme)
    {
        CardSpacerY = Length.Rem(1);
        CardSpacerX = Length.Rem(1);
        CardTitleSpacerY = Length.Rem(0.5f);
        CardBorderWidth = theme.BorderWidth;
        CardBorderColor = theme.BorderColor;
        CardBorderRadius = theme.BorderRadius;
        CardInnerBorderRadius = theme.BorderRadius - theme.BorderWidth;
        CardCapPaddingY = Length.Rem(0.5f);
        CardCapPaddingX = Length.Rem(1);
        CardCapBg = Color.FromRgba(0, 0, 0, (byte)(255 * 0.03f));
        CardBg = Color.White;
        CardColor = theme.BodyColor;
    }

    public Length CardSpacerY { get; set; }
    public Length CardSpacerX { get; set; }
    public Length CardTitleSpacerY { get; set; }
    public float CardBorderWidth { get; set; }
    public Color CardBorderColor { get; set; }
    public float CardBorderRadius { get; set; }
    public float CardInnerBorderRadius { get; set; }
    public Length CardCapPaddingY { get; set; }
    public Length CardCapPaddingX { get; set; }
    public Color CardCapBg { get; set; }
    public Color CardBg { get; set; }
    public Color CardColor { get; set; }
}

internal static class CardStyles
{
    internal static CssObject GenStyle(CardToken t)
    {
        return new CssObject
        {
            [".card"] = new()
            {
                Position = Position.Relative,
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                MinWidth = Length.Px(0),
                BackgroundColor = t.CardBg,
                Border = new Border(Length.Px(t.CardBorderWidth), BorderStyle.Solid, t.CardBorderColor),
                BorderRadius = t.CardBorderRadius
            },

            [".card-body"] = new()
            {
                FlexGrow = 1,
                Padding = new Padding(t.CardSpacerY, t.CardSpacerX),
                Color = t.CardColor
            },

            [".card-title"] = new()
            {
                MarginBottom = t.CardTitleSpacerY
            },

            [".card-subtitle"] = new()
            {
                MarginBottom = Length.Px(0)
            },

            [".card-text:last-child"] = new()
            {
                MarginBottom = Length.Px(0)
            },

            [".card-header"] = new()
            {
                Padding = new Padding(t.CardCapPaddingY, t.CardCapPaddingX),
                MarginBottom = Length.Px(0),
                BackgroundColor = t.CardCapBg,
                BorderBottom = new BorderSide(Length.Px(t.CardBorderWidth), BorderStyle.Solid, t.CardBorderColor),

                ["&:first-child"] = new()
                {
                    BorderTopLeftRadius = t.CardInnerBorderRadius,
                    BorderTopRightRadius = t.CardInnerBorderRadius
                }
            },

            [".card-footer"] = new()
            {
                Padding = new Padding(t.CardCapPaddingY, t.CardCapPaddingX),
                BackgroundColor = t.CardCapBg,
                BorderTop = new BorderSide(Length.Px(t.CardBorderWidth), BorderStyle.Solid, t.CardBorderColor),

                ["&:last-child"] = new()
                {
                    BorderBottomLeftRadius = t.CardInnerBorderRadius,
                    BorderBottomRightRadius = t.CardInnerBorderRadius
                }
            },

            [".card-img, .card-img-top, .card-img-bottom"] = new()
            {
                Width = Length.Percent(100)
            },

            [".card-img, .card-img-top"] = new()
            {
                BorderTopLeftRadius = t.CardInnerBorderRadius,
                BorderTopRightRadius = t.CardInnerBorderRadius
            },

            [".card-img, .card-img-bottom"] = new()
            {
                BorderBottomLeftRadius = t.CardInnerBorderRadius,
                BorderBottomRightRadius = t.CardInnerBorderRadius
            }
        };
    }
}
