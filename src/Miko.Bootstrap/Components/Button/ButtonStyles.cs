using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class ButtonToken
{
    public ButtonToken(Theme theme)
    {
        BtnPaddingX = Length.Rem(0.75f);
        BtnPaddingY = Length.Rem(0.375f);
        BtnFontSize = Length.Rem(1);
        BtnFontWeight = FontWeight.Normal;
        BtnLineHeight = 1.5f;
        BtnColor = theme.BodyColor;
        BtnBg = Transparent;
        BtnBorderWidth = theme.BorderWidth;
        BtnBorderRadius = theme.BorderRadius;
        BtnDisabledOpacity = 0.65f;
        BtnFocusWidth = Length.Rem(0.25f);
        BtnTransition = [
            Transition.For(x => x.Color).Duration(0.15f).EaseInOut(),
            Transition.For(x => x.BackgroundColor).Duration(0.15f).EaseInOut(),
            Transition.For(x => x.BorderColor).Duration(0.15f).EaseInOut(),
            Transition.For(x => x.BoxShadow).Duration(0.15f).EaseInOut()
        ];

        // Primary variant
        BtnPrimaryColor = Color.White;
        BtnPrimaryBg = theme.Primary;
        BtnPrimaryBorderColor = theme.Primary;
        BtnPrimaryHoverBg = Color.FromRgb(11, 94, 215); // Darker primary
        BtnPrimaryHoverBorderColor = Color.FromRgb(10, 88, 202);

        // Secondary variant
        BtnSecondaryColor = Color.White;
        BtnSecondaryBg = theme.Secondary;
        BtnSecondaryBorderColor = theme.Secondary;
        BtnSecondaryHoverBg = Color.FromRgb(86, 94, 100); // Darker secondary
        BtnSecondaryHoverBorderColor = Color.FromRgb(82, 88, 93);

        // Success variant
        BtnSuccessColor = Color.White;
        BtnSuccessBg = theme.Success;
        BtnSuccessBorderColor = theme.Success;
        BtnSuccessHoverBg = Color.FromRgb(20, 108, 67); // Darker success
        BtnSuccessHoverBorderColor = Color.FromRgb(19, 102, 63);

        // Danger variant
        BtnDangerColor = Color.White;
        BtnDangerBg = theme.Danger;
        BtnDangerBorderColor = theme.Danger;
        BtnDangerHoverBg = Color.FromRgb(187, 45, 59); // Darker danger
        BtnDangerHoverBorderColor = Color.FromRgb(176, 42, 55);

        // Warning variant
        BtnWarningColor = Color.Black;
        BtnWarningBg = theme.Warning;
        BtnWarningBorderColor = theme.Warning;
        BtnWarningHoverBg = Color.FromRgb(217, 164, 6); // Darker warning
        BtnWarningHoverBorderColor = Color.FromRgb(204, 154, 6);

        // Info variant
        BtnInfoColor = Color.Black;
        BtnInfoBg = theme.Info;
        BtnInfoBorderColor = theme.Info;
        BtnInfoHoverBg = Color.FromRgb(11, 172, 204); // Darker info
        BtnInfoHoverBorderColor = Color.FromRgb(10, 162, 192);

        // Light variant
        BtnLightColor = Color.Black;
        BtnLightBg = theme.Light;
        BtnLightBorderColor = theme.Light;
        BtnLightHoverBg = Color.FromRgb(211, 212, 213); // Darker light
        BtnLightHoverBorderColor = Color.FromRgb(198, 200, 202);

        // Dark variant
        BtnDarkColor = Color.White;
        BtnDarkBg = theme.Dark;
        BtnDarkBorderColor = theme.Dark;
        BtnDarkHoverBg = Color.FromRgb(28, 30, 33); // Darker dark
        BtnDarkHoverBorderColor = Color.FromRgb(26, 28, 30);
    }

    public Length BtnPaddingX { get; set; }
    public Length BtnPaddingY { get; set; }
    public Length BtnFontSize { get; set; }
    public FontWeight BtnFontWeight { get; set; }
    public float BtnLineHeight { get; set; }
    public Color BtnColor { get; set; }
    public Color BtnBg { get; set; }
    public Length BtnBorderWidth { get; set; }
    public Length BtnBorderRadius { get; set; }
    public float BtnDisabledOpacity { get; set; }
    public Length BtnFocusWidth { get; set; }
    public List<Transition> BtnTransition { get; set; }

    // Primary
    public Color BtnPrimaryColor { get; set; }
    public Color BtnPrimaryBg { get; set; }
    public Color BtnPrimaryBorderColor { get; set; }
    public Color BtnPrimaryHoverBg { get; set; }
    public Color BtnPrimaryHoverBorderColor { get; set; }

    // Secondary
    public Color BtnSecondaryColor { get; set; }
    public Color BtnSecondaryBg { get; set; }
    public Color BtnSecondaryBorderColor { get; set; }
    public Color BtnSecondaryHoverBg { get; set; }
    public Color BtnSecondaryHoverBorderColor { get; set; }

    // Success
    public Color BtnSuccessColor { get; set; }
    public Color BtnSuccessBg { get; set; }
    public Color BtnSuccessBorderColor { get; set; }
    public Color BtnSuccessHoverBg { get; set; }
    public Color BtnSuccessHoverBorderColor { get; set; }

    // Danger
    public Color BtnDangerColor { get; set; }
    public Color BtnDangerBg { get; set; }
    public Color BtnDangerBorderColor { get; set; }
    public Color BtnDangerHoverBg { get; set; }
    public Color BtnDangerHoverBorderColor { get; set; }

    // Warning
    public Color BtnWarningColor { get; set; }
    public Color BtnWarningBg { get; set; }
    public Color BtnWarningBorderColor { get; set; }
    public Color BtnWarningHoverBg { get; set; }
    public Color BtnWarningHoverBorderColor { get; set; }

    // Info
    public Color BtnInfoColor { get; set; }
    public Color BtnInfoBg { get; set; }
    public Color BtnInfoBorderColor { get; set; }
    public Color BtnInfoHoverBg { get; set; }
    public Color BtnInfoHoverBorderColor { get; set; }

    // Light
    public Color BtnLightColor { get; set; }
    public Color BtnLightBg { get; set; }
    public Color BtnLightBorderColor { get; set; }
    public Color BtnLightHoverBg { get; set; }
    public Color BtnLightHoverBorderColor { get; set; }

    // Dark
    public Color BtnDarkColor { get; set; }
    public Color BtnDarkBg { get; set; }
    public Color BtnDarkBorderColor { get; set; }
    public Color BtnDarkHoverBg { get; set; }
    public Color BtnDarkHoverBorderColor { get; set; }
}

