using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class BadgeToken
{
    public BadgeToken(Theme theme)
    {
        BadgePaddingX = Length.Rem(0.65f);
        BadgePaddingY = Length.Rem(0.35f);
        BadgeFontSize = Length.Rem(0.75f);
        BadgeFontWeight = FontWeight.Bold;
        BadgeColor = Color.White;
        BadgeBorderRadius = theme.BorderRadius;

        // Contextual colors - using theme colors directly
        BadgePrimaryBg = theme.Primary;
        BadgeSecondaryBg = theme.Secondary;
        BadgeSuccessBg = theme.Success;
        BadgeDangerBg = theme.Danger;
        BadgeWarningBg = theme.Warning;
        BadgeInfoBg = theme.Info;
        BadgeLightBg = theme.Light;
        BadgeDarkBg = theme.Dark;
    }

    public Length BadgePaddingX { get; set; }
    public Length BadgePaddingY { get; set; }
    public Length BadgeFontSize { get; set; }
    public FontWeight BadgeFontWeight { get; set; }
    public Color BadgeColor { get; set; }
    public Length BadgeBorderRadius { get; set; }

    public Color BadgePrimaryBg { get; set; }
    public Color BadgeSecondaryBg { get; set; }
    public Color BadgeSuccessBg { get; set; }
    public Color BadgeDangerBg { get; set; }
    public Color BadgeWarningBg { get; set; }
    public Color BadgeInfoBg { get; set; }
    public Color BadgeLightBg { get; set; }
    public Color BadgeDarkBg { get; set; }
}

internal static class BadgeStyles
{
    internal static CssObject GenStyle(BadgeToken t)
    {
        return new CssObject
        {
            [".badge"] = new()
            {
                Display = Display.InlineBlock,
                Padding = new Padding(t.BadgePaddingY, t.BadgePaddingX),
                FontSize = t.BadgeFontSize,
                FontWeight = t.BadgeFontWeight,
                LineHeight = Length.Px(1),
                Color = t.BadgeColor,
                TextAlign = TextAlign.Center,
                // NOTE: white-space: nowrap not supported in CssObject
                // NOTE: vertical-align: baseline not supported in CssObject
                BorderRadius = t.BadgeBorderRadius,

                ["&:empty"] = new()
                {
                    Display = Display.None
                }
            },

            [".btn .badge"] = new()
            {
                Position = Position.Relative,
                Top = Length.Px(-1)
            },

            // Contextual modifier classes
            [".badge.bg-primary"] = new()
            {
                BackgroundColor = t.BadgePrimaryBg
            },

            [".badge.bg-secondary"] = new()
            {
                BackgroundColor = t.BadgeSecondaryBg
            },

            [".badge.bg-success"] = new()
            {
                BackgroundColor = t.BadgeSuccessBg
            },

            [".badge.bg-danger"] = new()
            {
                BackgroundColor = t.BadgeDangerBg
            },

            [".badge.bg-warning"] = new()
            {
                BackgroundColor = t.BadgeWarningBg
            },

            [".badge.bg-info"] = new()
            {
                BackgroundColor = t.BadgeInfoBg
            },

            [".badge.bg-light"] = new()
            {
                BackgroundColor = t.BadgeLightBg,
                Color = Color.Black  // Light background needs dark text
            },

            [".badge.bg-dark"] = new()
            {
                BackgroundColor = t.BadgeDarkBg
            }
        };
    }
}

