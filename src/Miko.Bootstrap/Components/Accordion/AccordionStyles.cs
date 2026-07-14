using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class AccordionToken
{
    public AccordionToken(Theme theme)
    {
        AccordionColor = theme.BodyColor;
        AccordionBg = theme.BodyBg;
        AccordionTransition = [
            Transition.For(x => x.Color).Duration(0.15f).EaseInOut(),
            Transition.For(x => x.BackgroundColor).Duration(0.15f).EaseInOut(),
            Transition.For(x => x.BorderColor).Duration(0.15f).EaseInOut(),
            Transition.For(x => x.BoxShadow).Duration(0.15f).EaseInOut(),
            Transition.For(x => x.BorderRadius).Duration(0.15f).Ease(),
        ];
        AccordionBorderColor = theme.BorderColor;
        AccordionBorderWidth = theme.BorderWidth;
        AccordionBorderRadius = theme.BorderRadius;
        AccordionInnerBorderRadius = theme.BorderRadius - theme.BorderWidth;
        AccordionBtnPaddingX = Length.Rem(1.25f);
        AccordionBtnPaddingY = Length.Rem(1);
        AccordionBtnColor = theme.BodyColor;
        AccordionBtnBg = AccordionBg;
        AccordionBtnIcon = BackgroundImage.FromResource(typeof(AccordionToken).Assembly, "Miko.Bootstrap.Resources.Images.AccordionBtnIcon.svg");
        AccordionBtnIconWidth = Length.Rem(1.25f);
        AccordionBtnIconTransform = Transform.FromRotate(-180);
        AccordionBtnIconTransition = [
            Transition.For(x => x.Transform).Duration(0.2f).EaseInOut()
        ];
        AccordionBtnActiveIcon = BackgroundImage.FromResource(typeof(AccordionToken).Assembly, "Miko.Bootstrap.Resources.Images.AccordionBtnActiveIcon.svg");
        AccordionBtnFocusBoxShadow = new BoxShadow(0, 0, 0, Length.Rem(0.25f), Color.FromRgba(13, 110, 253, 0.25f));
        AccordionBodyPaddingX = Length.Rem(1.25f);
        AccordionBodyPaddingY = Length.Rem(1);
        AccordionActiveColor = theme.PrimaryTextEmphasis;
        AccordionActiveBg = theme.PrimaryBgSubtle;
    }

    public Color AccordionColor { get; set; }
    public Color AccordionBg { get; set; }
    public List<Transition> AccordionTransition { get; set; }
    public Color AccordionBorderColor { get; set; }
    public Length AccordionBorderWidth { get; set; }
    public Length AccordionBorderRadius { get; set; }
    public Length AccordionInnerBorderRadius { get; set; }
    public Length AccordionBtnPaddingX { get; set; }
    public Length AccordionBtnPaddingY { get; set; }
    public Color AccordionBtnColor { get; set; }
    public Color AccordionBtnBg { get; set; }
    public BackgroundImage AccordionBtnIcon { get; set; }
    public Length AccordionBtnIconWidth { get; set; }
    public Transform AccordionBtnIconTransform { get; set; }
    public List<Transition> AccordionBtnIconTransition { get; set; }
    public BackgroundImage AccordionBtnActiveIcon { get; set; }
    public BoxShadow AccordionBtnFocusBoxShadow { get; set; }
    public Length AccordionBodyPaddingX { get; set; }
    public Length AccordionBodyPaddingY { get; set; }
    public Color AccordionActiveColor { get; set; }
    public Color AccordionActiveBg { get; set; }
}

