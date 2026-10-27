using System.Globalization;
using System.Text.RegularExpressions;

namespace Miko.Ionic.Components;

/// <summary>
/// The locale-independent date utilities of <c>ion-datetime</c>, ported function for function from
/// Ionic's <c>datetime/utils</c>: comparison.ts, helpers.ts, manipulation.ts, parse.ts and state.ts.
/// The functions that format text (data.ts, format.ts) live in <see cref="DatetimeLocale"/> and
/// <see cref="DatetimeData"/>.
/// <para>
/// Ionic deliberately avoids JavaScript's <c>Date</c> here — it would shift values into the local
/// time zone — and so does this port: values are plain <see cref="DatetimeParts"/>, never
/// <see cref="DateTime"/>.
/// </para>
/// </summary>
internal static partial class DatetimeUtils
{
    // ---- comparison.ts -----------------------------------------------------------------------

    /// <summary>True if both parts fall on the same day.</summary>
    public static bool IsSameDay(DatetimeParts a, DatetimeParts b)
        => a.Month == b.Month && a.Day == b.Day && a.Year == b.Year;

    /// <summary>True if <paramref name="a"/> is a day before <paramref name="b"/>.</summary>
    public static bool IsBefore(DatetimeParts a, DatetimeParts b)
        => a.Year < b.Year
           || (a.Year == b.Year && a.Month < b.Month)
           || (a.Year == b.Year && a.Month == b.Month && a.Day is not null && a.Day < b.Day);

    /// <summary>True if <paramref name="a"/> is a day after <paramref name="b"/>.</summary>
    public static bool IsAfter(DatetimeParts a, DatetimeParts b)
        => a.Year > b.Year
           || (a.Year == b.Year && a.Month > b.Month)
           || (a.Year == b.Year && a.Month == b.Month && a.Day is not null && a.Day > b.Day);

    // ---- helpers.ts --------------------------------------------------------------------------

    public static bool IsLeapYear(int year) => (year % 4 == 0 && year % 100 != 0) || year % 400 == 0;

    /// <summary>Number of days in a 1-based month.</summary>
    public static int GetNumDaysInMonth(int month, int year)
        => month is 4 or 6 or 9 or 11 ? 30
           : month == 2 ? (IsLeapYear(year) ? 29 : 28)
           : 31;

    /// <summary>True for the 24-hour cycles (<c>h23</c>, <c>h24</c>).</summary>
    public static bool Is24Hour(string hourCycle) => hourCycle is "h23" or "h24";

    // ---- manipulation.ts ---------------------------------------------------------------------

    private static string TwoDigit(int? value) => Math.Abs(value ?? 0).ToString("00", CultureInfo.InvariantCulture)[^2..];

    private static string FourDigit(int? value) => Math.Abs(value ?? 0).ToString("0000", CultureInfo.InvariantCulture)[^4..];

    /// <summary>
    /// Formats parts as an ISO 8601 string (W3C NOTE-datetime), as far as they go: <c>YYYY</c>,
    /// <c>YYYY-MM</c>, <c>YYYY-MM-DD</c>, <c>YYYY-MM-DDTHH:mm:00</c>, or <c>HH:mm</c> for a time only.
    /// </summary>
    public static string ConvertDataToIso(DatetimeParts data)
    {
        if (data.Year is not null)
        {
            var result = FourDigit(data.Year);
            if (data.Month is not null)
            {
                result += "-" + TwoDigit(data.Month);
                if (data.Day is not null)
                {
                    result += "-" + TwoDigit(data.Day);
                    if (data.Hour is not null)
                        result += $"T{TwoDigit(data.Hour)}:{TwoDigit(data.Minute)}:00";
                }
            }
            return result;
        }

        return data.Hour is not null ? TwoDigit(data.Hour) + ":" + TwoDigit(data.Minute) : string.Empty;
    }

