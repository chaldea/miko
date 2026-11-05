// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.CoreObjectsTests;

using LiveChartsCore.Kernel;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class ActionThrottlerTester
{
    public ActionThrottlerTester() => CoreTestsSetup.Ensure();

    [Fact]
    public async Task SameThread()
    {
        var runs = 0;

        Func<Task> action =
            () => Task.Run(() =>
            {
                runs++;
            });

        var throttler = new ActionThrottler(action, TimeSpan.FromMilliseconds(50));

        throttler.Call();
        throttler.Call();
        throttler.Call();
        throttler.Call();
        throttler.Call();
        throttler.Call();

        await Task.Delay(TimeSpan.FromSeconds(0.5));

        (runs == 1 && throttler.Calls == 6).ShouldBeTrue();
    }

    [Fact]
    public async Task MultipleThreads()
    {
        var runs = 0;

        Func<Task> action =
            () => Task.Run(() =>
            {
                runs++;
            });

        var throttler = new ActionThrottler(action, TimeSpan.FromMilliseconds(50));

        new Thread(() => throttler.Call()).Start();
        new Thread(() => throttler.Call()).Start();
        new Thread(() => throttler.Call()).Start();

        await Task.WhenAll(
            Task.Factory.StartNew(() => throttler.Call()),
            Task.Factory.StartNew(() => throttler.Call()),
            Task.Factory.StartNew(() => throttler.Call()),
            Task.Run(() => throttler.Call()),
            Task.Run(() => throttler.Call()),
            Task.Run(() => throttler.Call()));

        await Task.Delay(TimeSpan.FromSeconds(0.5));

        var calls = throttler.Calls;
        (runs == 1 && throttler.Calls == 9).ShouldBeTrue();
    }
}
