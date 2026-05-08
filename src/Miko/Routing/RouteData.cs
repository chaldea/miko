namespace Miko.Routing;

public class RouteData
{
    public required string Template { get; init; }
    public required Type ComponentType { get; init; }
}
