// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

// LabelVisual / GeometryVisual / StackPanel and the other legacy visuals are obsolete but still tested (the source project set NoWarn CS0618).
#pragma warning disable CS0618

namespace Miko.Charts.Tests.LiveCharts.OtherTests;

using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Painting;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.VisualElements;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class ScaleFunctionsTests
{
    public ScaleFunctionsTests() => CoreTestsSetup.Ensure();

    [Fact]
    public void CartesianBasicScale()
    {
        var chart = new InMemoryCartesianChart
        {
            Width = 500,
            Height = 500,
            View =
            {
                Series = new[]
                {
                    new ScatterSeries<double>
                    {
                        Values = new double[] { 0, 1, 2 },
                        DataPadding = new LvcPoint(0, 0)
                    }
                },
                DrawMarginFrame = new DrawMarginFrame() { Stroke = new SolidColorPaint(TestColors.Yellow, 5) }
            }
        };

        chart.GetImage().Dispose();

        var location = chart.CoreChart.DrawMarginLocation;
        var size = chart.CoreChart.DrawMarginSize;

        var p00 = chart.View.ScaleDataToPixels(new LvcPointD(0, 0));
        (Math.Abs(p00.X - location.X) < 0.01 &&
            Math.Abs(p00.Y - (location.Y + size.Height)) < 0.01).ShouldBeTrue();

        var p33 = chart.View.ScaleDataToPixels(new LvcPointD(2, 2));
        (Math.Abs(p33.X - (location.X + size.Width)) < 0.01 &&
            Math.Abs(p33.Y - location.Y) < 0.01).ShouldBeTrue();

        var d00 = chart.View.ScalePixelsToData(new LvcPointD(location.X, location.Y + size.Height));
        (Math.Abs(d00.X - 0) < 0.01 &&
            Math.Abs(d00.Y - 0) < 0.01).ShouldBeTrue();

        var d33 = chart.View.ScalePixelsToData(new LvcPointD(location.X + size.Width, location.Y));
        (Math.Abs(d33.X - 2) < 0.01 &&
            Math.Abs(d33.Y - 2) < 0.01).ShouldBeTrue();
    }

    [Fact]
    public void CartesianTitleAndLegendsTopScale()
    {
        var chart = new InMemoryCartesianChart
        {
            Width = 500,
            Height = 500,
            View =
            {
                Series = new[]
                {
                    new ScatterSeries<double>
                    {
                        Values = new double[] { 0, 1, 2 },
                        DataPadding = new LvcPoint(0, 0)
                    }
                },
                DrawMarginFrame = new DrawMarginFrame() { Stroke = new SolidColorPaint(TestColors.Yellow, 5) },
                LegendPosition = LegendPosition.Top,
                Title = new LabelVisual
                {
                    Text = "My chart title",
                    TextSize = 25,
                    Padding = new Padding(10),
                    Paint = new SolidColorPaint(TestColors.White),
                    BackgroundColor = TestColors.Purple
                }
            }
        };

        chart.GetImage().Dispose();

        var location = chart.CoreChart.DrawMarginLocation;
        var size = chart.CoreChart.DrawMarginSize;

        var p00 = chart.View.ScaleDataToPixels(new LvcPointD(0, 0));
        (Math.Abs(p00.X - location.X) < 0.01 &&
            Math.Abs(p00.Y - (location.Y + size.Height)) < 0.01).ShouldBeTrue();

        var p33 = chart.View.ScaleDataToPixels(new LvcPointD(2, 2));
        (Math.Abs(p33.X - (location.X + size.Width)) < 0.01 &&
            Math.Abs(p33.Y - location.Y) < 0.01).ShouldBeTrue();

        var d00 = chart.View.ScalePixelsToData(new LvcPointD(location.X, location.Y + size.Height));
        (Math.Abs(d00.X - 0) < 0.01 &&
            Math.Abs(d00.Y - 0) < 0.01).ShouldBeTrue();

        var d33 = chart.View.ScalePixelsToData(new LvcPointD(location.X + size.Width, location.Y));
        (Math.Abs(d33.X - 2) < 0.01 &&
            Math.Abs(d33.Y - 2) < 0.01).ShouldBeTrue();
    }

    [Fact]
    public void CartesianTitleAndLegendsBottomScale()
    {
        var chart = new InMemoryCartesianChart
        {
            Width = 500,
            Height = 500,
            View =
            {
                Series = new[]
                {
                    new ScatterSeries<double>
                    {
                        Values = new double[] { 0, 1, 2 },
                        DataPadding = new LvcPoint(0, 0)
                    }
                },
                DrawMarginFrame = new DrawMarginFrame() { Stroke = new SolidColorPaint(TestColors.Yellow, 5) },
                LegendPosition = LegendPosition.Bottom,
                Title = new LabelVisual
                {
                    Text = "My chart title",
                    TextSize = 25,
                    Padding = new Padding(10),
                    Paint = new SolidColorPaint(TestColors.White),
                    BackgroundColor = TestColors.Purple
                }
            }
        };

        chart.GetImage().Dispose();

        var location = chart.CoreChart.DrawMarginLocation;
        var size = chart.CoreChart.DrawMarginSize;

        var p00 = chart.View.ScaleDataToPixels(new LvcPointD(0, 0));
        (Math.Abs(p00.X - location.X) < 0.01 &&
            Math.Abs(p00.Y - (location.Y + size.Height)) < 0.01).ShouldBeTrue();

        var p33 = chart.View.ScaleDataToPixels(new LvcPointD(2, 2));
        (Math.Abs(p33.X - (location.X + size.Width)) < 0.01 &&
            Math.Abs(p33.Y - location.Y) < 0.01).ShouldBeTrue();

        var d00 = chart.View.ScalePixelsToData(new LvcPointD(location.X, location.Y + size.Height));
        (Math.Abs(d00.X - 0) < 0.01 &&
            Math.Abs(d00.Y - 0) < 0.01).ShouldBeTrue();

        var d33 = chart.View.ScalePixelsToData(new LvcPointD(location.X + size.Width, location.Y));
        (Math.Abs(d33.X - 2) < 0.01 &&
            Math.Abs(d33.Y - 2) < 0.01).ShouldBeTrue();
    }

    [Fact]
    public void CartesianTitleAndLegendsLeftScale()
    {
        var chart = new InMemoryCartesianChart
        {
            Width = 500,
            Height = 500,
            View =
            {
                Series = new[]
                {
                    new ScatterSeries<double>
                    {
                        Values = new double[] { 0, 1, 2 },
                        DataPadding = new LvcPoint(0, 0)
                    }
                },
                DrawMarginFrame = new DrawMarginFrame() { Stroke = new SolidColorPaint(TestColors.Yellow, 5) },
                LegendPosition = LegendPosition.Left,
                Title = new LabelVisual
                {
                    Text = "My chart title",
                    TextSize = 25,
                    Padding = new Padding(10),
                    Paint = new SolidColorPaint(TestColors.White),
                    BackgroundColor = TestColors.Purple
                }
            }
        };

        chart.GetImage().Dispose();

        var location = chart.CoreChart.DrawMarginLocation;
        var size = chart.CoreChart.DrawMarginSize;

        var p00 = chart.View.ScaleDataToPixels(new LvcPointD(0, 0));
        (Math.Abs(p00.X - location.X) < 0.01 &&
            Math.Abs(p00.Y - (location.Y + size.Height)) < 0.01).ShouldBeTrue();

        var p33 = chart.View.ScaleDataToPixels(new LvcPointD(2, 2));
        (Math.Abs(p33.X - (location.X + size.Width)) < 0.01 &&
            Math.Abs(p33.Y - location.Y) < 0.01).ShouldBeTrue();

        var d00 = chart.View.ScalePixelsToData(new LvcPointD(location.X, location.Y + size.Height));
        (Math.Abs(d00.X - 0) < 0.01 &&
            Math.Abs(d00.Y - 0) < 0.01).ShouldBeTrue();

        var d33 = chart.View.ScalePixelsToData(new LvcPointD(location.X + size.Width, location.Y));
        (Math.Abs(d33.X - 2) < 0.01 &&
            Math.Abs(d33.Y - 2) < 0.01).ShouldBeTrue();
    }

    [Fact]
    public void CartesianTitleAndLegendsRightScale()
    {
        var chart = new InMemoryCartesianChart
        {
            Width = 500,
            Height = 500,
            View =
            {
                Series = new[]
                {
                    new ScatterSeries<double>
                    {
                        Values = new double[] { 0, 1, 2 },
                        DataPadding = new LvcPoint(0, 0)
                    }
                },
                DrawMarginFrame = new DrawMarginFrame() { Stroke = new SolidColorPaint(TestColors.Yellow, 5) },
                LegendPosition = LegendPosition.Right,
                Title = new LabelVisual
                {
                    Text = "My chart title",
                    TextSize = 25,
                    Padding = new Padding(10),
                    Paint = new SolidColorPaint(TestColors.White),
                    BackgroundColor = TestColors.Purple
                }
            }
        };

        chart.GetImage().Dispose();

        var location = chart.CoreChart.DrawMarginLocation;
        var size = chart.CoreChart.DrawMarginSize;

        var p00 = chart.View.ScaleDataToPixels(new LvcPointD(0, 0));
        (Math.Abs(p00.X - location.X) < 0.01 &&
            Math.Abs(p00.Y - (location.Y + size.Height)) < 0.01).ShouldBeTrue();

        var p33 = chart.View.ScaleDataToPixels(new LvcPointD(2, 2));
        (Math.Abs(p33.X - (location.X + size.Width)) < 0.01 &&
            Math.Abs(p33.Y - location.Y) < 0.01).ShouldBeTrue();

        var d00 = chart.View.ScalePixelsToData(new LvcPointD(location.X, location.Y + size.Height));
        (Math.Abs(d00.X - 0) < 0.01 &&
            Math.Abs(d00.Y - 0) < 0.01).ShouldBeTrue();

        var d33 = chart.View.ScalePixelsToData(new LvcPointD(location.X + size.Width, location.Y));
        (Math.Abs(d33.X - 2) < 0.01 &&
            Math.Abs(d33.Y - 2) < 0.01).ShouldBeTrue();
    }

    // Regression for issue #1533: when the draw margin collapses (here, the
    // explicit DrawMargin's left + right equal the chart width, so the scaler's
    // pixel delta is 0), ScalePixelsToData must still return finite values.
    // Otherwise the user-side range math (e.g. positionInData.X - range/2 in
    // the scrollbar/sections sample) propagates ±Infinity into the section's
    // Xi/Xj, which then turns into NaN on the next tick (Inf - Inf) and the
    // section freezes.
    [Fact]
    public void CartesianScale_ZeroWidthDrawMargin_ReturnsFiniteValues()
    {
        var chart = new InMemoryCartesianChart
        {
            Width = 150,
            Height = 100,
            View =
            {
                DrawMargin = new Margin(100, Margin.Auto, 50, Margin.Auto),
                XAxes = [new Axis { MinLimit = 0, MaxLimit = 100 }],
                Series = [new LineSeries<double> { Values = [1d, 2d, 3d] }]
            }
        };

        chart.GetImage().Dispose();

        var loc = chart.CoreChart.DrawMarginLocation;

        var atMinPx = chart.View.ScalePixelsToData(new LvcPointD(loc.X, loc.Y));
        var awayFromMinPx = chart.View.ScalePixelsToData(new LvcPointD(loc.X + 50, loc.Y));

        (IsFinite(atMinPx.X) && IsFinite(atMinPx.Y)).ShouldBeTrue($"ScalePixelsToData at _minPx returned ({atMinPx.X}, {atMinPx.Y})");
        (IsFinite(awayFromMinPx.X) && IsFinite(awayFromMinPx.Y)).ShouldBeTrue($"ScalePixelsToData away from _minPx returned ({awayFromMinPx.X}, {awayFromMinPx.Y})");

        static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    }
}
