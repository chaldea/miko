namespace Miko.Text.Bidi;

/// <summary>
/// Unicode Bidi_Class values (UAX #9, Table 4). The numeric values are the indices stored in the
/// generated <see cref="UnicodeBidiData"/> tables, so the order must not change.
/// </summary>
internal enum BidiClass : byte
{
    /// <summary>Left-to-right strong.</summary>
    L,
    /// <summary>Right-to-left strong (Hebrew and most RTL scripts).</summary>
    R,
    /// <summary>Right-to-left Arabic letter.</summary>
    AL,
    /// <summary>European number.</summary>
    EN,
    /// <summary>European number separator (+ -).</summary>
    ES,
    /// <summary>European number terminator (# $ % …).</summary>
    ET,
    /// <summary>Arabic number.</summary>
    AN,
    /// <summary>Common number separator (, . / :).</summary>
    CS,
    /// <summary>Nonspacing mark.</summary>
    NSM,
    /// <summary>Boundary neutral (format and control characters).</summary>
    BN,
    /// <summary>Paragraph separator.</summary>
    B,
    /// <summary>Segment separator (tab).</summary>
    S,
    /// <summary>Whitespace.</summary>
    WS,
    /// <summary>Other neutral.</summary>
    ON,
    LRE,
    LRO,
    RLE,
    RLO,
    PDF,
    LRI,
    RLI,
    FSI,
    PDI,
}
