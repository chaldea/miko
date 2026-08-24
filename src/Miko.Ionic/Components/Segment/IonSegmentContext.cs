using Miko.Components;
using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Styling;

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

    /// <summary>The value selected immediately before <see cref="Value"/>.</summary>
    public string? PreviousValue { get; init; }

    /// <summary>The enclosing segment palette, propagated to segment buttons.</summary>
    public string? SegmentColor { get; init; }

    /// <summary>Invoked by a button with its own value to request becoming the selected one.</summary>
    public EventCallback<string> Select { get; init; }

    internal Dictionary<string, int> ButtonIndexes { get; init; } = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Element> _indicators = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Element> _buttons = new(StringComparer.Ordinal);

    internal IReadOnlyDictionary<string, Element> Buttons => _buttons;

    internal int RegisterButton(string value)
    {
        if (ButtonIndexes.TryGetValue(value, out var index)) return index;
        index = ButtonIndexes.Count;
        ButtonIndexes[value] = index;
        return index;
    }

    internal int? TranslationToSelected(string value)
    {
        if (Value is null || !ButtonIndexes.TryGetValue(value, out var from) ||
            !ButtonIndexes.TryGetValue(Value, out var to))
            return null;

        return (to - from) * 100;
    }

    internal Style? IndicatorStyle(string value)
    {
        var translation = TranslationToSelected(value);
        if (translation is null or 0) return null;

        return new Style
        {
            Transform = new Transform(
                new TransformFunction.TranslateX(Length.Percent(translation.Value))),
        };
    }

    internal void RegisterIndicator(string value, Element indicator)
    {
        _indicators[value] = indicator;

        // The selected button can occur after buttons already built during this render. Recompute
        // all registered indicators whenever another index becomes available so earlier buttons
        // receive their final transform as soon as the selected index is known.
        foreach (var (buttonValue, element) in _indicators)
            element.Style = IndicatorStyle(buttonValue);
    }

    internal void RegisterButtonElement(string value, Element button)
        => _buttons[value] = button;
}
