// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.OtherTests;

using System.Diagnostics;
using System.Reflection;
using LiveChartsCore;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Painting;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class UpdateThrottlerTesting
{
    public UpdateThrottlerTesting() => CoreTestsSetup.Ensure();

    private sealed class CapturingTraceListener : TraceListener
    {
        private readonly System.Text.StringBuilder _buffer = new();

        public string Output => _buffer.ToString();

        public override void Write(string? message) => _ = _buffer.Append(message);
        public override void WriteLine(string? message) => _ = _buffer.AppendLine(message);
    }

    [Fact]
    public async Task ThrottledMeasure_Exception_IsTracedNotSwallowed()
    {
        // regression for #1826: UpdateThrottlerUnlocked schedules Measure inside a
        // fire-and-forget Task.Run. Without the try/catch, an exception during
        // Measure disappears into the unobserved-task pipeline and the developer
        // sees a silently blank chart. The fix wraps the measure body and writes
        // the exception to Trace so a TraceListener can surface it.

        // build a chart that's guaranteed to throw inside Measure: an axis with
        // a forced tiny step over a wide range trips ThrowInfiniteSeparators.
        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series =
                [
                    new LineSeries<double> { Values = [1, 2, 3] }
                ],
                XAxes =
                [
                    new Axis
                    {
                        MinLimit = 0,
                        MaxLimit = 100000,
                        ForceStepToMin = true,
                        MinStep = 1,
                        LabelsPaint = new SolidColorPaint(TestColors.Red),
                    }
                ],
            }
        };

        var coreChart = chart.CoreChart;
        coreChart.IsLoaded = true;

        var listener = new CapturingTraceListener();
        Trace.Listeners.Add(listener);
        try
        {
            var method = typeof(Chart).GetMethod(
                "UpdateThrottlerUnlocked",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method.ShouldNotBeNull("UpdateThrottlerUnlocked not found via reflection");

            var task = (Task)method.Invoke(coreChart, null)!;

            // without the fix, await rethrows the unobserved exception. the test
            // would already fail here. with the fix, the catch swallows it and
            // surfaces it via Trace.
            await task;

            listener.Output.Contains("chart update failed").ShouldBeTrue(
                $"Expected trace output to mention the failure. Got: {listener.Output}");
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }
    }
}
