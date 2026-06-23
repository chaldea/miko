using Miko.Components;
using Miko.Core;
using System.Reflection;

namespace Miko.Testing;

/// <summary>
/// Builder for setting component parameters in tests.
/// </summary>
public class ComponentParameterBuilder<TComponent> where TComponent : ComponentBase
{
    private readonly TComponent _component;
    private readonly Dictionary<string, object?> _parameters = new();

    internal ComponentParameterBuilder(TComponent component)
    {
        _component = component;
    }

    /// <summary>
    /// Adds a parameter value for the component.
    /// </summary>
    public ComponentParameterBuilder<TComponent> Add<TValue>(string parameterName, TValue value)
    {
        _parameters[parameterName] = value;
        return this;
    }

    /// <summary>
    /// Adds child content to the component.
    /// </summary>
    public ComponentParameterBuilder<TComponent> AddChildContent(RenderFragment childContent)
    {
        _parameters["ChildContent"] = childContent;
        return this;
    }

    internal void ApplyParameters()
    {
        var componentType = typeof(TComponent);

        foreach (var (name, value) in _parameters)
        {
            var property = componentType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Property '{name}' not found on component type '{componentType.Name}'");
            }

            // Check if property has [Parameter] attribute
            var hasParameterAttribute = property.GetCustomAttribute<ParameterAttribute>() != null;
            if (!hasParameterAttribute)
            {
                throw new InvalidOperationException(
                    $"Property '{name}' on component type '{componentType.Name}' is not marked with [Parameter] attribute");
            }

            property.SetValue(_component, value);
        }

        // Trigger OnParametersSet lifecycle method
        var onParametersSetMethod = componentType.GetMethod("OnParametersSet",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        onParametersSetMethod?.Invoke(_component, null);
    }
}
