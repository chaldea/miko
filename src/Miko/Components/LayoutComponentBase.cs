using Miko.Core;
using Miko.Routing;

namespace Miko.Components;

public abstract class LayoutComponentBase
{
    public NavigationManager? NavigationManager { get; internal set; }

    public Element? Body { get; internal set; }

    public abstract Element Build();
}
