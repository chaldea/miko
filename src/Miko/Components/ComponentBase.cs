using Miko.Core;
using Miko.Routing;

namespace Miko.Components;

public abstract class ComponentBase : IComponent
{
    public NavigationManager? NavigationManager { get; internal set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private Element? _rootElement;

    protected virtual void BuildRenderTree(RenderTreeBuilder builder) { }

    public virtual Element Build()
    {
        var builder = new RenderTreeBuilder();
        BuildRenderTree(builder);
        _rootElement = builder.Build();
        return _rootElement;
    }

    protected void StateHasChanged()
    {
        if (_rootElement?.Parent == null) return;

        var parent = _rootElement.Parent;
        var index = parent.Children.IndexOf(_rootElement);
        if (index < 0) return;

        var newElement = Build();
        parent.Children[index] = newElement;
        newElement.SetParent(parent);
        _rootElement = newElement;
    }
}
