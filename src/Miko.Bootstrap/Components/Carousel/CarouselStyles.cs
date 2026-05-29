using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class CarouselToken
{
    public CarouselToken(Theme theme)
    {
        CarouselControlWidth = Length.Percent(15);
        CarouselIndicatorHeight = Length.Px(3);
        CarouselCaptionColor = Color.White;
    }

    public Length CarouselControlWidth { get; set; }
    public Length CarouselIndicatorHeight { get; set; }
    public Color CarouselCaptionColor { get; set; }
}

internal static class CarouselStyles
{
    internal static CssObject GenStyle(CarouselToken t)
    {
        return new CssObject
        {
            [".carousel"] = new()
            {
                Position = Position.Relative
            },

            [".carousel-inner"] = new()
            {
                Position = Position.Relative,
                Width = Length.Percent(100),
                OverflowX = Overflow.Hidden
            },

            [".carousel-item"] = new()
            {
                Position = Position.Relative,
                Display = Display.None,
                Width = Length.Percent(100)
            },

            [".carousel-item.active"] = new()
            {
                Display = Display.Block
            },

            [".carousel-control-prev, .carousel-control-next"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(0),
                Bottom = Length.Px(0),
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = t.CarouselControlWidth,
                Opacity = 0.5f
            },

            [".carousel-indicators"] = new()
            {
                Position = Position.Absolute,
                Right = Length.Px(0),
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                Display = Display.Flex,
                JustifyContent = JustifyContent.Center,
                PaddingLeft = Length.Px(0),
                MarginBottom = Length.Rem(1)
            }
        };
    }
}
