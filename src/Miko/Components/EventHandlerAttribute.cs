namespace Miko.Components;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class EventHandlerAttribute(
    string attributeName, Type eventArgsType,
    bool enableStopPropagation = false, bool enablePreventDefault = false) : Attribute
{
    public string AttributeName { get; } = attributeName;
    public Type EventArgsType { get; } = eventArgsType;
}
