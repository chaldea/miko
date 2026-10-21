using Miko.Core;

namespace Miko.Components;

/// <summary>
/// The value <c>@ref</c> captures for an element when the field is declared Blazor-style
/// (<c>ElementReference _ref;</c>). Miko elements are objects, so a field of the element's own type
/// (<c>CanvasElement? _canvas;</c>) or <see cref="Core.Element"/> works too, and is usually more
/// useful.
/// <para>
/// The Razor compiler also uses this type to detect <c>@ref</c> support — without it,
/// <c>@ref</c> compiled into a literal attribute and every field stayed null (ISSUE-153 problem 4).
/// </para>
/// </summary>
public readonly struct ElementReference : IEquatable<ElementReference>
{
    public ElementReference(Element element)
    {
        Element = element;
    }

    /// <summary>The captured element, or null for a default value.</summary>
    public Element? Element { get; }

    public static implicit operator ElementReference(Element element) => new(element);

    public bool Equals(ElementReference other) => ReferenceEquals(Element, other.Element);

    public override bool Equals(object? obj) => obj is ElementReference other && Equals(other);

    public override int GetHashCode() => Element?.GetHashCode() ?? 0;
}
