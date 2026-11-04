// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors),
// tests/CoreTests/CoreObjectsTests/ChangingPaintTasks.cs (DrawChart).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts;

using LiveChartsCore.MikoView.Charts;
using LiveChartsCore.MikoView.Drawing;

/// <summary>
/// <c>ChangingPaintTasks.DrawChart</c> of the LiveCharts tests, shared by several test classes: measures the chart
/// and draws frames until the motion canvas has nothing left to animate.
/// </summary>
internal static class ChartDrawing
{
    /// <summary>Measures <paramref name="view"/> and draws it into a 100×100 surface until its canvas is valid.</summary>
    /// <returns>The number of frames drawn.</returns>
    public static int DrawChart(ChartView view, bool animated = false)
    {
        var coreChart = view.CoreChart;

        coreChart.IsLoaded = true;
        coreChart._isFirstDraw = true;
        coreChart.Measure();

        var canvas = view.CoreCanvas;
        canvas.DisableAnimations = !animated;
        var frames = 0;

        using var surface = CoreTestsSetup.Graphics.CreateSurface(100, 100);
        while (!canvas.IsValid)
        {
            frames++;
            canvas.DrawFrame(new MikoDrawingContext(canvas, surface.Context, TestColors.Transparent));
        }

        return frames;
    }

    /// <inheritdoc cref="DrawChart(ChartView, bool)"/>
    public static int DrawChart<TView>(InMemoryChartBase<TView> chart, bool animated = false)
        where TView : ChartView =>
        DrawChart(chart.View, animated);
}
