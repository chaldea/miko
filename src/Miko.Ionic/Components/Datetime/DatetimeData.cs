using System.Globalization;
using static Miko.Ionic.Components.DatetimeUtils;

namespace Miko.Ionic.Components;

/// <summary>A cell of the month grid (Ionic <c>getDaysOfMonth</c>): a day of the month, a day of
/// the previous / next month (<see cref="IsAdjacentDay"/>), or a filler (<see cref="Day"/> null).
/// <see cref="DayOfWeek"/> is the cell's column, counted from the first day of the week.</summary>
internal readonly record struct CalendarDayCell(int? Day, int? DayOfWeek, bool IsAdjacentDay);

/// <summary>What a calendar day button shows (Ionic <c>getCalendarDayState</c>, without the aria
/// attributes Miko elements do not have).</summary>
internal readonly record struct CalendarDayState(bool Disabled, bool IsActive, bool IsToday, string? Text);

/// <summary>The data of the three time columns (Ionic <c>getTimeColumnsData</c>).</summary>
internal sealed record TimeColumnsData(
    List<WheelColumnOption> HoursData, List<WheelColumnOption> MinutesData, List<WheelColumnOption> DayPeriodData);

/// <summary>Allowed hours and minutes for a day (Ionic <c>generateTime</c>).</summary>
internal sealed record GeneratedTime(List<int> Hours, List<int> Minutes, bool Am, bool Pm);

/// <summary>
/// Ionic's <c>datetime/utils/data.ts</c> plus the rest of format.ts and the calendar-day state
/// of state.ts: the data each part of the datetime renders.
/// </summary>
internal static class DatetimeData
{
    private static readonly int[] Hour11 = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
    private static readonly int[] Hour12 = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
    private static readonly int[] Hour23 = Enumerable.Range(0, 24).ToArray();
    private static readonly int[] Hour24 = [.. Enumerable.Range(1, 23), 0];
    private static readonly int[] Minutes = Enumerable.Range(0, 60).ToArray();

    /// <summary>Now, in the user's time zone, as parts (Ionic <c>getToday</c> + <c>parseDate</c>).</summary>
    public static DatetimeParts GetToday(TimeProvider time)
    {
        var now = time.GetLocalNow();
        return new DatetimeParts(now.Year, now.Month, now.Day, now.Hour, now.Minute, ParseAmPm(now.Hour));
    }

    /// <summary>
    /// The cells of a month grid (Ionic <c>getDaysOfMonth</c>): fillers before the first so it
    /// sits under its weekday, then the days. With <paramref name="showAdjacentDays"/> the fillers
    /// are the previous month's last days and the next month's first days pad the grid to six
    /// weeks (42 cells).
    /// </summary>
    public static List<CalendarDayCell> GetDaysOfMonth(int month, int year, int firstDayOfWeek, bool showAdjacentDays = false)
    {
        var numDays = GetNumDaysInMonth(month, year);
        var previousNumDays = month == 1 ? GetNumDaysInMonth(12, year - 1) : GetNumDaysInMonth(month - 1, year);
        var firstOfMonth = (int)new DateTime(Math.Clamp(year, 1, 9999), month, 1).DayOfWeek;

        // One filler per offset step, inclusive: offset -1 means the first is in column zero.
        var offset = firstOfMonth >= firstDayOfWeek
            ? firstOfMonth - (firstDayOfWeek + 1)
            : 6 - (firstDayOfWeek - firstOfMonth);

        var days = new List<CalendarDayCell>(42);
        if (showAdjacentDays)
        {
            for (var i = offset; i >= 0; i--)
                days.Add(new CalendarDayCell(previousNumDays - i, (previousNumDays - i) % 7, true));
        }
        else
        {
            for (var i = 0; i <= offset; i++)
                days.Add(new CalendarDayCell(null, null, false));
        }

        for (var i = 1; i <= numDays; i++)
            days.Add(new CalendarDayCell(i, (offset + i) % 7, false));

        if (showAdjacentDays)
        {
            var positiveOffset = 41 - (numDays + offset);
            for (var i = 0; i < positiveOffset; i++)
                days.Add(new CalendarDayCell(i + 1, (numDays + offset + i) % 7, true));
        }

        return days;
    }

    private static int[] GetHourData(string hourCycle) => hourCycle switch
    {
        "h11" => Hour11,
        "h12" => Hour12,
        "h23" => Hour23,
        "h24" => Hour24,
        _ => throw new ArgumentException($"Invalid hour cycle \"{hourCycle}\"", nameof(hourCycle)),
    };

