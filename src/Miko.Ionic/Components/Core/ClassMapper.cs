namespace Miko.Ionic.Components;

/// <summary>
/// Utility class for building CSS class names dynamically.
/// Provides a fluent API for adding conditional and unconditional class names.
/// </summary>
public sealed class ClassMapper
{
    private readonly List<string> _classes = new();

    /// <summary>
    /// Gets the combined CSS class string.
    /// </summary>
    public string Class => string.Join(" ", _classes.Where(c => !string.IsNullOrWhiteSpace(c)));

    /// <summary>
    /// Clears all registered class names.
    /// </summary>
    /// <returns>The current instance for chaining.</returns>
    public ClassMapper Clear()
    {
        _classes.Clear();
        return this;
    }

    /// <summary>
    /// Adds a class name unconditionally.
    /// </summary>
    /// <param name="className">The class name to add. Null or whitespace values are ignored.</param>
    /// <returns>The current instance for chaining.</returns>
    public ClassMapper Add(string? className)
    {
        if (!string.IsNullOrWhiteSpace(className))
        {
            _classes.Add(className);
        }
        return this;
    }

    /// <summary>
    /// Adds a class name conditionally.
    /// </summary>
    /// <param name="condition">When true, the class name is added.</param>
    /// <param name="className">The class name to add.</param>
    /// <returns>The current instance for chaining.</returns>
    public ClassMapper If(bool condition, string className)
    {
        if (condition && !string.IsNullOrWhiteSpace(className))
        {
            _classes.Add(className);
        }
        return this;
    }

    /// <summary>
    /// Adds a class name when the value is not null or whitespace, optionally with a prefix.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="prefix">Optional prefix to prepend to the value (e.g., "button-").</param>
    /// <returns>The current instance for chaining.</returns>
    public ClassMapper AddValue(string? value, string? prefix = null)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var className = string.IsNullOrEmpty(prefix)
                ? value
                : $"{prefix}{value.ToLowerInvariant()}";
            _classes.Add(className);
        }
        return this;
    }

    /// <summary>
    /// Adds a color class with the "ion-color-" prefix.
    /// </summary>
    /// <param name="color">The color name (e.g., "primary", "danger").</param>
    /// <returns>The current instance for chaining.</returns>
    public ClassMapper AddColor(string? color)
    {
        return AddValue(color, "ion-color-");
    }
}
