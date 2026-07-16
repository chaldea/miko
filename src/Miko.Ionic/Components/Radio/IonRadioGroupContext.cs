using Miko.Components;

namespace Miko.Ionic.Components;

/// <summary>
/// Selection state cascaded from an <see cref="IonRadioGroup"/> down to its <see cref="IonRadio"/>
/// children. Mirrors how Ionic's <c>ion-radio-group</c> owns the selected <c>value</c> and each
/// <c>ion-radio</c> derives its checked state by comparing its own value to the group's value
/// (<c>updateState</c> / <c>isOptionSelected</c>).
/// <para>
/// The group and its radios share a subtree, so this travels via a
/// <see cref="CascadingValue{TValue}"/> (matched by type), exactly like
/// <see cref="IonAccordionGroupContext"/>. Ionic wires them with runtime DOM events
/// (<c>ionValueChange</c>) and <c>closest('ion-radio-group')</c>; Miko has neither, so each radio
/// reads this context to derive its checked/disabled state and calls <see cref="RequestSelect"/> to
/// ask the group to change the value. Radio groups are single-select only.
/// </para>
/// </summary>
public sealed class IonRadioGroupContext
{
    /// <summary>The currently selected radio value (<c>null</c> when nothing is selected).</summary>
    public string? Value { get; init; }

    /// <summary>Whether the whole group is disabled (blocks interaction on its radios).</summary>
    public bool Disabled { get; init; }

    /// <summary>Whether a checked radio can be tapped again to clear the selection (Ionic
    /// <c>allowEmptySelection</c>). Read by radios to know a re-tap may deselect.</summary>
    public bool AllowEmptySelection { get; init; }

    /// <summary>Invoked by a radio (with its own value) to ask the group to select it. Mirrors the
    /// group's <c>onClick</c> handler: selects the value, or clears it when it is already selected
    /// and <see cref="AllowEmptySelection"/> is set.</summary>
    public EventCallback<string> RequestSelect { get; init; }

    /// <summary>True when <paramref name="value"/> is the group's selected value.</summary>
    public bool IsSelected(string value) => Value == value;
}
