using System.Diagnostics.CodeAnalysis;

namespace Miko.Routing;

public class RouteData
{
    public required string Template { get; init; }

    [DynamicallyAccessedMembers(Components.ComponentTypeMembers.Activation)]
    public required Type ComponentType { get; init; }
}
