using System.Diagnostics.CodeAnalysis;

namespace Miko.Routing;

public class RouteData
{
    public required string Template { get; init; }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    public required Type ComponentType { get; init; }
}
