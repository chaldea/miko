using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Core;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Styling;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Key styles of <c>ion-datetime</c> in both modes (datetime.md.scss / datetime.ios.scss): the
/// selected day and today, the header, the weekday labels, disabled and adjacent days, the time
/// button, and the month/year wheel replacing the calendar.
/// </summary>
public class IonDatetimeStyleTests : IonicComponentTestBase
{
    private static readonly IonicTheme Md = IonicTheme.CreateMd();
    private static readonly IonicTheme Ios = IonicTheme.CreateIos();

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    public IonDatetimeStyleTests()
    {
        // Today is 2026-07-10.
        Context.Services.AddSingleton<TimeProvider>(new FixedClock(new DateTimeOffset(2026, 7, 10, 9, 5, 0, TimeSpan.Zero)));
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
    }

    private ComponentUnderTest Render(Action<ComponentParameterBuilder<IonDatetime>>? configure = null)
    {
        return Context.Render<IonDatetime>(p =>
        {
            p.Add(nameof(IonDatetime.Locale), "en-US");
            p.Add(nameof(IonDatetime.Value), "2026-07-15T13:30:00");
            p.Add(nameof(IonDatetime.Presentation), "date");
            configure?.Invoke(p);
        });
    }

    private static Element Day(ComponentUnderTest cut, string text)
        => cut.FindByClass("calendar-day").Single(d => !d.HasClass("calendar-day-padding") && !d.HasClass("calendar-day-adjacent-day") && d.TextContent == text);

    private static ComputedStyle Style(ComponentUnderTest cut, Element element) => cut.GetComputedStyle(element).ShouldNotBeNull();

    [Fact]
    public void Should_fill_the_selected_day_and_ring_today_on_md()
    {
        var cut = Render();

        var active = Style(cut, Day(cut, "15"));
        active.BackgroundColor.ShouldBe(Md.Primary);
        active.Color.ShouldBe(Color.White);
        active.BorderTopColor.ShouldBe(Md.Primary);

        var today = Style(cut, Day(cut, "10"));
        today.BorderTopWidth.ShouldBe(Length.Px(1));
        today.BorderTopColor.ShouldBe(Md.Primary);
        today.Color.ShouldBe(Md.Primary);
        today.BackgroundColor.A.ShouldBe((byte)0);

        var plain = Style(cut, Day(cut, "20"));
        plain.BackgroundColor.A.ShouldBe((byte)0);
        plain.Width.ShouldBe(Length.Px(42));
        plain.BorderTopLeftRadius.ShouldBe(Length.Percent(50));
    }

    [Fact]
    public void Should_tint_the_selected_day_on_ios()
    {
        UsePlatform(HostPlatform.Ios);
        var cut = Render();

        var active = Style(cut, Day(cut, "15"));
        active.BackgroundColor.ShouldBe(new Color(Ios.Primary.R, Ios.Primary.G, Ios.Primary.B, 51));
        active.Color.ShouldBe(Ios.Primary);
        active.FontSize.ShouldBe(Length.Px(22));
        active.FontWeight.ShouldBe(FontWeight.SemiBold);
        Style(cut, Day(cut, "16")).Width.ShouldBe(Length.Px(40));
        Style(cut, Day(cut, "10")).Color.ShouldBe(Ios.Primary);

        // Selected and today: a filled circle.
        var both = Render(p => p.Add(nameof(IonDatetime.Value), "2026-07-10"));
        var filled = Style(both, Day(both, "10"));
        filled.BackgroundColor.ShouldBe(Ios.Primary);
        filled.Color.ShouldBe(Color.White);
    }

    [Fact]
    public void Should_style_the_md_header()
    {
        var cut = Render(p => p.Add(nameof(IonDatetime.ShowDefaultTitle), true));

        Style(cut, cut.FindByClass("datetime-header").Single()).BackgroundColor.ShouldBe(Md.Primary);
        var title = Style(cut, cut.FindByClass("datetime-title").Single());
        title.Color.ShouldBe(Color.White);
        title.TextTransform.ShouldBe(TextTransform.Uppercase);
        Style(cut, cut.FindByClass("datetime-selected-date").Single()).FontSize.ShouldBe(Length.Px(34));
    }

