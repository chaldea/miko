using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;

namespace MikoApp1.Components;

public class ButtonExample : MikoComponent
{
    public override Element Build()
    {
        var hoverButtons = new DivElement
        {
            Class = "row",
            Children =
            {
                new ButtonElement { TextContent = "Primary", Class = "btn-primary" },
                new ButtonElement { TextContent = "Secondary", Class = "btn-secondary" },
                new ButtonElement { TextContent = "Success", Class = "btn-success" },
                new ButtonElement { TextContent = "Danger", Class = "btn-danger" },
                new ButtonElement { TextContent = "Warning", Class = "btn-warning" },
                new ButtonElement { TextContent = "Info", Class = "btn-info" },
                new ButtonElement { TextContent = "Light", Class = "btn-light" },
                new ButtonElement { TextContent = "Dark", Class = "btn-dark" },
            }
        };
        foreach (var elm in hoverButtons.Children)
            elm.SetState(ElementState.Hover);

        return new DivElement
        {
            Class = "container",
            Children =
            {
                new H1Element { TextContent = "Bootstrap Button Examples" },

                new H2Element { TextContent = "Standard Buttons" },
                new DivElement
                {
                    Class = "row",
                    Children =
                    {
                        new ButtonElement { TextContent = "Primary", Class = "btn-primary" },
                        new ButtonElement { TextContent = "Secondary", Class = "btn-secondary" },
                        new ButtonElement { TextContent = "Success", Class = "btn-success" },
                        new ButtonElement { TextContent = "Danger", Class = "btn-danger" },
                        new ButtonElement { TextContent = "Warning", Class = "btn-warning" },
                        new ButtonElement { TextContent = "Info", Class = "btn-info" },
                        new ButtonElement { TextContent = "Light", Class = "btn-light" },
                        new ButtonElement { TextContent = "Dark", Class = "btn-dark" },
                    }
                },
                new H2Element { TextContent = "Standard Hover Buttons" },
                hoverButtons,

                new H2Element { TextContent = "Outline Buttons" },
                new DivElement
                {
                    Class = "row",
                    Children =
                    {
                        new ButtonElement { TextContent = "Primary", Class = "btn-outline-primary" },
                        new ButtonElement { TextContent = "Secondary", Class = "btn-outline-secondary" },
                        new ButtonElement { TextContent = "Success", Class = "btn-outline-success" },
                        new ButtonElement { TextContent = "Danger", Class = "btn-outline-danger" },
                        new ButtonElement { TextContent = "Warning", Class = "btn-outline-warning" },
                        new ButtonElement { TextContent = "Info", Class = "btn-outline-info" },
                        new ButtonElement { TextContent = "Dark", Class = "btn-outline-dark" },
                    }
                },

                new H2Element { TextContent = "Button Sizes" },
                new DivElement
                {
                    Class = "row",
                    Children =
                    {
                        new ButtonElement { TextContent = "Small Button", Class = "btn-primary btn-sm" },
                        new ButtonElement { TextContent = "Medium Button", Class = "btn-primary" },
                        new ButtonElement { TextContent = "Large Button", Class = "btn-primary btn-lg" },
                    }
                },

                new H2Element { TextContent = "Mixed Examples" },
                new DivElement
                {
                    Class = "row",
                    Children =
                    {
                        new ButtonElement { TextContent = "Small Success", Class = "btn-success btn-sm" },
                        new ButtonElement { TextContent = "Large Danger", Class = "btn-danger btn-lg" },
                        new ButtonElement { TextContent = "Small Outline", Class = "btn-outline-info btn-sm" },
                    }
                },
            }
        };
    }
}
