// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.SeriesTests;

using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Tooltips;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class PolarLineSeriesTest
{
    public PolarLineSeriesTest() => CoreTestsSetup.Ensure();

    [Fact]
    public void ShouldScaleProperly()
    {
        var sutSeries = new PolarLineSeries<ObservablePolarPoint>
        {
            Values = new[]
            {
                new ObservablePolarPoint(360 * 1 / 10d, 1),
                new ObservablePolarPoint(360 * 2 / 10d, 2),
                new ObservablePolarPoint(360 * 3 / 10d, 3),
                new ObservablePolarPoint(360 * 4 / 10d, 4),
                new ObservablePolarPoint(360 * 5 / 10d, 5),
                new ObservablePolarPoint(360 * 6 / 10d, 6),
                new ObservablePolarPoint(360 * 7 / 10d, 7),
                new ObservablePolarPoint(360 * 8 / 10d, 8),
                new ObservablePolarPoint(360 * 9 / 10d, 9),
                new ObservablePolarPoint(360 * 10 / 10d, 10)
            },
            GeometrySize = 0,
            IsClosed = false,
            Fill = null
        };

        var chart = new InMemoryPolarChart
        {
            Width = 300,
            Height = 300,
            View =
            {
                Series = new[] { sutSeries },
                AngleAxes = new[] { new PolarAxis { MinLimit = 0, MaxLimit = 360, IsVisible = false } },
                RadiusAxes = new[] { new PolarAxis { MinLimit = 0, MaxLimit = 10, IsVisible = false } }
            }
        };

        chart.GetImage().Dispose();
        //chart.SaveImage("test.png"); // use this method to see the actual tested image

        var datafactory = sutSeries.DataFactory;
        var points = datafactory.Fetch(sutSeries, chart.CoreChart).ToArray();

        var unit = points.First();
        var typedUnit = sutSeries.ConvertToTypedChartPoint(unit);
        var tuv = typedUnit.Visual!;

        var toCompareGuys = points.Where(x => x != unit).Select(sutSeries.ConvertToTypedChartPoint);

        var uHyp = Math.Sqrt(Math.Pow(tuv.X - 150, 2) + Math.Pow(tuv.Y - 150, 2));

        foreach (var sutPoint in toCompareGuys)
        {
            var sutHyp = Math.Sqrt(Math.Pow(sutPoint.Visual!.X - 150, 2) + Math.Pow(sutPoint.Visual.Y - 150, 2));
            (Math.Abs(uHyp * (1 + sutPoint.Index) - sutHyp) < 0.0001).ShouldBeTrue();
        }
    }

    [Fact]
    public void ShouldPlaceToolTipsCorrectly()
    {
        var sutSeries = new PolarLineSeries<double>
        {
            GeometrySize = 10,
            Values = new double[] { 1, 2, 3, 4, 5 },
            DataPadding = new LvcPoint(0, 0)
        };

        var tooltip = new DefaultTooltip { Easing = null! };

        var chart = new InMemoryPolarChart
        {
            Width = 300,
            Height = 300,
            ExplicitDisposing = true,
            View =
            {
                Tooltip = tooltip,
                TooltipPosition = TooltipPosition.Top,
                Series = new[] { sutSeries },
                AngleAxes = new[] { new PolarAxis { IsVisible = false } },
                RadiusAxes = new[] { new PolarAxis { IsVisible = true, MinLimit = 1, MaxLimit = 5 } }
            }
        };

        chart.CoreChart._isPointerIn = true;
        chart.CoreChart._isToolTipOpen = true;
        chart.CoreChart._pointerPosition = new(150, 150);

        chart.View.TooltipPosition = TooltipPosition.Top;
        chart.GetImage().Dispose();

        LvcRectangle tp;
        void UpdateTooltipRect()
        {
            var g = tooltip;
            tp = new LvcRectangle(new(g.X, g.Y), tooltip.Measure());
        }

        UpdateTooltipRect();
        (
            Math.Abs(tp.X + tp.Width * 0.5f - 150) < 0.1 &&
            Math.Abs(tp.Y - (150 - tp.Height)) < 0.1).ShouldBeTrue(
            "Tool tip on top failed");

        chart.View.TooltipPosition = TooltipPosition.Bottom;
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            Math.Abs(tp.X + tp.Width * 0.5f - 150) < 0.1 &&
            Math.Abs(tp.Y - 150) < 0.1).ShouldBeTrue(
            "Tool tip on bottom failed");

        chart.View.TooltipPosition = TooltipPosition.Left;
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            Math.Abs(tp.X - (150 - tp.Width)) < 0.1 &&
            Math.Abs(tp.Y + tp.Height * 0.5f - 150) < 0.1).ShouldBeTrue(
            "Tool tip on left failed");

        chart.View.TooltipPosition = TooltipPosition.Right;
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            Math.Abs(tp.X - 150) < 0.1 &&
            Math.Abs(tp.Y + tp.Height * 0.5f - 150) < 0.1).ShouldBeTrue(
            "Tool tip on right failed");

        chart.View.TooltipPosition = TooltipPosition.Center;
        chart.GetImage().Dispose();
        UpdateTooltipRect();
        (
            Math.Abs(tp.X + tp.Width * 0.5f - 150) < 0.1 &&
            Math.Abs(tp.Y + tp.Height * 0.5f - 150) < 0.1).ShouldBeTrue(
            "Tool tip on center failed");

        // ToDo? test auto pos? (the original's commented-out [AUTO] checks are not carried over)
    }
}
