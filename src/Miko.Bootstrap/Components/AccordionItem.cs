using Miko.Animation;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class AccordionItem : ComponentBase
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public RenderFragment? Content { get; set; }

    public bool IsExpanded { get; set; }

    public override Element Build()
    {
        var item = new DivElement { Class = "accordion-item" };

        var header = new H2Element { Class = "accordion-header" };
        var button = new ButtonElement
        {
            Class = "accordion-button",
            TextContent = Title ?? "",
            Style = new Style
            {
                BackgroundColor = IsExpanded ? Color.FromHex("cfe2ff") : Color.White,
                Color = IsExpanded ? Color.FromHex("052c65") : Color.FromHex("212529")
            }
        };

        var icon = new SpanElement
        {
            Class = "accordion-icon",
            TextContent = "❯", // ❯ chevron
            Style = new Style
            {
                Transform = IsExpanded
                    ? Transform.FromRotate(90)
                    : Transform.FromRotate(0)
            }
        };

        var collapse = new DivElement
        {
            Class = "accordion-collapse",
            Style = new Style { Display = IsExpanded ? Display.Block : Display.None }
        };

        button.OnClick = _ =>
        {
            IsExpanded = !IsExpanded;
            collapse.Style = new Style { Display = IsExpanded ? Display.Block : Display.None };
            button.Style = new Style
            {
                BackgroundColor = IsExpanded ? Color.FromHex("cfe2ff") : Color.White,
                Color = IsExpanded ? Color.FromHex("052c65") : Color.FromHex("212529")
            };
            icon.Style = new Style
            {
                Transform = IsExpanded
                    ? Transform.FromRotate(90)
                    : Transform.FromRotate(0)
            };
        };

        button.AddChild(icon);
        header.AddChild(button);
        item.AddChild(header);

        var body = new DivElement { Class = "accordion-body" };
        if (Content != null)
        {
            var builder = new RenderTreeBuilder();
            Content(builder);
            body.AddChild(builder.Build());
        }
        else if (ChildContent != null)
        {
            var builder = new RenderTreeBuilder();
            ChildContent(builder);
            body.AddChild(builder.Build());
        }
        collapse.AddChild(body);
        item.AddChild(collapse);

        return item;
    }
}