    /// <summary>
    /// The hours and minutes allowed on the day of <paramref name="refParts"/> (Ionic
    /// <c>generateTime</c>): filtered by the <c>*Values</c> lists, and by min / max on their own
    /// day; none at all on a day outside the range.
    /// </summary>
    public static GeneratedTime GenerateTime(
        string hourCycle,
        DatetimeParts refParts,
        DatetimeParts? minParts = null,
        DatetimeParts? maxParts = null,
        IReadOnlyList<int>? hourValues = null,
        IReadOnlyList<int>? minuteValues = null)
    {
        var use24Hour = Is24Hour(hourCycle);
        IEnumerable<int> hours = GetHourData(hourCycle);
        IEnumerable<int> minutes = Minutes;
        var amAllowed = true;
        var pmAllowed = true;

        if (hourValues is not null) hours = hours.Where(hourValues.Contains);
        if (minuteValues is not null) minutes = minutes.Where(minuteValues.Contains);

        if (minParts is { } min)
        {
            if (IsSameDay(refParts, min))
            {
                if (min.Hour is { } minHour)
                {
                    hours = hours.Where(hour =>
                    {
                        var converted = refParts.Ampm == "pm" ? (hour + 12) % 24 : hour;
                        return (use24Hour ? hour : converted) >= minHour;
                    });
                    amAllowed = minHour < 13;
                }
                if (min.Minute is { } minMinute)
                {
                    // Past the min hour every minute is allowed: with a min of 09:30, 10:00 is fine.
                    var isPastMinHour = min.Hour is not null && refParts.Hour is not null && refParts.Hour > min.Hour;
                    minutes = minutes.Where(minute => isPastMinHour || minute >= minMinute);
                }
            }
            else if (IsBefore(refParts, min))
            {
                hours = [];
                minutes = [];
                amAllowed = pmAllowed = false;
            }
        }

        if (maxParts is { } max)
        {
            if (IsSameDay(refParts, max))
            {
                if (max.Hour is { } maxHour)
                {
                    hours = hours.Where(hour =>
                    {
                        var converted = refParts.Ampm == "pm" ? (hour + 12) % 24 : hour;
                        return (use24Hour ? hour : converted) <= maxHour;
                    });
                    pmAllowed = maxHour >= 12;
                }
                // Only the max hour itself limits the minutes: with a max of 10:30, 09:45 is fine.
                if (max.Minute is { } maxMinute && refParts.Hour == max.Hour)
                    minutes = minutes.Where(minute => minute <= maxMinute);
            }
            else if (IsAfter(refParts, max))
            {
                hours = [];
                minutes = [];
                amAllowed = pmAllowed = false;
            }
        }

        return new GeneratedTime(hours.ToList(), minutes.ToList(), amAllowed, pmAllowed);
    }

    /// <summary>The previous, current and next month (Ionic <c>generateMonths</c>). A forced date
    /// in another month replaces the neighbor on its side.</summary>
    public static List<DatetimeParts> GenerateMonths(DatetimeParts refParts, DatetimeParts? forcedDate = null)
    {
        var current = new DatetimeParts(refParts.Year, refParts.Month, refParts.Day);
        if (forcedDate is { } forced && (refParts.Month != forced.Month || refParts.Year != forced.Year))
        {
            var forcedMonth = new DatetimeParts(forced.Year, forced.Month, forced.Day);
            return IsBefore(forcedMonth, current)
                ? [forcedMonth, current, GetNextMonth(refParts)]
                : [GetPreviousMonth(refParts), current, forcedMonth];
        }
        return [GetPreviousMonth(refParts), current, GetNextMonth(refParts)];
    }

    /// <summary>The month wheel's options for the year of <paramref name="refParts"/> (Ionic
    /// <c>getMonthColumnData</c>).</summary>
    public static List<WheelColumnOption> GetMonthColumnData(
        DatetimeLocale locale,
        DatetimeParts refParts,
        DatetimeParts? minParts = null,
        DatetimeParts? maxParts = null,
        IReadOnlyList<int>? monthValues = null,
        DatetimeFormat? format = null)
    {
        format ??= DatetimeLocale.MonthFormat;
        var year = refParts.Year;
        var months = new List<WheelColumnOption>();

        IEnumerable<int> processed;
        if (monthValues is not null)
        {
            processed = monthValues;
            if (maxParts?.Month is { } maxMonth) processed = processed.Where(m => m <= maxMonth);
            if (minParts?.Month is { } minMonth) processed = processed.Where(m => m >= minMonth);
        }
        else
        {
            var maxMonth = maxParts is { } max && max.Year == year ? max.Month ?? 12 : 12;
            var minMonth = minParts is { } min && min.Year == year ? min.Month ?? 1 : 1;
            processed = Enumerable.Range(minMonth, Math.Max(0, maxMonth - minMonth + 1));
        }

        foreach (var month in processed)
            months.Add(new WheelColumnOption(locale.Format(new DatetimeParts(year, month, 1), format), Invariant(month)));
        return months;
    }

