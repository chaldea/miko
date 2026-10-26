namespace Miko.Common;

internal static class TextAlignExtensions
{
    /// <summary>
    /// Maps the logical <c>text-align</c> values onto physical ones for a paragraph direction
    /// (ISSUE-157): <c>start</c> (the initial value) is left in LTR and right in RTL. <c>justify</c>
    /// is laid out like <c>start</c> — Miko does not stretch lines.
    /// </summary>
    public static TextAlign ResolveFor(this TextAlign align, Direction direction) => align switch
    {
        TextAlign.Start or TextAlign.Justify => direction == Direction.Rtl ? TextAlign.Right : TextAlign.Left,
        TextAlign.End => direction == Direction.Rtl ? TextAlign.Left : TextAlign.Right,
        _ => align,
    };
}
