using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// The data the datetime renders (Ionic datetime/test/data.spec.ts and format.spec.ts, plus the
/// month grid of <c>getDaysOfMonth</c>), and the locale layer that replaces <c>Intl</c>.
/// </summary>
public class IonDatetimeDataTests
{
    private static readonly DatetimeLocale EnUs = DatetimeLocale.Get("en-US");

    private static DatetimeParts D(int? year, int? month, int? day) => new(year, month, day);

    private static DatetimeParts T(int? year, int? month, int? day, int? hour, int? minute, string? ampm = null)
        => new(year, month, day, hour, minute, ampm);

    // ---- getTimeColumnsData ------------------------------------------------------------------

    [Fact]
    public void Should_generate_h12_hours_and_day_periods()
    {
        var data = DatetimeData.GetTimeColumnsData(EnUs, D(2021, 5, 1) with { Hour = 4, Minute = 30 }, "h12");

        data.HoursData.Select(h => (h.Text, h.Value)).ShouldBe(
            [("12", "0"), ("1", "1"), ("2", "2"), ("3", "3"), ("4", "4"), ("5", "5"),
             ("6", "6"), ("7", "7"), ("8", "8"), ("9", "9"), ("10", "10"), ("11", "11")]);
        data.DayPeriodData.Select(p => (p.Text, p.Value)).ShouldBe([("AM", "am"), ("PM", "pm")]);
        data.MinutesData.Count.ShouldBe(60);
        data.MinutesData[5].Text.ShouldBe("05");
    }

    [Fact]
    public void Should_generate_h23_hours_without_day_periods()
    {
        var data = DatetimeData.GetTimeColumnsData(EnUs, D(2021, 5, 1) with { Hour = 4, Minute = 30 }, "h23");

        data.HoursData.Select(h => h.Text).ShouldBe(Enumerable.Range(0, 24).Select(h => h.ToString("00")));
        data.HoursData.Select(h => h.Value).ShouldBe(Enumerable.Range(0, 24).Select(h => h.ToString()));
        data.DayPeriodData.ShouldBeEmpty();
    }

    [Fact]
    public void Should_generate_h11_and_h24_hours()
    {
        var h11 = DatetimeData.GetTimeColumnsData(EnUs, D(2021, 5, 1) with { Hour = 4, Minute = 30 }, "h11");
        h11.HoursData.Select(h => h.Text).ShouldBe(Enumerable.Range(0, 12).Select(h => h.ToString()));
        h11.DayPeriodData.Count.ShouldBe(2);

        var h24 = DatetimeData.GetTimeColumnsData(EnUs, D(2021, 5, 1) with { Hour = 4, Minute = 30 }, "h24");
        h24.HoursData.First().ShouldBe(new WheelColumnOption("01", "1"));
        h24.HoursData.Last().ShouldBe(new WheelColumnOption("24", "0"));
        h24.DayPeriodData.ShouldBeEmpty();
    }

    [Fact]
    public void Should_give_pm_hours_their_24_hour_values()
    {
        var data = DatetimeData.GetTimeColumnsData(EnUs, T(2021, 5, 1, 16, 30, "pm"), "h12");

        data.HoursData[0].ShouldBe(new WheelColumnOption("12", "12"));
        data.HoursData[1].ShouldBe(new WheelColumnOption("1", "13"));
    }

    // ---- generateTime ------------------------------------------------------------------------

    [Fact]
    public void Should_filter_times_by_min_and_max_on_their_day()
    {
        var today = T(2021, 5, 19, 5, 43);

        var all = DatetimeData.GenerateTime("h12", today);
        all.Hours.Count.ShouldBe(12);
        all.Minutes.Count.ShouldBe(60);

        var min = DatetimeData.GenerateTime("h12", today, minParts: T(2021, 5, 19, 2, 40));
        min.Hours.Count.ShouldBe(10);
        min.Minutes.Count.ShouldBe(60);

        DatetimeData.GenerateTime("h12", T(2021, 5, 20, 5, 43), minParts: T(2021, 5, 19, 2, 40)).Hours.Count.ShouldBe(12);

        var max = DatetimeData.GenerateTime("h12", T(2021, 5, 19, 7, 43), maxParts: T(2021, 5, 19, 7, 44));
        max.Hours.Count.ShouldBe(8);
        max.Minutes.Count.ShouldBe(45);

        DatetimeData.GenerateTime("h12", T(2021, 5, 20, 5, 43), maxParts: T(2021, 5, 21, 2, 40)).Hours.Count.ShouldBe(12);
    }