    /// <summary>Converts a 12-hour value to 24 hours.</summary>
    public static int Convert12HourTo24Hour(int hour, string? ampm) => ampm switch
    {
        null => hour,
        "am" => hour == 12 ? 0 : hour,
        _ => hour == 12 ? 12 : hour + 12,
    };

    /// <summary>The previous month, its day clamped to that month's length.</summary>
    public static DatetimeParts GetPreviousMonth(DatetimeParts refParts)
    {
        var month = refParts.Month == 1 ? 12 : (refParts.Month ?? 1) - 1;
        var year = refParts.Month == 1 ? (refParts.Year ?? 0) - 1 : refParts.Year ?? 0;
        var days = GetNumDaysInMonth(month, year);
        return new DatetimeParts(year, month, days < refParts.Day ? days : refParts.Day);
    }

    /// <summary>The next month, its day clamped to that month's length.</summary>
    public static DatetimeParts GetNextMonth(DatetimeParts refParts)
    {
        var month = refParts.Month == 12 ? 1 : (refParts.Month ?? 1) + 1;
        var year = refParts.Month == 12 ? (refParts.Year ?? 0) + 1 : refParts.Year ?? 0;
        var days = GetNumDaysInMonth(month, year);
        return new DatetimeParts(year, month, days < refParts.Day ? days : refParts.Day);
    }

    /// <summary>The 24-hour value of a wheel hour: unchanged for 24-hour cycles, otherwise
    /// converted with <paramref name="ampm"/>.</summary>
    public static int GetInternalHourValue(int hour, bool use24Hour, string? ampm)
        => use24Hour ? hour : Convert12HourTo24Hour(hour, ampm);

    /// <summary>
    /// The 24-hour value after switching the day period: hours are stored in 24-hour time, so
    /// toggling AM/PM shifts them by twelve.
    /// </summary>
    public static int? CalculateHourFromAmPm(DatetimeParts currentParts, string newAmPm)
    {
        var hour = currentParts.Hour;
        if (hour is null) return null;

        if (currentParts.Ampm == "am" && newAmPm == "pm") return Convert12HourTo24Hour(hour.Value, "pm");
        if (currentParts.Ampm == "pm" && newAmPm == "am") return Math.Abs(hour.Value - 12);
        return hour;
    }

    /// <summary>
    /// Makes parts valid: clamps them into the min/max range, moves a day past the end of the
    /// month (January 31 → February) to the last day, and on the min/max day moves the time back
    /// into range.
    /// </summary>
    public static DatetimeParts ValidateParts(DatetimeParts parts, DatetimeParts? minParts, DatetimeParts? maxParts)
    {
        var copy = ClampDate(parts, minParts, maxParts);

        if (parts.Day is { } day && parts.Month is { } month && parts.Year is { } year)
        {
            var numDays = GetNumDaysInMonth(month, year);
            if (numDays < day) copy = copy with { Day = numDays };
        }

        if (minParts is { } min && IsSameDay(copy, min) && copy.Hour is not null && min.Hour is not null)
        {
            if (copy.Hour < min.Hour)
                copy = copy with { Hour = min.Hour, Minute = min.Minute };
            else if (copy.Hour == min.Hour && copy.Minute is not null && min.Minute is not null && copy.Minute < min.Minute)
                copy = copy with { Minute = min.Minute };
        }

        if (maxParts is { } max && IsSameDay(parts, max) && copy.Hour is not null && max.Hour is not null)
        {
            if (copy.Hour > max.Hour)
                copy = copy with { Hour = max.Hour, Minute = max.Minute };
            else if (copy.Hour == max.Hour && copy.Minute is not null && max.Minute is not null && copy.Minute > max.Minute)
                copy = copy with { Minute = max.Minute };
        }

        return copy;
    }