    /// <summary>The day wheel's options for the month of <paramref name="refParts"/> (Ionic
    /// <c>getDayColumnData</c>), limited by min / max in that month.</summary>
    public static List<WheelColumnOption> GetDayColumnData(
        DatetimeLocale locale,
        DatetimeParts refParts,
        DatetimeParts? minParts = null,
        DatetimeParts? maxParts = null,
        IReadOnlyList<int>? dayValues = null,
        DatetimeFormat? format = null)
    {
        format ??= DatetimeLocale.DayFormat;
        var month = refParts.Month ?? 1;
        var year = refParts.Year ?? 2023;
        var numDaysInMonth = GetNumDaysInMonth(month, year);
        var maxDay = maxParts is { Day: { } md } max && max.Year == year && max.Month == month ? md : numDaysInMonth;
        var minDay = minParts is { Day: { } nd } min && min.Year == year && min.Month == month ? nd : 1;

        IEnumerable<int> processed = dayValues is not null
            ? dayValues.Where(day => day >= minDay && day <= maxDay)
            : Enumerable.Range(minDay, Math.Max(0, maxDay - minDay + 1));

        // Days past the end of the month (31 in dayValues for February) are above maxDay.
        return processed
            .Select(day => new WheelColumnOption(locale.Format(new DatetimeParts(year, month, day), format), Invariant(day)))
            .ToList();
    }

    /// <summary>The year wheel's options (Ionic <c>getYearColumnData</c>): <paramref name="yearValues"/>
    /// within min / max, else from min (or 100 years back) to max (or this year).</summary>
    public static List<WheelColumnOption> GetYearColumnData(
        DatetimeLocale locale,
        DatetimeParts refParts,
        DatetimeParts? minParts = null,
        DatetimeParts? maxParts = null,
        IReadOnlyList<int>? yearValues = null)
    {
        IEnumerable<int> processed;
        if (yearValues is not null)
        {
            processed = yearValues;
            if (maxParts?.Year is { } maxYear) processed = processed.Where(y => y <= maxYear);
            if (minParts?.Year is { } minYear) processed = processed.Where(y => y >= minYear);
        }
        else
        {
            var year = refParts.Year ?? 2023;
            var maxYear = maxParts?.Year ?? year;
            var minYear = minParts?.Year ?? year - 100;
            processed = Enumerable.Range(minYear, Math.Max(0, maxYear - minYear + 1));
        }

        return processed
            .Select(year => new WheelColumnOption(
                locale.GetYear(new DatetimeParts(year, refParts.Month, refParts.Day)), Invariant(year)))
            .ToList();
    }

    /// <summary>Every month from <paramref name="current"/> to <paramref name="max"/>, inclusive
    /// (Ionic <c>getAllMonthsInRange</c>).</summary>
    private static List<DatetimeParts> GetAllMonthsInRange(DatetimeParts current, DatetimeParts max)
    {
        var months = new List<DatetimeParts>();
        // Ionic recurses until the months meet; a min after the max would never end, so stop there.
        while (months.Count < 12 * 200)
        {
            months.Add(current);
            if ((current.Month == max.Month && current.Year == max.Year) || IsAfter(current with { Day = null }, max with { Day = null }))
                break;
            current = GetNextMonth(current);
        }
        return months;
    }

    /// <summary>
    /// The combined date wheel of <c>date-time</c> / <c>time-date</c> with <c>PreferWheel</c>
    /// (Ionic <c>getCombinedDateColumnData</c>): one option per day from <paramref name="minParts"/>
    /// to <paramref name="maxParts"/>, written <c>Thu, Jun 2</c>, today as <c>Today</c>. Option
    /// values are <c>year-month-day</c> without padding, as in Ionic.
    /// </summary>
    public static (List<DatetimeParts> Parts, List<WheelColumnOption> Items) GetCombinedDateColumnData(
        DatetimeLocale locale,
        DatetimeParts todayParts,
        DatetimeParts minParts,
        DatetimeParts maxParts,
        IReadOnlyList<int>? dayValues = null,
        IReadOnlyList<int>? monthValues = null)
    {
        var parts = new List<DatetimeParts>();
        var items = new List<WheelColumnOption>();

        var months = GetAllMonthsInRange(minParts, maxParts);
        if (monthValues is not null) months = months.Where(m => m.Month is { } mm && monthValues.Contains(mm)).ToList();

        foreach (var monthObject in months)
        {
            var referenceMonth = new DatetimeParts(monthObject.Year, monthObject.Month, null);
            foreach (var dayObject in GetDayColumnData(locale, referenceMonth, minParts, maxParts, dayValues, DatetimeLocale.CombinedDateFormat))
            {
                var day = int.Parse(dayObject.Value, CultureInfo.InvariantCulture);
                var isToday = IsSameDay(referenceMonth with { Day = day }, todayParts);
                items.Add(new WheelColumnOption(
                    isToday ? locale.GetTodayLabel() : dayObject.Text,
                    $"{referenceMonth.Year}-{referenceMonth.Month}-{day}"));
                parts.Add(new DatetimeParts(referenceMonth.Year, referenceMonth.Month, day));
            }
        }

        return (parts, items);
    }