    [Fact]
    public void Should_offer_no_time_on_a_day_outside_the_range()
    {
        var beforeMin = DatetimeData.GenerateTime("h12", T(2021, 5, 20, 5, 43), minParts: T(2021, 5, 21, 2, 40));
        beforeMin.Hours.ShouldBeEmpty();
        beforeMin.Minutes.ShouldBeEmpty();

        var afterMax = DatetimeData.GenerateTime("h12", T(2021, 5, 22, 5, 43), maxParts: T(2021, 5, 21, 2, 40));
        afterMax.Hours.ShouldBeEmpty();
        afterMax.Minutes.ShouldBeEmpty();
    }

    [Fact]
    public void Should_apply_hour_and_minute_values_and_both_day_periods()
    {
        var today = T(2021, 5, 22, 5, 43);
        DatetimeData.GenerateTime("h12", today, minParts: D(2021, 5, 22), maxParts: D(2021, 5, 22)).Hours.Count.ShouldBe(12);

        var values = DatetimeData.GenerateTime("h12", today, hourValues: [1, 2, 3], minuteValues: [10, 15, 20]);
        values.Hours.ShouldBe([1, 2, 3]);
        values.Minutes.ShouldBe([10, 15, 20]);

        var both = DatetimeData.GenerateTime("h12", today, T(2021, 5, 22, 11, 14), T(2021, 5, 22, 12, 14));
        both.Am.ShouldBeTrue();
        both.Pm.ShouldBeTrue();
    }

    [Fact]
    public void Should_filter_24_hour_times_of_a_time_only_value()
    {
        DatetimeData.GenerateTime("h23", T(null, null, null, 19, 50), minParts: T(null, null, null, 19, 50))
            .Hours.ShouldBe([19, 20, 21, 22, 23]);

        var pastMinHour = DatetimeData.GenerateTime("h23", T(null, null, null, 20, 22), minParts: T(null, null, null, 19, 30));
        pastMinHour.Hours.ShouldBe([19, 20, 21, 22, 23]);
        pastMinHour.Minutes.Count.ShouldBe(60);

        DatetimeData.GenerateTime("h23", T(null, null, null, 20, 30), T(null, null, null, 19, 30), T(null, null, null, 20, 40))
            .Hours.ShouldBe([19, 20]);
        DatetimeData.GenerateTime("h23", T(null, null, null, 13, 0), maxParts: T(null, null, null, 13, 2))
            .Minutes.ShouldBe([0, 1, 2]);
        DatetimeData.GenerateTime("h23", T(null, null, null, 12, 0), maxParts: T(null, null, null, 13, 2))
            .Minutes.Count.ShouldBe(60);
    }

    // ---- generateMonths / getDaysOfMonth -----------------------------------------------------

    [Fact]
    public void Should_generate_the_surrounding_months()
    {
        DatetimeData.GenerateMonths(D(2021, 5, 1)).ShouldBe([D(2021, 4, 1), D(2021, 5, 1), D(2021, 6, 1)]);
        DatetimeData.GenerateMonths(D(2021, 5, 1), D(2021, 9, 3)).ShouldBe([D(2021, 4, 1), D(2021, 5, 1), D(2021, 9, 3)]);
    }

    [Fact]
    public void Should_pad_the_month_grid_to_the_first_weekday()
    {
        // 2026-07-01 is a Wednesday: three fillers with Sunday first, two with Monday first.
        var sunday = DatetimeData.GetDaysOfMonth(7, 2026, 0);
        sunday.Count(d => d.Day is null).ShouldBe(3);
        sunday.Count(d => d.Day is not null).ShouldBe(31);
        sunday[3].ShouldBe(new CalendarDayCell(1, 3, false));

        DatetimeData.GetDaysOfMonth(7, 2026, 1).Count(d => d.Day is null).ShouldBe(2);
        // 2026-02-01 is a Sunday: no filler.
        DatetimeData.GetDaysOfMonth(2, 2026, 0)[0].Day.ShouldBe(1);
        // Saturday first: Wednesday is the fifth column.
        DatetimeData.GetDaysOfMonth(7, 2026, 6).Count(d => d.Day is null).ShouldBe(4);
    }

    [Fact]
    public void Should_fill_six_weeks_with_adjacent_days()
    {
        var days = DatetimeData.GetDaysOfMonth(7, 2026, 0, showAdjacentDays: true);

        days.Count.ShouldBe(42);
        days.Take(3).Select(d => d.Day).ShouldBe([28, 29, 30]);
        days.Take(3).ShouldAllBe(d => d.IsAdjacentDay);
        days[3].ShouldBe(new CalendarDayCell(1, 3, false));
        days.Skip(34).Select(d => d.Day).ShouldBe([1, 2, 3, 4, 5, 6, 7, 8]);
        days.Skip(34).ShouldAllBe(d => d.IsAdjacentDay);
    }

