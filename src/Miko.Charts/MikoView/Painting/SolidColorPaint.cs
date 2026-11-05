// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Rewritten to draw through Miko.Graphics instead of SkiaSharp.

using LiveChartsCore.Drawing;
using LiveChartsCore.MikoView.Drawing;
using LiveChartsCore.Painting;
using Miko.Common;

namespace LiveChartsCore.MikoView.Painting;

/// <summary>Defines a set of geometries that will be painted using a solid color.</summary>
public class SolidColorPaint : MikoPaint
{
    /// <summary>Initializes a new instance of the <see cref="SolidColorPaint"/> class.</summary>
    public SolidColorPaint()
    { }

    /// <summary>Initializes a new instance of the <see cref="SolidColorPaint"/> class.</summary>
    /// <param name="color">The color.</param>
    public SolidColorPaint(LvcColor color)
    {
        Color = color;
    }

    /// <summary>Initializes a new instance of the <see cref="SolidColorPaint"/> class.</summary>
    /// <param name="color">The color.</param>
    /// <param name="strokeWidth">Width of the stroke.</param>
    public SolidColorPaint(LvcColor color, float strokeWidth)
        : base(strokeWidth)
    {
        Color = color;
    }

    /// <summary>Gets or sets the color.</summary>
    public LvcColor Color { get; set; }

    /// <inheritdoc cref="Paint.CloneTask" />
    public override Paint CloneTask()
    {
        var clone = new SolidColorPaint { Color = Color };
        Map(this, clone);
        return clone;
    }

    internal override void OnPaintStarted(DrawingContext drawingContext, IDrawnElement? drawnElement)
    {
        var paint = UpdateGraphicsPaint((MikoDrawingContext)drawingContext, drawnElement);
        paint.ClearGradient();
        paint.Color = new Color(Color.R, Color.G, Color.B, Color.A);
    }

    internal override Paint Transitionate(float progress, Paint target)
    {
        if (target is not SolidColorPaint to) return target;

        Color = new LvcColor(
            (byte)(Color.R + progress * (to.Color.R - Color.R)),
            (byte)(Color.G + progress * (to.Color.G - Color.G)),
            (byte)(Color.B + progress * (to.Color.B - Color.B)),
            (byte)(Color.A + progress * (to.Color.A - Color.A)));

        return this;
    }

    /// <inheritdoc />
    public override string ToString() => $"({Color.R}, {Color.G}, {Color.B})";
}
