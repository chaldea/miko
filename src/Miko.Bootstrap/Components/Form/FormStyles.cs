using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class FormToken
{
    public Theme BS { get; }

    public FormToken(Theme bs)
    {
        BS = bs;
        
        // Check Token
        FormCheckBg = bs.BodyBg;

        FormLabelMarginBottom = Length.Rem(0.5f);
        FormLabelColor = bs.BodyColor;

        FormTextMarginTop = Length.Rem(0.25f);
        FormTextFontSize = Length.Rem(0.875f);
        FormTextColor = bs.SecondaryColor;

        InputPaddingY = Length.Rem(0.375f);
        InputPaddingX = Length.Rem(0.75f);
        InputFontSize = Length.Rem(1f);
        InputLineHeight = 1.5f;
        InputColor = bs.BodyColor;
        InputBg = bs.BodyBg;
        InputBorderWidth = bs.BorderWidth;
        InputBorderColor = bs.BorderColor;
        InputBorderRadius = bs.BorderRadius;
        InputFocusBorderColor = bs.Primary;
        InputDisabledBg = bs.SecondaryBg;

        InputPaddingYSm = Length.Rem(0.25f);
        InputPaddingXSm = Length.Rem(0.5f);
        InputFontSizeSm = Length.Rem(0.875f);
        InputBorderRadiusSm = bs.BorderRadiusSm;

        InputPaddingYLg = Length.Rem(0.5f);
        InputPaddingXLg = Length.Rem(1f);
        InputFontSizeLg = Length.Rem(1.25f);
        InputBorderRadiusLg = bs.BorderRadiusLg;

        FormCheckPaddingStart = Length.Rem(1.5f);
        FormCheckInputWidth = Length.Rem(1f);
        FormCheckInputBorderColor = bs.BorderColor;
        FormCheckInputBorderRadius = bs.BorderRadiusSm;
        FormCheckInputCheckedBg = bs.Primary;
        FormCheckInputCheckedBorderColor = bs.Primary;

        InputGroupBorderColor = bs.BorderColor;
        InputGroupTextBg = bs.TertiaryBg;
        InputGroupTextColor = bs.BodyColor;

        FormValidColor = bs.FormValidColor;
        FormValidBorderColor = bs.FormValidBorderColor;
        FormInvalidColor = bs.FormInvalidColor;
        FormInvalidBorderColor = bs.FormInvalidBorderColor;
    }


    public Color FormCheckBg { get; set; }
    public BackgroundImage FormCheckBgImage { get; set; }

    public Length FormLabelMarginBottom { get; set; }
    public Color FormLabelColor { get; set; }

    public Length FormTextMarginTop { get; set; }
    public Length FormTextFontSize { get; set; }
    public Color FormTextColor { get; set; }

    public Length InputPaddingY { get; set; }
    public Length InputPaddingX { get; set; }
    public Length InputFontSize { get; set; }
    public float InputLineHeight { get; set; }
    public Color InputColor { get; set; }
    public Color InputBg { get; set; }
    public float InputBorderWidth { get; set; }
    public Color InputBorderColor { get; set; }
    public float InputBorderRadius { get; set; }
    public Color InputFocusBorderColor { get; set; }
    public Color InputDisabledBg { get; set; }

    public Length InputPaddingYSm { get; set; }
    public Length InputPaddingXSm { get; set; }
    public Length InputFontSizeSm { get; set; }
    public float InputBorderRadiusSm { get; set; }

    public Length InputPaddingYLg { get; set; }
    public Length InputPaddingXLg { get; set; }
    public Length InputFontSizeLg { get; set; }
    public float InputBorderRadiusLg { get; set; }

    public Length FormCheckPaddingStart { get; set; }
    public Length FormCheckInputWidth { get; set; }
    public Color FormCheckInputBorderColor { get; set; }
    public float FormCheckInputBorderRadius { get; set; }
    public Color FormCheckInputCheckedBg { get; set; }
    public Color FormCheckInputCheckedBorderColor { get; set; }

    public Color InputGroupBorderColor { get; set; }
    public Color InputGroupTextBg { get; set; }
    public Color InputGroupTextColor { get; set; }

    public Color FormValidColor { get; set; }
    public Color FormValidBorderColor { get; set; }
    public Color FormInvalidColor { get; set; }
    public Color FormInvalidBorderColor { get; set; }
}

