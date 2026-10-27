using Miko.Common;

namespace Miko.Ionic.Components;

/// <summary>
/// How <see cref="IonDatetime"/> formats the text it shows (Ionic <c>formatOptions</c>): the
/// selected date in the header and the time on the time button.
/// <para>
/// Ionic validates that the presentation has what it needs: <c>date</c>, <c>month</c>,
/// <c>month-year</c> and <c>year</c> read <see cref="Date"/>, <c>time</c> reads <see cref="Time"/>,
/// <c>date-time</c> / <c>time-date</c> read either.
/// </para>
/// </summary>
public sealed record DatetimeFormatOptions
{
    /// <summary>Format of the selected date in the header, e.g.
    /// <c>new() { Weekday = "long", Month = "long", Day = "numeric" }</c>.</summary>
    public DatetimeFormat? Date { get; init; }

    /// <summary>Format of the time button, e.g. <c>new() { Hour = "2-digit", Minute = "2-digit" }</c>.</summary>
    public DatetimeFormat? Time { get; init; }
}

/// <summary>
/// The fields of a date or time to show and their width — the subset of JavaScript's
/// <c>Intl.DateTimeFormatOptions</c> that Ionic's datetime uses. The text follows the datetime's
/// locale: field order, separators and names come from its .NET <c>CultureInfo</c>.
/// Time zones are not supported, as in Ionic (values never carry one).
/// </summary>
public sealed record DatetimeFormat
{
    /// <summary>Day of the week: <c>"long"</c> (Wednesday), <c>"short"</c> (Wed) or <c>"narrow"</c>
    /// (shown like short).</summary>
    public string? Weekday { get; init; }

    /// <summary><c>"numeric"</c> (2026) or <c>"2-digit"</c> (26).</summary>
    public string? Year { get; init; }

    /// <summary><c>"numeric"</c> (7), <c>"2-digit"</c> (07), <c>"long"</c> (July), <c>"short"</c>
    /// (Jul) or <c>"narrow"</c> (shown like short). Locales that write the month as a number next to
    /// a unit (<c>7月</c>) keep the number for the text widths.</summary>
    public string? Month { get; init; }

    /// <summary><c>"numeric"</c> (5) or <c>"2-digit"</c> (05).</summary>
    public string? Day { get; init; }

    /// <summary><c>"numeric"</c> or <c>"2-digit"</c>. The hour cycle comes from the datetime's
    /// <c>HourCycle</c> or its locale.</summary>
    public string? Hour { get; init; }

    /// <summary><c>"numeric"</c> or <c>"2-digit"</c>; minutes always show two digits after an hour.</summary>
    public string? Minute { get; init; }

    /// <summary>A .NET custom date and time format string (<c>"dddd d MMMM"</c>) used instead of
    /// the fields above. Not available in Ionic.</summary>
    public string? Pattern { get; init; }
}

/// <summary>Custom colors for one highlighted calendar day (Ionic <c>DatetimeHighlightStyle</c>), as
/// returned by <see cref="IonDatetime.HighlightedDatesCallback"/>. Unset values keep the normal day
/// style.</summary>
public sealed record DatetimeHighlightStyle
{
    /// <summary>The day number's color.</summary>
    public Color? TextColor { get; init; }

    /// <summary>The day circle's fill.</summary>
    public Color? BackgroundColor { get; init; }

    /// <summary>The day circle's border.</summary>
    public Border? Border { get; init; }
}

/// <summary>A highlighted calendar day for <see cref="IonDatetime.HighlightedDates"/> (Ionic
/// <c>DatetimeHighlight</c>).</summary>
public sealed record DatetimeHighlight
{
    /// <summary>The day as an ISO date, <c>yyyy-MM-dd</c>.</summary>
    public string Date { get; init; } = string.Empty;

    /// <summary>The day number's color.</summary>
    public Color? TextColor { get; init; }

    /// <summary>The day circle's fill.</summary>
    public Color? BackgroundColor { get; init; }

    /// <summary>The day circle's border.</summary>
    public Border? Border { get; init; }
}
