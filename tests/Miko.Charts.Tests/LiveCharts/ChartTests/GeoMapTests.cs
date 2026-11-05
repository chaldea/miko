// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.ChartTests;

using LiveChartsCore.MikoView;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class GeoMapTests
{
    public GeoMapTests() => CoreTestsSetup.Ensure();

    // https://github.com/Live-Charts/LiveCharts2/issues/962
    //
    // Swapping the Series collection on a GeoMap should preserve the heat fill
    // on lands that exist in both the old and the new series. Before the fix,
    // GeoMapChart.Measure() painted the new series first and then called
    // Delete() on the departed series, which nulled Shape.Fill on every land
    // the old series had ever painted -- including ones the new series just
    // painted -- so shared lands appeared blank until the next measure.
    [Fact]
    public void GeoMap_SwappingSeries_PreservesFillOnSharedLands()
    {
        // Miko has no in-memory map (SKGeoMap); InMemoryGeoMap hosts a GeoMapView the same way.
        var chart = new InMemoryGeoMap
        {
            Width = 400,
            Height = 400,
            View =
            {
                Series = [
                    new HeatLandSeries
                    {
                        Lands = [
                            new() { Name = "fra", Value = 10 },
                            new() { Name = "usa", Value = 5 },
                        ]
                    }
                ]
            }
        };

        chart.CoreChart.Measure();

        var fra = chart.View.ActiveMap.FindLand("fra");
        var usa = chart.View.ActiveMap.FindLand("usa");
        fra.ShouldNotBeNull("fra should exist in the world map");
        usa.ShouldNotBeNull("usa should exist in the world map");
        fra.Data.All(d => d.Shape?.Fill is not null).ShouldBeTrue("fra should be painted after the first measure");

        // Swap to a series that still contains "fra" but drops "usa".
        chart.View.Series = [
            new HeatLandSeries
            {
                Lands = [
                    new() { Name = "fra", Value = 1 },
                    new() { Name = "bra", Value = 99 },
                ]
            }
        ];

        chart.CoreChart.Measure();

        fra.Data.All(d => d.Shape?.Fill is not null).ShouldBeTrue(
            "fra is shared between the old and new series and must stay painted after swap (#962)");
        usa.Data.All(d => d.Shape?.Fill is null).ShouldBeTrue(
            "usa is no longer in any series and must be cleared after swap");
    }
}
