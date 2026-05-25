using Miko.Core;
using Miko.Layout;
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
        if (_rootElement == null) return;

        if (_rootElement.Parent == null)
        {
            var newElement = BuildNew();
            ReplaceElementContent(_rootElement, newElement);
            return;
        }

        var parent = _rootElement.Parent;
        var index = parent.Children.IndexOf(_rootElement);
        if (index < 0) return;

        var oldElement = _rootElement;
        var rebuilt = Build();
        TransferLayoutBox(oldElement, rebuilt);
        parent.Children[index] = rebuilt;
        rebuilt.SetParent(parent);
        _rootElement = rebuilt;
    }

    private Element BuildNew()
    {
        var builder = new RenderTreeBuilder();
        BuildRenderTree(builder);
        return builder.Build();
    }

    private static void ReplaceElementContent(Element target, Element source)
    {
        var oldChildren = new List<Element>(target.Children);
        target.Children.Clear();
        for (int i = 0; i < source.Children.Count; i++)
        {
            var newChild = source.Children[i];
            target.Children.Add(newChild);
            newChild.SetParent(target);

            if (i < oldChildren.Count)
                TransferLayoutBox(oldChildren[i], newChild);
        }
        target.Style = source.Style;
        target.TextContent = source.TextContent;
        target.Id = source.Id;
        target.Class = source.Class;
        target.IsDirty = true;
    }

    private static void TransferLayoutBox(Element oldElement, Element newElement)
    {
        if (oldElement.LayoutBox != null)
        {
            newElement.LayoutBox = new LayoutBox
            {
                Element = newElement,
                ComputedStyle = oldElement.LayoutBox.ComputedStyle,
                Children = oldElement.LayoutBox.Children
            };
        }

        int count = Math.Min(oldElement.Children.Count, newElement.Children.Count);
        for (int i = 0; i < count; i++)
            TransferLayoutBox(oldElement.Children[i], newElement.Children[i]);
    }
}
