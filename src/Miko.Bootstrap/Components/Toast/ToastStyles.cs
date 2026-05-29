using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class ToastToken
{
    public ToastToken(Theme theme)
    {
        ToastMaxWidth = Length.Px(350);
        ToastPaddingX = Length.Rem(0.75f);
        ToastPaddingY = Length.Rem(0.5f);
        ToastFontSize = Length.Rem(0.875f);
        ToastBg = Color.FromRgba(255, 255, 255, 217);
        ToastBorderWidth = theme.BorderWidth;
        ToastBorderColor = theme.BorderColorTranslucent;
        ToastBorderRadius = theme.BorderRadius;
        ToastHeaderBg = Color.FromRgba(255, 255, 255, 217);
        ToastHeaderBorderColor = theme.BorderColorTranslucent;
    }

    public Length ToastMaxWidth { get; set; }
    public Length ToastPaddingX { get; set; }
    public Length ToastPaddingY { get; set; }
    public Length ToastFontSize { get; set; }
    public Color ToastBg { get; set; }
    public float ToastBorderWidth { get; set; }
    public Color ToastBorderColor { get; set; }
    public float ToastBorderRadius { get; set; }
    public Color ToastHeaderBg { get; set; }
    public Color ToastHeaderBorderColor { get; set; }
}

internal static class ToastStyles
{
    internal static CssObject GenStyle(ToastToken t)
    {
        return new CssObject
        {
            [".toast"] = new()
            {
                Width = t.ToastMaxWidth,
                MaxWidth = Length.Percent(100),
                FontSize = t.ToastFontSize,
                BackgroundColor = t.ToastBg,
                Border = new Border(Length.Px(t.ToastBorderWidth), BorderStyle.Solid, t.ToastBorderColor),
                BorderRadius = t.ToastBorderRadius
            },

            [".toast:not(.show)"] = new()
            {
                Display = Display.None
            },

            [".toast-header"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Padding = new Padding(t.ToastPaddingY, t.ToastPaddingX),
                BackgroundColor = t.ToastHeaderBg,
                BorderBottom = new BorderSide(Length.Px(t.ToastBorderWidth), BorderStyle.Solid, t.ToastHeaderBorderColor)
            },

            [".toast-body"] = new()
            {
                Padding = new Padding(t.ToastPaddingX)
            }
        };
    }
}
