// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.CoreObjectsTests;

using LiveChartsCore;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Painting;
using Shouldly;

// DrawChart (measure + draw frames into an SKSurface until the canvas is valid) lives in Shared/ChartDrawing.cs,
// drawing into a Miko RenderSurface, because other test classes use it too.
[Collection(ChartsCollection.Name)]
public class ChangingPaintTasks
{
    public ChangingPaintTasks() => CoreTestsSetup.Ensure();

    private static int DrawChart(InMemoryCartesianChart chart, bool animated = false) =>
        ChartDrawing.DrawChart(chart, animated);

    [Fact]
    public async Task EnsureChartIsMeasured()
    {
        var chart = new InMemoryCartesianChart
        {
            View =
            {
                Series = [new LineSeries<int>([1, 2, 3])]
            }
        };

        var measureCount = 0;
        chart.View.Measuring += c => measureCount++;

        var frames = DrawChart(chart, false);

        await Task.Delay(1000);

        (measureCount == 1 && frames == 1).ShouldBeTrue();
    }

    [Fact]
    public async Task EnsureChartIsAnimated()
    {
        var chart = new InMemoryCartesianChart
        {
            View =
            {
                AnimationsSpeed = TimeSpan.FromSeconds(1),
                EasingFunction = EasingFunctions.Lineal,
                Series = [new LineSeries<int>([1, 2, 3])],
                XAxes = [new Axis()],
                YAxes = [new Axis()],
            }
        };

        var measureCount = 0;
        chart.View.Measuring += c => measureCount++;

        var frames = DrawChart(chart, true);

        await Task.Delay(1000);

        (measureCount == 1 && frames > 1).ShouldBeTrue();
    }

    [Fact]
    public void DrawableSeriesFillChanged()
    {
        var series = new LineSeries<int>
        {
            Values = new List<int> { 1, 6, 4, 2 }
        };

        var chart = new InMemoryCartesianChart
        {
            View =
            {
                Series = new List<ISeries> { series },
                XAxes = new[] { new Axis() },
                YAxes = new[] { new Axis() },
            }
        };

        var canvas = chart.CoreCanvas;

        _ = DrawChart(chart);

        var drawables = canvas.DrawablesCount;
        var geometries = canvas.CountGeometries();

        // on changing the fill task, the previous instance should be removed.
        series.Fill = new SolidColorPaint();

        _ = DrawChart(chart);

        (drawables == canvas.DrawablesCount &&
            geometries == canvas.CountGeometries()).ShouldBeTrue();
    }

    [Fact]
    public void DrawableSeriesStrokeChanged()
    {
        var series = new LineSeries<int>
        {
            Values = new List<int> { 1, 6, 4, 2 }
        };

        var chart = new InMemoryCartesianChart
        {
            View =
            {
                Series = new List<ISeries> { series },
                XAxes = new[] { new Axis() },
                YAxes = new[] { new Axis() },
            }
        };

        var canvas = chart.CoreCanvas;

        _ = DrawChart(chart);

        var drawables = canvas.DrawablesCount;
        var geometries = canvas.CountGeometries();

        series.Stroke = new SolidColorPaint();

        _ = DrawChart(chart);

        (drawables == canvas.DrawablesCount &&
            geometries == canvas.CountGeometries()).ShouldBeTrue();
    }

    [Fact]
    public void LineSeriesGeometryPaintsChanged()
    {
        var series = new LineSeries<int>
        {
            Values = new List<int> { 1, 6, 4, 2 }
        };

        var chart = new InMemoryCartesianChart
        {
            View =
            {
                Series = new List<ISeries> { series },
                XAxes = new[] { new Axis() },
                YAxes = new[] { new Axis() },
            }
        };

        var canvas = chart.CoreCanvas;

        _ = DrawChart(chart);

        var drawables = canvas.DrawablesCount;
        var geometries = canvas.CountGeometries();

        series.GeometryFill = new SolidColorPaint();
        series.GeometryStroke = new SolidColorPaint();

        _ = DrawChart(chart);

        (drawables == canvas.DrawablesCount &&
            geometries == canvas.CountGeometries()).ShouldBeTrue();
    }

    [Fact]
    public void SeriesRemoved()
    {
        var series = new LineSeries<int> { Values = new List<int> { 1, 6, 4, 2 } };

        var seriesCollection = new List<ISeries> { series };

        var chart = new InMemoryCartesianChart
        {
            View =
            {
                Series = seriesCollection,
                XAxes = new[] { new Axis() },
                YAxes = new[] { new Axis() },
            }
        };

        var canvas = chart.CoreCanvas;

        _ = DrawChart(chart);

        var drawables = canvas.DrawablesCount;
        var geometries = canvas.CountGeometries();
        seriesCollection.Add(new LineSeries<int> { Values = new List<int> { 1, 6, 4, 2 } });

        _ = DrawChart(chart);

        seriesCollection.RemoveAt(0);
        _ = DrawChart(chart);

        (drawables == canvas.DrawablesCount &&
            geometries == canvas.CountGeometries()).ShouldBeTrue();
    }

