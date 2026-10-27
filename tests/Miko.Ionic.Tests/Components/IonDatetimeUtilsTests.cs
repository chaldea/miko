using Miko.Ionic.Components;
using Shouldly;
using static Miko.Ionic.Components.DatetimeUtils;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// The locale-independent datetime utilities, ported with Ionic's own specs
/// (datetime/test/comparison.spec.ts, helpers.spec.ts, manipulation.spec.ts, parse.spec.ts and
/// state.spec.ts). Expected values are Ionic's.
/// </summary>
public class IonDatetimeUtilsTests
{
    private static DatetimeParts D(int? year, int? month, int? day) => new(year, month, day);

    private static DatetimeParts T(int? year, int? month, int? day, int? hour, int? minute, string? ampm = null)
        => new(year, month, day, hour, minute, ampm);

    // ---- comparison.spec.ts ------------------------------------------------------------------

    [Fact]
    public void Should_compare_same_day()
    {
        var reference = D(2021, 1, 1);
        IsSameDay(reference, D(2021, 1, 1)).ShouldBeTrue();
        IsSameDay(reference, D(2021, 2, 1)).ShouldBeFalse();
        IsSameDay(reference, D(2021, 1, 2)).ShouldBeFalse();
        IsSameDay(reference, D(2022, 1, 1)).ShouldBeFalse();
        IsSameDay(reference, D(0, 0, 0)).ShouldBeFalse();
        IsSameDay(reference, D(null, null, null)).ShouldBeFalse();
    }

    [Fact]
    public void Should_compare_before()
    {
        var reference = D(2021, 1, 1);
        IsBefore(reference, D(2021, 1, 1)).ShouldBeFalse();
        IsBefore(reference, D(2021, 2, 1)).ShouldBeTrue();
        IsBefore(reference, D(2021, 1, 2)).ShouldBeTrue();
        IsBefore(reference, D(2022, 1, 1)).ShouldBeTrue();
        IsBefore(reference, D(2020, 1, 1)).ShouldBeFalse();
        IsBefore(reference, D(0, 0, 0)).ShouldBeFalse();
        IsBefore(reference, D(null, null, null)).ShouldBeFalse();
    }

    [Fact]
    public void Should_compare_after()
    {
        var reference = D(2021, 2, 2);
        IsAfter(reference, D(2021, 2, 2)).ShouldBeFalse();
        IsAfter(reference, D(2021, 2, 1)).ShouldBeTrue();
        IsAfter(reference, D(2021, 1, 2)).ShouldBeTrue();
        IsAfter(reference, D(2020, 1, 1)).ShouldBeTrue();
        IsAfter(reference, D(2022, 1, 1)).ShouldBeFalse();
        IsAfter(reference, D(0, 0, 0)).ShouldBeTrue();
    }

    // ---- helpers.spec.ts ---------------------------------------------------------------------

    [Theory]
    [InlineData(1, 2019, 31)]
    [InlineData(2, 2019, 28)]
    [InlineData(4, 2019, 30)]
    [InlineData(12, 2019, 31)]
    [InlineData(2, 2020, 29)]
    [InlineData(2, 1900, 28)]
    [InlineData(2, 1800, 28)]
    [InlineData(2, 2400, 29)]
    public void Should_count_days_in_month(int month, int year, int expected)
        => GetNumDaysInMonth(month, year).ShouldBe(expected);

    [Theory]
    [InlineData(2096, true)]
    [InlineData(2021, false)]
    [InlineData(2012, true)]
    [InlineData(2000, true)]
    [InlineData(1900, false)]
    [InlineData(1800, false)]
    public void Should_detect_leap_years(int year, bool expected) => IsLeapYear(year).ShouldBe(expected);

    [Theory]
    [InlineData("h11", false)]
    [InlineData("h12", false)]
    [InlineData("h23", true)]
    [InlineData("h24", true)]
    public void Should_tell_24_hour_cycles(string hourCycle, bool expected) => Is24Hour(hourCycle).ShouldBe(expected);

    // ---- manipulation.spec.ts ----------------------------------------------------------------

    [Fact]
    public void Should_get_internal_hour_value()
    {
        GetInternalHourValue(12, true, null).ShouldBe(12);
        GetInternalHourValue(12, false, "am").ShouldBe(0);
        GetInternalHourValue(12, false, "pm").ShouldBe(12);
        GetInternalHourValue(1, true, null).ShouldBe(1);
        GetInternalHourValue(1, false, "am").ShouldBe(1);
        GetInternalHourValue(1, false, "pm").ShouldBe(13);
    }

