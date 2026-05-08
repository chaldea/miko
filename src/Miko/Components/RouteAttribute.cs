namespace Miko.Components;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RouteAttribute : Attribute
{
    public string Template { get; }

    public RouteAttribute(string template)
    {
        Template = template;
    }
}
