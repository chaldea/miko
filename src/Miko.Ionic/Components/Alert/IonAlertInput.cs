namespace Miko.Ionic.Components;

/// <summary>
/// A single alert input. Mirrors Ionic's <c>AlertInput</c> interface. Inputs of one alert must all
/// be the same kind (all <c>text</c>/<c>textarea</c>-style, all <c>radio</c>, or all
/// <c>checkbox</c>) — the alert renders the group matching the first input's <see cref="Type"/>.
/// </summary>
public sealed class IonAlertInput
{
    /// <summary>Input type: <c>"radio"</c>, <c>"checkbox"</c>, or a text-field type such as
    /// <c>"text"</c> (default), <c>"number"</c>, <c>"password"</c>, <c>"email"</c>, <c>"tel"</c>,
    /// or <c>"textarea"</c> for a multi-line field.</summary>
    public string Type { get; set; } = "text";

    /// <summary>Form field name (Ionic <c>name</c>). Not used by this port.</summary>
    public string? Name { get; set; }

    /// <summary>Placeholder text for text-style inputs.</summary>
    public string? Placeholder { get; set; }

    /// <summary>The input value, reported in the dismiss <c>Data</c>. For a text field it starts as
    /// the initial text and follows what the user types (as in Ionic, the alert writes it back).</summary>
    public string? Value { get; set; }

    /// <summary>Label shown beside a radio/checkbox.</summary>
    public string? Label { get; set; }

    /// <summary>Whether a radio/checkbox is checked. The alert updates it when a row is tapped.</summary>
    public bool Checked { get; set; }

    /// <summary>When true, a radio/checkbox row ignores taps and a text field cannot be focused or
    /// edited; both are dimmed.</summary>
    public bool Disabled { get; set; }

    /// <summary>Extra CSS class(es) (Ionic <c>cssClass</c>), added to the input element or the
    /// radio/checkbox button.</summary>
    public string? CssClass { get; set; }

    /// <summary>True for a radio input.</summary>
    public bool IsRadio => string.Equals(Type, "radio", StringComparison.OrdinalIgnoreCase);

    /// <summary>True for checkbox inputs.</summary>
    public bool IsCheckbox => string.Equals(Type, "checkbox", StringComparison.OrdinalIgnoreCase);

    /// <summary>True for a textarea input.</summary>
    public bool IsTextarea => string.Equals(Type, "textarea", StringComparison.OrdinalIgnoreCase);
}
