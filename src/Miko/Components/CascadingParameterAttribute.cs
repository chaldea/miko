namespace Miko.Components;

/// <summary>
/// Marks a component property that should receive its value from an ancestor
/// <see cref="CascadingValue{TValue}"/> rather than from a parent-supplied parameter.
/// Mirrors Blazor's <c>[CascadingParameter]</c>.
/// <para>
/// By default the value is matched by the property's type (the nearest ancestor
/// <see cref="CascadingValue{TValue}"/> whose declared <c>TValue</c> is assignable to the
/// property type). Set <see cref="Name"/> to instead match a named cascading value.
/// </para>
/// <para>
/// Cascading parameters are resolved in <see cref="ComponentBase.Build"/> before the lifecycle
/// methods run, so they are available inside <see cref="ComponentBase.OnInitialized"/> /
/// <see cref="ComponentBase.OnParametersSet"/>.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CascadingParameterAttribute : Attribute
{
    /// <summary>
    /// Optional name to match a named <see cref="CascadingValue{TValue}.Name"/>.
    /// When null (the default), the value is matched by type.
    /// </summary>
    public string? Name { get; set; }
}
