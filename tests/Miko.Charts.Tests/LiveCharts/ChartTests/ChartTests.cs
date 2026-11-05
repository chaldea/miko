// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.ChartTests;

using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.VisualElements;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class ChartTests
{
    public ChartTests() => CoreTestsSetup.Ensure();

    // based on https://github.com/beto-rodriguez/LiveCharts2/issues/1422

    // in theory LiveCharts properties should never be null, but we can't control the user input
    // specially on this case where DataContext could be null

    [Fact]
    public void CartesianShouldHandleNullParams()
    {
        // we are testing the properties defined on LiveChartsCore/Kernel/Sketches/ICartesianChartView.cs

        var chart = new InMemoryCartesianChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                XAxes = null!,
                YAxes = null!,
                Sections = null!,
                Series = null!,
                DrawMarginFrame = null,
                VisualElements = null!
            }
        };

        using var image = chart.GetImage();

        (image is not null).ShouldBeTrue();
    }

    [Fact]
    public void PieShouldHandleNullParams()
    {
        // we are testing the properties defined on LiveChartsCore/Kernel/Sketches/IPieChartView.cs

        var chart = new InMemoryPieChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = null!,
                VisualElements = null!
            }
        };

        using var image = chart.GetImage();

        (image is not null).ShouldBeTrue();
    }

    [Fact]
    public void PieShouldResizeAngularTicks()
    {
        // https://github.com/Live-Charts/LiveCharts2/issues/2012

        var needle = new NeedleVisual { Value = 45 };
        var ticks = new AngularTicksVisual
        {
            Labeler = value => value.ToString("N1"),
            LabelsSize = 16,
            LabelsOuterOffset = 15,
            OuterOffset = 65,
            TicksLength = 20
        };

        var chart = new InMemoryPieChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                MaxAngle = 270,
                MinValue = 0,
                MaxValue = 100,
                Series = [],
                VisualElements = [needle, ticks]
            }
        };

        chart.GetImage().Dispose();
        chart.View.MaxValue = 10;
        chart.GetImage().Dispose();
    }

    [Fact]
    public void PolarShouldHandleNullParams()
    {
        // we are testing the properties defined on LiveChartsCore/Kernel/Sketches/IPolarChartView.cs

        var chart = new InMemoryPolarChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = null!,
                AngleAxes = null!,
                RadiusAxes = null!,
                VisualElements = null!
            }
        };

        using var image = chart.GetImage();

        (image is not null).ShouldBeTrue();
    }

    [Fact]
    public void MapShouldHandleNullParams()
    {
        // we are testing the properties defined on LiveChartsCore/Kernel/Sketches/IGeoMapView.cs

        // Miko has no in-memory map (SKGeoMap); InMemoryGeoMap hosts a GeoMapView the same way.
        var chart = new InMemoryGeoMap
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                Series = null!,
            }
        };

        using var image = chart.GetImage();

        (image is not null).ShouldBeTrue();
    }

    // DesignerModeShortCircuitsLoad (https://github.com/Live-Charts/LiveCharts2/issues/2182) is not ported: it
    // overrides SKCartesianChart.IsDesignerMode, and Miko's ChartView has no designer mode (DesignerMode is a
    // constant false, there is no WinForms designer host to protect).
}
