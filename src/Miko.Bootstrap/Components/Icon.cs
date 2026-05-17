using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

public class Icon : ComponentBase
{
    [Parameter] public string Name { get; set; } = "";
    [Parameter] public IconSize Size { get; set; } = IconSize.Default;

    public override Element Build()
    {
        var span = new SpanElement
        {
            Class = "bi",
            TextContent = Name
        };

        if (Size != IconSize.Default)
        {
            var fontSize = Size switch
            {
                IconSize.x2 => 32f,
                IconSize.x3 => 48f,
                IconSize.x4 => 64f,
                IconSize.x5 => 80f,
                _ => 16f
            };
            span.Style = new Style { FontSize = Length.Px(fontSize) };
        }

        return span;
    }
}