    [Fact]
    public void Should_switch_the_hour_with_the_day_period()
    {
        CalculateHourFromAmPm(new DatetimeParts { Hour = 12, Ampm = "am" }, "pm").ShouldBe(12);
        for (var hour = 1; hour <= 11; hour++)
            CalculateHourFromAmPm(new DatetimeParts { Hour = hour, Ampm = "am" }, "pm").ShouldBe(hour + 12);
        for (var hour = 13; hour <= 23; hour++)
            CalculateHourFromAmPm(new DatetimeParts { Hour = hour, Ampm = "pm" }, "am").ShouldBe(hour - 12);
        CalculateHourFromAmPm(new DatetimeParts { Hour = 0, Ampm = "pm" }, "am").ShouldBe(12);
    }

    [Fact]
    public void Should_convert_12_hour_to_24_hour()
    {
        Convert12HourTo24Hour(12, "am").ShouldBe(0);
        for (var hour = 1; hour <= 11; hour++) Convert12HourTo24Hour(hour, "am").ShouldBe(hour);
        Convert12HourTo24Hour(12, "pm").ShouldBe(12);
        for (var hour = 1; hour <= 11; hour++) Convert12HourTo24Hour(hour, "pm").ShouldBe(hour + 12);
    }

    [Fact]
    public void Should_move_by_months_clamping_the_day()
    {
        GetNextMonth(D(2021, 5, 31)).ShouldBe(D(2021, 6, 30));
        GetNextMonth(D(2021, 12, 15)).ShouldBe(D(2022, 1, 15));
        GetPreviousMonth(D(2021, 3, 31)).ShouldBe(D(2021, 2, 28));
        GetPreviousMonth(D(2021, 1, 15)).ShouldBe(D(2020, 12, 15));
        GetNextMonth(D(2021, 5, null)).ShouldBe(D(2021, 6, null));
    }

    [Fact]
    public void Should_validate_parts()
    {
        ValidateParts(T(2022, 2, 31, 8, 0), null, null).ShouldBe(T(2022, 2, 28, 8, 0));
        ValidateParts(T(2022, 1, 1, 8, 0), T(2022, 1, 1, 9, 0), null).ShouldBe(T(2022, 1, 1, 9, 0));
        ValidateParts(T(2022, 1, 1, 9, 20), T(2022, 1, 1, 9, 30), null).ShouldBe(T(2022, 1, 1, 9, 30));
        ValidateParts(T(2022, 1, 1, 8, 30), T(2022, 1, 1, 9, 0), null).ShouldBe(T(2022, 1, 1, 9, 0));
        ValidateParts(T(2022, 1, 1, 10, 0), null, T(2022, 1, 1, 9, 0)).ShouldBe(T(2022, 1, 1, 9, 0));
        ValidateParts(T(2022, 1, 1, 9, 40), null, T(2022, 1, 1, 9, 30)).ShouldBe(T(2022, 1, 1, 9, 30));
        ValidateParts(T(2022, 1, 1, 10, 20), null, T(2022, 1, 1, 9, 30)).ShouldBe(T(2022, 1, 1, 9, 30));
    }

    [Fact]
    public void Should_find_the_closest_valid_date_for_a_date()
    {
        GetClosestValidDate(D(2023, 10, 10),
                monthValues: [2, 3, 7, 9, 10],
                dayValues: [4, 15, 25],
                yearValues: [2020, 2021, 2023],
                minParts: D(2021, 4, 10),
                maxParts: D(2021, 9, 14))
            .ShouldBe(D(2021, 9, 4));
    }

    [Fact]
    public void Should_find_the_closest_valid_date_before_the_min()
    {
        GetClosestValidDate(T(2020, 4, 2, 15, 20),
                monthValues: [4, 9, 11],
                dayValues: [11, 12, 13, 14],
                yearValues: [2020, 2021, 2023],
                hourValues: [9, 10, 11],
                minuteValues: [11, 12, 13, 14, 15],
                minParts: T(2021, 9, 10, 10, 10),
                maxParts: T(2021, 9, 14, 10, 11))
            .ShouldBe(T(2021, 9, 11, 11, 15, "am"));

        GetClosestValidDate(T(2020, 4, 2, 15, 20),
                monthValues: [4, 9, 11],
                dayValues: [10, 12, 13, 14],
                yearValues: [2020, 2021, 2023],
                hourValues: [9, 10, 11],
                minuteValues: [11, 12, 13, 14, 15],
                minParts: T(2021, 9, 10, 10, 10),
                maxParts: T(2021, 9, 10, 10, 15))
            .ShouldBe(T(2021, 9, 10, 10, 15, "am"));
    }

