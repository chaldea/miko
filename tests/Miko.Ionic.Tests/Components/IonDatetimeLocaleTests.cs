using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// The locale layer that stands in for <c>Intl.DateTimeFormat</c>: Ionic's format.spec.ts and
/// helpers.spec.ts expectations, plus the field sets the datetime shows in several locales (the
/// text is derived from each culture's own .NET patterns).
/// </summary>
public class IonDatetimeLocaleTests
{
    private static DatetimeLocale L(string locale) => DatetimeLocale.Get(locale);

    private static DatetimeParts D(int year, int month, int day) => new(year, month, day);

    private static DatetimeParts T(int? year, int? month, int? day, int hour, int minute) => new(year, month, day, hour, minute);

    [Theory]
    [InlineData("en-US", 2021, 5, "May 2021")]
    [InlineData("es-ES", 2021, 5, "mayo de 2021")]
    [InlineData("en-US", 2006, 4, "April 2006")]
    [InlineData("es-ES", 2006, 4, "abril de 2006")]
    [InlineData("zh-CN", 2026, 7, "2026年7月")]
    [InlineData("ja-JP", 2026, 7, "2026年7月")]
    [InlineData("de-DE", 2026, 7, "Juli 2026")]
    public void Should_write_the_month_and_year(string locale, int year, int month, string expected)
        => L(locale).GetMonthAndYear(D(year, month, 11)).ShouldBe(expected);

