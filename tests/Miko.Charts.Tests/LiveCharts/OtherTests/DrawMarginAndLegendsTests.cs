// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

// LabelVisual is obsolete but still tested (the source project set NoWarn CS0618).
#pragma warning disable CS0618

namespace Miko.Charts.Tests.LiveCharts.OtherTests;

using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Extensions;
using LiveChartsCore.MikoView.Painting;
using LiveChartsCore.MikoView.Tooltips;
using LiveChartsCore.MikoView.VisualElements;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class DrawMarginAndLegendsTests
{
    public DrawMarginAndLegendsTests() => CoreTestsSetup.Ensure();

    [Fact]
    public void CartesianBasicCase()
    {
        var legend = new DefaultLegend();

        var chart = new InMemoryCartesianChart
        {
            Width = 500,
            Height = 500,
            ExplicitDisposing = true,
            View =
            {
                Series = new[] { new LineSeries<double> { Values = new double[] { 1, 2, 3 } } },
                XAxes = new[] { new Axis { IsVisible = false, } },
                YAxes = new[] { new Axis { IsVisible = false, } },
                DrawMarginFrame = new DrawMarginFrame() { Stroke = new SolidColorPaint(TestColors.Yellow, 5) },
                LegendPosition = LegendPosition.Top,
                Legend = legend,
            }
        };

        // TEST TOP LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Top;
        chart.GetImage().Dispose();

        var legendSize = legend.Measure(chart.CoreChart);
        var dml = chart.CoreChart.DrawMarginLocation;
        var dms = chart.CoreChart.DrawMarginSize;
        var x = legend.X;
        var y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - legendSize.Height) < 0.01).ShouldBeTrue();

        // x should be centered
        (Math.Abs(x - (500 * 0.5f - legendSize.Width * 0.5f)) < 0.01).ShouldBeTrue();

        // y should be 0
        (Math.Abs(y - 0) < 0.01).ShouldBeTrue();

        // TEST BOTTOM LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Bottom;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - legendSize.Height) < 0.01).ShouldBeTrue();

        // x should be centered
        (Math.Abs(x - (500 * 0.5f - legendSize.Width * 0.5f)) < 0.01).ShouldBeTrue();

        // y should be 500 - legendsSize.Height
        (Math.Abs(y - (500 - legendSize.Height)) < 0.01).ShouldBeTrue();

        // TEST LEFT LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Left;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width - legendSize.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height) < 0.01).ShouldBeTrue();

        // x should be 0
        (Math.Abs(x) < 0.01).ShouldBeTrue();

        // y should be centered
        (Math.Abs(y - (500 * 0.5f - legendSize.Height * 0.5f)) < 0.01).ShouldBeTrue();

        // TEST RIGHT LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Right;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width - legendSize.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height) < 0.01).ShouldBeTrue();

        // x should be 500 - legendsSize.Width
        (Math.Abs(x - (500 - legendSize.Width)) < 0.01).ShouldBeTrue();

        // y should be centered
        (Math.Abs(y - (500 * 0.5f - legendSize.Height * 0.5f)) < 0.01).ShouldBeTrue();
    }

    [Fact]
    public void CartesianWithTitleCase()
    {
        var legend = new DefaultLegend();
        legend.Geometry.Fill = new SolidColorPaint(TestColors.LightGray);

        var title = new LabelVisual
        {
            Text = "My chart title",
            TextSize = 25,
            Padding = new Padding(10),
            Paint = new SolidColorPaint(TestColors.White),
            BackgroundColor = TestColors.Purple
        };

        var chart = new InMemoryCartesianChart
        {
            Width = 500,
            Height = 500,
            ExplicitDisposing = true,
            View =
            {
                Series = new[] { new LineSeries<double> { Values = new double[] { 1, 2, 3 } } },
                XAxes = new[] { new Axis { IsVisible = false, } },
                YAxes = new[] { new Axis { IsVisible = false, } },
                DrawMarginFrame = new DrawMarginFrame() { Stroke = new SolidColorPaint(TestColors.Yellow, 5) },
                LegendPosition = LegendPosition.Top,
                Title = title,
                Legend = legend,
            }
        };

        // TEST TOP LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Top;
        chart.GetImage().Dispose();

        var legendSize = legend.Measure(chart.CoreChart);
        var titleSize = title.Measure(chart.CoreChart);
        var dml = chart.CoreChart.DrawMarginLocation;
        var dms = chart.CoreChart.DrawMarginSize;
        var x = legend.X;
        var y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - legendSize.Height - titleSize.Height) < 0.01).ShouldBeTrue();

        // x should be centered
        (Math.Abs(x - (500 * 0.5f - legendSize.Width * 0.5f)) < 0.01).ShouldBeTrue();

        // y should be titleSize.Height
        (Math.Abs(y - titleSize.Height) < 0.01).ShouldBeTrue();

        // TEST BOTTOM LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Bottom;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - legendSize.Height - titleSize.Height) < 0.01).ShouldBeTrue();

        // x should be centered
        (Math.Abs(x - (500 * 0.5f - legendSize.Width * 0.5f)) < 0.01).ShouldBeTrue();

        // y should be 500 - legendsSize.Height
        (Math.Abs(y - (500 - legendSize.Height)) < 0.01).ShouldBeTrue();

        // TEST LEFT LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Left;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width - legendSize.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - titleSize.Height) < 0.01).ShouldBeTrue();

        // x should be 0
        (Math.Abs(x) < 0.01).ShouldBeTrue();

        // y should be centered
        (Math.Abs(y - (500 * 0.5f - legendSize.Height * 0.5f)) < 0.01).ShouldBeTrue();

        // TEST RIGHT LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Right;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width - legendSize.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - titleSize.Height) < 0.01).ShouldBeTrue();

        // x should be 500 - legendsSize.Width
        (Math.Abs(x - (500 - legendSize.Width)) < 0.01).ShouldBeTrue();

        // y should be centered
        (Math.Abs(y - (500 * 0.5f - legendSize.Height * 0.5f)) < 0.01).ShouldBeTrue();
    }

    [Fact]
    public void PieBasicCase()
    {
        var legend = new DefaultLegend();

        var chart = new InMemoryPieChart
        {
            Width = 500,
            Height = 500,
            ExplicitDisposing = true,
            View =
            {
                Series = new[] { 1, 2, 3 }.AsPieSeries(),
                LegendPosition = LegendPosition.Top,
                Legend = legend,
            }
        };

        // TEST TOP LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Top;
        chart.GetImage().Dispose();

        var legendSize = legend.Measure(chart.CoreChart);
        var dml = chart.CoreChart.DrawMarginLocation;
        var dms = chart.CoreChart.DrawMarginSize;
        var x = legend.X;
        var y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - legendSize.Height) < 0.01).ShouldBeTrue();

        // x should be centered
        (Math.Abs(x - (500 * 0.5f - legendSize.Width * 0.5f)) < 0.01).ShouldBeTrue();

        // y should be 0
        (Math.Abs(y - 0) < 0.01).ShouldBeTrue();

        // TEST BOTTOM LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Bottom;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - legendSize.Height) < 0.01).ShouldBeTrue();

        // x should be centered
        (Math.Abs(x - (500 * 0.5f - legendSize.Width * 0.5f)) < 0.01).ShouldBeTrue();

        // y should be 500 - legendsSize.Height
        (Math.Abs(y - (500 - legendSize.Height)) < 0.01).ShouldBeTrue();

        // TEST LEFT LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Left;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width - legendSize.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height) < 0.01).ShouldBeTrue();

        // x should be 0
        (Math.Abs(x) < 0.01).ShouldBeTrue();

        // y should be centered
        (Math.Abs(y - (500 * 0.5f - legendSize.Height * 0.5f)) < 0.01).ShouldBeTrue();

        // TEST RIGHT LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Right;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width - legendSize.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height) < 0.01).ShouldBeTrue();

        // x should be 500 - legendsSize.Width
        (Math.Abs(x - (500 - legendSize.Width)) < 0.01).ShouldBeTrue();

        // y should be centered
        (Math.Abs(y - (500 * 0.5f - legendSize.Height * 0.5f)) < 0.01).ShouldBeTrue();
    }

    [Fact]
    public void PieWithTitleCase()
    {
        var legend = new DefaultLegend();
        legend.Geometry.Fill = new SolidColorPaint(TestColors.LightGray);

        var title = new LabelVisual
        {
            Text = "My chart title",
            TextSize = 25,
            Padding = new Padding(10),
            Paint = new SolidColorPaint(TestColors.White),
            BackgroundColor = TestColors.Purple
        };

        var chart = new InMemoryPieChart
        {
            Width = 500,
            Height = 500,
            ExplicitDisposing = true,
            View =
            {
                Series = new[] { 1, 2, 3 }.AsPieSeries(),
                LegendPosition = LegendPosition.Top,
                Title = title,
                Legend = legend,
            }
        };

        // TEST TOP LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Top;
        chart.GetImage().Dispose();

        var legendSize = legend.Measure(chart.CoreChart);
        var titleSize = title.Measure(chart.CoreChart);
        var dml = chart.CoreChart.DrawMarginLocation;
        var dms = chart.CoreChart.DrawMarginSize;
        var x = legend.X;
        var y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - legendSize.Height - titleSize.Height) < 0.01).ShouldBeTrue();

        // x should be centered
        (Math.Abs(x - (500 * 0.5f - legendSize.Width * 0.5f)) < 0.01).ShouldBeTrue();

        // y should be titleSize.Height
        (Math.Abs(y - titleSize.Height) < 0.01).ShouldBeTrue();

        // TEST BOTTOM LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Bottom;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - legendSize.Height - titleSize.Height) < 0.01).ShouldBeTrue();

        // x should be centered
        (Math.Abs(x - (500 * 0.5f - legendSize.Width * 0.5f)) < 0.01).ShouldBeTrue();

        // y should be 500 - legendsSize.Height
        (Math.Abs(y - (500 - legendSize.Height)) < 0.01).ShouldBeTrue();

        // TEST LEFT LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Left;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width - legendSize.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - titleSize.Height) < 0.01).ShouldBeTrue();

        // x should be 0
        (Math.Abs(x) < 0.01).ShouldBeTrue();

        // y should be centered
        (Math.Abs(y - (500 * 0.5f - legendSize.Height * 0.5f)) < 0.01).ShouldBeTrue();

        // TEST RIGHT LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Right;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        // all the width should be used
        (Math.Abs(500 - dms.Width - legendSize.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - titleSize.Height) < 0.01).ShouldBeTrue();

        // x should be 500 - legendsSize.Width
        (Math.Abs(x - (500 - legendSize.Width)) < 0.01).ShouldBeTrue();

        // y should be centered
        (Math.Abs(y - (500 * 0.5f - legendSize.Height * 0.5f)) < 0.01).ShouldBeTrue();
    }

    [Fact]
    public void PolarBasicCase()
    {
        var legend = new DefaultLegend();

        var chart = new InMemoryPolarChart
        {
            Width = 500,
            Height = 500,
            ExplicitDisposing = true,
            View =
            {
                Series = new[] { new PolarLineSeries<double> { Values = new double[] { 1, 2, 3 } } },
                LegendPosition = LegendPosition.Top,
                Legend = legend,
            }
        };

        // TEST TOP LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Top;
        chart.GetImage().Dispose();

        var legendSize = legend.Measure(chart.CoreChart);
        var dml = chart.CoreChart.DrawMarginLocation;
        var dms = chart.CoreChart.DrawMarginSize;
        var x = legend.X;
        var y = legend.Y;

        var m = Math.Min(dml.X, dml.Y) * 2;

        // all the width should be used
        (Math.Abs(500 - dms.Width - m) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - m - legendSize.Height) < 0.01).ShouldBeTrue();

        // x should be centered
        (Math.Abs(x - (500 * 0.5f - legendSize.Width * 0.5f)) < 0.01).ShouldBeTrue();

        // y should be 0
        (Math.Abs(y - 0) < 0.01).ShouldBeTrue();

        // TEST BOTTOM LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Bottom;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        m = Math.Min(dml.X, dml.Y) * 2;

        // all the width should be used
        (Math.Abs(500 - dms.Width - m) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - m - legendSize.Height) < 0.01).ShouldBeTrue();

        // x should be centered
        (Math.Abs(x - (500 * 0.5f - legendSize.Width * 0.5f)) < 0.01).ShouldBeTrue();

        // y should be 500 - legendsSize.Height
        (Math.Abs(y - (500 - legendSize.Height)) < 0.01).ShouldBeTrue();

        // TEST LEFT LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Left;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        m = Math.Min(dml.X, dml.Y) * 2;

        // all the width should be used
        (Math.Abs(500 - dms.Width - m - legendSize.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - m) < 0.01).ShouldBeTrue();

        // x should be 0
        (Math.Abs(x) < 0.01).ShouldBeTrue();

        // y should be centered
        (Math.Abs(y - (500 * 0.5f - legendSize.Height * 0.5f)) < 0.01).ShouldBeTrue();

        // TEST RIGHT LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Right;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        m = Math.Min(dml.X, dml.Y) * 2;

        // all the width should be used
        (Math.Abs(500 - dms.Width - m - legendSize.Width) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - m) < 0.01).ShouldBeTrue();

        // x should be 500 - legendsSize.Width
        (Math.Abs(x - (500 - legendSize.Width)) < 0.01).ShouldBeTrue();

        // y should be centered
        (Math.Abs(y - (500 * 0.5f - legendSize.Height * 0.5f)) < 0.01).ShouldBeTrue();
    }

    [Fact]
    public void PolarWithTitleCase()
    {
        var legend = new DefaultLegend();
        legend.Geometry.Fill = new SolidColorPaint(TestColors.LightGray);

        var title = new LabelVisual
        {
            Text = "My chart title",
            TextSize = 25,
            Padding = new Padding(10),
            Paint = new SolidColorPaint(TestColors.White),
            BackgroundColor = TestColors.Purple
        };

        var chart = new InMemoryPolarChart
        {
            Width = 500,
            Height = 500,
            ExplicitDisposing = true,
            View =
            {
                Series = new[] { new PolarLineSeries<double> { Values = new double[] { 1, 2, 3 } } },
                LegendPosition = LegendPosition.Top,
                Title = title,
                Legend = legend,
            }
        };

        // TEST TOP LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Top;
        chart.GetImage().Dispose();

        var legendSize = legend.Measure(chart.CoreChart);
        var titleSize = title.Measure(chart.CoreChart);
        var dml = chart.CoreChart.DrawMarginLocation;
        var dms = chart.CoreChart.DrawMarginSize;
        var x = legend.X;
        var y = legend.Y;

        var m = Math.Min(dml.X, dml.Y) * 2;

        // all the width should be used
        (Math.Abs(500 - dms.Width - m) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - m - legendSize.Height - titleSize.Height) < 0.01).ShouldBeTrue();

        // x should be centered
        (Math.Abs(x - (500 * 0.5f - legendSize.Width * 0.5f)) < 0.01).ShouldBeTrue();

        // y should be titleSize.Height
        (Math.Abs(y - titleSize.Height) < 0.01).ShouldBeTrue();

        // TEST BOTTOM LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Bottom;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        m = Math.Min(dml.X, dml.Y) * 2;

        // all the width should be used
        (Math.Abs(500 - dms.Width - m) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - m - legendSize.Height - titleSize.Height) < 0.01).ShouldBeTrue();

        // x should be centered
        (Math.Abs(x - (500 * 0.5f - legendSize.Width * 0.5f)) < 0.01).ShouldBeTrue();

        // y should be 500 - legendsSize.Height
        (Math.Abs(y - (500 - legendSize.Height)) < 0.01).ShouldBeTrue();

        // TEST LEFT LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Left;
        chart.GetImage().Dispose();

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        m = Math.Min(dml.X, dml.Y) * 2;
        var dx = dml.X - legendSize.Width; // <- why this distance?

        // all the width should be used
        (Math.Abs(500 - dms.Width - dml.X - dx) < 0.01).ShouldBeTrue();

        // all the height should be used
        (Math.Abs(500 - dms.Height - titleSize.Height - dx * 2) < 0.01).ShouldBeTrue();

        // x should be 0
        (Math.Abs(x) < 0.01).ShouldBeTrue();

        // y should be centered
        (Math.Abs(y - (500 * 0.5f - legendSize.Height * 0.5f)) < 0.01).ShouldBeTrue();

        // TEST RIGHT LEGEND ================================================
        chart.View.LegendPosition = LegendPosition.Right;
        chart.GetImage().Dispose();
        chart.SaveImage("mamá.png");

        legendSize = legend.Measure(chart.CoreChart);
        dml = chart.CoreChart.DrawMarginLocation;
        dms = chart.CoreChart.DrawMarginSize;
        x = legend.X;
        y = legend.Y;

        m = Math.Min(dml.X, dml.Y) * 2;
        dx = dml.X + legendSize.Width; // <- why this distance?

        // all the width should be used
        (Math.Abs(500 - dms.Width - dml.X - dx) < 0.01).ShouldBeTrue();

        // all the height should be used
        //(Math.Abs(500 - dms.Height - dml.Y) < 0.01).ShouldBeTrue();

        // x should be 500 - legendsSize.Width
        (Math.Abs(x - (500 - legendSize.Width)) < 0.01).ShouldBeTrue();

        // y should be centered
        (Math.Abs(y - (500 * 0.5f - legendSize.Height * 0.5f)) < 0.01).ShouldBeTrue();
    }
}
