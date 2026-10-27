namespace Miko.Ionic.Components;

/// <summary>
/// The pieces of a date and time the datetime works with (Ionic's <c>DatetimeParts</c>,
/// datetime-interface.ts). Months are 1-based. Every field is optional because Ionic values are
/// partial ISO strings: <c>"2026"</c>, <c>"2026-07"</c>, <c>"13:30"</c> … A null <see cref="Day"/>
/// in a month grid is a leading filler cell.
/// <para>
/// Ionic compares these with JavaScript's relational operators, where <c>undefined</c> is never
/// less or greater than a number and equals only itself. C# lifted operators on <c>int?</c> behave
/// the same way, so the ported comparisons read like the TypeScript.
/// </para>
/// </summary>
internal readonly record struct DatetimeParts
{
    public int? Year { get; init; }
    public int? Month { get; init; }
    public int? Day { get; init; }

    /// <summary>0 – 23.</summary>
    public int? Hour { get; init; }

    public int? Minute { get; init; }

    /// <summary><c>"am"</c> or <c>"pm"</c>.</summary>
    public string? Ampm { get; init; }

    /// <summary>A day of the previous or next month shown by <c>ShowAdjacentDays</c>.</summary>
    public bool IsAdjacentDay { get; init; }

    public DatetimeParts(int? year, int? month, int? day)
    {
        Year = year;
        Month = month;
        Day = day;
    }

    public DatetimeParts(int? year, int? month, int? day, int? hour, int? minute, string? ampm = null)
    {
        Year = year;
        Month = month;
        Day = day;
        Hour = hour;
        Minute = minute;
        Ampm = ampm;
    }
}

/// <summary>One option of a wheel column (Ionic's <c>WheelColumnOption</c>, utils/data.ts).
/// Picker column values are strings, so numeric values are stored in their invariant form.</summary>
internal readonly record struct WheelColumnOption(string Text, string Value, bool Disabled = false);
