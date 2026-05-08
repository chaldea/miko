using Miko.Core;
using Miko.Routing;

namespace Miko.Components;

public abstract class ComponentBase
{
    public NavigationManager? NavigationManager { get; internal set; }

    public abstract Element Build();
}
