// Miko render layer for LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Replaces SkiaSharpView/SkiaSharpProvider.cs.

using LiveChartsCore.Drawing;
using LiveChartsCore.Geo;
using LiveChartsCore.Kernel.Providers;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Motion;
using LiveChartsCore.Painting;
using LiveChartsCore.MikoView.Drawing.Geometries;
using LiveChartsCore.MikoView.Painting;

namespace LiveChartsCore.MikoView;

/// <summary>
/// Renders LiveCharts through Miko.Graphics. Initializes default axis, map factories and the solid-color paint.
/// </summary>
public sealed class ChartProvider : ChartEngine
{
    /// <inheritdoc />
    public override IMapFactory GetDefaultMapFactory() => new MapFactory();

    /// <inheritdoc />
    public override ICartesianAxis GetDefaultCartesianAxis() => new Axis();

    /// <inheritdoc />
    public override IPolarAxis GetDefaultPolarAxis() => new PolarAxis();

    /// <inheritdoc />
    public override Paint GetSolidColorPaint(LvcColor color) => new SolidColorPaint(color);

    /// <inheritdoc />
    public override BoundedDrawnGeometry InitializeZoommingSection(CoreMotionCanvas canvas)
    {
        var rectangle = new RectangleGeometry();
        var paint = new SolidColorPaint(new LvcColor(33, 150, 243, 50))
        {
            PaintStyle = PaintStyle.Fill,
            ZIndex = int.MaxValue
        };
        paint.AddGeometryToPaintTask(canvas, rectangle);
        canvas.AddDrawableTask(paint);
        return rectangle;
    }
}
