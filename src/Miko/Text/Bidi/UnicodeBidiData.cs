namespace Miko.Text.Bidi;

/// <summary>
/// Lookups over the Bidi_Class and Bidi_Paired_Bracket tables generated from the UCD
/// (<c>UnicodeBidiData.g.cs</c>, see tools/Miko.UnicodeGen).
/// </summary>
internal static partial class UnicodeBidiData
{
    /// <summary>Bidi classes of U+0000..U+00FF, which is nearly all of the text a UI renders.</summary>
    private static readonly BidiClass[] Latin1 = BuildLatin1();

    public static BidiClass GetBidiClass(int codePoint)
    {
        if ((uint)codePoint < (uint)Latin1.Length)
        {
            return Latin1[codePoint];
        }
        return Lookup(codePoint);
    }

    /// <summary>
    /// The bracket a code point pairs with and whether it opens, or <c>false</c> when it is not a
    /// paired bracket (Bidi_Paired_Bracket_Type = None).
    /// </summary>
    public static bool TryGetBracket(int codePoint, out int pair, out bool isOpen)
    {
        var points = BracketCodePoints;
        int index = points.BinarySearch(codePoint);
        if (index < 0)
        {
            pair = 0;
            isOpen = false;
            return false;
        }
        pair = BracketPairs[index];
        isOpen = BracketIsOpen[index] != 0;
        return true;
    }

    private static BidiClass Lookup(int codePoint)
    {
        var starts = RangeStarts;
        int index = starts.BinarySearch(codePoint);
        // Not an exact range start: the run containing it starts at the preceding entry.
        if (index < 0) index = ~index - 1;
        return (BidiClass)RangeClasses[index];
    }

    private static BidiClass[] BuildLatin1()
    {
        var table = new BidiClass[0x100];
        for (int cp = 0; cp < table.Length; cp++)
        {
            table[cp] = Lookup(cp);
        }
        return table;
    }
}
