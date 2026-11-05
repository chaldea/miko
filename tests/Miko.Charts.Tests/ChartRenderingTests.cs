using System.Collections.ObjectModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Drawing.Geometries;
using LiveChartsCore.MikoView.Painting;
using LiveChartsCore.Drawing;
using Miko.Common;
using Shouldly;

namespace Miko.Charts.Tests;

/// <summary>
/// End-to-end: each chart component rendered by a real engine onto an offscreen surface, checked by its
/// pixels. The series paints use colors that nothing else on a white page uses.
/// </summary>
[Collection(ChartsCollection.Name)]
public class ChartRenderingTests
{
    private static readonly LvcColor Red = new(220, 30, 40);
    private static readonly LvcColor Blue = new(30, 60, 220);

    [Fact]
    public void Should_draw_the_columns_of_a_cartesian_chart_in_their_paint()
    {
        using var harness = new ChartHarness();
        harness.Mount(new CartesianChart
        {
            Series = [new ColumnSeries<double>([2, 5, 3, 8]) { Fill = new SolidColorPaint(Red) }],
            EasingFunction = null,
        });

        harness.RenderUntilIdle();

        harness.HasColor(new RectF(0, 0, 400, 300), Color.FromRgb(220, 30, 40)).ShouldBeTrue();
    }

    [Fact]
    public void Should_draw_a_line_series_and_its_axis_labels()
    {
        using var harness = new ChartHarness();
        harness.Mount(new CartesianChart
        {
            Series = [new LineSeries<double>([1, 4, 2, 6, 3]) { Stroke = new SolidColorPaint(Blue, 4), Fill = null, GeometrySize = 0 }],
            EasingFunction = null,
        });

        harness.RenderUntilIdle();

        harness.HasColor(new RectF(0, 0, 400, 300), Color.FromRgb(30, 60, 220)).ShouldBeTrue();

        // the y axis labels sit in the left margin, the x labels along the bottom
        harness.CountInk(new RectF(0, 0, 40, 280)).ShouldBeGreaterThan(0);
        harness.CountInk(new RectF(40, 280, 360, 20)).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Should_draw_the_slices_of_a_pie_chart()
    {
        using var harness = new ChartHarness();
        harness.Mount(new PieChart
        {
            Series =
            [
                new PieSeries<double>([3]) { Fill = new SolidColorPaint(Red) },
                new PieSeries<double>([1]) { Fill = new SolidColorPaint(Blue) },
            ],
            EasingFunction = null,
        });

        harness.RenderUntilIdle();

        harness.HasColor(new RectF(0, 0, 400, 300), Color.FromRgb(220, 30, 40)).ShouldBeTrue();
        harness.HasColor(new RectF(0, 0, 400, 300), Color.FromRgb(30, 60, 220)).ShouldBeTrue();

        // the pie is round and centered: the corners stay white
        harness.CountInk(new RectF(0, 0, 20, 20)).ShouldBe(0);
    }

    [Fact]
    public void Should_draw_a_polar_line_series()
    {
        using var harness = new ChartHarness();
        harness.Mount(new PolarChart
        {
            Series = [new PolarLineSeries<double>([5, 3, 4, 2, 5]) { Stroke = new SolidColorPaint(Blue, 4), Fill = null, IsClosed = true }],
            EasingFunction = null,
        });

        harness.RenderUntilIdle();

        harness.HasColor(new RectF(0, 0, 400, 300), Color.FromRgb(30, 60, 220)).ShouldBeTrue();
    }

    [Fact]
    public void Should_draw_the_world_map_with_heat_colored_lands()
    {
        using var harness = new ChartHarness(600, 300);
        harness.Mount(new GeoMap
        {
            Series =
            [
                new HeatLandSeries
                {
                    HeatMap = [new LvcColor(255, 0, 0), new LvcColor(255, 0, 0)],
                    Lands = [new HeatLand { Name = "bra", Value = 1 }, new HeatLand { Name = "usa", Value = 1 }],
                }
            ],
        });

        harness.RenderUntilIdle();

        harness.HasColor(new RectF(0, 0, 600, 300), Color.FromRgb(255, 0, 0), tolerance: 40).ShouldBeTrue();
        harness.CountInk(new RectF(0, 0, 600, 300)).ShouldBeGreaterThan(200);
    }

    [Fact]
    public void Should_redraw_when_an_observable_collection_changes()
    {
        using var harness = new ChartHarness();
        var values = new ObservableCollection<ObservableValue> { new(1), new(1) };
        harness.Mount(new CartesianChart
        {
            Series = [new ColumnSeries<ObservableValue>(values) { Fill = new SolidColorPaint(Red) }],
            YAxes = [new Axis { MinLimit = 0, MaxLimit = 10 }],
            EasingFunction = null,
        });
        harness.RenderUntilIdle();
        var before = harness.CountInk(new RectF(40, 0, 360, 280));

        values.Add(new ObservableValue(9));
        values[0].Value = 9;
        harness.RenderUntilIdle();

        harness.CountInk(new RectF(40, 0, 360, 280)).ShouldBeGreaterThan(before);
    }

    [Fact]
    public void Should_keep_requesting_frames_while_an_animation_runs_and_stop_when_it_settles()
    {
        using var harness = new ChartHarness();
        harness.Mount(new CartesianChart
        {
            Series = [new ColumnSeries<double>([2, 5, 3]) { Fill = new SolidColorPaint(Red) }],
            AnimationsSpeed = TimeSpan.FromMilliseconds(300),
            EasingFunction = EasingFunctions.Lineal,
        });

        // the first frames measure and start the transitions
        var animating = false;
        for (var i = 0; i < 40 && !animating; i++)
        {
            harness.Surface.Context.Clear(Color.White);
            harness.Engine.Render(harness.Surface.Context);
            animating = harness.Engine.HasPendingRenderWork;
            Thread.Sleep(20);
        }

        animating.ShouldBeTrue();

        harness.RenderUntilIdle();
        harness.Engine.HasPendingRenderWork.ShouldBeFalse();
        harness.HasColor(new RectF(0, 0, 400, 300), Color.FromRgb(220, 30, 40)).ShouldBeTrue();
    }
}

/// <summary>
/// LiveCharts keeps its settings (provider, theme, the label text factory) in process-wide statics, so the
/// chart tests do not run in parallel with each other.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ChartsCollection
{
    public const string Name = "Charts";
}
