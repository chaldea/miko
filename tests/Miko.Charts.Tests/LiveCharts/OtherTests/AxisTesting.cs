// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.OtherTests;

using System.Collections.ObjectModel;
using System.Diagnostics;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Painting;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class AxisTesting
{
    public AxisTesting() => CoreTestsSetup.Ensure();

    [Fact]
    public void BasicCase()
    {
        var x = new Axis();
        var y = new Axis();

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series =
                [
                    new LineSeries<double>
                    {
                        Values = [1, 2, 3]
                    }
                ],
                XAxes = [x],
                YAxes = [y]
            }
        };

        chart.GetImage().Dispose();

        // ensure the separators are created
        (x.activeSeparators[chart.CoreChart].Values.Count > 0).ShouldBeTrue();
        (y.activeSeparators[chart.CoreChart].Values.Count > 0).ShouldBeTrue();
    }

    [Fact]
    public void RejectUnnecessaryCases()
    {
        // The axis class throws when ~10,000 separators are drawn in the same axis (only in DEBUG mode).

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series =
                [
                    new LineSeries<double>
                    {
                        Values = [1, 2, 3]
                    },
                ],
                XAxes = [new Axis { MinLimit = 0, MaxLimit = 100000, ForceStepToMin = true, MinStep = 1, LabelsPaint = new SolidColorPaint(TestColors.Red) }]
            }
        };

        bool succeed;
        try
        {
            chart.GetImage().Dispose();
            succeed = true;
        }
        catch
        {
            succeed = false;
        }

        (!succeed).ShouldBeTrue();
    }

    [Fact]
    public void AutoLimits()
    {
        // based on https://github.com/beto-rodriguez/LiveCharts2/issues/755
        // when the range in the axis is zero, there is a change that the for instruction that
        // draws the separators never ends, this is because the step is calculated as 0
        // this test ensures the step is never 0.

        var x = new Axis { MinLimit = 10, MaxLimit = 10, LabelsPaint = new SolidColorPaint(TestColors.Red) };

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series =
                [
                    new LineSeries<double>
                    {
                        Values = [1, 2, 3]
                    }
                ],
                XAxes = [x]
            }
        };

        var sw = new Stopwatch();
        sw.Start();

        chart.GetImage().Dispose();
        (x.activeSeparators[chart.CoreChart].Values.Count > 0 && sw.ElapsedMilliseconds < 1000).ShouldBeTrue();
    }

    [Fact]
    public void DateTimeWithInvalidUnitWidth()
    {
        // based on https://github.com/beto-rodriguez/LiveCharts2/issues/763

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series =
                [
                    new ColumnSeries<DateTimePoint>
                    {
                        Values = new ObservableCollection<DateTimePoint>
                        {
                            new(new DateTime(2021, 1, 1), 3)
                        }
                    }
                ]
            }
        };

        var sw = new Stopwatch();
        sw.Start();
        chart.GetImage().Dispose();
        (sw.ElapsedMilliseconds < 1000).ShouldBeTrue();
    }

    [Fact]
    public void DoublePrecisionPossibleError()
    {
        // I have seen cases (not able to reproduce yet) where the calculated step in the axis is smaller than the double type precision.
        // this results in an infinite loop when drawing the separators.
        // I am not completely sure if that issue is already fixed, also not able to reproduce it.

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series =
                [
                    new LineSeries<double>
                    {
                        Values = [1, 2, 3]
                    }
                ],
                XAxes = [new Axis { MinLimit = 0, MaxLimit = 1e100, LabelsPaint = new SolidColorPaint(TestColors.Red) }]
            }
        };

        var sw = new Stopwatch();
        sw.Start();
        chart.GetImage().Dispose();
        (sw.ElapsedMilliseconds < 1000).ShouldBeTrue();
    }

    [Fact]
    public void InvertedAxis()
    {
        var x1 = new Axis { MaxLimit = 10, MinLimit = 0 };
        var y1 = new Axis { MaxLimit = 10, MinLimit = 0 };
        var chart1 = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = [new LineSeries<double>([1, 2, 3])],
                XAxes = [x1],
                YAxes = [y1]
            }
        };

        var x2 = new Axis { MinLimit = 0, MaxLimit = 10, IsInverted = true };
        var y2 = new Axis { MinLimit = 0, MaxLimit = 10, IsInverted = true };
        var chart2 = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = [new LineSeries<double>([1, 2, 3])],
                XAxes = [x2],
                YAxes = [y2]
            }
        };

        _ = ChartDrawing.DrawChart(chart1);
        _ = ChartDrawing.DrawChart(chart2);

        (x1._size.Width > 0 && x2._size.Height > 0).ShouldBeTrue();
        (x1._size == x2._size).ShouldBeTrue();
        (x1.activeSeparators.Count == x2.activeSeparators.Count).ShouldBeTrue();

        (y1._size.Width > 0 && y2._size.Height > 0).ShouldBeTrue();
        (y1._size == y2._size).ShouldBeTrue();
        (y1.activeSeparators.Count == y2.activeSeparators.Count).ShouldBeTrue();
    }

    [Fact]
    public void LimitsValidation()
    {
        // When an axis has not enough data, it must decide the limits by itself.
        // this test ensures that this decision is made correctly.

        // when there is no data in the axis, the limits are initialized to
        // double.MaxValue or double.MinValue depending on the axis direction.
        const double unset = double.MaxValue;
        const double minDefault = 0;
        const double maxDefault = 10;

        // ===============================================================
        // CASE 1
        // when both not set, limits are 0 and 10, just a default value.
        // ===============================================================
        var max = unset;
        var min = unset;
        var step = 0d; // the step is 0 by default, meaning that the library will decide it.

        AxisLimit.ValidateLimits(ref min, ref max, step);
        (min == minDefault && max == maxDefault).ShouldBeTrue();

        // ===============================================================
        // CASE 2
        // when the step is defined, we need to consider it.
        // ===============================================================
        max = unset;
        min = unset;
        step = 0.1;

        AxisLimit.ValidateLimits(ref min, ref max, step);
        (min == minDefault * step && max == maxDefault * step).ShouldBeTrue();

        // ==============================================================
        // CASE 3
        // when the min is set, but the max is not, we need to calculate the max
        // ===============================================================
        min = 5;
        max = unset;
        step = 0d;

        AxisLimit.ValidateLimits(ref min, ref max, step);
        (min == 5 && max == 5 + (maxDefault - minDefault)).ShouldBeTrue();

        // ==============================================================
        // CASE 4
        // when the max is set, but the min is not, we need to calculate the min
        // ===============================================================
        min = unset;
        max = 5;
        step = 0d;

        AxisLimit.ValidateLimits(ref min, ref max, step);
        (min == 5 - (maxDefault - minDefault) && max == 5).ShouldBeTrue();

        // ==============================================================
        // CASE 5
        // when both are set, we do nothing.
        // ===============================================================
        min = 5;
        max = 10;
        step = 0d;

        AxisLimit.ValidateLimits(ref min, ref max, step);
        (min == 5 && max == 10).ShouldBeTrue();

        // ==============================================================
        // CASE 6
        // when both are set, but the step is defined, we do nothing.
        // ===============================================================
        min = 5;
        max = 10;
        step = 0.1;

        AxisLimit.ValidateLimits(ref min, ref max, step);
        (min == 5 && max == 10).ShouldBeTrue();

        // ==============================================================
        // CASE 7
        // when min is set, max is not, step defined, we calculate the max
        // ===============================================================
        min = 5;
        max = unset;
        step = 0.1;

        AxisLimit.ValidateLimits(ref min, ref max, step);
        (min == 5 && max == 5 + (maxDefault - minDefault) * step).ShouldBeTrue();

        // ==============================================================
        // CASE 8
        // when max is set, min is not, step defined, we calculate the min
        // ===============================================================
        min = unset;
        max = 5;
        step = 0.1;

        AxisLimit.ValidateLimits(ref min, ref max, step);
        (min == 5 - (maxDefault - minDefault) * step && max == 5).ShouldBeTrue();

        // ==============================================================
        // CASE 9
        // ensure double.MinValue is also treated as unset.
        // ===============================================================
        min = double.MinValue;
        max = double.MinValue;
        step = 0d;

        AxisLimit.ValidateLimits(ref min, ref max, step);
        (min == minDefault && max == maxDefault).ShouldBeTrue();
    }
}