    [Fact]
    public void Should_clamp_minutes_only_on_the_min_or_max_hour()
    {
        GetClosestValidDate(T(2020, 4, 2, 9, 16),
                monthValues: [4, 9, 11],
                dayValues: [10, 12, 13, 14],
                yearValues: [2020, 2021, 2023],
                hourValues: [9, 10, 11],
                minuteValues: [10, 15, 16],
                minParts: T(2021, 9, 10, 10, 10),
                maxParts: T(2021, 9, 10, 11, 15))
            .ShouldBe(T(2021, 9, 10, 10, 16, "am"));

        GetClosestValidDate(T(2022, 4, 2, 9, 16),
                monthValues: [4, 9, 11],
                dayValues: [2, 10, 12, 13, 14],
                yearValues: [2020, 2021, 2022, 2023],
                hourValues: [9, 10, 11],
                minuteValues: [10, 15, 16],
                minParts: T(2021, 9, 10, 10, 10),
                maxParts: T(2023, 9, 10, 11, 15))
            .ShouldBe(T(2022, 4, 2, 9, 16, "am"));
    }

    [Fact]
    public void Should_convert_parts_to_iso()
    {
        ConvertDataToIso(D(2026, null, null)).ShouldBe("2026");
        ConvertDataToIso(D(2026, 7, null)).ShouldBe("2026-07");
        ConvertDataToIso(D(2026, 7, 5)).ShouldBe("2026-07-05");
        ConvertDataToIso(T(2026, 7, 5, 9, 3)).ShouldBe("2026-07-05T09:03:00");
        ConvertDataToIso(T(null, null, null, 13, 30)).ShouldBe("13:30");
        ConvertDataToIso(new DatetimeParts { Hour = 8 }).ShouldBe("08:00");
    }

    // ---- parse.spec.ts -----------------------------------------------------------------------

    [Fact]
    public void Should_parse_dates()
    {
        ParseDate(null).ShouldBeNull();
        ParseDate("").ShouldBeNull();
        ParseDate("foo").ShouldBeNull();
        ParseDate("2022-12-15T13:47").ShouldBe(T(2022, 12, 15, 13, 47, "pm"));
        // Time zones are not parsed: the value is taken as written.
        ParseDate("2022-12-15T13:47:30-02:00").ShouldBe(T(2022, 12, 15, 13, 47, "pm"));
        ParseDate("2023-03-23T20:19:33.517Z").ShouldBe(T(2023, 3, 23, 20, 19, "pm"));
        ParseDate("09:05").ShouldBe(T(null, null, null, 9, 5, "am"));
        ParseDate("2026").ShouldBe(new DatetimeParts { Year = 2026, Ampm = "pm" });
    }

    [Fact]
    public void Should_parse_an_array_and_reject_it_when_one_fails()
    {
        ParseDates(["2022-12-15T13:47", "2023-03-23T20:19:33.517Z"])
            .ShouldBe([T(2022, 12, 15, 13, 47, "pm"), T(2023, 3, 23, 20, 19, "pm")]);
        ParseDates(["2022-12-15", "nope"]).ShouldBeNull();
    }

    [Fact]
    public void Should_clamp_dates()
    {
        var min = D(2021, 6, 5);
        var max = D(2021, 8, 19);
        ClampDate(D(2022, 5, 24), min, max).ShouldBe(max);
        ClampDate(D(2020, 5, 24), min, max).ShouldBe(min);
        ClampDate(D(2021, 7, 10), min, max).ShouldBe(D(2021, 7, 10));
    }

    [Fact]
    public void Should_parse_am_pm()
    {
        ParseAmPm(12).ShouldBe("pm");
        ParseAmPm(13).ShouldBe("pm");
        ParseAmPm(11).ShouldBe("am");
    }

    [Fact]
    public void Should_fill_min_and_max_parts()
    {
        var today = T(2022, 3, 14, 2, 4);
        ParseMinParts("2012", today).ShouldBe(T(2012, 1, 1, 0, 0));
        ParseMinParts("04:30", today).ShouldBe(T(2022, 1, 1, 4, 30));
        ParseMinParts(null, today).ShouldBeNull();
        ParseMinParts("foo", today).ShouldBeNull();

        ParseMaxParts("2012", today).ShouldBe(T(2012, 12, 31, 23, 59));
        ParseMaxParts("04:30", today).ShouldBe(T(2022, 12, 31, 4, 30));
        ParseMaxParts("2012-02", today).ShouldBe(T(2012, 2, 29, 23, 59));
        ParseMaxParts(null, today).ShouldBeNull();
        ParseMaxParts("foo", today).ShouldBeNull();
    }

