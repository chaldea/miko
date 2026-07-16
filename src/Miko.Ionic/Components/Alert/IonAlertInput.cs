namespace Miko.Ionic.Components;

/// <summary>
/// A single alert input. Mirrors Ionic's <c>AlertInput</c> interface. Inputs of one alert must all
/// be the same kind (all <c>text</c>/<c>textarea</c>-style, all <c>radio</c>, or all
/// <c>checkbox</c>) — the alert renders the group matching the first input's <see cref="Type"/>.
/// </summary>
public sealed class IonAlertInput
{
    /// <summary>Input type: <c>"text"</c> (default), <c>"textarea"</c>, <c>"number"</c>,
    /// <c>"password"</c>, <c>"email"</c>, <c>"tel"</c>, <c>"radio"</c>, or <c>"checkbox"</c>.</summary>
    public string Type { get; set; } = "text";

    /// <summary>Form field name.</summary>
    public string? Name { get; set; }

    /// <summary>Placeholder text for text-style inputs.</summary>
    public string? Placeholder { get; set; }

    /// <summary>The input value.</summary>
    public string? Value { get; set; }

    /// <summary>Label shown beside a radio/checkbox.</summary>
    public string? Label { get; set; }

    /// <summary>Whether a radio/checkbox is checked.</summary>
    public bool Checked { get; set; }

    /// <summary>Whether the input is disabled.</summary>
    public bool Disabled { get; set; }

    /// <summary>Extra CSS class(es) to add.</summary>
    public string? CssClass { get; set; }

    /// <summary>True for radio/checkbox inputs (rendered as a tappable group).</summary>
    public bool IsRadio => string.Equals(Type, "radio", StringComparison.OrdinalIgnoreCase);

    /// <summary>True for checkbox inputs.</summary>
    public bool IsCheckbox => string.Equals(Type, "checkbox", StringComparison.OrdinalIgnoreCase);

    /// <summary>True for a textarea input.</summary>
    public bool IsTextarea => string.Equals(Type, "textarea", StringComparison.OrdinalIgnoreCase);
}
