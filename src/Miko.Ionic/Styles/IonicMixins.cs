// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Styling;

namespace Miko.Ionic.Styles;

/// <summary>
/// C# ports of Ionic's shared SCSS mixins (<c>core/src/themes/ionic.mixins.scss</c>).
/// Each returns a <see cref="CssObject"/> of pre-set declarations meant to be spread into a rule
/// via the <c>["..."]</c> merge operator, mirroring SCSS <c>@include</c>. For example:
/// <code>
/// [".button-native"] = new() { ["..."] = TextInherit(), Display = Display.Flex }
/// </code>
/// </summary>
public static class IonicMixins
{
    /// <summary>
    /// Port of the Ionic <c>text-inherit()</c> mixin: every text-related property inherits from the
    /// parent (mapped to the subset Miko's <see cref="Style"/> supports). Used so a native child
    /// (e.g. <c>.button-native</c>) picks up the host's font / color / tracking rather than resetting.
    /// <para>
    /// Ionic also inherits <c>text-indent</c> and <c>text-overflow</c>; Miko's <c>TextOverflow</c> has
    /// no inherit path and there is no text-indent property, so those are omitted.
    /// </para>
    /// </summary>
    public static CssObject TextInherit() => new()
    {
        FontFamily = Inherit,
        FontSize = Inherit,
        FontStyle = Inherit,
        FontWeight = Inherit,
        LetterSpacing = Inherit,
        TextDecoration = Inherit,
        TextTransform = Inherit,
        TextAlign = Inherit,
        WhiteSpace = Inherit,
        Color = Inherit,
    };
}
