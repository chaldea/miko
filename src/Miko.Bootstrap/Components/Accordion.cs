using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;

namespace Miko.Bootstrap.Components;

public class Accordion : ComponentBase
{
    public override Element Build()
    {
        var container = new DivElement { Class = "accordion" };
        if (ChildContent != null)
        {
            var builder = new RenderTreeBuilder();
            ChildContent(builder);
            var content = builder.Build();
            foreach (var child in content.Children.ToList())
            {
                content.RemoveChild(child);
                container.AddChild(child);
            }
        }
        return container;
    }
}
