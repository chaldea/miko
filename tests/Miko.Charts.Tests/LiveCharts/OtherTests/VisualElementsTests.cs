// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

// LabelVisual / GeometryVisual / StackPanel and the other legacy visuals are obsolete but still tested (the source project set NoWarn CS0618).
#pragma warning disable CS0618

namespace Miko.Charts.Tests.LiveCharts.OtherTests;

using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Painting;
using LiveChartsCore.MikoView.Drawing;
using LiveChartsCore.MikoView.Drawing.Geometries;
using LiveChartsCore.MikoView.VisualElements;
using LiveChartsCore.VisualElements;
using LiveChartsCore.Kernel;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class VisualElementsTests
{
    public VisualElementsTests() => CoreTestsSetup.Ensure();

    [Fact]
    public void Dispose()
    {
        var suts = new List<IChartElement>
        {
            new StackPanel<RectangleGeometry>(),
            new RelativePanel<RectangleGeometry>(),
            new TableLayout<RectangleGeometry>(),
            new GeometryVisual<RectangleGeometry>(),
            new VariableGeometryVisual(new RectangleGeometry()),
            new NeedleVisual
            {
                Fill = new SolidColorPaint(TestColors.Red)
            },
            new AngularTicksVisual
            {
                Stroke = new SolidColorPaint(TestColors.Black),
                LabelsPaint = new SolidColorPaint(TestColors.Blue)
            }
        };

        var chart = new InMemoryPieChart
        {
            Width = 1000,
            Height = 1000,
            View =
            {
                MaxValue = 100
            }
        };

        void Draw()
        {
            var coreChart = chart.CoreChart;

            chart.CoreCanvas.DisableAnimations = true;
            coreChart.IsLoaded = true;
            coreChart._isFirstDraw = true;
            coreChart.Measure();

            using var surface = CoreTestsSetup.Graphics.CreateSurface(chart.Width, chart.Height);

            chart.CoreCanvas.DrawFrame(
                new MikoDrawingContext(
                    chart.CoreCanvas,
                    surface.Context,
                    TestColors.White));
        }

        Draw();

        // get the count of geometries and paint tasks without the visuals
        var g = chart.CoreCanvas.CountGeometries();
        var p = chart.CoreCanvas.CountPaintTasks();

        // add trhe visuals and ensure that the visuals were drawn
        chart.View.VisualElements = suts;
        Draw();
        (chart.CoreCanvas.CountGeometries() > g &&
            chart.CoreCanvas.CountPaintTasks() > p).ShouldBeTrue();

        // clear the visuals and ensure that all the geometries and paints were removed
        chart.View.VisualElements = new List<IChartElement>();
        Draw();
        (chart.CoreCanvas.CountGeometries() == g &&
            chart.CoreCanvas.CountPaintTasks() == p).ShouldBeTrue();
    }

    [Fact]
    public void VisualElement_ZIndexPropagatesToPaints_RendersAboveSeries()
    {
        // Regression for #1856: VisualElement paints used to default to ZIndex=0
        // while a default-ZIndex ColumnSeries puts its Fill at SeriesId+0.1, so
        // visuals were drawn behind the series. Setting VisualElement.ZIndex must
        // push every paint produced by the visual to that z-index.
        var visual = new GeometryVisual<RectangleGeometry>
        {
            X = 10,
            Y = 10,
            Width = 50,
            Height = 50,
            Fill = new SolidColorPaint(TestColors.Blue),
            Stroke = new SolidColorPaint(TestColors.Red),
            Label = "x",
            LabelPaint = new SolidColorPaint(TestColors.Black),
            LabelSize = 12,
            ZIndex = 1000,
            LocationUnit = MeasureUnit.Pixels,
            SizeUnit = MeasureUnit.Pixels
        };

        var chart = new InMemoryCartesianChart
        {
            Width = 400,
            Height = 400,
            View =
            {
                Series = [
                    new ColumnSeries<double> { Values = [1d, 2d, 3d] }
                ],
                VisualElements = [visual]
            }
        };

        using var image = chart.GetImage();

        visual.Fill!.ZIndex.ShouldBe(1000d);
        visual.Stroke!.ZIndex.ShouldBe(1000d);
        visual.LabelPaint!.ZIndex.ShouldBe(1000d);
    }

    [Fact]
    public void Visual_ZIndexPropagatesToDrawnTask_RendersAboveSeries()
    {
        // Same regression as above (#1856) but for the Visual base class, used
        // by the canonical docs samples (RectangleVisual et al.).
        var visual = new TestRectangleVisual { ZIndex = 1000 };

        var chart = new InMemoryCartesianChart
        {
            Width = 400,
            Height = 400,
            View =
            {
                Series = [
                    new ColumnSeries<double> { Values = [1d, 2d, 3d] }
                ],
                VisualElements = [visual]
            }
        };

        using var image = chart.GetImage();

        var drawnTask = visual.GetPaintTasksForTest()[0];
        drawnTask.ShouldNotBeNull();
        drawnTask!.ZIndex.ShouldBe(1000d);
    }

    private sealed class TestRectangleVisual : Visual
    {
        protected internal override RectangleGeometry DrawnElement { get; } =
            new RectangleGeometry { Fill = new SolidColorPaint(TestColors.Red) };

        protected override void Measure(LiveChartsCore.Chart chart)
        {
            DrawnElement.X = 10;
            DrawnElement.Y = 10;
            DrawnElement.Width = 40;
            DrawnElement.Height = 40;
        }

        public LiveChartsCore.Painting.Paint?[] GetPaintTasksForTest() => GetPaintTasks();
    }
}
