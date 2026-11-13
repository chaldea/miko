using Miko.Components;

namespace Miko.Docs.Demo;

/// <summary>
/// Hosts a catalog component and applies the values discovered from its public parameters.
/// The descriptor carries the trimming requirements through to the runtime component type.
/// </summary>
public sealed class DynamicDemoComponent : ComponentBase
{
    [Parameter] public DemoDescriptor Descriptor { get; set; } = null!;
    [Parameter] public IReadOnlyDictionary<string, object?> Values { get; set; } = null!;
    [Parameter] public RenderFragment? Start { get; set; }
    [Parameter] public RenderFragment? End { get; set; }
    [Parameter] public RenderFragment? IconOnly { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent(0, Descriptor.ComponentType);
        var sequence = 1;
        foreach (var (name, value) in Values)
        {
            builder.AddComponentParameter(sequence++, name, value);
        }
        if (Start is not null) builder.AddComponentParameter(sequence++, nameof(Start), Start);
        if (End is not null) builder.AddComponentParameter(sequence++, nameof(End), End);
        if (IconOnly is not null) builder.AddComponentParameter(sequence++, nameof(IconOnly), IconOnly);
        if (ChildContent is not null)
            builder.AddComponentParameter(sequence, nameof(ChildContent), ChildContent);
        builder.CloseComponent();
    }
}
