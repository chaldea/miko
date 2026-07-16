using System.Globalization;

namespace Miko.Ionic.Components;

/// <summary>
/// A single cell in the datetime calendar month grid. Mirrors the day buttons Ionic builds in
/// <c>renderMonth</c> via <c>getDaysOfMonth</c>: leading blanks (padding) before the first-of-month,
/// then the numbered days, each flagged active/today.
/// </summary>
public readonly struct IonCalendarDay
{
    /// <summary>The day-of-month number, or null for a leading padding cell.</summary>
    public int? Day { get; }

    /// <summary>True when this day is the selected (active) date.</summary>
    public bool IsActive { get; }

    /// <summary>True when this day is today.</summary>
    public bool IsToday { get; }

    /// <summary>True for a leading blank grid cell before the first-of-month.</summary>
    public bool IsPadding => Day is null;

    public IonCalendarDay(int? day, bool isActive, bool isToday)
    {
        Day = day;
        IsActive = isActive;
        IsToday = isToday;
    }

    /// <summary>
    /// Builds the month grid for <paramref name="year"/>/<paramref name="month"/> (1-based month),
    /// mirroring Ionic's <c>getDaysOfMonth</c>: leading padding cells to align the first day under
    /// its weekday column (respecting <paramref name="firstDayOfWeek"/>, 0 = Sunday), then the days.
    /// <paramref name="activeDate"/> flags the selected day and <paramref name="today"/> flags today.
    /// </summary>
    public static IReadOnlyList<IonCalendarDay> BuildMonth(
        int year, int month, int firstDayOfWeek, DateTime? activeDate, DateTime today)
    {
        var days = new List<IonCalendarDay>();
        var firstOfMonth = new DateTime(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);

        // Leading blanks so the 1st sits under the correct weekday column.
        int firstWeekday = (int)firstOfMonth.DayOfWeek;             // 0 = Sunday
        int offset = (firstWeekday - firstDayOfWeek + 7) % 7;
        for (int i = 0; i < offset; i++)
        {
            days.Add(new IonCalendarDay(null, false, false));
        }

        for (int day = 1; day <= daysInMonth; day++)
        {
            bool isActive = activeDate is { } a && a.Year == year && a.Month == month && a.Day == day;
            bool isToday = today.Year == year && today.Month == month && today.Day == day;
            days.Add(new IonCalendarDay(day, isActive, isToday));
        }

        return days;
    }

    /// <summary>The localized short weekday labels starting at <paramref name="firstDayOfWeek"/>
    /// (0 = Sunday), mirroring Ionic's <c>getDaysOfWeek</c> (e.g. S M T W T F S).</summary>
    public static IReadOnlyList<string> DaysOfWeek(int firstDayOfWeek, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.InvariantCulture;
        var names = culture.DateTimeFormat.AbbreviatedDayNames; // index 0 = Sunday
        var result = new List<string>(7);
        for (int i = 0; i < 7; i++)
        {
            result.Add(names[(firstDayOfWeek + i) % 7]);
        }
        return result;
    }
}
