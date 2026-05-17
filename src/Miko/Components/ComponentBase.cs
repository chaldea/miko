using Miko.Core;
using Miko.Routing;

namespace Miko.Components;

public abstract class ComponentBase : IComponent
{
    public NavigationManager? NavigationManager { get; internal set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected virtual void BuildRenderTree(RenderTreeBuilder builder) { }

    public virtual Element Build()
    {
        var builder = new RenderTreeBuilder();
        BuildRenderTree(builder);
        return builder.Build();
    }
}
