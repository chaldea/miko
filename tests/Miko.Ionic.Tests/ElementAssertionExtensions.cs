using Miko.Core;
using Shouldly;

namespace Miko.Ionic.Tests;

/// <summary>
/// Shouldly-style assertions over an <see cref="Element"/>'s CSS class list.
/// <para>
/// <see cref="Element.Class"/> is a nullable <see cref="string"/> (null = no class attribute),
/// so asserting directly with <c>element.Class.ShouldContain(...)</c> produces CS8604 (possible
/// null argument). These helpers guard the null first, then delegate to the existing
/// substring-based Shouldly string assertions, preserving the original semantics while keeping
/// the null-state failures readable.
/// </para>
/// </summary>
public static class ElementAssertionExtensions
{
    /// <summary>
    /// Asserts the element's <see cref="Element.Class"/> is non-null and contains
    /// <paramref name="className"/> as a substring. Returns the element for chaining.
    /// </summary>
    public static Element ShouldHaveClass(this Element element, string className)
    {
        element.Class.ShouldNotBeNull();
        element.Class.ShouldContain(className);
        return element;
    }

    /// <summary>
    /// Asserts the element's <see cref="Element.Class"/> is non-null and does not contain
    /// <paramref name="className"/> as a substring. Returns the element for chaining.
    /// </summary>
    public static Element ShouldNotHaveClass(this Element element, string className)
    {
        element.Class.ShouldNotBeNull();
        element.Class.ShouldNotContain(className);
        return element;
    }
}
