// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Rewritten to draw through Miko.Graphics instead of SkiaSharp.

using Miko.Common;
using Miko.Graphics;
using LiveChartsCore.Drawing;
using LiveChartsCore.MikoView.Drawing;
using LiveChartsCore.MikoView.Painting.Effects;
using LiveChartsCore.MikoView.Painting.ImageFilters;
using LiveChartsCore.Painting;

namespace LiveChartsCore.MikoView.Painting;

/// <summary>
/// Base class of the paints that draw through Miko.Graphics. A paint is a task: the set of geometries it
/// draws, plus how (color or gradient, stroke geometry, dash, blur or shadow, font).
/// </summary>
/// <param name="strokeThickness">The stroke thickness.</param>
/// <param name="strokeMiter">The stroke miter.</param>
public abstract class MikoPaint(float strokeThickness = 1f, float strokeMiter = 0f)
    : Paint(strokeThickness, strokeMiter)
{
    private GraphicsPaint? _graphicsPaint;

    /// <summary>Gets or sets the font family (a comma separated fallback chain, as in CSS). Null uses the default.</summary>
    public string? FontFamily { get; set; }

    /// <summary>Gets or sets the font weight used by text drawn with this paint.</summary>
    public FontWeight FontWeight { get; set; } = FontWeight.Normal;

    /// <summary>Gets or sets how a stroke ends.</summary>
    public StrokeCap StrokeCap { get; set; }

    /// <summary>Gets or sets how stroked segments are joined.</summary>
    public StrokeJoin StrokeJoin { get; set; }

    /// <summary>Gets or sets the path effect (dash).</summary>
    public PathEffect? PathEffect { get; set; }

    /// <summary>Gets or sets the image filter (blur, drop shadow).</summary>
    public ImageFilter? ImageFilter { get; set; }

    /// <summary>Gets the font family used when a paint does not set one.</summary>
    internal static string ResolveFontFamily(MikoPaint? paint) =>
        paint?.FontFamily ?? LiveChartsMiko.DefaultTextSettings.DefaultFontFamily;

    /// <summary>The Miko.Graphics paint behind this task, brought up to date with the properties above.</summary>
    internal GraphicsPaint GraphicsPaint => UpdateGraphicsPaint(null);

    /// <summary>Copies the settings common to every paint, interpolating the numeric ones.</summary>
    internal static void Map(MikoPaint from, MikoPaint to, float progress = 1)
    {
        to.PaintStyle = from.PaintStyle;
        to.IsAntialias = from.IsAntialias;
        to.StrokeCap = from.StrokeCap;
        to.StrokeJoin = from.StrokeJoin;
        to.FontFamily = from.FontFamily;
        to.FontWeight = from.FontWeight;

        to.StrokeThickness = from.StrokeThickness + progress * (to.StrokeThickness - from.StrokeThickness);
        to.StrokeMiter = from.StrokeMiter + progress * (to.StrokeMiter - from.StrokeMiter);
        to.PathEffect = PathEffect.Transitionate(from.PathEffect, to.PathEffect, progress);
        to.ImageFilter = ImageFilter.Transitionate(from.ImageFilter, to.ImageFilter, progress);
    }

    /// <summary>Applies the task settings to the graphics paint and, when drawing, makes it the context's active paint.</summary>
    internal GraphicsPaint UpdateGraphicsPaint(MikoDrawingContext? context, IDrawnElement? drawnElement = null)
    {
        var paint = _graphicsPaint ??= new GraphicsPaint();

        paint.IsStroke = PaintStyle.HasFlag(PaintStyle.Stroke);
        paint.IsAntialias = IsAntialias;
        paint.StrokeCap = StrokeCap;
        paint.StrokeJoin = StrokeJoin;
        paint.StrokeWidth = StrokeThickness;
        if (StrokeMiter > 0) paint.StrokeMiter = StrokeMiter;

        PathEffect?.Apply(paint);
        if (PathEffect is null) paint.ClearDash();
        ApplyImageFilter(paint);

        if (drawnElement is not null) paint.StrokeWidth = drawnElement.StrokeThickness;

        // a measure has no context yet
        if (context is not null) context.ActiveMikoPaint = paint;

        return paint;
    }

    /// <summary>Puts the paint's own filter back after a geometry-specific shadow replaced it.</summary>
    internal void RestoreImageFilter()
    {
        if (_graphicsPaint is not null) ApplyImageFilter(_graphicsPaint);
    }

    /// <inheritdoc />
    internal override void OnPaintFinished(DrawingContext context, IDrawnElement? drawnElement)
    {
        // nothing to release: the graphics paint is reused by the next frame.
    }

    /// <inheritdoc />
    internal override void ApplyOpacityMask(DrawingContext context, float opacity, IDrawnElement? drawnElement)
    {
        if (context is MikoDrawingContext { ActiveMikoPaint: { } active })
            active.Opacity = opacity;
    }

    /// <inheritdoc />
    internal override void RestoreOpacityMask(DrawingContext context, float opacity, IDrawnElement? drawnElement)
    {
        if (context is MikoDrawingContext { ActiveMikoPaint: { } active })
            active.Opacity = 1;
    }

    /// <inheritdoc />
    internal override void DisposeTask()
    {
        _graphicsPaint?.Dispose();
        _graphicsPaint = null;
    }

    private void ApplyImageFilter(GraphicsPaint paint)
    {
        if (ImageFilter is null) paint.ClearFilter();
        else ImageFilter.Apply(paint);
    }
}
