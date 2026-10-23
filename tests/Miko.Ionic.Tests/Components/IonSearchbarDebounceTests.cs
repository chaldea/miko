using Microsoft.Extensions.DependencyInjection;
using Miko.Core;
using Miko.Ionic.Tests.Fixtures;
using Miko.Ionic.Tests.Fixtures.Routed;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// <see cref="Miko.Ionic.Components.IonSearchbar.Debounce"/> (P0-C1): OnInput waits until typing
/// pauses, then reports the final value once; @bind-Value is not delayed. The parameter used to be
/// accepted and ignored. Time comes from an injected clock, never from sleeping (a sleep-based
/// assertion flips with the host's load).
/// </summary>
public sealed class IonSearchbarDebounceTests : IDisposable
{
    private readonly ManualClock _clock = new();
    private readonly RoutedApp _app;

    public IonSearchbarDebounceTests()
    {
        _app = new RoutedApp(
            builder => builder.Services.AddSingleton<TimeProvider>(_clock),
            ("/", typeof(SearchPageFixture)));
    }

    public void Dispose() => _app.Dispose();

    private string Text(string className) => _app.Single(className).TextContent ?? string.Empty;

    private void TypeInto(string searchbar, params string[] keystrokes)
    {
        _app.Tap(_app.Single(searchbar).FindByClass("searchbar-input").Single());
        foreach (var keystroke in keystrokes)
        {
            _app.App.Controller.OnTextInput(keystroke);
            _app.Render();
        }
    }

    [Fact]
    public void Should_raise_on_input_once_with_the_final_value_after_typing_pauses()
    {
        TypeInto("debounced", "a", "b", "c");

        Text("bound-value").ShouldBe("abc", "@bind-Value follows every keystroke");
        Text("search-log").ShouldBeEmpty("OnInput waits for the pause");

        _clock.Advance(TimeSpan.FromMilliseconds(299));
        _app.Render();
        Text("search-log").ShouldBeEmpty();

        _clock.Advance(TimeSpan.FromMilliseconds(1));
        _app.Render();
        Text("search-log").ShouldBe("abc");
    }

    [Fact]
    public void Should_restart_the_wait_on_every_edit()
    {
        TypeInto("debounced", "a");
        _clock.Advance(TimeSpan.FromMilliseconds(200));
        _app.App.Controller.OnTextInput("b");
        _app.Render();
        _clock.Advance(TimeSpan.FromMilliseconds(200));
        _app.Render();

        Text("search-log").ShouldBeEmpty("the second edit restarted the 300 ms wait");

        _clock.Advance(TimeSpan.FromMilliseconds(100));
        _app.Render();
        Text("search-log").ShouldBe("ab");
    }

    [Fact]
    public void Should_raise_on_input_for_every_edit_without_debounce()
    {
        TypeInto("immediate", "a", "b");

        Text("immediate-log").ShouldBe("a|ab");
    }

    /// <summary>A clock whose timers fire only when the test advances it.</summary>
    private sealed class ManualClock : TimeProvider
    {
        private readonly List<ManualTimer> _timers = new();
        private TimeSpan _now;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            timer.Change(dueTime, period);
            return timer;
        }

        public void Advance(TimeSpan by)
        {
            _now += by;
            foreach (var timer in _timers.ToArray())
            {
                if (timer.DueAt is { } due && due <= _now)
                {
                    timer.DueAt = null;
                    timer.Fire();
                }
            }
        }

        private sealed class ManualTimer(ManualClock clock, TimerCallback callback, object? state) : ITimer
        {
            public TimeSpan? DueAt { get; set; }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                DueAt = dueTime == Timeout.InfiniteTimeSpan ? null : clock._now + dueTime;
                if (!clock._timers.Contains(this)) clock._timers.Add(this);
                return true;
            }

            public void Fire() => callback(state);

            public void Dispose() => clock._timers.Remove(this);

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