    /// <summary>
    /// The hour, minute and day-period wheels (Ionic <c>getTimeColumnsData</c>). Hour options
    /// carry the 24-hour value for the current day period; the day period column is empty for a
    /// 24-hour cycle.
    /// </summary>
    public static TimeColumnsData GetTimeColumnsData(
        DatetimeLocale locale,
        DatetimeParts refParts,
        string? hourCycle = null,
        DatetimeParts? minParts = null,
        DatetimeParts? maxParts = null,
        IReadOnlyList<int>? allowedHourValues = null,
        IReadOnlyList<int>? allowedMinuteValues = null)
    {
        var computedHourCycle = locale.GetHourCycle(hourCycle);
        var use24Hour = Is24Hour(computedHourCycle);
        var time = GenerateTime(computedHourCycle, refParts, minParts, maxParts, allowedHourValues, allowedMinuteValues);

        var hours = time.Hours
            .Select(hour => new WheelColumnOption(
                GetFormattedHour(hour, computedHourCycle),
                Invariant(GetInternalHourValue(hour, use24Hour, refParts.Ampm))))
            .ToList();
        var minutes = time.Minutes
            .Select(minute => new WheelColumnOption(AddTimePadding(minute), Invariant(minute)))
            .ToList();

        var dayPeriods = new List<WheelColumnOption>();
        if (time.Am && !use24Hour) dayPeriods.Add(new WheelColumnOption(locale.GetLocalizedDayPeriod("am"), "am"));
        if (time.Pm && !use24Hour) dayPeriods.Add(new WheelColumnOption(locale.GetLocalizedDayPeriod("pm"), "pm"));

        return new TimeColumnsData(hours, minutes, dayPeriods);
    }

    /// <summary>
    /// The state of a calendar day button (Ionic <c>getCalendarDayState</c>): whether it is
    /// selected (any of <paramref name="activeParts"/>), today, disabled, and its text.
    /// </summary>
    public static CalendarDayState GetCalendarDayState(
        DatetimeLocale locale,
        DatetimeParts refParts,
        IReadOnlyList<DatetimeParts> activeParts,
        DatetimeParts todayParts,
        DatetimeParts? minParts = null,
        DatetimeParts? maxParts = null,
        IReadOnlyList<int>? dayValues = null)
    {
        var isActive = activeParts.Any(parts => IsSameDay(refParts, parts));
        var isToday = IsSameDay(refParts, todayParts);
        var disabled = IsDayDisabled(refParts, minParts, maxParts, dayValues);

        return new CalendarDayState(disabled, isActive, isToday, refParts.Day is not null ? locale.GetDay(refParts) : null);
    }

    // ---- format.ts (locale independent) -------------------------------------------------------

    /// <summary>Pads a time value to two digits (Ionic <c>addTimePadding</c>).</summary>
    public static string AddTimePadding(int value)
    {
        var text = value.ToString(CultureInfo.InvariantCulture);
        return text.Length > 1 ? text : "0" + text;
    }

    /// <summary>
    /// The text of a wheel hour (Ionic <c>getFormattedHour</c>): two digits for 24-hour cycles,
    /// and midnight as 0 (h11), 12 (h12), 00 (h23) or 24 (h24).
    /// </summary>
    public static string GetFormattedHour(int hour, string hourCycle)
    {
        if (hour == 0)
        {
            return hourCycle switch
            {
                "h11" => "0",
                "h12" => "12",
                "h23" => "00",
                "h24" => "24",
                _ => throw new ArgumentException($"Invalid hour cycle \"{hourCycle}\"", nameof(hourCycle)),
            };
        }
        return Is24Hour(hourCycle) ? AddTimePadding(hour) : hour.ToString(CultureInfo.InvariantCulture);
    }

    private static string Invariant(int value) => value.ToString(CultureInfo.InvariantCulture);
}