    [Fact]
    public void SeriesCollectionInstanceChanged()
    {
        var series = new LineSeries<int> { Values = new List<int> { 1, 6, 4, 2 } };

        var seriesCollection = new List<ISeries> { series };

        var chart = new InMemoryCartesianChart
        {
            View =
            {
                Series = seriesCollection,
                XAxes = new[] { new Axis() },
                YAxes = new[] { new Axis() },
            }
        };

        var canvas = chart.CoreCanvas;

        _ = DrawChart(chart);

        var drawables = canvas.DrawablesCount;
        var geometries = canvas.CountGeometries();

        chart.View.Series = new List<ISeries>
        {
            new LineSeries<int> { Values = new List<int> { 1, 6, 4, 2 } }
        };

        _ = DrawChart(chart);

        (drawables == canvas.DrawablesCount &&
            geometries == canvas.CountGeometries()).ShouldBeTrue();
    }

    [Fact]
    public void AxisTextBrushChanged()
    {
        var axis = new Axis();

        var chart = new InMemoryCartesianChart
        {
            View =
            {
                Series = new List<ISeries>
                    {
                        new LineSeries<int> { Values = new List<int> { 1, 6, 4, 2 } },
                    },
                XAxes = new[] { axis },
                YAxes = new[] { new Axis() },
            }
        };

        var canvas = chart.CoreCanvas;

        _ = DrawChart(chart);

        var drawables = canvas.DrawablesCount;
        var geometries = canvas.CountGeometries();

        axis.LabelsPaint = new SolidColorPaint();

        _ = DrawChart(chart);

        (drawables == canvas.DrawablesCount &&
            geometries == canvas.CountGeometries()).ShouldBeTrue();
    }

    [Fact]
    public void AxisSeparatorBrushChanged()
    {
        var axis = new Axis();

        var chart = new InMemoryCartesianChart
        {
            View =
            {
                Series = new List<ISeries>
                    {
                        new LineSeries<int> { Values = new List<int> { 1, 6, 4, 2 } },
                    },
                XAxes = new[] { new Axis() },
                YAxes = new[] { axis },
            }
        };

        var canvas = chart.CoreCanvas;

        _ = DrawChart(chart);

        var drawables = canvas.DrawablesCount;
        var geometries = canvas.CountGeometries();

        axis.SeparatorsPaint = new SolidColorPaint();

        _ = DrawChart(chart);

        (drawables == canvas.DrawablesCount &&
            geometries == canvas.CountGeometries()).ShouldBeTrue();
    }

    [Fact]
    public void AxisCollectionInstanceChanged()
    {
        var chart = new InMemoryCartesianChart
        {
            View =
            {
                Series = new List<ISeries>
                    {
                        new LineSeries<int> { Values = new List<int> { 1, 6, 4, 2 } },
                    },
                XAxes = new[] { new Axis() },
                YAxes = new[] { new Axis() },
            }
        };

        var canvas = chart.CoreCanvas;

        _ = DrawChart(chart);

        var drawables = canvas.DrawablesCount;
        var geometries = canvas.CountGeometries();

        chart.View.XAxes = new[] { new Axis() };

        _ = DrawChart(chart);

        (drawables == canvas.DrawablesCount &&
            geometries == canvas.CountGeometries()).ShouldBeTrue();
    }

    [Fact]
    public void SectionBrushChanged()
    {
        var section = new RectangularSection
        {
            Fill = new SolidColorPaint()
        };

        var chart = new InMemoryCartesianChart
        {
            View =
            {
                Series = new List<ISeries>
                    {
                        new LineSeries<int> { Values = new List<int> { 1, 6, 4, 2 } },
                    },
                XAxes = new[] { new Axis() },
                YAxes = new[] { new Axis() },
                Sections = new[] { section }
            }
        };

        var canvas = chart.CoreCanvas;

        _ = DrawChart(chart);

        var drawables = canvas.DrawablesCount;
        var geometries = canvas.CountGeometries();

        section.Fill = new SolidColorPaint();

        _ = DrawChart(chart);

        (drawables == canvas.DrawablesCount &&
            geometries == canvas.CountGeometries()).ShouldBeTrue();
    }

    [Fact]
    public void SectionsInstanceChanged()
    {
        var chart = new InMemoryCartesianChart
        {
            View =
            {
                Series = new List<ISeries>
                    {
                        new LineSeries<int> { Values = new List<int> { 1, 6, 4, 2 } },
                    },
                XAxes = new[] { new Axis() },
                YAxes = new[] { new Axis() },
                Sections = new[] { new RectangularSection() }
            }
        };

        var canvas = chart.CoreCanvas;

        _ = DrawChart(chart);

        var drawables = canvas.DrawablesCount;
        var geometries = canvas.CountGeometries();

        chart.View.Sections = new[] { new RectangularSection() };

        _ = DrawChart(chart);

        (drawables == canvas.DrawablesCount &&
            geometries == canvas.CountGeometries()).ShouldBeTrue();
    }
}
