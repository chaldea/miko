namespace Miko.Ionic.Components;

/// <summary>
/// The data an <see cref="IonSelectOption"/> contributes to its parent <see cref="IonSelect"/>.
/// Mirrors the shape Ionic derives from each <c>ion-select-option</c> element when it builds the
/// overlay contents — <c>createOverlaySelectOptions</c> / <c>createAlertInputs</c> /
/// <c>createActionSheetButtons</c> in <c>select.tsx</c> all read the option's value, text content,
/// disabled flag and copied css classes.
/// <para>
/// In Ionic the value falls back to the option's text when no <c>value</c> is set
/// (<c>getOptionValue()</c>); this port applies the same fallback at registration time, so
/// <see cref="Value"/> is always the identity the select compares against.
/// </para>
/// </summary>
public sealed class IonSelectOptionData
{
    /// <summary>The option's identity, compared against the select's value to derive the checked
    /// state. Falls back to <see cref="Text"/> when the option declares no explicit value.</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>The option's visible label (the option element's text content in Ionic).</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Whether the option cannot be selected. Ionic ignores this for the
    /// <c>action-sheet</c> interface, since action-sheet buttons cannot be disabled.</summary>
    public bool Disabled { get; init; }

    /// <summary>Extra css class(es) declared on the option, copied onto the overlay's rendered
    /// option (Ionic concatenates them after the <c>select-interface-option</c> marker).</summary>
    public string? CssClass { get; init; }
}
