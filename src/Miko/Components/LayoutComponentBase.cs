using Miko.Core;
using Miko.Routing;

namespace Miko.Components;

public abstract class LayoutComponentBase : ComponentBase
{
    public new NavigationManager? NavigationManager
    {
        get => base.NavigationManager;
        internal set => base.NavigationManager = value;
    }

    public RenderFragment? Body { get; internal set; }

    public Element? BodyElement { get; internal set; }
}
