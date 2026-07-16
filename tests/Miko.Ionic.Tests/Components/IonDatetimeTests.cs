using System.Globalization;
using Miko.Common;
using Miko.Components;
using Miko.Events;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-datetime</c> (default calendar/date view) and <c>ion-datetime-button</c>.
/// Covers the DOM contract (header + calendar header/weekdays/month grid + optional footer), the
/// month grid's leading-padding + day-count, active/today day marking, the value round-trip on day
/// selection, month navigation, and the datetime-button pair.
/// </summary>
public class IonDatetimeTests : IonicComponentTestBase
{
    private static ComponentUnderTest RenderDatetime(TestContext ctx,
        Action<ComponentParameterBuilder<IonDatetime>>? configure = null)
        => ctx.Render<IonDatetime>(p =>
        {
            // A fixed value keeps the visible month deterministic (July 2026).
            p.Add(nameof(IonDatetime.Value), "2026-07-15");
            configure?.Invoke(p);
        });

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonDatetime_RendersCalendarContract()
    {
        var cut = RenderDatetime(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-datetime");
        cut.Root.ShouldHaveClass("datetime-presentation-date");
        cut.Root.ShouldHaveClass("datetime-size-fixed");
        cut.FindByClass("datetime-calendar").ShouldHaveSingleItem();
        cut.FindByClass("calendar-header").ShouldHaveSingleItem();
        cut.FindByClass("calendar-days-of-week").ShouldHaveSingleItem();
        cut.FindByClass("calendar-body").ShouldHaveSingleItem();
        cut.FindByClass("calendar-month-grid").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonDatetime_RendersHeader_WhenShowDefaultTitle()
    {
        var cut = RenderDatetime(Context);

        cut.FindByClass("datetime-header").ShouldHaveSingleItem();
        cut.FindByClass("datetime-title").ShouldHaveSingleItem();
        cut.FindByClass("datetime-selected-date").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonDatetime_OmitsHeader_WhenShowDefaultTitleFalse()
    {
        var cut = RenderDatetime(Context, p => p.Add(nameof(IonDatetime.ShowDefaultTitle), false));

        cut.FindByClass("datetime-header").ShouldBeEmpty();
    }

    [Fact]
    public void IonDatetime_RendersFooter_WhenShowDefaultButtons()
    {
        var cut = RenderDatetime(Context, p => p.Add(nameof(IonDatetime.ShowDefaultButtons), true));

        cut.FindByClass("datetime-footer").ShouldHaveSingleItem();
        cut.FindByClass("datetime-action-button").Count.ShouldBe(2);
    }

    [Fact]
    public void IonDatetime_OmitsFooter_ByDefault()
    {
        var cut = RenderDatetime(Context);

        cut.FindByClass("datetime-footer").ShouldBeEmpty();
    }

    [Fact]
    public void IonDatetime_RendersSevenWeekdayLabels()
    {
        var cut = RenderDatetime(Context);

        cut.FindByClass("day-of-week").Count.ShouldBe(7);
    }

    // ---- Month grid --------------------------------------------------------

    [Fact]
    public void IonDatetime_July2026_RendersThirtyOneDayButtons()
    {
        var cut = RenderDatetime(Context);

        // July has 31 days; padding cells are separate (.calendar-day-padding).
        var dayButtons = cut.FindByClass("calendar-day")
            .Where(d => !d.HasClass("calendar-day-padding"))
            .ToList();
        dayButtons.Count.ShouldBe(31);
    }

    [Fact]
    public void IonDatetime_July2026_HasThreeLeadingPaddingCells()
    {
        // 2026-07-01 is a Wednesday → with Sunday as the first column, 3 leading blanks (Sun/Mon/Tue).
        var cut = RenderDatetime(Context);

        cut.FindByClass("calendar-day-padding").Count.ShouldBe(3);
    }

    [Fact]
    public void IonDatetime_MarksActiveDay()
    {
        var cut = RenderDatetime(Context);

        var active = cut.FindByClass("calendar-day-active").ShouldHaveSingleItem();
        active.TextContent.ShouldBe("15");
    }

    [Fact]
    public void IonDatetime_MarksTodayWhenVisible()
    {
        // Show the current month so "today" is present, and assert it is flagged.
        var today = DateTime.Today;
        var cut = Context.Render<IonDatetime>(p =>
            p.Add(nameof(IonDatetime.Value), today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));

        var todayCells = cut.FindByClass("calendar-day-today");
        todayCells.ShouldNotBeEmpty();
        todayCells.First().TextContent.ShouldBe(today.Day.ToString());
    }

    // ---- Interaction -------------------------------------------------------

    [Fact]
    public async Task IonDatetime_SelectDay_UpdatesValue()
    {
        string? changed = null;
        var datetime = new IonDatetime
        {
            Value = "2026-07-15",
            ValueChanged = EventCallback.Factory.Create<string?>(this, v => changed = v),
        };
        datetime.Build();

        await InvokeAsync(datetime, "SelectDayAsync", 20);

        changed.ShouldBe("2026-07-20");
    }

    [Fact]
    public void IonDatetime_NextMonth_AdvancesVisibleMonth()
    {
        var datetime = new IonDatetime { Value = "2026-07-15" };
        datetime.Build();

        Invoke(datetime, "NextMonth", new MouseEventArgs());

        // The month/year label reflects the advanced month (August 2026).
        var label = (string)datetime.GetType()
            .GetMethod("MonthYearLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(datetime, null)!;
        label.ShouldBe("August 2026");
    }

    [Fact]
    public void IonDatetime_PrevMonth_GoesBackVisibleMonth()
    {
        var datetime = new IonDatetime { Value = "2026-07-15" };
        datetime.Build();

        Invoke(datetime, "PrevMonth", new MouseEventArgs());

        var label = (string)datetime.GetType()
            .GetMethod("MonthYearLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(datetime, null)!;
        label.ShouldBe("June 2026");
    }

    [Fact]
    public void IonDatetime_Disabled_StampsClass()
    {
        var cut = RenderDatetime(Context, p => p.Add(nameof(IonDatetime.Disabled), true));

        cut.Root.ShouldHaveClass("datetime-disabled");
    }

    [Fact]
    public void IonDatetime_UsesIosClass_OnIosPlatform()
    {
        UsePlatform(HostPlatform.Ios);

        var cut = RenderDatetime(Context);

        cut.Root.Class.ShouldStartWith("ios ion-datetime");
    }

    // ---- Key style ---------------------------------------------------------

    [Fact]
    public void IonDatetime_ActiveDay_HasPrimaryBackground_Md()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = RenderDatetime(Context);

        var active = cut.FindByClass("calendar-day-active").ShouldHaveSingleItem();
        var bg = cut.GetComputedStyle(active)!.BackgroundColor;
        // md fills the active day with the solid primary color (#0054e9).
        bg.R.ShouldBe((byte)0x00);
        bg.G.ShouldBe((byte)0x54);
        bg.B.ShouldBe((byte)0xe9);
    }

    private static void Invoke(object component, string method, object arg)
    {
        var mi = component.GetType().GetMethod(method,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        mi.Invoke(component, new[] { arg });
    }

    private static async Task InvokeAsync(object component, string method, object arg)
    {
        var mi = component.GetType().GetMethod(method,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)mi.Invoke(component, new[] { arg })!;
    }
}
