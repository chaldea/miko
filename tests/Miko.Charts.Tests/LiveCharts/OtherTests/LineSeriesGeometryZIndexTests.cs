// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.OtherTests;

using LiveChartsCore.Painting;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Painting;
using Shouldly;

// CoreLineSeries was assigning the wrong PaintConstants to its geometry
// paints — SeriesGeometryStrokeZIndexOffset for GeometryFill, and
// SeriesDataLabelsZIndexOffset for GeometryStroke. CoreStepLineSeries and
// CorePolarLineSeries already used the correctly-named constants.
//
// In addition to being misleading, this collided GeometryStroke with
// DataLabelsPaint (both at offset 0.5), which is order-dependent.
[Collection(ChartsCollection.Name)]
public class LineSeriesGeometryZIndexTests
{
    public LineSeriesGeometryZIndexTests() => CoreTestsSetup.Ensure();

    [Fact]
    public void LineSeries_GeometryPaintZIndices_Match_NamedConstants()
    {
        var geometryFill = new SolidColorPaint(TestColors.White);
        var geometryStroke = new SolidColorPaint(TestColors.Black, 4);

        var chart = new InMemoryCartesianChart
        {
            Width = 200,
            Height = 200,
            View =
            {
                Series = [
                    new LineSeries<double>
                    {
                        Values = [1, 2, 3, 4],
                        GeometryFill = geometryFill,
                        GeometryStroke = geometryStroke
                    }
                ]
            }
        };

        using var image = chart.GetImage();

        // Default series have ZIndex == null, so actualZIndex == 0 in
        // CoreLineSeries and the paint ZIndex equals the offset itself.
        geometryFill.ZIndex.ShouldBe(
            PaintConstants.SeriesGeometryFillZIndexOffset,
            "GeometryFill.ZIndex must use SeriesGeometryFillZIndexOffset.");
        geometryStroke.ZIndex.ShouldBe(
            PaintConstants.SeriesGeometryStrokeZIndexOffset,
            "GeometryStroke.ZIndex must use SeriesGeometryStrokeZIndexOffset.");

        // Stroke must render above Fill so the marker outline isn't hidden.
        (geometryStroke.ZIndex > geometryFill.ZIndex).ShouldBeTrue();
    }
}