    // ---- column data -------------------------------------------------------------------------

    [Fact]
    public void Should_generate_combined_date_items_across_years()
    {
        var (parts, items) = DatetimeData.GetCombinedDateColumnData(EnUs, D(2021, 1, 1), D(2020, 12, 31), D(2021, 1, 2));

        parts.ShouldBe([D(2020, 12, 31), D(2021, 1, 1), D(2021, 1, 2)]);
        items.ShouldBe(
        [
            new WheelColumnOption("Thu, Dec 31", "2020-12-31"),
            new WheelColumnOption("Today", "2021-1-1"),
            new WheelColumnOption("Sat, Jan 2", "2021-1-2"),
        ]);
    }

    [Fact]
    public void Should_limit_month_day_and_year_columns_by_min_and_max()
    {
        var working = D(2026, 7, 15);
        var min = T(2026, 3, 10, 0, 0);
        var max = T(2026, 7, 20, 23, 59);

        DatetimeData.GetMonthColumnData(EnUs, working, min, max).Select(m => m.Text)
            .ShouldBe(["March", "April", "May", "June", "July"]);
        DatetimeData.GetDayColumnData(EnUs, working, min, max).Select(d => d.Value).Last().ShouldBe("20");
        DatetimeData.GetDayColumnData(EnUs, D(2026, 3, 1), min, max).First().Value.ShouldBe("10");
        DatetimeData.GetYearColumnData(EnUs, working, min, max).Select(y => y.Text).ShouldBe(["2026"]);
        DatetimeData.GetYearColumnData(EnUs, working).Count.ShouldBe(101);
        DatetimeData.GetMonthColumnData(EnUs, working, monthValues: [6, 7, 8]).Select(m => m.Value).ShouldBe(["6", "7", "8"]);
        DatetimeData.GetDayColumnData(EnUs, D(2026, 2, 1), dayValues: [1, 15, 30, 31]).Select(d => d.Value).ShouldBe(["1", "15"]);
        DatetimeData.GetYearColumnData(EnUs, working, yearValues: [2024, 2028, 2032], maxParts: T(2030, 1, 1, 0, 0))
            .Select(y => y.Value).ShouldBe(["2024", "2028"]);
    }

    [Fact]
    public void Should_describe_a_calendar_day()
    {
        var a = D(2019, 1, 1);
        var b = D(2021, 1, 1);
        var c = D(2023, 1, 1);

        DatetimeData.GetCalendarDayState(EnUs, a, [b], c).ShouldBe(new CalendarDayState(false, false, false, "1"));
        DatetimeData.GetCalendarDayState(EnUs, a, [a], c).ShouldBe(new CalendarDayState(false, true, false, "1"));
        DatetimeData.GetCalendarDayState(EnUs, a, [b], a).ShouldBe(new CalendarDayState(false, false, true, "1"));
        DatetimeData.GetCalendarDayState(EnUs, a, [a], a, dayValues: [1]).ShouldBe(new CalendarDayState(false, true, true, "1"));
        DatetimeData.GetCalendarDayState(EnUs, a, [a], a, dayValues: [2]).ShouldBe(new CalendarDayState(true, true, true, "1"));
        DatetimeData.GetCalendarDayState(EnUs, D(2019, 1, null), [a], a).ShouldBe(new CalendarDayState(true, false, false, null));
    }

    // ---- format.ts ---------------------------------------------------------------------------

    [Fact]
    public void Should_format_hours_for_each_cycle()
    {
        DatetimeData.GetFormattedHour(1, "h11").ShouldBe("1");
        DatetimeData.GetFormattedHour(1, "h12").ShouldBe("1");
        DatetimeData.GetFormattedHour(1, "h23").ShouldBe("01");
        DatetimeData.GetFormattedHour(1, "h24").ShouldBe("01");
        DatetimeData.GetFormattedHour(0, "h11").ShouldBe("0");
        DatetimeData.GetFormattedHour(0, "h12").ShouldBe("12");
        DatetimeData.GetFormattedHour(0, "h23").ShouldBe("00");
        DatetimeData.GetFormattedHour(0, "h24").ShouldBe("24");

        DatetimeData.AddTimePadding(0).ShouldBe("00");
        DatetimeData.AddTimePadding(9).ShouldBe("09");
        DatetimeData.AddTimePadding(100).ShouldBe("100");
    }
}