internal static class FormStyles
{
    internal static CssObject GenStyle(FormToken t)
    {
        return new CssObject
        {
            [".form-label"] = new()
            {
                MarginBottom = t.FormLabelMarginBottom,
                Color = t.FormLabelColor
            },

            [".col-form-label"] = new()
            {

            },

            [".form-text"] = new()
            {
                MarginTop = t.FormTextMarginTop,
                FontSize = t.FormTextFontSize,
                Color = t.FormTextColor
            },

            [".form-control"] = new()
            {
                Display = Display.Block,
                Width = Percent(100),
                Padding = new Padding(Rem(0.375f), Rem(0.375f)),
                FontSize = Rem(1f),
                FontWeight = FontWeight.Normal,
                LineHeight = Number(1.5f),
                Color = t.BS.BodyColor,
                BackgroundColor = t.BS.BodyBg,
                Border = new Border(t.BS.BorderWidth, BorderStyle.Solid, t.BS.BorderColor),
                BorderRadius = t.BS.BorderRadius,
                Transitions = new List<Transition> {
                    Transition.For(x => x.BorderColor).Duration(0.15f).EaseInOut(),
                    Transition.For(x => x.BoxShadow).Duration(0.15f).EaseInOut(),
                },
                ["&:focus"] = new()
                {
                    Color = t.BS.BodyColor,
                    BackgroundColor = t.BS.BodyBg,
                    BorderColor = (Color)"#86b7fe",
                    BoxShadow = new List<BoxShadow> { new BoxShadow(0, 0, 0, Rem(0.25f), Rgba(13, 110, 253, 0.25f)) }
                },
                ["&:disabled"] = new()
                {
                    BackgroundColor = t.BS.SecondaryBg,
                    Opacity = 1f
                },
            },

            [".form-control-plaintext"] = new()
            {
                Display = Display.Block,
                Width = Percent(100),
                Padding = new Padding(Rem(0.375f), 0),
                MarginBottom = Length.Px(0),
                LineHeight = Rem(1.5f),
                Color = t.BS.BorderColor,
                BackgroundColor = Transparent,
                Border = new Border(0, BorderStyle.Solid, Transparent),
                BorderWidth = Length.Px(t.BS.BorderWidth),

                ["&:focus"] = new()
                {
                    // Outline = 0,
                },

                ["&.form-control-sm,&.form-control-lg"] = new()
                {
                    PaddingRight = Length.Px(0),
                    PaddingLeft = Length.Px(0),
                }
            },

            [".form-control-sm"] = new()
            {
                MinHeight = Em(1.5f) + Rem(0.5f) + t.BS.BorderWidth * 2,
                Padding = new Padding(Rem(0.25f), Rem(0.5f)),
                FontSize = Rem(0.875f),
                BorderRadius = t.BS.BorderRadiusSm
            },

            [".form-control-lg"] = new()
            {
                MinHeight = Em(1.5f) + Rem(1f) + t.BS.BorderWidth * 2,
                Padding = new Padding(Rem(0.5f), Rem(1)),
                FontSize = Rem(1.25f),
                BorderRadius = t.BS.BorderRadiusLg
            },

            ["textarea"] = new()
            {
                ["&.form-control"] = new()
                {
                    MinHeight = Em(1.5f) + Rem(0.75f) + t.BS.BorderWidth * 2
                },
                ["&.form-control-sm"] = new()
                {
                    MinHeight = Em(1.5f) + Rem(0.5f) + t.BS.BorderWidth * 2
                },
                ["&.form-control-lg"] = new()
                {
                    MinHeight = Em(1.5f) + Rem(1f) + t.BS.BorderWidth * 2
                },
            },

            [".form-control-color"] = new()
            {
                Width = Rem(3f),
                Height = Em(1.5f) + Rem(0.75f) + t.BS.BorderWidth * 2,
                Padding = Rem(0.375f),

                ["&:not(:disabled):not([readonly])"] = new()
                {

                },

                ["&.form-control-sm"] = new()
                {
                    Height = Em(1.5f) + Rem(0.5f) + t.BS.BorderWidth * 2
                },

                ["&.form-control-lg"] = new()
                {
                    Height = Em(1.5f) + Rem(1f) + t.BS.BorderWidth * 2
                }
            },

            [".form-select"] = new()
            {
                Display = Display.Block,
                Width = Percent(100),
                Padding = new Padding(Rem(0.375f), Rem(2.25f), Rem(0.375f), Rem(0.75f)),
                FontSize = Rem(1),
                LineHeight = Number(1.5f),
                Color = t.BS.BodyColor,
                BackgroundColor = t.BS.BodyBg,
                // BackgroundImage = 
                // BackgroundRepeat = BackgroundRepeat.NoRepeat,
                // BackgroundPosition = 
                // BackgroundSize = BackgroundSize.From(Px(16), Px(12)),
                Border = new Border(Px(t.BS.BorderWidth), BorderStyle.Solid, t.BS.BorderColor),
                BorderRadius = t.BS.BorderRadius,
                Transitions = new List<Transition> {
                    Transition.For(x => x.BorderColor).Duration(0.15f).EaseInOut(),
                    Transition.For(x => x.BoxShadow).Duration(0.15f).EaseInOut(),
                },

                ["&:focus"] = new()
                {
                    BorderColor = (Color)"#86b7fe",
                    // Outline = 0,
                    BoxShadow = new List<BoxShadow> { new BoxShadow(0, 0, 0, Rem(0.25f), Rgba(13, 110, 253, 0.25f)) }
                },

                ["&:disabled"] = new()
                {
                    BackgroundColor = t.BS.SecondaryColor
                }
            },

            [".form-select-sm"] = new()
            {
                PaddingTop = Rem(0.25f),
                PaddingBottom = Rem(0.25f),
                PaddingLeft = Rem(0.5f),
                FontSize = Rem(0.875f),
                BorderRadius = t.BS.BorderRadiusSm
            },

            [".form-select-lg"] = new()
            {
                PaddingTop = Rem(0.5f),
                PaddingBottom = Rem(0.5f),
                PaddingLeft = Rem(1f),
                FontSize = Rem(1.25f),
                BorderRadius = t.BS.BorderRadiusLg
            },

            [".form-check"] = new()
            {
                Display = Display.Block,
                MinHeight = Rem(1.5f),
                PaddingLeft = Em(1.5f),
                MarginBottom = Rem(0.125f),

                [".form-check-input"] = new()
                {
                    // 不应该再支持float样式，可以用其它布局方式来替代
                    // Float = Float.Left,
                    // MarginLeft = Em(-1.5f),
                }
            },

            [".form-check-reverse"] = new()
            {
                MarginRight = Em(1.5f),
                PaddingLeft = Length.Px(0),
                TextAlign = TextAlign.Right,
                [".form-check-input"] = new()
                {
                    // Float = Float.Right,
                    // MarginRight = Em(-1.5f),
                    // MarginLeft = 0,
                }
            },

            [".form-check-input"] = new()
            {
                FlexShrink = 0,
                Width = Em(1f),
                Height = Em(1f),
                MarginTop = Em(0.25f),
                VerticalAlign = VerticalAlign.Top,
                BackgroundColor = t.FormCheckBg,
                BackgroundImage = t.FormCheckBgImage,
                BackgroundRepeat = BackgroundRepeat.NoRepeat,
                BackgroundPosition = BackgroundPosition.Center,
                BackgroundSize = BackgroundSize.Contain,
                Border = new Border(Px(t.BS.BorderWidth), BorderStyle.Solid, t.BS.BorderColor),

                ["&[type=checkbox]"] = new()
                {
                    BorderRadius = Em(0.25f),
                },

                ["&[type=radio]"] = new()
                {
                    BorderRadius = Percent(50),
                },

                // ["&:active"] = new()
                // {
                //     // filter: brightness(90%);
                // },

                ["&:focus"] = new()
                {
                    BorderColor = t.InputFocusBorderColor,
                    // Outline = 0,
                    BoxShadow = new List<BoxShadow> { new BoxShadow(0, 0, 0, Rem(0.25f), Rgba(13, 110, 253, 0.25f)) }
                },

                ["&:checked"] = new()
                {
                    BackgroundColor = (Color)"#0d6efd",
                    BorderColor = (Color)"#0d6efd"
                },

                ["&:disabled"] = new()
                {
                    Opacity = 0.5f
                }
            },

            [".form-check-label"] = new()
            {
                Color = t.InputColor
            },

            [".form-range"] = new()
            {
                Width = Percent(100),
                Height = Rem(1.5f),
                Padding = new Padding(0),
                BackgroundColor = Transparent,
                // ["&:focus"] = new()
                // {
                //     Outline = 0,
                // },
                //
                // ["&:disabled"] = new()
                // {
                // }

                ["&::range-thumb"] = new()
                {
                    Width = Rem(1f),
                    Height = Rem(1f),
                    // MarginTop =
                    BackgroundColor = (Color)"#0d6efd",
                    Border = new Border(0),
                    BorderRadius = Rem(1f),
                    Transitions = new List<Transition> {
                        Transition.For(x => x.BackgroundColor).Duration(0.15f).EaseInOut(),
                        Transition.For(x => x.BorderColor).Duration(0.15f).EaseInOut(),
                        Transition.For(x => x.BoxShadow).Duration(0.15f).EaseInOut(),
                    }
                },

                ["&::range-track"] = new()
                {
                    Width = Percent(100),
                    Height = Rem(0.5f),
                    Color = Transparent,
                    BackgroundColor = t.BS.SecondaryBg,
                    BorderColor = Transparent,
                    BorderRadius = Rem(1f)
                },

                ["&::range-progress"] = new()
                {
                    BackgroundColor = Transparent,
                },
            },

            [".input-group"] = new()
            {
                Position = Position.Relative,
                Display = Display.Flex,
                FlexWrap = FlexWrap.Wrap,
                AlignItems = AlignItems.Stretch,
                Width = Percent(100),

                // ["> .form-control,> .form-select,> .form-floating"] = new()
                // {
                //     Position = Position.Relative,
                //     FlexGrow = 1,
                //     FlexShrink = 1,
                //     FlexBasis = 0,
                //     Width = Percent(1),
                //     MinWidth = Px(0)
                // },

                ["> .form-control:focus,> .form-select:focus"] = new()
                {
                    ZIndex = 5,
                },

                [".btn"] = new()
                {
                    Position = Position.Relative,
                    ZIndex = 2,
                    ["&:focus"] = new()
                    {
                        ZIndex = 5,
                    }
                },

                ["&:not(.has-validation)"] = new()
                {
                    ["> :not(:last-child):not(.dropdown-toggle):not(.dropdown-menu):not(.form-floating)"] = new()
                    {
                        BorderTopRightRadius = Length.Px(0),
                        BorderBottomRightRadius = Length.Px(0),
                    }
                },

                ["&.has-validation"] = new()
                {
                    ["> :not(.dropdown-toggle):not(.dropdown-menu):not(.form-floating)"] = new()
                    {
                        BorderTopRightRadius = Length.Px(0),
                        BorderBottomRightRadius = Length.Px(0),
                    }
                },

                ["> :not(:first-child):not(.dropdown-menu):not(.valid-tooltip):not(.valid-feedback):not(.invalid-tooltip):not(.invalid-feedback)"] = new()
                {
                    MarginLeft = Length.Px(-1 * t.BS.BorderWidth),
                    BorderTopLeftRadius = Length.Px(0),
                    BorderBottomLeftRadius = Length.Px(0),
                },

                ["> .form-floating:not(:first-child) > .form-control,> .form-floating:not(:first-child) > .form-select"] = new()
                {
                    BorderTopLeftRadius = Length.Px(0),
                    BorderBottomLeftRadius = Length.Px(0),
                }
            },

            [".input-group-text"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Padding = new Padding(Rem(0.375f), Rem(0.375f)),
                FontSize = Rem(1),
                FontWeight = FontWeight.Normal,
                LineHeight = Number(1.5f),
                Color = t.BS.BodyColor,
                TextAlign = TextAlign.Center,
                WhiteSpace = WhiteSpace.Nowrap,
                BackgroundColor = t.BS.TertiaryBg,
                Border = new Border(Px(t.BS.BorderWidth), BorderStyle.Solid, t.BS.BorderColor),
                BorderRadius = t.BS.BorderRadius
            },

            [".input-group-lg > .form-control,.input-group-lg > .form-select,.input-group-lg > .input-group-text,.input-group-lg > .btn"] = new()
            {
                Padding = new Padding(Rem(0.25f), Rem(0.5f)),
                FontSize = Rem(1.25f),
                BorderRadius = t.BS.BorderRadiusLg,
            },
            
            [".input-group-sm > .form-control,.input-group-sm > .form-select,.input-group-sm > .input-group-text,.input-group-sm > .btn"] = new()
            {
                Padding = new Padding(Rem(0.25f), Rem(0.5f)),
                FontSize = Rem(0.875f),
                BorderRadius = t.BS.BorderRadiusLg,
            },

            [".input-group-lg > .form-select,.input-group-sm > .form-select"] = new()
            {
                PaddingRight = Rem(3),
            },

            [".form-control.is-valid"] = new()
            {
                BorderColor = t.FormValidBorderColor
            },

            [".form-control.is-invalid"] = new()
            {
                BorderColor = t.FormInvalidBorderColor
            },

            [".valid-feedback"] = new()
            {
                Display = Display.None,
                Width = Percent(100),
                MarginTop = Rem(0.25f),
                FontSize = Rem(0.875f),
                Color = t.FormValidColor
            },

            [".invalid-feedback"] = new()
            {
                Display = Display.None,
                Width = Percent(100),
                MarginTop = Rem(0.25f),
                FontSize = Rem(0.875f),
                Color = t.FormInvalidColor
            },

            [".is-valid ~ .valid-feedback"] = new()
            {
                Display = Display.Block
            },

            [".is-invalid ~ .invalid-feedback"] = new()
            {
                Display = Display.Block
            }
        };
    }
}
