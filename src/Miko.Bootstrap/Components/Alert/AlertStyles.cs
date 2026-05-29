using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class AlertToken
{
    public AlertToken(Theme theme)
    {
        AlertPaddingX = Length.Rem(1);
        AlertPaddingY = Length.Rem(1);
        AlertMarginBottom = Length.Rem(1);
        AlertBorderRadius = theme.BorderRadius;
        AlertBorderWidth = theme.BorderWidth;
        AlertLinkFontWeight = FontWeight.Bold;
        AlertDismissiblePaddingR = Length.Rem(3);

        // Contextual colors
        AlertPrimaryColor = theme.PrimaryTextEmphasis;
        AlertPrimaryBg = theme.PrimaryBgSubtle;
        AlertPrimaryBorderColor = theme.PrimaryBorderSubtle;

        AlertSecondaryColor = theme.SecondaryTextEmphasis;
        AlertSecondaryBg = theme.SecondaryBgSubtle;
        AlertSecondaryBorderColor = theme.SecondaryBorderSubtle;

        AlertSuccessColor = theme.SuccessTextEmphasis;
        AlertSuccessBg = theme.SuccessBgSubtle;
        AlertSuccessBorderColor = theme.SuccessBorderSubtle;

        AlertDangerColor = theme.DangerTextEmphasis;
        AlertDangerBg = theme.DangerBgSubtle;
        AlertDangerBorderColor = theme.DangerBorderSubtle;

        AlertWarningColor = theme.WarningTextEmphasis;
        AlertWarningBg = theme.WarningBgSubtle;
        AlertWarningBorderColor = theme.WarningBorderSubtle;

        AlertInfoColor = theme.InfoTextEmphasis;
        AlertInfoBg = theme.InfoBgSubtle;
        AlertInfoBorderColor = theme.InfoBorderSubtle;

        AlertLightColor = theme.LightTextEmphasis;
        AlertLightBg = theme.LightBgSubtle;
        AlertLightBorderColor = theme.LightBorderSubtle;

        AlertDarkColor = theme.DarkTextEmphasis;
        AlertDarkBg = theme.DarkBgSubtle;
        AlertDarkBorderColor = theme.DarkBorderSubtle;
    }

    public Length AlertPaddingX { get; set; }
    public Length AlertPaddingY { get; set; }
    public Length AlertMarginBottom { get; set; }
    public Length AlertBorderRadius { get; set; }
    public Length AlertBorderWidth { get; set; }
    public FontWeight AlertLinkFontWeight { get; set; }
    public Length AlertDismissiblePaddingR { get; set; }

    // Primary
    public Color AlertPrimaryColor { get; set; }
    public Color AlertPrimaryBg { get; set; }
    public Color AlertPrimaryBorderColor { get; set; }

    // Secondary
    public Color AlertSecondaryColor { get; set; }
    public Color AlertSecondaryBg { get; set; }
    public Color AlertSecondaryBorderColor { get; set; }

    // Success
    public Color AlertSuccessColor { get; set; }
    public Color AlertSuccessBg { get; set; }
    public Color AlertSuccessBorderColor { get; set; }

    // Danger
    public Color AlertDangerColor { get; set; }
    public Color AlertDangerBg { get; set; }
    public Color AlertDangerBorderColor { get; set; }

    // Warning
    public Color AlertWarningColor { get; set; }
    public Color AlertWarningBg { get; set; }
    public Color AlertWarningBorderColor { get; set; }

    // Info
    public Color AlertInfoColor { get; set; }
    public Color AlertInfoBg { get; set; }
    public Color AlertInfoBorderColor { get; set; }

    // Light
    public Color AlertLightColor { get; set; }
    public Color AlertLightBg { get; set; }
    public Color AlertLightBorderColor { get; set; }

    // Dark
    public Color AlertDarkColor { get; set; }
    public Color AlertDarkBg { get; set; }
    public Color AlertDarkBorderColor { get; set; }
}

internal static class AlertStyles
{
    internal static CssObject GenStyle(AlertToken t)
    {
        return new CssObject
        {
            [".alert"] = new()
            {
                Position = Position.Relative,
                Padding = new Padding(t.AlertPaddingY, t.AlertPaddingX),
                MarginBottom = t.AlertMarginBottom,
                Border = new Border(t.AlertBorderWidth, BorderStyle.Solid, Color.Transparent),
                BorderRadius = t.AlertBorderRadius
            },

            [".alert-heading"] = new()
            {
                // NOTE: color: inherit not directly supported in CssObject
            },

            [".alert-link"] = new()
            {
                FontWeight = t.AlertLinkFontWeight,
                // NOTE: Color inheritance handled by contextual classes
            },

            [".alert-dismissible"] = new()
            {
                PaddingRight = t.AlertDismissiblePaddingR,

                [".btn-close"] = new()
                {
                    Position = Position.Absolute,
                    Top = Length.Px(0),
                    Right = Length.Px(0),
                    ZIndex = 2,
                    Padding = new Padding(Length.Rem(1.25f), t.AlertPaddingX)
                }
            },

            // Contextual modifier classes
            [".alert-primary"] = new()
            {
                Color = t.AlertPrimaryColor,
                BackgroundColor = t.AlertPrimaryBg,
                BorderColor = t.AlertPrimaryBorderColor
            },

            [".alert-secondary"] = new()
            {
                Color = t.AlertSecondaryColor,
                BackgroundColor = t.AlertSecondaryBg,
                BorderColor = t.AlertSecondaryBorderColor
            },

            [".alert-success"] = new()
            {
                Color = t.AlertSuccessColor,
                BackgroundColor = t.AlertSuccessBg,
                BorderColor = t.AlertSuccessBorderColor
            },

            [".alert-danger"] = new()
            {
                Color = t.AlertDangerColor,
                BackgroundColor = t.AlertDangerBg,
                BorderColor = t.AlertDangerBorderColor
            },

            [".alert-warning"] = new()
            {
                Color = t.AlertWarningColor,
                BackgroundColor = t.AlertWarningBg,
                BorderColor = t.AlertWarningBorderColor
            },

            [".alert-info"] = new()
            {
                Color = t.AlertInfoColor,
                BackgroundColor = t.AlertInfoBg,
                BorderColor = t.AlertInfoBorderColor
            },

            [".alert-light"] = new()
            {
                Color = t.AlertLightColor,
                BackgroundColor = t.AlertLightBg,
                BorderColor = t.AlertLightBorderColor
            },

            [".alert-dark"] = new()
            {
                Color = t.AlertDarkColor,
                BackgroundColor = t.AlertDarkBg,
                BorderColor = t.AlertDarkBorderColor
            }
        };
    }
}