    [Fact]
    public void Should_style_the_ios_header_and_weekdays()
    {
        UsePlatform(HostPlatform.Ios);
        var cut = Render(p => p.Add(nameof(IonDatetime.ShowDefaultTitle), true));

        var header = Style(cut, cut.FindByClass("datetime-header").Single());
        header.BorderBottomColor.ShouldBe(Color.FromHex("cccccc"));
        header.BorderBottomWidth.ShouldBe(Length.Px(0.55f));
        Style(cut, cut.FindByClass("datetime-title").Single()).Color.ShouldBe(Color.FromHex("666666"));

        var weekdays = Style(cut, cut.FindByClass("calendar-days-of-week").Single());
        weekdays.TextTransform.ShouldBe(TextTransform.Uppercase);
        weekdays.Color.ShouldBe(Color.FromHex("b3b3b3"));
        weekdays.Display.ShouldBe(Display.Grid);
        // Grid presentations reserve 350px on iOS.
        Style(cut, cut.Root).MinHeight.ShouldBe(Length.Px(350));
    }

    [Fact]
    public void Should_dim_disabled_days_but_not_readonly_ones()
    {
        var bounded = Render(p => p.Add(nameof(IonDatetime.Min), "2026-07-10"));
        Style(bounded, Day(bounded, "5")).Opacity.ShouldBe(0.4f);
        Style(bounded, Day(bounded, "12")).Opacity.ShouldBe(1f);

        var readOnly = Render(p => p.Add(nameof(IonDatetime.Readonly), true));
        Day(readOnly, "12").IsDisabled.ShouldBeTrue();
        Style(readOnly, Day(readOnly, "12")).Opacity.ShouldBe(1f);
    }

    [Fact]
    public void Should_gray_adjacent_days()
    {
        var cut = Render(p => p.Add(nameof(IonDatetime.ShowAdjacentDays), true));

        var adjacent = cut.FindByClass("calendar-day-adjacent-day").First();
        Style(cut, adjacent).Color.ShouldBe(Color.FromHex("808080"));
    }

    [Fact]
    public void Should_style_the_time_button()
    {
        var cut = Render(p => p.Add(nameof(IonDatetime.Presentation), "date-time"));

        var body = Style(cut, cut.FindByClass("time-body").Single());
        body.BackgroundColor.ShouldBe(Color.FromHex("edeef0"));
        body.BorderTopLeftRadius.ShouldBe(Length.Px(8));
        Style(cut, cut.FindByClass("time-header").Single()).Color.ShouldBe(Color.FromHex("595959"));
    }

    [Fact]
    public void Should_replace_the_calendar_with_the_month_and_year_wheels()
    {
        var cut = Render(p => p.Add(nameof(IonDatetime.Presentation), "month-year"));
        cut.GetComputedStyle(cut.Root).ShouldNotBeNull();

        // On a grid the wheel container is hidden until its toggle opens it.
        var grid = Render();
        grid.FindLayoutBox(grid.FindByClass("datetime-year").Single()).ShouldBeNull();
        grid.FindLayoutBox(grid.FindByClass("calendar-body").Single()).ShouldNotBeNull();
    }

    [Fact]
    public void Should_order_wheel_columns_for_year_first_locales()
    {
        var cut = Render(p =>
        {
            p.Add(nameof(IonDatetime.Locale), "zh-CN");
            p.Add(nameof(IonDatetime.PreferWheel), true);
        });

        var columns = cut.FindByClass("ion-picker-column");
        Style(cut, columns.Single(c => c.HasClass("year-column"))).Order.ShouldBe(1);
        Style(cut, columns.Single(c => c.HasClass("month-column"))).Order.ShouldBe(2);
        Style(cut, columns.Single(c => c.HasClass("day-column"))).Order.ShouldBe(3);
        var year = cut.GetBoxModel(columns.Single(c => c.HasClass("year-column")))!.BorderBox;
        var day = cut.GetBoxModel(columns.Single(c => c.HasClass("day-column")))!.BorderBox;
        year.X.ShouldBeLessThan(day.X);
    }

    [Fact]
    public void Should_use_the_palette_color_on_ios()
    {
        UsePlatform(HostPlatform.Ios);
        var cut = Render(p => p.Add(nameof(IonDatetime.Color), "danger"));

        Style(cut, Day(cut, "15")).BackgroundColor.ShouldBe(new Color(Ios.Danger.R, Ios.Danger.G, Ios.Danger.B, 51));
        var next = cut.FindByClass("calendar-next-prev").Single().FindByClass("button-native")[1];
        Style(cut, next).Color.ShouldBe(Ios.Danger);
    }
}
