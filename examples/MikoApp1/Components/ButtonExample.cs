using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;

namespace MikoApp1.Components;

public class ButtonExample : MikoComponent
{
    public override Element Build()
    {
        return new DivElement
        {
            Style = new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Padding = new Padding(Length.Px(20)),
                BackgroundColor = new Color(245, 245, 245)
            },
            Children =
            {
                new H1Element { TextContent = "Miko + Silk.NET Demo" },
                new DivElement
                {
                    Style = new Style
                    {
                        Display = Display.Flex,
                        FlexDirection = FlexDirection.Row,
                        Padding = new Padding(Length.Px(8))
                    },
                    Children =
                    {
                        new ButtonElement
                        {
                            TextContent = "Click Me",
                            Style = new Style
                            {
                                Padding = new Padding(Length.Px(8), Length.Px(16)),
                                BackgroundColor = new Color(0, 120, 215),
                                Color = new Color(255, 255, 255),
                                BorderRadius = new BorderRadius(4)
                            }
                        },
                        new ButtonElement
                        {
                            TextContent = "Another Button",
                            Style = new Style
                            {
                                Padding = new Padding(Length.Px(8), Length.Px(16)),
                                BackgroundColor = new Color(108, 117, 125),
                                Color = new Color(255, 255, 255),
                                BorderRadius = new BorderRadius(4),
                                Margin = new Margin(Length.Auto, Length.Px(8))
                            }
                        }
                    }
                }
            }
        };
    }
}
