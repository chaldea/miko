using Miko.Components;

namespace Miko.Ionic.Components;

/// <summary>
/// Selection state cascaded from an <see cref="IonSegment"/> down to its
/// <see cref="IonSegmentButton"/> children. Mirrors how Ionic's <c>ion-segment</c> owns the
/// selected <c>value</c> and toggles the checked state of its buttons.
/// <para>
/// The segment and its buttons share a subtree, so this travels via a
/// <see cref="CascadingValue{TValue}"/> (matched by type). A button reads it through a
/// <see cref="CascadingParameterAttribute"/>, derives whether it is selected, and raises
/// <see cref="Select"/> on click to ask the segment to change the value.
/// </para>
/// </summary>
public sealed class IonSegmentContext
{
    /// <summary>The currently selected button value.</summary>
    public string? Value { get; init; }

    /// <summary>Invoked by a button with its own value to request becoming the selected one.</summary>
    public EventCallback<string> Select { get; init; }
}
