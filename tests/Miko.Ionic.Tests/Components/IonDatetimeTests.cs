using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-datetime</c>: the DOM contract of every presentation (calendar grid, wheels,
/// time button), the constraints (min / max, value lists, <c>IsDateEnabled</c>, highlighted and
/// adjacent days), locales, and the selection model — immediate without buttons, on Done with
/// them, several dates with <c>Multiple</c>. Today is pinned to 2026-07-10 09:05.
/// </summary>
public class IonDatetimeTests : IonicComponentTestBase
{
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 7, 10, 9, 5, 0, TimeSpan.Zero));

    /// <summary>A datetime and its tree. The datetime re-renders in place, so the tree stays current.</summary>
    private sealed record Rendered(IonDatetime Datetime, ComponentUnderTest Cut)
    {
        public Element Root => Cut.Root;

        public List<Element> Find(string className) => Root.FindByClass(className).Where(e => e.HasClass(className)).ToList();

        public Element Single(string className) => Find(className).ShouldHaveSingleItem();

        /// <summary>The day buttons of the month (fillers excluded).</summary>
        public List<Element> Days => Find("calendar-day").Where(d => !d.HasClass("calendar-day-padding")).ToList();

        public Element Day(string text) => Days.Single(d => Text(d) == text && !d.HasClass("calendar-day-adjacent-day"));

        public List<Element> Columns => Find("ion-picker-column");

        public List<Element> Options(Element column) => column.FindByClass("ion-picker-column-option");

        public Element Option(Element column, string text) => Options(column).Single(o => Text(o) == text);

        public Element Button(string id) => Root.FindById(id).ShouldNotBeNull().FindByClass("button-native").Single();
    }

    private Rendered Render(Action<IonDatetime>? configure = null, string? value = "2026-07-15T13:30:00")
    {
        var datetime = new IonDatetime { Locale = "en-US", Value = value };
        configure?.Invoke(datetime);
        return new Rendered(datetime, Context.RenderElement(Build(datetime)));
    }

    private Element Build(ComponentBase component)
    {
        var services = new ServiceCollection()
            .AddSingleton<IPlatformInfo>(Platform)
            .AddSingleton<IPlatformService>(Platform)
            .AddSingleton<TimeProvider>(Clock)
            .BuildServiceProvider();
        using (ComponentServiceScope.Push(services)) return component.Build();
    }

    private static void Click(Element element) => element.OnClick!.Invoke(new MouseEventArgs { Target = element });

    /// <summary>All the text inside the element, trimmed.</summary>
    private static string Text(Element element)
    {
        var builder = new System.Text.StringBuilder();
        Collect(element);
        return builder.ToString().Trim();

        void Collect(Element node)
        {
            if (node is Miko.Core.DomElements.TextNode text) builder.Append(text.Text);
            foreach (var child in node.Children) Collect(child);
        }
    }

    private static List<string> Texts(IEnumerable<Element> elements) => elements.Select(Text).ToList();

    // ---- presentations -----------------------------------------------------------------------

    [Fact]
    public void Should_default_to_a_date_time_grid()
    {
        var r = Render();

        r.Root.TagName.ShouldBe("div");
        r.Root.Class.ShouldBe("md ion-datetime datetime-presentation-date-time datetime-size-fixed datetime-grid");
        r.Root.Children.Select(c => c.Class).ShouldBe(["datetime-calendar", "datetime-year", "datetime-time"]);
        Text(r.Single("time-header")).ShouldBe("Time");
        Text(r.Single("time-body")).ShouldBe("1:30\u00A0PM");
        r.Find("datetime-header").ShouldBeEmpty();
        r.Find("datetime-footer").ShouldBeEmpty();
    }

    [Fact]
    public void Should_render_the_calendar_contract()
    {
        var r = Render(d => d.Presentation = "date");

        r.Root.Children.Select(c => c.Class).ShouldBe(["datetime-calendar", "datetime-year"]);
        var calendar = r.Single("datetime-calendar");
        calendar.Children.Select(c => c.Class).ShouldBe(["calendar-header", "calendar-body ion-focusable"]);
        r.Single("calendar-header").Children.Select(c => c.Class).ShouldBe(["calendar-action-buttons", "calendar-days-of-week"]);
        r.Single("calendar-month-year-toggle").FindById("toggle-wrapper").ShouldNotBeNull();
        Text(r.Single("calendar-month-year-toggle")).ShouldBe("July 2026");
        r.Single("calendar-next-prev").FindByClass("ion-button").Count.ShouldBe(2);
        Texts(r.Find("day-of-week")).ShouldBe(["S", "M", "T", "W", "T", "F", "S"]);
        r.Single("calendar-body").Children.ShouldHaveSingleItem().HasClass("calendar-month").ShouldBeTrue();
        r.Find("calendar-day-wrapper").Count.ShouldBe(34);
        r.Days.Count.ShouldBe(31);
        // 2026-07-01 is a Wednesday: Sunday, Monday and Tuesday are fillers.
        r.Find("calendar-day-padding").Count.ShouldBe(3);
    }

    [Fact]
    public void Should_put_the_time_first_in_time_date()
    {
        var r = Render(d => d.Presentation = "time-date");

        r.Root.Children.Select(c => c.Class).ShouldBe(["datetime-time", "datetime-calendar", "datetime-year"]);
    }

    [Fact]
    public void Should_render_hour_minute_and_day_period_wheels_for_time()
    {
        var r = Render(d => d.Presentation = "time");

        r.Root.Class.ShouldBe("md ion-datetime datetime-presentation-time datetime-size-fixed");
        r.Find("datetime-calendar").ShouldBeEmpty();
        var picker = r.Single("datetime-time").Children.ShouldHaveSingleItem();
        picker.HasClass("ion-picker").ShouldBeTrue();
        r.Columns.Count.ShouldBe(3);
        r.Options(r.Columns[0]).Count.ShouldBe(12);
        r.Options(r.Columns[1]).Count.ShouldBe(60);
        Texts(r.Options(r.Columns[2])).ShouldBe(["AM", "PM"]);
        // 13:30 → 1 PM.
        Text(r.Options(r.Columns[0]).Single(o => o.HasClass("option-active"))).ShouldBe("1");
        Text(r.Options(r.Columns[1]).Single(o => o.HasClass("option-active"))).ShouldBe("30");
        Text(r.Options(r.Columns[2]).Single(o => o.HasClass("option-active"))).ShouldBe("PM");
    }

    [Fact]
    public void Should_render_24_hour_wheels_without_day_period()
    {
        var r = Render(d =>
        {
            d.Presentation = "time";
            d.HourCycle = "h23";
        });

        r.Columns.Count.ShouldBe(2);
        r.Options(r.Columns[0]).Count.ShouldBe(24);
        Text(r.Options(r.Columns[0]).Single(o => o.HasClass("option-active"))).ShouldBe("13");
    }

    [Theory]
    [InlineData("month-year", new[] { "month-column", "year-column" })]
    [InlineData("month", new[] { "month-column" })]
    [InlineData("year", new[] { "year-column" })]
    public void Should_render_month_and_year_wheels(string presentation, string[] columns)
    {
        var r = Render(d => d.Presentation = presentation);

        r.Root.ShouldHaveClass("show-month-and-year");
        r.Root.ShouldNotHaveClass("month-year-picker-open");
        r.Root.ShouldNotHaveClass("datetime-grid");
        r.Root.Children.ShouldHaveSingleItem().Class.ShouldBe("wheel-order-month-first");
        r.Columns.Select(c => c.Class!.Split(' ')[0]).ShouldBe(columns);
    }

    [Fact]
    public void Should_render_date_wheels_in_the_locale_order_with_prefer_wheel()
    {
        var us = Render(d =>
        {
            d.Presentation = "date";
            d.PreferWheel = true;
        });
        us.Root.Class.ShouldBe("md ion-datetime datetime-presentation-date datetime-size-fixed datetime-prefer-wheel");
        us.Columns.Select(c => c.Class!.Split(' ')[0]).ShouldBe(["month-column", "day-column", "year-column"]);
        Text(us.Options(us.Columns[0]).Single(o => o.HasClass("option-active"))).ShouldBe("July");
        Text(us.Options(us.Columns[1]).Single(o => o.HasClass("option-active"))).ShouldBe("15");
        Text(us.Options(us.Columns[2]).Single(o => o.HasClass("option-active"))).ShouldBe("2026");

        var gb = Render(d =>
        {
            d.Presentation = "date";
            d.PreferWheel = true;
            d.Locale = "en-GB";
        });
        gb.Columns.Select(c => c.Class!.Split(' ')[0]).ShouldBe(["day-column", "month-column", "year-column"]);

        var zh = Render(d =>
        {
            d.Presentation = "date";
            d.PreferWheel = true;
            d.Locale = "zh-CN";
        });
        zh.Root.Children.Single(c => c.Class!.StartsWith("wheel-order")).Class.ShouldBe("wheel-order-year-first");
        Text(zh.Options(zh.Columns[2]).Single(o => o.HasClass("option-active"))).ShouldBe("2026年");
    }

    [Fact]
    public void Should_combine_date_and_time_wheels_with_prefer_wheel()
    {
        var r = Render(d => d.PreferWheel = true);

        r.Columns.Count.ShouldBe(4);
        r.Columns[0].HasClass("date-column").ShouldBeTrue();
        // Three months around July; July 10 is today.
        r.Options(r.Columns[0]).Count.ShouldBe(30 + 31 + 31);
        Texts(r.Options(r.Columns[0])).ShouldContain("Today");
        Text(r.Options(r.Columns[0]).Single(o => o.HasClass("option-active"))).ShouldBe("Wed, Jul 15");

        var timeFirst = Render(d =>
        {
            d.Presentation = "time-date";
            d.PreferWheel = true;
        });
        timeFirst.Columns.Last().HasClass("date-column").ShouldBeTrue();
    }

    // ---- header and footer ------------------------------------------------------------------

    [Fact]
    public void Should_show_the_header_on_request()
    {
        var r = Render(d => d.ShowDefaultTitle = true);

        var header = r.Single("datetime-header");
        Text(header.FindByClass("datetime-title").Single()).ShouldBe("Select Date");
        Text(header.FindByClass("datetime-selected-date").Single()).ShouldBe("Wed, Jul 15");
    }

    [Fact]
    public void Should_show_a_title_slot_without_the_selected_date_on_wheels()
    {
        var r = Render(d =>
        {
            d.Presentation = "time";
            d.Title = b => b.AddContent("Pick a time");
        });

        Text(r.Single("datetime-title")).ShouldBe("Pick a time");
        r.Find("datetime-selected-date").ShouldBeEmpty();
    }

    [Fact]
    public void Should_format_the_header_date_with_format_options()
    {
        var r = Render(d =>
        {
            d.ShowDefaultTitle = true;
            d.FormatOptions = new DatetimeFormatOptions
            {
                Date = new DatetimeFormat { Weekday = "long", Month = "long", Day = "numeric" },
                Time = new DatetimeFormat { Hour = "2-digit", Minute = "2-digit" },
            };
        });

        Text(r.Single("datetime-selected-date")).ShouldBe("Wednesday, July 15");
        Text(r.Single("time-body")).ShouldBe("01:30\u00A0PM");
    }

    [Fact]
    public void Should_render_the_default_and_clear_buttons()
    {
        var r = Render(d =>
        {
            d.ShowDefaultButtons = true;
            d.ShowClearButton = true;
            d.CancelText = "Back";
        });

        var actions = r.Single("datetime-footer").FindByClass("datetime-action-buttons").Single();
        actions.HasClass("has-clear-button").ShouldBeTrue();
        Text(r.Root.FindById("cancel-button")!).ShouldBe("Back");
        Text(r.Root.FindById("clear-button")!).ShouldBe("Clear");
        Text(r.Root.FindById("confirm-button")!).ShouldBe("Done");
        r.Single("datetime-action-buttons-container").FindByClass("ion-button").Count.ShouldBe(2);
        r.Root.FindById("confirm-button")!.ShouldHaveClass("ion-color-primary");
    }

    [Fact]
    public void Should_replace_the_default_buttons_with_the_buttons_slot()
    {
        var r = Render(d => d.Buttons = b =>
        {
            var button = b.OpenElement<Miko.Core.DomElements.ButtonElement>();
            button.Class = "my-confirm";
            b.CloseElement();
        });

        r.Single("datetime-footer").FindByClass("my-confirm").ShouldHaveSingleItem();
        r.Root.FindById("confirm-button").ShouldBeNull();
    }

    // ---- days --------------------------------------------------------------------------------

    [Fact]
    public void Should_mark_the_selected_day_and_today()
    {
        var r = Render(d => d.Presentation = "date");

        Text(r.Find("calendar-day-active").ShouldHaveSingleItem()).ShouldBe("15");
        Text(r.Find("calendar-day-today").ShouldHaveSingleItem()).ShouldBe("10");
    }

    [Fact]
    public void Should_start_the_week_on_the_first_day_of_week()
    {
        var r = Render(d =>
        {
            d.Presentation = "date";
            d.FirstDayOfWeek = 1;
        });

        Texts(r.Find("day-of-week")).ShouldBe(["M", "T", "W", "T", "F", "S", "S"]);
        r.Find("calendar-day-padding").Count.ShouldBe(2);
    }

    [Fact]
    public void Should_disable_days_outside_min_and_max()
    {
        var r = Render(d =>
        {
            d.Presentation = "date";
            d.Min = "2026-07-05";
            d.Max = "2026-07-20";
        });

        r.Days.Where(day => day.IsDisabled).Select(Text).ShouldBe(
            [.. Enumerable.Range(1, 4).Select(i => i.ToString()), .. Enumerable.Range(21, 11).Select(i => i.ToString())]);
        r.Single("calendar-next-prev").FindByClass("ion-button").ShouldAllBe(b => b.HasClass("button-disabled"));
    }

    [Fact]
    public void Should_disable_days_from_value_lists_and_is_date_enabled()
    {
        var dayValues = Render(d =>
        {
            d.Presentation = "date";
            d.DayValues = "1,15";
        });
        dayValues.Days.Where(day => !day.IsDisabled).Select(Text).ShouldBe(["1", "15"]);

        var monthValues = Render(d =>
        {
            d.Presentation = "date";
            d.MonthValues = "1,2";
        });
        monthValues.Days.ShouldAllBe(day => day.IsDisabled);

        var weekdays = Render(d =>
        {
            d.Presentation = "date";
            d.IsDateEnabled = iso =>
            {
                var date = DateTime.Parse(iso, System.Globalization.CultureInfo.InvariantCulture);
                return date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
            };
        });
        weekdays.Day("4").IsDisabled.ShouldBeTrue();
        weekdays.Day("5").IsDisabled.ShouldBeTrue();
        weekdays.Day("6").IsDisabled.ShouldBeFalse();
    }

    [Fact]
    public void Should_highlight_dates_but_not_the_selected_one()
    {
        var red = Color.FromHex("ff0000");
        var r = Render(d =>
        {
            d.Presentation = "date";
            d.HighlightedDates =
            [
                new DatetimeHighlight { Date = "2026-07-03", TextColor = red, BackgroundColor = Color.FromHex("ffeeee") },
                new DatetimeHighlight { Date = "2026-07-15", TextColor = red },
            ];
        });

        var highlighted = r.Day("3").Style.ShouldNotBeNull();
        highlighted.Color!.Value.ShouldBe(red);
        r.Day("15").Style.ShouldBeNull();
        r.Day("4").Style.ShouldBeNull();

        var callback = Render(d =>
        {
            d.Presentation = "date";
            d.HighlightedDatesCallback = iso => iso.EndsWith("-07") ? new DatetimeHighlightStyle { Border = new Border(Length.Px(2), red) } : null;
        });
        callback.Day("7").Style.ShouldNotBeNull();
    }

    [Fact]
    public void Should_fill_six_weeks_with_adjacent_days()
    {
        var r = Render(d =>
        {
            d.Presentation = "date";
            d.ShowAdjacentDays = true;
        });

        r.Find("calendar-day").Count.ShouldBe(42);
        r.Find("calendar-day-padding").ShouldBeEmpty();
        Texts(r.Find("calendar-day-adjacent-day")).ShouldBe(["28", "29", "30", "1", "2", "3", "4", "5", "6", "7", "8"]);
    }

    [Fact]
    public void Should_localize_the_calendar()
    {
        var r = Render(d =>
        {
            d.Presentation = "date";
            d.Locale = "es-ES";
            d.ShowDefaultTitle = true;
        });

        Text(r.Single("calendar-month-year-toggle")).ShouldBe("julio de 2026");
        Texts(r.Find("day-of-week")).First().ShouldBe("D");
        Text(r.Single("datetime-selected-date")).ShouldBe("mié, 15 de jul");
    }

    [Fact]
    public void Should_use_ios_names_and_icons_on_ios()
    {
        UsePlatform(HostPlatform.Ios);

        var r = Render(d => d.Presentation = "date");

        r.Root.Class.ShouldStartWith("ios ion-datetime");
        Texts(r.Find("day-of-week")).ShouldBe(["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"]);
    }

    // ---- selecting ---------------------------------------------------------------------------

    [Fact]
    public void Should_select_a_day_right_away_without_buttons()
    {
        string? changed = null;
        string? confirmed = null;
        var r = Render(d =>
        {
            d.Presentation = "date";
            d.ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v);
            d.OnConfirm = EventCallback.Factory.Create<string?>(this, v => confirmed = v);
        });

        Click(r.Day("20"));

        changed.ShouldBe("2026-07-20T13:30:00");
        confirmed.ShouldBe("2026-07-20T13:30:00");
        r.Datetime.Value.ShouldBe("2026-07-20T13:30:00");
        Text(r.Find("calendar-day-active").ShouldHaveSingleItem()).ShouldBe("20");
    }

    [Fact]
    public void Should_keep_the_form_of_the_value()
    {
        string? dateOnly = null;
        var r = Render(d => d.ValueChanged = EventCallback.Factory.Create<string?>(this, v => dateOnly = v), "2026-07-15");
        Click(r.Day("20"));
        dateOnly.ShouldBe("2026-07-20");

        // Without a value, a pick carries the current time, as in Ionic.
        string? fromNow = null;
        var empty = Render(d => d.ValueChanged = EventCallback.Factory.Create<string?>(this, v => fromNow = v), null);
        empty.Find("calendar-day-active").ShouldBeEmpty();
        Click(empty.Day("20"));
        fromNow.ShouldBe("2026-07-20T09:05:00");
    }

    [Fact]
    public void Should_wait_for_done_with_buttons()
    {
        var changes = new List<string?>();
        var cancelled = false;
        var r = Render(d =>
        {
            d.ShowDefaultButtons = true;
            d.ValueChanged = EventCallback.Factory.Create<string?>(this, v => changes.Add(v));
            d.OnCancel = EventCallback.Factory.Create(this, () => cancelled = true);
        });

        Click(r.Day("20"));
        changes.ShouldBeEmpty();
        Text(r.Find("calendar-day-active").ShouldHaveSingleItem()).ShouldBe("20");

        Click(r.Button("cancel-button"));
        cancelled.ShouldBeTrue();
        changes.ShouldBeEmpty();

        Click(r.Button("confirm-button"));
        changes.ShouldBe(["2026-07-20T13:30:00"]);
    }

    [Fact]
    public async Task Should_confirm_and_reset_from_code()
    {
        string? changed = "unchanged";
        var r = Render(d =>
        {
            d.ShowDefaultButtons = true;
            d.ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v);
        });

        Click(r.Day("22"));
        await r.Datetime.ResetAsync("2026-09-01");
        Text(r.Single("calendar-month-year-toggle")).ShouldBe("September 2026");
        Text(r.Find("calendar-day-active").ShouldHaveSingleItem()).ShouldBe("1");
        changed.ShouldBe("unchanged");

        await r.Datetime.ConfirmAsync();
        changed.ShouldBe("2026-09-01");
    }

    [Fact]
    public void Should_clear_the_value()
    {
        string? changed = "unchanged";
        var r = Render(d =>
        {
            d.ShowClearButton = true;
            d.ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v);
        });

        Click(r.Button("clear-button"));

        changed.ShouldBeNull();
        r.Find("calendar-day-active").ShouldBeEmpty();
    }

    [Fact]
    public void Should_select_several_dates_with_multiple()
    {
        IReadOnlyList<string>? values = null;
        var r = Render(d =>
        {
            d.Presentation = "date";
            d.Multiple = true;
            d.ShowDefaultTitle = true;
            d.Values = ["2026-07-01"];
            d.ValuesChanged = EventCallback.Factory.Create<IReadOnlyList<string>?>(this, v => values = v);
        }, value: null);

        Text(r.Single("datetime-selected-date")).ShouldBe("Wed, Jul 1");

        Click(r.Day("2"));
        values.ShouldBe(["2026-07-01", "2026-07-02"]);
        Texts(r.Find("calendar-day-active")).ShouldBe(["1", "2"]);
        Text(r.Single("datetime-selected-date")).ShouldBe("2 days");

        Click(r.Day("1"));
        values.ShouldBe(["2026-07-02"]);

        Click(r.Day("2"));
        values.ShouldBeNull();
        Text(r.Single("datetime-selected-date")).ShouldBe("0 days");
    }

    [Fact]
    public void Should_format_the_multiple_dates_title()
    {
        var r = Render(d =>
        {
            d.Presentation = "date";
            d.Multiple = true;
            d.ShowDefaultTitle = true;
            d.Values = ["2026-07-01", "2026-07-03"];
            d.TitleSelectedDatesFormatter = dates => $"{dates.Count} picked";
        }, value: null);

        Text(r.Single("datetime-selected-date")).ShouldBe("2 picked");
    }

    [Fact]
    public void Should_page_months_without_changing_the_value()
    {
        var changed = false;
        var r = Render(d => d.ValueChanged = EventCallback.Factory.Create<string?>(this, _ => changed = true));
        var buttons = r.Single("calendar-next-prev").FindByClass("button-native");

        Click(buttons[1]);
        Text(r.Single("calendar-month-year-toggle")).ShouldBe("August 2026");
        r.Find("calendar-day-active").ShouldBeEmpty();

        Click(r.Single("calendar-next-prev").FindByClass("button-native")[0]);
        Click(r.Single("calendar-next-prev").FindByClass("button-native")[0]);
        Text(r.Single("calendar-month-year-toggle")).ShouldBe("June 2026");
        changed.ShouldBeFalse();
    }

    [Fact]
    public void Should_swipe_between_months()
    {
        var r = Render(d => d.Presentation = "date");
        var body = r.Single("calendar-body");

        r.Root.OnPointerDown!.Invoke(new PointerEventArgs { Target = body, X = 300, Y = 100, PointerType = PointerType.Touch });
        r.Root.OnPointerUp!.Invoke(new PointerEventArgs { Target = body, X = 120, Y = 110, PointerType = PointerType.Touch });
        Text(r.Single("calendar-month-year-toggle")).ShouldBe("August 2026");

        r.Root.OnPointerDown!.Invoke(new PointerEventArgs { Target = r.Single("calendar-body"), X = 100, Y = 100, PointerType = PointerType.Touch });
        r.Root.OnPointerUp!.Invoke(new PointerEventArgs { Target = r.Single("calendar-body"), X = 130, Y = 100, PointerType = PointerType.Touch });
        Text(r.Single("calendar-month-year-toggle")).ShouldBe("August 2026");
    }

    [Fact]
    public void Should_open_the_month_and_year_wheels_from_the_calendar()
    {
        string? changed = null;
        var r = Render(d =>
        {
            d.Presentation = "date";
            d.ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v);
        });
        r.Single("datetime-year").Children.ShouldBeEmpty();

        Click(r.Single("calendar-month-year-toggle"));

        r.Root.ShouldHaveClass("show-month-and-year");
        r.Root.ShouldHaveClass("month-year-picker-open");
        r.Single("datetime-year").Children.ShouldHaveSingleItem().Class.ShouldBe("wheel-order-month-first");
        r.Columns.Select(c => c.Class!.Split(' ')[0]).ShouldBe(["month-column", "year-column"]);

        Click(r.Option(r.Columns[0], "August"));
        changed.ShouldBe("2026-08-15T13:30:00");
        Text(r.Single("calendar-month-year-toggle")).ShouldBe("August 2026");

        Click(r.Single("calendar-month-year-toggle"));
        r.Root.ShouldNotHaveClass("show-month-and-year");
        r.Single("datetime-year").Children.ShouldBeEmpty();
    }

    [Fact]
    public void Should_change_the_time_from_the_wheels()
    {
        var changes = new List<string?>();
        var r = Render(d =>
        {
            d.Presentation = "time";
            d.ValueChanged = EventCallback.Factory.Create<string?>(this, v => changes.Add(v));
        });

        Click(r.Option(r.Columns[0], "3"));
        Click(r.Option(r.Columns[1], "45"));
        Click(r.Option(r.Columns[2], "AM"));

        changes.ShouldBe(["2026-07-15T15:30:00", "2026-07-15T15:45:00", "2026-07-15T03:45:00"]);
    }

    [Fact]
    public void Should_keep_a_time_only_value_time_only()
    {
        string? changed = null;
        var r = Render(d =>
        {
            d.Presentation = "time";
            d.HourCycle = "h23";
            d.ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v);
        }, "13:30");

        Click(r.Option(r.Columns[1], "45"));

        changed.ShouldBe("13:45");
    }

    [Fact]
    public void Should_limit_the_wheels_to_min_and_max()
    {
        var r = Render(d =>
        {
            d.Presentation = "month-year";
            d.Min = "2025-03";
            d.Max = "2026-08";
        });

        Texts(r.Options(r.Columns[0])).ShouldBe(["January", "February", "March", "April", "May", "June", "July", "August"]);
        Texts(r.Options(r.Columns[1])).ShouldBe(["2025", "2026"]);
    }

    [Fact]
    public void Should_change_the_year_from_the_year_wheel()
    {
        string? changed = null;
        var r = Render(d =>
        {
            d.Presentation = "month-year";
            d.ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v);
        });

        Click(r.Option(r.Columns[1], "2025"));

        changed.ShouldBe("2025-07-15T13:30:00");
    }

    [Fact]
    public void Should_confirm_the_initial_wheel_position_with_prefer_wheel()
    {
        // Ionic's prefer-wheel spec: nothing picked, Done confirms what the wheels show.
        string? changed = null;
        var r = Render(d =>
        {
            d.PreferWheel = true;
            d.Max = "2021";
            d.ShowDefaultButtons = true;
            d.ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v);
        }, null);

        Click(r.Button("confirm-button"));

        changed.ShouldBe("2021-12-31T23:59:00");
    }

    [Fact]
    public void Should_open_the_time_wheels_in_a_popover()
    {
        string? changed = null;
        var r = Render(d => d.ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v));
        var popover = r.Single("datetime-time-popover");
        popover.ShouldHaveClass("overlay-hidden");
        popover.FindByClass("ion-picker").ShouldBeEmpty();

        Click(r.Single("time-body"));

        r.Single("time-body").HasClass("time-body-active").ShouldBeTrue();
        popover = r.Single("datetime-time-popover");
        popover.ShouldNotHaveClass("overlay-hidden");
        var columns = popover.FindByClass("ion-picker-column");
        columns.Count.ShouldBe(3);

        Click(r.Option(columns[0], "4"));
        changed.ShouldBe("2026-07-15T16:30:00");
        Text(r.Single("time-body")).ShouldBe("4:30\u00A0PM");

        Click(r.Single("datetime-time-popover").FindByClass("popover-backdrop").Single());
        r.Single("datetime-time-popover").ShouldHaveClass("overlay-hidden");
        r.Single("time-body").HasClass("time-body-active").ShouldBeFalse();
    }

    [Fact]
    public void Should_not_change_when_disabled()
    {
        var changed = false;
        var r = Render(d =>
        {
            d.Disabled = true;
            d.ValueChanged = EventCallback.Factory.Create<string?>(this, _ => changed = true);
        });

        r.Root.ShouldHaveClass("datetime-disabled");
        r.Days.ShouldAllBe(day => day.IsDisabled);
        r.Single("calendar-month-year-toggle").IsDisabled.ShouldBeTrue();
        r.Single("time-body").IsDisabled.ShouldBeTrue();
        r.Single("calendar-next-prev").FindByClass("ion-button").ShouldAllBe(b => b.HasClass("button-disabled"));

        Click(r.Day("20"));
        Click(r.Single("time-body"));
        changed.ShouldBeFalse();
        r.Single("datetime-time-popover").ShouldHaveClass("overlay-hidden");
    }

    [Fact]
    public void Should_browse_but_not_select_when_readonly()
    {
        var changed = false;
        var r = Render(d =>
        {
            d.Readonly = true;
            d.ValueChanged = EventCallback.Factory.Create<string?>(this, _ => changed = true);
        });

        r.Root.ShouldHaveClass("datetime-readonly");
        r.Days.ShouldAllBe(day => day.IsDisabled);
        // Only days that min / max / the value lists rule out are "constrained" (the fillers are).
        r.Days.ShouldAllBe(day => !day.HasClass("calendar-day-constrained"));

        Click(r.Single("calendar-next-prev").FindByClass("button-native")[1]);
        Text(r.Single("calendar-month-year-toggle")).ShouldBe("August 2026");
        changed.ShouldBeFalse();
    }

    [Fact]
    public void Should_follow_a_value_set_from_outside()
    {
        var host = new Fixtures.DatetimeHostFixture { Value = "2026-07-15" };
        var root = Build(host);
        Text(root.FindByClass("calendar-month-year-toggle").Single()).ShouldBe("July 2026");
        var datetime = host.Datetime;

        host.Update("date", "2027-01-09");

        host.Datetime.ShouldBeSameAs(datetime);
        Text(root.FindByClass("calendar-month-year-toggle").Single()).ShouldBe("January 2027");
        Text(root.FindByClass("calendar-day-active").ShouldHaveSingleItem()).ShouldBe("9");

        // Paging is the user's until the value changes again.
        Click(root.FindByClass("calendar-next-prev").Single().FindByClass("button-native")[1]);
        host.Update("date", "2027-01-09");
        Text(root.FindByClass("calendar-month-year-toggle").Single()).ShouldBe("February 2027");
    }

    [Fact]
    public void Should_close_the_month_and_year_wheels_when_the_presentation_changes()
    {
        var host = new Fixtures.DatetimeHostFixture { Value = "2026-07-15" };
        var root = Build(host);
        Click(root.FindByClass("calendar-month-year-toggle").Single());
        root.FindByClass("ion-datetime").Single().ShouldHaveClass("show-month-and-year");

        host.Update("date-time", "2026-07-15");

        var datetime = root.FindByClass("ion-datetime").Single();
        datetime.ShouldNotHaveClass("show-month-and-year");
        datetime.FindByClass("datetime-time").ShouldHaveSingleItem();
    }
}