    [Fact]
    public void Should_name_the_days_of_the_week_per_mode()
    {
        L("en-US").GetDaysOfWeek("ios", 0).ShouldBe(["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"]);
        L("en-US").GetDaysOfWeek("md", 0).ShouldBe(["S", "M", "T", "W", "T", "F", "S"]);
        L("es-ES").GetDaysOfWeek("ios", 0).ShouldBe(["dom", "lun", "mar", "mié", "jue", "vie", "sáb"]);
        L("es-ES").GetDaysOfWeek("md", 0).Take(2).ShouldBe(["D", "L"]);
        L("en-US").GetDaysOfWeek("ios", 1).ShouldBe(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"]);
        // The shared "周" prefix is not part of the narrow names.
        L("zh-CN").GetDaysOfWeek("md", 0).ShouldBe(["日", "一", "二", "三", "四", "五", "六"]);
    }

    [Theory]
    [InlineData("en-US", null, "h12")]
    [InlineData("en-US", "h23", "h23")]
    [InlineData("en-US", "h12", "h12")]
    [InlineData("en-US-u-hc-h23", null, "h23")]
    [InlineData("en-GB", null, "h23")]
    [InlineData("en-GB", "h23", "h23")]
    [InlineData("en-GB", "h12", "h12")]
    [InlineData("en-GB-u-hc-h12", null, "h12")]
    [InlineData("en-GB", "h11", "h11")]
    [InlineData("en-GB-u-hc-h11", null, "h11")]
    [InlineData("en-GB", "h24", "h24")]
    [InlineData("en-GB-u-hc-h24", null, "h24")]
    public void Should_resolve_the_hour_cycle(string locale, string? hourCycle, string expected)
        => L(locale).GetHourCycle(hourCycle).ShouldBe(expected);

    [Theory]
    [InlineData("en-US", true)]
    [InlineData("en-GB", true)]
    [InlineData("es-ES", true)]
    [InlineData("ro-RO", true)]
    [InlineData("zh-CN", false)]
    [InlineData("ja-JP", false)]
    [InlineData("ko-KR", false)]
    public void Should_tell_month_first_locales(string locale, bool expected) => L(locale).IsMonthFirst().ShouldBe(expected);

    [Fact]
    public void Should_order_month_and_day_columns_by_locale()
    {
        L("en-US").IsMonthFirst(beforeDay: true).ShouldBeTrue();
        L("en-GB").IsMonthFirst(beforeDay: true).ShouldBeFalse();
        L("zh-CN").IsMonthFirst(beforeDay: true).ShouldBeTrue();
    }

    // ICU writes a narrow no-break space before AM/PM (CLDR 42+); the datetime shows a no-break space.
    [Fact]
    public void Should_localize_times()
    {
        L("en-US").GetLocalizedTime(T(2022, 1, 1, 13, 40), "h12").ShouldBe("1:40\u00A0PM");
        L("en-US").GetLocalizedTime(T(2022, 1, 1, 9, 40), "h12").ShouldBe("9:40\u00A0AM");
        // A 12-hour clock in a 24-hour locale.
        L("en-GB").GetLocalizedTime(T(2022, 1, 1, 0, 0), "h12").ShouldBe("12:00\u00A0am");
        // Time-only values.
        L("en-US").GetLocalizedTime(T(null, null, null, 22, 40), "h12").ShouldBe("10:40\u00A0PM");
        L("en-US").GetLocalizedTime(T(null, null, null, 22, 40), "h23").ShouldBe("22:40");
        L("en-US").GetLocalizedTime(new DatetimeParts { Hour = 9 }, "h12").ShouldBe("Invalid Time");
    }

    [Fact]
    public void Should_count_hours_from_zero_or_to_twenty_four()
    {
        L("en-US").GetLocalizedTime(T(2022, 1, 1, 12, 5), "h11").ShouldBe("0:05\u00A0PM");
        L("en-US").GetLocalizedTime(T(2022, 1, 1, 0, 5), "h24").ShouldBe("24:05");
        L("en-US").GetLocalizedTime(T(2022, 1, 1, 7, 5), "h24").ShouldBe("07:05");
    }

    [Fact]
    public void Should_use_time_format_options()
    {
        L("en-US").GetLocalizedTime(T(2022, 1, 1, 9, 40), "h12", new DatetimeFormat { Hour = "2-digit", Minute = "2-digit" })
            .ShouldBe("09:40\u00A0AM");
        L("en-US").GetLocalizedTime(T(2022, 1, 1, 9, 40), "h12", new DatetimeFormat { Pattern = "HH.mm" })
            .ShouldBe("09.40");
    }

    [Fact]
    public void Should_place_the_day_period_as_the_locale_writes_it()
    {
        L("ko-KR").IsDayPeriodFirst().ShouldBeTrue();
        L("en-US").IsDayPeriodFirst().ShouldBeFalse();
        L("ko-KR").GetLocalizedTime(T(2022, 1, 1, 13, 40), "h12").ShouldBe("오후 1:40");
        L("zh-CN").GetLocalizedTime(T(2022, 1, 1, 13, 40), "h12").ShouldBe("下午1:40");
        L("zh-CN").GetLocalizedTime(T(2022, 1, 1, 13, 40), "h23").ShouldBe("13:40");
    }

    [Fact]
    public void Should_write_the_header_date_in_the_locale_order()
    {
        var date = D(2026, 7, 15);
        L("en-US").Format(date, DatetimeLocale.HeaderDateFormat).ShouldBe("Wed, Jul 15");
        L("zh-CN").Format(date, DatetimeLocale.HeaderDateFormat).ShouldBe("7月15日 周三");
        L("ja-JP").Format(date, DatetimeLocale.HeaderDateFormat).ShouldBe("7月15日 水");
        L("en-US").Format(date, new DatetimeFormat { Weekday = "long", Month = "long", Day = "numeric" })
            .ShouldBe("Wednesday, July 15");
        L("en-US").Format(date, new DatetimeFormat { Month = "short", Day = "numeric", Year = "numeric" }).ShouldBe("Jul 15, 2026");
        L("en-US").Format(date, new DatetimeFormat { Month = "numeric", Day = "numeric" }).ShouldBe("7/15");
        L("en-US").Format(date, new DatetimeFormat { Weekday = "short", Month = "numeric", Day = "numeric", Year = "numeric" })
            .ShouldBe("Wed, 7/15/2026");
    }

    [Fact]
    public void Should_write_single_fields_with_their_units()
    {
        var date = D(2026, 7, 15);
        L("en-US").GetYear(date).ShouldBe("2026");
        L("zh-CN").GetYear(date).ShouldBe("2026年");
        L("ko-KR").GetYear(date).ShouldBe("2026년");
        L("en-US").Format(date, DatetimeLocale.DayFormat).ShouldBe("15");
        L("zh-CN").Format(date, DatetimeLocale.DayFormat).ShouldBe("15日");
        L("en-US").Format(date, DatetimeLocale.MonthFormat).ShouldBe("July");
        L("zh-CN").Format(date, DatetimeLocale.MonthFormat).ShouldBe("七月");
        L("es-ES").Format(date, DatetimeLocale.MonthFormat).ShouldBe("julio");
        L("zh-CN").GetDay(date).ShouldBe("15");
    }

    [Fact]
    public void Should_localize_today_and_the_day_periods()
    {
        L("en-US").GetTodayLabel().ShouldBe("Today");
        L("zh-CN").GetTodayLabel().ShouldBe("今天");
        L("es-ES").GetTodayLabel().ShouldBe("Hoy");
        L("en-US").GetLocalizedDayPeriod("am").ShouldBe("AM");
        L("en-US").GetLocalizedDayPeriod("pm").ShouldBe("PM");
        L("zh-CN").GetLocalizedDayPeriod("am").ShouldBe("上午");
    }
}