    /// <summary>
    /// The date closest to <paramref name="refParts"/> that is allowed by the <c>*Values</c> lists
    /// and the min/max range.
    /// </summary>
    public static DatetimeParts GetClosestValidDate(
        DatetimeParts refParts,
        IReadOnlyList<int>? monthValues = null,
        IReadOnlyList<int>? dayValues = null,
        IReadOnlyList<int>? yearValues = null,
        IReadOnlyList<int>? hourValues = null,
        IReadOnlyList<int>? minuteValues = null,
        DatetimeParts? minParts = null,
        DatetimeParts? maxParts = null)
    {
        var copy = refParts;

        if (yearValues is not null && refParts.Year is { } year)
        {
            var allowed = yearValues
                .Where(y => !(minParts is { } min && y < min.Year) && !(maxParts is { } max && y > max.Year))
                .ToList();
            copy = copy with { Year = FindClosestValue(year, allowed) };
        }

        if (monthValues is not null && refParts.Month is { } month)
        {
            var allowed = monthValues
                .Where(m => !(minParts is { } min && copy.Year == min.Year && m < min.Month)
                            && !(maxParts is { } max && copy.Year == max.Year && m > max.Month))
                .ToList();
            copy = copy with { Month = FindClosestValue(month, allowed) };
        }

        if (refParts.Day is { } day && dayValues is not null)
        {
            var allowed = dayValues
                .Where(d => !(minParts is { } min && IsBefore(copy with { Day = d }, min))
                            && !(maxParts is { } max && IsAfter(copy with { Day = d }, max)))
                .ToList();
            copy = copy with { Day = FindClosestValue(day, allowed) };
        }

        if (refParts.Hour is { } hour && hourValues is not null)
        {
            var allowed = hourValues
                .Where(h => !(minParts is { Hour: not null } min && IsSameDay(copy, min) && h < min.Hour)
                            && !(maxParts is { Hour: not null } max && IsSameDay(copy, max) && h > max.Hour))
                .ToList();
            var closest = FindClosestValue(hour, allowed);
            copy = copy with { Hour = closest, Ampm = ParseAmPm(closest) };
        }

        if (refParts.Minute is { } minute && minuteValues is not null)
        {
            var allowed = minuteValues
                .Where(m => !(minParts is { Minute: not null } min && IsSameDay(copy, min) && copy.Hour == min.Hour && m < min.Minute)
                            && !(maxParts is { Minute: not null } max && IsSameDay(copy, max) && copy.Hour == max.Hour && m > max.Minute))
                .ToList();
            copy = copy with { Minute = FindClosestValue(minute, allowed) };
        }

        return copy;
    }

    /// <summary>
    /// The value in the ascending <paramref name="values"/> closest to <paramref name="reference"/>;
    /// the smaller one on a tie. Null when there is none (Ionic yields <c>undefined</c>).
    /// </summary>
    private static int? FindClosestValue(int reference, IReadOnlyList<int> values)
    {
        if (values.Count == 0) return null;
        var closest = values[0];
        var rank = Math.Abs(closest - reference);
        for (var i = 1; i < values.Count; i++)
        {
            var valueRank = Math.Abs(values[i] - reference);
            if (valueRank < rank)
            {
                closest = values[i];
                rank = valueRank;
            }
        }
        return closest;
    }

    // ---- parse.ts ----------------------------------------------------------------------------

    [GeneratedRegex(@"^(\d{4}|[+\-]\d{6})(?:-(\d{2})(?:-(\d{2}))?)?(?:T(\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{3}))?)?(?:(Z)|([+\-])(\d{2})(?::(\d{2}))?)?)?$")]
    private static partial Regex Iso8601Regex();

    [GeneratedRegex(@"^((\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{3}))?)?(?:(Z)|([+\-])(\d{2})(?::(\d{2}))?)?)?$")]
    private static partial Regex TimeRegex();

