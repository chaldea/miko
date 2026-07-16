using Miko.Components;

namespace Miko.Ionic.Components;

/// <summary>
/// Selection state cascaded from an <see cref="IonPickerColumn"/> down to its
/// <see cref="IonPickerColumnOption"/> children. Mirrors how Ionic's <c>ion-picker-column</c> owns
/// the selected <c>value</c> (a single string — a picker column is single-select) and updates it via
/// <c>setValue()</c> when an option is tapped.
/// <para>
/// The column and its options share a subtree, so this travels via a
/// <see cref="CascadingValue{TValue}"/> (matched by type), exactly like
/// <see cref="IonAccordionGroupContext"/> and <see cref="IonSegmentContext"/>. Ionic wires them with
/// imperative DOM lookups (<c>el.closest('ion-picker-column')</c>) and scroll/click events; Miko has
/// neither, so each option reads this context to derive its own active state and calls
/// <see cref="RequestSelect"/> to ask the column to change the value.
/// </para>
/// </summary>
public sealed class IonPickerColumnContext
{
    /// <summary>The column's currently selected option value.</summary>
    public string? Value { get; init; }

    /// <summary>Whether the whole column is disabled (dims and blocks interaction).</summary>
    public bool Disabled { get; init; }

    /// <summary>Invoked by an option with its own value to ask the column to make it the selected
    /// one. Mirrors Ionic's <c>pickerColumn.setValue(option.value)</c>.</summary>
    public EventCallback<string> RequestSelect { get; init; }

    /// <summary>True when <paramref name="value"/> is the column's selected value.</summary>
    public bool IsSelected(string value) => Value == value;
}