internal static class ButtonStyles
{
    internal static CssObject GenStyle(ButtonToken t)
    {
        return new CssObject
        {
            [".btn"] = new()
            {
                Display = Display.InlineBlock,
                Padding = new Padding(t.BtnPaddingY, t.BtnPaddingX),
                FontSize = t.BtnFontSize,
                FontWeight = t.BtnFontWeight,
                LineHeight = Number(t.BtnLineHeight),
                Color = t.BtnColor,
                TextAlign = TextAlign.Center,
                TextDecoration = TextDecoration.None,
                WhiteSpace = WhiteSpace.Nowrap,
                VerticalAlign = VerticalAlign.Middle,
                Border = new Border(t.BtnBorderWidth, BorderStyle.Solid, Color.Transparent),
                BorderRadius = t.BtnBorderRadius,
                BackgroundColor = t.BtnBg,
                Transitions = t.BtnTransition
            },

            [".btn:disabled"] = new()
            {
                Opacity = t.BtnDisabledOpacity
                // NOTE: pointer-events not supported
            },

            // Primary variant
            [".btn-primary"] = new()
            {
                Color = t.BtnPrimaryColor,
                BackgroundColor = t.BtnPrimaryBg,
                BorderColor = t.BtnPrimaryBorderColor,

                ["&:hover"] = new()
                {
                    BackgroundColor = t.BtnPrimaryHoverBg,
                    BorderColor = t.BtnPrimaryHoverBorderColor
                }
            },

            // Secondary variant
            [".btn-secondary"] = new()
            {
                Color = t.BtnSecondaryColor,
                BackgroundColor = t.BtnSecondaryBg,
                BorderColor = t.BtnSecondaryBorderColor,

                ["&:hover"] = new()
                {
                    BackgroundColor = t.BtnSecondaryHoverBg,
                    BorderColor = t.BtnSecondaryHoverBorderColor
                }
            },

            // Success variant
            [".btn-success"] = new()
            {
                Color = t.BtnSuccessColor,
                BackgroundColor = t.BtnSuccessBg,
                BorderColor = t.BtnSuccessBorderColor,

                ["&:hover"] = new()
                {
                    BackgroundColor = t.BtnSuccessHoverBg,
                    BorderColor = t.BtnSuccessHoverBorderColor
                }
            },

            // Danger variant
            [".btn-danger"] = new()
            {
                Color = t.BtnDangerColor,
                BackgroundColor = t.BtnDangerBg,
                BorderColor = t.BtnDangerBorderColor,

                ["&:hover"] = new()
                {
                    BackgroundColor = t.BtnDangerHoverBg,
                    BorderColor = t.BtnDangerHoverBorderColor
                }
            },

            // Warning variant
            [".btn-warning"] = new()
            {
                Color = t.BtnWarningColor,
                BackgroundColor = t.BtnWarningBg,
                BorderColor = t.BtnWarningBorderColor,

                ["&:hover"] = new()
                {
                    BackgroundColor = t.BtnWarningHoverBg,
                    BorderColor = t.BtnWarningHoverBorderColor
                }
            },

            // Info variant
            [".btn-info"] = new()
            {
                Color = t.BtnInfoColor,
                BackgroundColor = t.BtnInfoBg,
                BorderColor = t.BtnInfoBorderColor,

                ["&:hover"] = new()
                {
                    BackgroundColor = t.BtnInfoHoverBg,
                    BorderColor = t.BtnInfoHoverBorderColor
                }
            },

            // Light variant
            [".btn-light"] = new()
            {
                Color = t.BtnLightColor,
                BackgroundColor = t.BtnLightBg,
                BorderColor = t.BtnLightBorderColor,

                ["&:hover"] = new()
                {
                    BackgroundColor = t.BtnLightHoverBg,
                    BorderColor = t.BtnLightHoverBorderColor
                }
            },

            // Dark variant
            [".btn-dark"] = new()
            {
                Color = t.BtnDarkColor,
                BackgroundColor = t.BtnDarkBg,
                BorderColor = t.BtnDarkBorderColor,

                ["&:hover"] = new()
                {
                    BackgroundColor = t.BtnDarkHoverBg,
                    BorderColor = t.BtnDarkHoverBorderColor
                }
            }
        };
    }
}

