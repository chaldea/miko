namespace Miko.Components;

[AttributeUsage(AttributeTargets.Class)]
public class LayoutAttribute(Type layoutType) : Attribute
{
    public Type LayoutType { get; } = layoutType;
}