    /// <summary>
    /// Parses Ionic's <c>*Values</c> input — a comma-separated list such as <c>"0,15,30,45"</c>
    /// (brackets and whitespace are ignored) — into numbers. Null when no list is given.
    /// </summary>
    public static List<int>? ConvertToArrayOfNumbers(string? input)
    {
        if (input is null) return null;
        var cleaned = input.Replace("[", "").Replace("]", "");
        cleaned = string.Concat(cleaned.Where(c => !char.IsWhiteSpace(c)));
        var values = new List<int>();
        foreach (var piece in cleaned.Split(','))
        {
            // parseInt(num, 10): leading digits count, the rest is ignored.
            var digits = piece.Length > 0 && (piece[0] == '-' || piece[0] == '+') ? 1 : 0;
            while (digits < piece.Length && char.IsAsciiDigit(piece[digits])) digits++;
            if (int.TryParse(piece.AsSpan(0, digits), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
                values.Add(value);
        }
        return values;
    }

    /// <summary>
    /// Parses an ISO 8601 string into parts without any time-zone conversion: <c>"2026"</c>,
    /// <c>"2026-07-15"</c>, <c>"2026-07-15T13:47:20Z"</c>, or a time alone (<c>"13:47"</c>).
    /// Seconds and the zone are ignored. Null when the string does not parse.
    /// </summary>
    public static DatetimeParts? ParseDate(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        int? year = null, month = null, day = null, hour = null, minute = null;
        var time = TimeRegex().Match(value);
        if (time.Success)
        {
            hour = ParseGroup(time.Groups[2]);
            minute = ParseGroup(time.Groups[3]);
        }
        else
        {
            var iso = Iso8601Regex().Match(value);
            if (!iso.Success) return null;
            year = ParseGroup(iso.Groups[1]);
            month = ParseGroup(iso.Groups[2]);
            day = ParseGroup(iso.Groups[3]);
            hour = ParseGroup(iso.Groups[4]);
            minute = ParseGroup(iso.Groups[5]);
        }

        // parse[4] < 12 ? 'am' : 'pm' — undefined < 12 is false, so a value without a time is "pm".
        return new DatetimeParts(year, month, day, hour, minute, hour < 12 ? "am" : "pm");
    }

    /// <summary>Parses every value; null when any of them does not parse.</summary>
    public static List<DatetimeParts>? ParseDates(IEnumerable<string> values)
    {
        var parsed = new List<DatetimeParts>();
        foreach (var value in values)
        {
            if (ParseDate(value) is not { } parts) return null;
            parsed.Add(parts);
        }
        return parsed;
    }

    private static int? ParseGroup(Group group)
        => group.Success && int.TryParse(group.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v) ? v : null;

    /// <summary>The min when the date is before it, the max when after it, else the date.</summary>
    public static DatetimeParts ClampDate(DatetimeParts dateParts, DatetimeParts? minParts, DatetimeParts? maxParts)
    {
        if (minParts is { } min && IsBefore(dateParts, min)) return min;
        if (maxParts is { } max && IsAfter(dateParts, max)) return max;
        return dateParts;
    }

    /// <summary><c>"pm"</c> from noon on, else <c>"am"</c>.</summary>
    public static string ParseAmPm(int? hour) => hour >= 12 ? "pm" : "am";

    /// <summary>
    /// Parses a <c>max</c> value, filling what it leaves out with the latest value: <c>"2012"</c>
    /// becomes December 31, 2012 at 23:59; a time alone uses the current year.
    /// </summary>
    public static DatetimeParts? ParseMaxParts(string? max, DatetimeParts todayParts)
    {
        if (ParseDate(max) is not { } result) return null;
        var year = result.Year ?? todayParts.Year ?? 0;
        var month = result.Month ?? 12;
        return new DatetimeParts(year, month, result.Day ?? GetNumDaysInMonth(month, year), result.Hour ?? 23, result.Minute ?? 59);
    }

    /// <summary>
    /// Parses a <c>min</c> value, filling what it leaves out with the earliest value: <c>"2012"</c>
    /// becomes January 1, 2012 at 00:00; a time alone uses the current year.
    /// </summary>
    public static DatetimeParts? ParseMinParts(string? min, DatetimeParts todayParts)
    {
        if (ParseDate(min) is not { } result) return null;
        return new DatetimeParts(result.Year ?? todayParts.Year, result.Month ?? 1, result.Day ?? 1, result.Hour ?? 0, result.Minute ?? 0);
    }

    // ---- state.ts ----------------------------------------------------------------------------

    public static bool IsYearDisabled(int? refYear, DatetimeParts? minParts, DatetimeParts? maxParts)
        => (minParts is { } min && min.Year > refYear) || (maxParts is { } max && max.Year < refYear);

    /// <summary>
    /// True when a day must not be interactive: a filler cell, a day missing from
    /// <paramref name="dayValues"/>, or a day outside the min/max range.
    /// </summary>
    public static bool IsDayDisabled(DatetimeParts refParts, DatetimeParts? minParts, DatetimeParts? maxParts, IReadOnlyList<int>? dayValues = null)
    {
        if (refParts.Day is not { } day) return true;
        if (dayValues is not null && !dayValues.Contains(day)) return true;
        if (minParts is { } min && IsBefore(refParts, min)) return true;
        if (maxParts is { } max && IsAfter(refParts, max)) return true;
        return false;
    }

    /// <summary>True when the month is outside the min/max range.</summary>
    public static bool IsMonthDisabled(DatetimeParts refParts, DatetimeParts? minParts, DatetimeParts? maxParts)
    {
        if (IsYearDisabled(refParts.Year, minParts, maxParts)) return true;
        return (minParts is { } min && IsBefore(refParts, min)) || (maxParts is { } max && IsAfter(refParts, max));
    }

    /// <summary>True when the previous-month button must be disabled.</summary>
    public static bool IsPrevMonthDisabled(DatetimeParts refParts, DatetimeParts? minParts = null, DatetimeParts? maxParts = null)
        => IsMonthDisabled(GetPreviousMonth(refParts) with { Day = null }, minParts, maxParts);

    /// <summary>True when the next-month button must be disabled.</summary>
    public static bool IsNextMonthDisabled(DatetimeParts refParts, DatetimeParts? maxParts)
        => IsMonthDisabled(GetNextMonth(refParts) with { Day = null }, null, maxParts);

    /// <summary>
    /// The custom colors of a highlighted day: the entry of <paramref name="highlights"/> for the
    /// date part of <paramref name="dateIso"/>, else what <paramref name="callback"/> returns.
    /// A callback that throws is ignored, as in Ionic, so it cannot break the calendar.
    /// </summary>
    public static DatetimeHighlightStyle? GetHighlightStyles(
        IReadOnlyList<DatetimeHighlight>? highlights,
        Func<string, DatetimeHighlightStyle?>? callback,
        string dateIso)
    {
        if (highlights is not null)
        {
            var date = dateIso.Split('T')[0];
            foreach (var highlight in highlights)
            {
                if (highlight.Date == date)
                    return new DatetimeHighlightStyle { TextColor = highlight.TextColor, BackgroundColor = highlight.BackgroundColor, Border = highlight.Border };
            }
            return null;
        }

        if (callback is null) return null;
        try
        {
            return callback(dateIso);
        }
        catch (Exception e)
        {
            DatetimeLog.Error("Exception thrown from provided `HighlightedDatesCallback`. Please check your function and try again.", e);
            return null;
        }
    }
}

/// <summary>Ionic's <c>printIonWarning</c> / <c>printIonError</c> for the datetime.</summary>
internal static class DatetimeLog
{
    public static void Warning(string message)
        => System.Diagnostics.Debug.WriteLine($"[ion-datetime] - {message}");

    public static void Error(string message, Exception? exception = null)
        => System.Diagnostics.Debug.WriteLine($"[ion-datetime] - {message}{(exception is null ? "" : " " + exception)}");
}