internal static class AccordionStyles
{
    internal static CssObject GenStyle(AccordionToken t)
    {
        return new CssObject
        {
            [".collapse:not(.show)"] = new()
            {
                Display = Display.None
            },

            [".accordion-button"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Width = Length.Percent(100),
                Padding = new Padding(t.AccordionBtnPaddingY, t.AccordionBtnPaddingX),
                FontSize = Length.Px(16),
                Color = t.AccordionBtnColor,
                TextAlign = TextAlign.Left,
                BackgroundColor = t.AccordionBtnBg,
                Border = new Border(Length.Px(0), BorderStyle.None, Color.Transparent),
                BorderRadius = new BorderRadius(Length.Px(0)),
                Transitions = t.AccordionTransition,

                ["&:not(.collapsed)"] = new()
                {
                    Color = t.AccordionActiveColor,
                    BackgroundColor = t.AccordionActiveBg,
                    BoxShadow = new List<BoxShadow> { new BoxShadow(0, -1 * t.AccordionBorderWidth.Value, 0, 0, t.AccordionBorderColor, true) },

                    ["&::after"] = new()
                    {
                        BackgroundImage = t.AccordionBtnActiveIcon,
                        Transform = t.AccordionBtnIconTransform
                    }
                },

                ["&::after"] = new()
                {
                    FlexShrink = 0,
                    Width = t.AccordionBtnIconWidth,
                    Height = t.AccordionBtnIconWidth,
                    MarginLeft = Length.Auto,
                    Content = "",
                    BackgroundImage = t.AccordionBtnIcon,
                    BackgroundRepeat = BackgroundRepeat.NoRepeat,
                    BackgroundSize = BackgroundSize.From(t.AccordionBtnIconWidth),
                    Transitions = t.AccordionBtnIconTransition
                },

                ["&:hover"] = new()
                {
                    ZIndex = 2
                },

                ["&:focus"] = new()
                {
                    ZIndex = 3,
                    BoxShadow = new List<BoxShadow> { t.AccordionBtnFocusBoxShadow }
                },

            },

            [".accordion-header"] = new()
            {
                MarginBottom = Length.Px(0)
            },

            [".accordion-item"] = new()
            {
                Color = t.AccordionColor,
                BackgroundColor = t.AccordionBg,
                Border = new Border(t.AccordionBorderWidth, BorderStyle.Solid, t.AccordionBorderColor),

                ["&:first-of-type"] = new()
                {
                    BorderTopLeftRadius = t.AccordionBorderRadius,
                    BorderTopRightRadius = t.AccordionBorderRadius,

                    ["> .accordion-header .accordion-button"] = new()
                    {
                        BorderTopLeftRadius = t.AccordionInnerBorderRadius,
                        BorderTopRightRadius = t.AccordionInnerBorderRadius
                    },
                },

                ["&:not(:first-of-type)"] = new()
                {
                    BorderTop = new BorderSide(0)
                },

                ["&:last-of-type"] = new()
                {
                    BorderBottomRightRadius = t.AccordionBorderRadius,
                    BorderBottomLeftRadius = t.AccordionBorderRadius,

                    ["> .accordion-header .accordion-button.collapsed"] = new()
                    {
                        BorderBottomRightRadius = t.AccordionInnerBorderRadius,
                        BorderBottomLeftRadius = t.AccordionInnerBorderRadius,
                    },

                    ["> .accordion-collapse"] = new()
                    {
                        BorderBottomRightRadius = t.AccordionBorderRadius,
                        BorderBottomLeftRadius = t.AccordionBorderRadius,
                    },
                },
            },

            [".accordion-body"] = new()
            {
                Padding = new Padding(t.AccordionBodyPaddingY, t.AccordionBodyPaddingX)
            },

            [".accordion-flush"] = new()
            {
                ["> .accordion-item"] = new()
                {
                    BorderRight = new BorderSide(0),
                    BorderLeft = new BorderSide(0),
                    BorderRadius = 0,

                    ["&:first-child"] = new()
                    {
                        BorderTop = new BorderSide(0),
                    },

                    ["&:first-child"] = new()
                    {
                        BorderBottom = new BorderSide(0),
                    },
                },

                ["> .accordion-collapse,> .accordion-header .accordion-button,> .accordion-header .accordion-button.collapsed"] = new()
                {
                    BorderRadius = 0,
                }
            },


        };
    }
}
