// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

// LabelVisual / GeometryVisual are obsolete but still tested (the source project set NoWarn CS0618).
#pragma warning disable CS0618

namespace Miko.Charts.Tests.LiveCharts.OtherTests;

using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Drawing.Geometries;
using LiveChartsCore.MikoView.Painting;
using LiveChartsCore.MikoView.VisualElements;
using LiveChartsCore.VisualElements;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class EventsTests
{
    public EventsTests() => CoreTestsSetup.Ensure();

    [Fact]
    public void CartesianBasicCase()
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
                XAxes = new[] { new Axis { IsVisible = false, } },
                YAxes = new[] { new Axis { IsVisible = false, } },
                VisualElements = new VisualElement[]
                {
                    new LabelVisual
                    {
                        X = 1,
                        Y = 1,
                        Text = "Hello world",
                        TextSize = 20,
                        Paint = new SolidColorPaint(TestColors.Red),
                        LocationUnit = MeasureUnit.ChartValues
                    },
                    new GeometryVisual<RectangleGeometry>
                    {
                        X = 1,
                        Y = 1,
                        Width = 0.5,
                        Height = 0.5,
                        Fill = new SolidColorPaint(TestColors.Blue),
                        LocationUnit = MeasureUnit.ChartValues,
                        SizeUnit = MeasureUnit.ChartValues
                    }
                }
            }
        };

        chart.GetImage().Dispose();

        // Test points.
        // Charts use the Series.FindHitPoints method to check if the mouse is over a point.
        var strategy = chart.View.Series.GetFindingStrategy();
        var s = chart.View.Series
            .SelectMany(x => x.FindHitPoints(chart.CoreChart, new LvcPoint(251, 251), strategy, FindPointFor.HoverEvent))
            .ToArray();
        (s.Length == 1).ShouldBeTrue();

        // Test visual elements.
        // Charts use the VisualElement.IsHitBy method to check if the mouse is over a visual element.
        var v = chart.View.VisualElements
            .Cast<VisualElement>()
            .SelectMany(x => x.IsHitBy(chart.CoreChart, new LvcPoint(251, 251)))
            .ToArray();
        (v.Length == 2).ShouldBeTrue();
    }
}
