using Microsoft.Extensions.DependencyInjection;
using Miko.Core;
using Miko.Ionic.Tests.Fixtures;
using Miko.Ionic.Tests.Fixtures.Routed;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// <c>ion-datetime</c> in a real app: routed page, <c>IonApp</c> overlay layer, Ionic styles from
/// the registry, and taps entering through the interaction controller. Covers the bound value,
/// the time popover in the overlay layer, and Done / Cancel closing the enclosing modal with
/// Ionic's <c>datetime-confirm</c> / <c>datetime-cancel</c> roles.
/// </summary>
public sealed class IonDatetimeEndToEndTests : IDisposable
{
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private readonly RoutedApp _app = new(
        builder => builder.Services.AddSingleton<TimeProvider>(new FixedClock(new DateTimeOffset(2026, 7, 10, 9, 5, 0, TimeSpan.Zero))),
        ("/", typeof(DatetimeInteractionPageFixture)));

    public void Dispose() => _app.Dispose();

    private Element Picker(string className) => _app.Single(className);

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

    private Element Day(string picker, string day) => Picker(picker).FindByClass("calendar-day")
        .Single(d => d.HasClass("calendar-day") && !d.HasClass("calendar-day-padding") && Text(d) == day);

    private float ScrollTopOf(Element element)
    {
        return Find(_app.App.Engine.GetCurrentLayout().ShouldNotBeNull())!.ScrollTop;

        Miko.Layout.LayoutBox? Find(Miko.Layout.LayoutBox box)
        {
            if (ReferenceEquals(box.Element, element)) return box;
            foreach (var child in box.Children)
                if (Find(child) is { } found) return found;
            return null;
        }
    }

    private void Tap(Element element)
    {
        _app.Tap(element);
        _app.Render();
    }

    [Fact]
    public void Should_write_a_tapped_day_to_the_bound_value()
    {
        Tap(Day("inline-picker", "20"));

        Text(_app.Single("bound-value")).ShouldBe("2026-07-20T13:30:00");
        Text(Picker("inline-picker").FindByClass("calendar-day-active").Single()).ShouldBe("20");
    }

    [Fact]
    public void Should_page_months_with_the_navigation_buttons()
    {
        var next = Picker("inline-picker").FindByClass("calendar-next-prev").Single().FindByClass("button-native")[1];

        Tap(next);

        Text(Picker("inline-picker").FindByClass("calendar-month-year-toggle").Single()).ShouldBe("August 2026");
        Text(_app.Single("bound-value")).ShouldBe("2026-07-15T13:30:00");
    }

    [Fact]
    public void Should_pick_the_time_in_a_popover_of_the_overlay_layer()
    {
        Tap(Picker("inline-picker").FindByClass("time-body").Single());

        var host = _app.Single("ion-overlay-host");
        var popover = host.FindByClass("datetime-time-popover").ShouldHaveSingleItem();
        popover.HasClass("overlay-hidden").ShouldBeFalse();
        var hours = popover.FindByClass("ion-picker-column")[0];

        // The wheel is built when the popover opens, so it starts on the current hour: 1 PM is
        // option 1, centered under the 200px wheel at (3 spacers + 1) * 34 + 17 - 100.
        var opts = hours.FindByClass("picker-opts").Single();
        ScrollTopOf(opts).ShouldBe((3 + 1) * 34 + 17 - 100, 0.5);

        var three = hours.FindByClass("ion-picker-column-option").Single(o => Text(o) == "3");
        three.OnClick!.Invoke(new Miko.Events.MouseEventArgs { Target = three });
        _app.Render();

        Text(_app.Single("bound-value")).ShouldBe("2026-07-15T15:30:00");
        Text(Picker("inline-picker").FindByClass("time-body").Single()).ShouldBe("3:30\u00A0PM");
    }

    [Fact]
    public void Should_confirm_in_a_modal_and_close_it()
    {
        Tap(_app.Single("open-modal").FindByClass("button-native").Single());
        Tap(Day("modal-picker", "22"));
        Text(_app.Single("modal-value")).ShouldBe("2026-07-15");

        Tap(_app.Root.FindById("confirm-button")!.FindByClass("button-native").Single());

        Text(_app.Single("modal-value")).ShouldBe("2026-07-22");
        Text(_app.Single("modal-role")).ShouldBe("datetime-confirm");
        _app.Root.FindByClass("modal-picker").ShouldBeEmpty();
    }

    [Fact]
    public void Should_cancel_in_a_modal_and_close_it()
    {
        Tap(_app.Single("open-modal").FindByClass("button-native").Single());
        Tap(Day("modal-picker", "22"));

        Tap(_app.Root.FindById("cancel-button")!.FindByClass("button-native").Single());

        Text(_app.Single("modal-value")).ShouldBe("2026-07-15");
        Text(_app.Single("modal-role")).ShouldBe("datetime-cancel");
        _app.Root.FindByClass("modal-picker").ShouldBeEmpty();
    }

    [Fact]
    public void Should_lay_the_calendar_out_in_seven_equal_columns_and_six_rows()
    {
        var picker = Picker("inline-picker");
        var weekdays = picker.FindByClass("day-of-week").Select(_app.BorderBoxOf).ToList();
        weekdays.Count.ShouldBe(7);
        weekdays.Select(b => MathF.Round(b.Width, 1)).Distinct().Count().ShouldBe(1);
        weekdays.Zip(weekdays.Skip(1)).ShouldAllBe(pair => pair.Second.X > pair.First.X);

        // MD keeps six week rows of 42px (plus 4px padding above and below).
        _app.BorderBoxOf(picker.FindByClass("calendar-month-grid").Single()).Height.ShouldBe(6 * 42 + 8, 0.5);
        var first = _app.BorderBoxOf(Day("inline-picker", "1"));
        var eighth = _app.BorderBoxOf(Day("inline-picker", "8"));
        eighth.X.ShouldBe(first.X, 0.5);
        (eighth.Y - first.Y).ShouldBe(42, 0.5);
        // The datetime is at most 350px wide.
        _app.BorderBoxOf(picker).Width.ShouldBe(350, 0.5);
    }
}
