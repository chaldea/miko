using static Miko.Text.Bidi.BidiClass;

namespace Miko.Text.Bidi;

/// <summary>
/// The Unicode Bidirectional Algorithm (UAX #9, Unicode 15.1) for one paragraph, through rule L2
/// (ISSUE-157). The structure follows the Unicode reference implementation (BidiReference.java):
/// explicit levels (X1–X10), then for each isolating run sequence the weak (W1–W7), paired
/// bracket (N0), neutral (N1–N2) and implicit (I1–I2) rules.
/// <para>
/// Input and output are per code point. Characters that rule X9 removes (embedding controls and
/// BN) stay in place and take the level of the preceding character, so there is one level per
/// input position. Paired brackets (BD16) are recognised only when code points are supplied; the
/// class-only conformance file (BidiTest.txt) has none.
/// </para>
/// </summary>
internal sealed partial class BidiParagraph
{
    /// <summary>max_depth (BD2).</summary>
    private const int MaxDepth = 125;

    private readonly int _length;
    private readonly BidiClass[] _initialTypes;
    // X1–X8 apply directional overrides here; the W/N rules then work on per-sequence copies.
    private readonly BidiClass[] _resultTypes;
    private readonly byte[] _levels;
    private readonly byte _paragraphLevel;
    // BD9: the PDI matching an isolate initiator (_length when unmatched), and the reverse map (-1).
    private readonly int[] _matchingPdi;
    private readonly int[] _matchingIsolateInitiator;
    // BD14/BD15: 0 = not a bracket, 1 = opening, 2 = closing; both ends of a pair share a key.
    private readonly byte[]? _bracketTypes;
    private readonly int[]? _bracketKeys;

    /// <param name="types">Bidi class of each code point.</param>
    /// <param name="codePoints">The code points, for paired-bracket identification; may be empty.</param>
    /// <param name="paragraphLevel">0 (LTR) or 1 (RTL); any other value applies rules P2–P3.</param>
    public BidiParagraph(ReadOnlySpan<BidiClass> types, ReadOnlySpan<int> codePoints, int paragraphLevel)
    {
        _length = types.Length;
        _initialTypes = types.ToArray();
        _resultTypes = types.ToArray();
        _levels = new byte[_length];
        _matchingPdi = new int[_length];
        _matchingIsolateInitiator = new int[_length];

        if (!codePoints.IsEmpty)
        {
            _bracketTypes = new byte[_length];
            _bracketKeys = new int[_length];
            for (int i = 0; i < _length; i++)
            {
                if (UnicodeBidiData.TryGetBracket(codePoints[i], out int pair, out bool isOpen))
                {
                    _bracketTypes[i] = isOpen ? (byte)1 : (byte)2;
                    _bracketKeys[i] = CanonicalBracket(isOpen ? codePoints[i] : pair);
                }
            }
        }

        DetermineMatchingIsolates();
        _paragraphLevel = paragraphLevel is 0 or 1 ? (byte)paragraphLevel : DetermineParagraphLevel(0, _length);
        DetermineExplicitLevels();
        // Every sequence takes its sos/eos from the explicit levels, so all are built before any resolves.
        foreach (var sequence in DetermineIsolatingRunSequences())
        {
            sequence.Resolve();
        }
        AssignLevelsToCharactersRemovedByX9();
    }

    public byte ParagraphLevel => _paragraphLevel;

    /// <summary>Whether rule X9 removes characters of this class (they take no part in reordering).</summary>
    public static bool IsRemovedByX9(BidiClass type) => type is LRE or RLE or LRO or RLO or PDF or BN;

    /// <summary>
    /// The resolved levels with rule L1 applied. <paramref name="lineEnds"/> are the exclusive end
    /// offsets of the lines, ascending; whitespace (and isolate/format controls) before each is reset
    /// to the paragraph level, as are segment and paragraph separators and the whitespace before them.
    /// </summary>
    public byte[] GetLevels(ReadOnlySpan<int> lineEnds)
    {
        var result = (byte[])_levels.Clone();
        for (int i = 0; i < _length; i++)
        {
            if (_initialTypes[i] is B or S)
            {
                result[i] = _paragraphLevel;
                for (int j = i - 1; j >= 0 && IsWhitespaceForL1(_initialTypes[j]); j--)
                {
                    result[j] = _paragraphLevel;
                }
            }
        }

        int start = 0;
        foreach (int limit in lineEnds)
        {
            for (int j = limit - 1; j >= start && IsWhitespaceForL1(_initialTypes[j]); j--)
            {
                result[j] = _paragraphLevel;
            }
            start = limit;
        }
        return result;
    }

    /// <summary>
    /// Rule L2 for one line: the logical index shown at each visual position, left to right.
    /// From the highest level down to the lowest odd level, every maximal run at or above that
    /// level is reversed.
    /// </summary>
    public static int[] ComputeReordering(ReadOnlySpan<byte> levels)
    {
        var result = new int[levels.Length];
        byte highest = 0;
        byte lowestOdd = MaxDepth + 2;
        for (int i = 0; i < levels.Length; i++)
        {
            result[i] = i;
            byte level = levels[i];
            if (level > highest) highest = level;
            if ((level & 1) != 0 && level < lowestOdd) lowestOdd = level;
        }

        for (int level = highest; level >= lowestOdd; level--)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] < level) continue;
                int limit = i + 1;
                while (limit < levels.Length && levels[limit] >= level) limit++;
                Array.Reverse(result, i, limit - i);
                i = limit;
            }
        }
        return result;
    }

    private static bool IsWhitespaceForL1(BidiClass type)
        => type is WS or BN or LRE or RLE or LRO or RLO or PDF or LRI or RLI or FSI or PDI;

    // U+2329/U+232A are canonically equivalent to U+3008/U+3009, so they pair with them (BD16).
    private static int CanonicalBracket(int codePoint) => codePoint switch
    {
        0x2329 => 0x3008,
        0x232A => 0x3009,
        _ => codePoint,
    };

    private static BidiClass TypeForLevel(int level) => (level & 1) == 0 ? L : R;
}
