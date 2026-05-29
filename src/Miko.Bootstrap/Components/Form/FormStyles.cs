using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class FormToken
{
    public FormToken(Theme theme)
    {
        FormLabelMarginBottom = Length.Rem(0.5f);
        FormLabelColor = theme.BodyColor;

        FormTextMarginTop = Length.Rem(0.25f);
        FormTextFontSize = Length.Rem(0.875f);
        FormTextColor = theme.SecondaryColor;

        InputPaddingY = Length.Rem(0.375f);
        InputPaddingX = Length.Rem(0.75f);
        InputFontSize = Length.Rem(1f);
        InputLineHeight = 1.5f;
        InputColor = theme.BodyColor;
        InputBg = theme.BodyBg;
        InputBorderWidth = theme.BorderWidth;
        InputBorderColor = theme.BorderColor;
        InputBorderRadius = theme.BorderRadius;
        InputFocusBorderColor = theme.Primary;
        InputDisabledBg = theme.SecondaryBg;

        InputPaddingYSm = Length.Rem(0.25f);
        InputPaddingXSm = Length.Rem(0.5f);
        InputFontSizeSm = Length.Rem(0.875f);
        InputBorderRadiusSm = theme.BorderRadiusSm;

        InputPaddingYLg = Length.Rem(0.5f);
        InputPaddingXLg = Length.Rem(1f);
        InputFontSizeLg = Length.Rem(1.25f);
        InputBorderRadiusLg = theme.BorderRadiusLg;

        FormCheckPaddingStart = Length.Rem(1.5f);
        FormCheckInputWidth = Length.Rem(1f);
        FormCheckInputBorderColor = theme.BorderColor;
        FormCheckInputBorderRadius = theme.BorderRadiusSm;
        FormCheckInputCheckedBg = theme.Primary;
        FormCheckInputCheckedBorderColor = theme.Primary;

        InputGroupBorderColor = theme.BorderColor;
        InputGroupTextBg = theme.TertiaryBg;
        InputGroupTextColor = theme.BodyColor;

        FormValidColor = theme.FormValidColor;
        FormValidBorderColor = theme.FormValidBorderColor;
        FormInvalidColor = theme.FormInvalidColor;
        FormInvalidBorderColor = theme.FormInvalidBorderColor;
    }

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

            [".form-text"] = new()
            {
                MarginTop = t.FormTextMarginTop,
                FontSize = t.FormTextFontSize,
                Color = t.FormTextColor
            },

            [".form-control"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                Padding = new Padding(t.InputPaddingY, t.InputPaddingX),
                FontSize = t.InputFontSize,
                LineHeight = Length.Px(t.InputLineHeight),
                Color = t.InputColor,
                BackgroundColor = t.InputBg,
                Border = new Border(Length.Px(t.InputBorderWidth), BorderStyle.Solid, t.InputBorderColor),
                BorderRadius = t.InputBorderRadius,
                // NOTE: appearance: none not supported in CssObject
                // NOTE: transition not added (border-color .15s ease-in-out, box-shadow .15s ease-in-out)

                ["&:focus"] = new()
                {
                    BorderColor = t.InputFocusBorderColor,
                    // NOTE: box-shadow: 0 0 0 .25rem rgba(primary, .25) not supported
                },

                ["&:disabled"] = new()
                {
                    BackgroundColor = t.InputDisabledBg,
                    Opacity = 1f
                }
            },

            [".form-control-sm"] = new()
            {
                Padding = new Padding(t.InputPaddingYSm, t.InputPaddingXSm),
                FontSize = t.InputFontSizeSm,
                BorderRadius = t.InputBorderRadiusSm
            },

            [".form-control-lg"] = new()
            {
                Padding = new Padding(t.InputPaddingYLg, t.InputPaddingXLg),
                FontSize = t.InputFontSizeLg,
                BorderRadius = t.InputBorderRadiusLg
            },

            [".form-select"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(100),
                Padding = new Padding(t.InputPaddingY, Length.Rem(2.25f), t.InputPaddingY, t.InputPaddingX),
                FontSize = t.InputFontSize,
                LineHeight = Length.Px(t.InputLineHeight),
                Color = t.InputColor,
                BackgroundColor = t.InputBg,
                Border = new Border(Length.Px(t.InputBorderWidth), BorderStyle.Solid, t.InputBorderColor),
                BorderRadius = t.InputBorderRadius,
                // NOTE: appearance: none, background-image (chevron SVG) not supported

                ["&:focus"] = new()
                {
                    BorderColor = t.InputFocusBorderColor
                },

                ["&:disabled"] = new()
                {
                    BackgroundColor = t.InputDisabledBg
                }
            },

            [".form-select-sm"] = new()
            {
                Padding = new Padding(t.InputPaddingYSm, Length.Rem(1.75f), t.InputPaddingYSm, t.InputPaddingXSm),
                FontSize = t.InputFontSizeSm,
                BorderRadius = t.InputBorderRadiusSm
            },

            [".form-select-lg"] = new()
            {
                Padding = new Padding(t.InputPaddingYLg, Length.Rem(2.75f), t.InputPaddingYLg, t.InputPaddingXLg),
                FontSize = t.InputFontSizeLg,
                BorderRadius = t.InputBorderRadiusLg
            },

            [".form-check"] = new()
            {
                Display = Display.Block,
                MinHeight = Length.Rem(1.5f),
                PaddingLeft = t.FormCheckPaddingStart,
                MarginBottom = Length.Rem(0.125f)
            },

            [".form-check-input"] = new()
            {
                Width = t.FormCheckInputWidth,
                Height = t.FormCheckInputWidth,
                MarginTop = Length.Rem(0.25f),
                MarginRight = Length.Rem(0.5f),
                BackgroundColor = t.InputBg,
                Border = new Border(Length.Px(t.InputBorderWidth), BorderStyle.Solid, t.FormCheckInputBorderColor),
                BorderRadius = t.FormCheckInputBorderRadius,
                // NOTE: appearance: none, float: left not supported

                ["&:checked"] = new()
                {
                    BackgroundColor = t.FormCheckInputCheckedBg,
                    BorderColor = t.FormCheckInputCheckedBorderColor
                },

                ["&:focus"] = new()
                {
                    BorderColor = t.InputFocusBorderColor
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
                Width = Length.Percent(100),
                Height = Length.Rem(1.5f),
                Padding = new Padding(0),
                BackgroundColor = Color.Transparent
                // NOTE: appearance: none, custom thumb/track pseudo-elements not supported
            },

            [".input-group"] = new()
            {
                Position = Position.Relative,
                Display = Display.Flex,
                FlexWrap = FlexWrap.Wrap,
                AlignItems = AlignItems.Stretch,
                Width = Length.Percent(100),

                ["> .form-control"] = new()
                {
                    Position = Position.Relative,
                    FlexGrow = 1,
                    FlexBasis = Length.Px(0),
                    MinWidth = Length.Px(0)
                },

                ["> .form-select"] = new()
                {
                    Position = Position.Relative,
                    FlexGrow = 1,
                    FlexBasis = Length.Px(0),
                    MinWidth = Length.Px(0)
                }
            },

            [".input-group-text"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Padding = new Padding(t.InputPaddingY, t.InputPaddingX),
                FontSize = t.InputFontSize,
                Color = t.InputGroupTextColor,
                BackgroundColor = t.InputGroupTextBg,
                Border = new Border(Length.Px(t.InputBorderWidth), BorderStyle.Solid, t.InputGroupBorderColor),
                BorderRadius = t.InputBorderRadius
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
                Width = Length.Percent(100),
                MarginTop = Length.Rem(0.25f),
                FontSize = Length.Rem(0.875f),
                Color = t.FormValidColor
            },

            [".invalid-feedback"] = new()
            {
                Display = Display.None,
                Width = Length.Percent(100),
                MarginTop = Length.Rem(0.25f),
                FontSize = Length.Rem(0.875f),
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
