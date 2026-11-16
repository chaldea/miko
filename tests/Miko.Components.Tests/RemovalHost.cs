using Miko.Components;
using Miko.Core.DomElements;

namespace Miko.Components.Tests;

/// <summary>Exercises the framework's removal lifecycle instead of calling IDisposable directly.</summary>
internal sealed class RemovalHost(ComponentBase child) : ComponentBase
{
    private bool _visible = true;
    public void Remove() { _visible = false; StateHasChanged(); }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement<DivElement>();
        if (_visible) builder.AddElement(1, child.Build());
        builder.CloseElement();
    }
}