    [Fact]
    public void Should_convert_value_lists_to_numbers()
    {
        ConvertToArrayOfNumbers(null).ShouldBeNull();
        ConvertToArrayOfNumbers("0,15,30,45").ShouldBe([0, 15, 30, 45]);
        ConvertToArrayOfNumbers("[ 6, 7 ,8 ]").ShouldBe([6, 7, 8]);
        ConvertToArrayOfNumbers("5").ShouldBe([5]);
        ConvertToArrayOfNumbers("1,,x,12abc").ShouldBe([1, 12]);
    }

    // ---- state.spec.ts -----------------------------------------------------------------------

    [Fact]
    public void Should_disable_days_outside_the_range()
    {
        var day = D(2021, 5, 12);
        IsDayDisabled(day, null, null).ShouldBeFalse();
        IsDayDisabled(day, D(2021, 5, 12), null).ShouldBeFalse();
        IsDayDisabled(day, D(2021, 6, 12), null).ShouldBeTrue();
        IsDayDisabled(day, D(2022, 5, 13), null).ShouldBeTrue();
        IsDayDisabled(day, null, D(2021, 5, 12)).ShouldBeFalse();
        IsDayDisabled(day, null, D(2021, 4, 12)).ShouldBeTrue();
        IsDayDisabled(day, null, D(2021, 5, 11)).ShouldBeTrue();
        IsDayDisabled(D(2021, 5, null), null, null).ShouldBeTrue();
        IsDayDisabled(day, null, null, [1, 12]).ShouldBeFalse();
        IsDayDisabled(day, null, null, [1, 2]).ShouldBeTrue();
    }

    [Fact]
    public void Should_disable_the_previous_month_button()
    {
        IsPrevMonthDisabled(D(2021, 5, null), D(2021, 6, null)).ShouldBeTrue();
        IsPrevMonthDisabled(D(2021, 1, null), D(2021, 1, null)).ShouldBeTrue();
        IsPrevMonthDisabled(D(2021, 1, null), D(2021, null, null)).ShouldBeTrue();
        IsPrevMonthDisabled(D(2021, 5, null), D(2022, null, null)).ShouldBeTrue();
        IsPrevMonthDisabled(D(2021, 12, null), D(2021, 9, null), D(2021, 10, null)).ShouldBeTrue();
        IsPrevMonthDisabled(D(2022, 1, null), D(2021, 9, null), D(2021, 10, null)).ShouldBeTrue();

        IsPrevMonthDisabled(D(2021, 12, null)).ShouldBeFalse();
        IsPrevMonthDisabled(D(2021, 12, null), D(2021, null, null)).ShouldBeFalse();
        IsPrevMonthDisabled(D(2021, 2, null), D(2021, null, null)).ShouldBeFalse();
    }

    [Fact]
    public void Should_disable_the_next_month_button()
    {
        IsNextMonthDisabled(D(2021, 10, null), D(2021, 10, null)).ShouldBeTrue();
        IsNextMonthDisabled(D(2021, 10, null), D(2021, 9, null)).ShouldBeTrue();
        IsNextMonthDisabled(D(2022, 10, null), D(2021, 12, null)).ShouldBeTrue();

        IsNextMonthDisabled(D(2021, 10, null), null).ShouldBeFalse();
        IsNextMonthDisabled(D(2021, 10, null), D(2021, 11, null)).ShouldBeFalse();
    }

    [Fact]
    public void Should_find_highlight_styles_by_date_or_callback()
    {
        var red = Miko.Common.Color.FromHex("ff0000");
        var highlights = new[] { new DatetimeHighlight { Date = "2026-07-15", TextColor = red } };

        GetHighlightStyles(highlights, null, "2026-07-15T10:00:00")!.TextColor.ShouldBe(red);
        GetHighlightStyles(highlights, null, "2026-07-16").ShouldBeNull();
        GetHighlightStyles(null, iso => iso == "2026-07-16" ? new DatetimeHighlightStyle { BackgroundColor = red } : null, "2026-07-16")!
            .BackgroundColor.ShouldBe(red);
        // A throwing callback must not break the calendar.
        GetHighlightStyles(null, _ => throw new InvalidOperationException(), "2026-07-16").ShouldBeNull();
    }
}
