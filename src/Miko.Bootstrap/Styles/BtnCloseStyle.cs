using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class BtnCloseStyle
{
    internal static CssObject GenStyle(Theme t)
    {
        return new CssObject
        {
            [".btn-close"] = new()
            {
                BoxSizing = BoxSizing.ContentBox,
                Width = Length.Rem(1),
                Height = Length.Rem(1),
                Color = Color.FromHex("#000"),
                BackgroundColor = Color.Transparent,
                BackgroundImage = BackgroundImage.FromResource(typeof(BtnCloseStyle).Assembly, "Miko.Bootstrap.Resources.Images.BtnClose.svg"),
                BackgroundPosition = BackgroundPosition.Center,
                BackgroundSize = BackgroundSize.Auto,
                BackgroundRepeat = BackgroundRepeat.NoRepeat,
                Border = Border.None,
                BorderRadius = Length.Rem(0.375f),
                Opacity = 0.5f,
            }
        };
    }
}