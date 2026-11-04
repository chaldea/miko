// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Rewritten to draw through Miko.Graphics instead of SkiaSharp.

using LiveChartsCore.Drawing;
using LiveChartsCore.MikoView.Drawing;
using LiveChartsCore.Painting;
using Miko.Common;
using Miko.Graphics;

namespace LiveChartsCore.MikoView.Painting;

/// <summary>
/// Paints with a linear gradient. The start and end points are fractions of the area being drawn
/// (0,0 is its top left corner and 1,1 the bottom right).
/// </summary>
public class LinearGradientPaint(
    LvcColor[] gradientStops,
    LvcPoint startPoint,
    LvcPoint endPoint,
    float[]? colorPos = null,
    GradientTileMode tileMode = GradientTileMode.Clamp)
        : MikoPaint
{
    private RectF _appliedClip = new(float.NaN, 0, 0, 0);
    private bool _stale = true;

    private LvcColor[] GradientStops { get; set; } = gradientStops;

    private LvcPoint StartPoint { get; set; } = startPoint;

    private LvcPoint EndPoint { get; set; } = endPoint;

    private float[]? ColorPos { get; set; } = colorPos;

    /// <summary>The default start point, the middle of the left edge.</summary>
    public static readonly LvcPoint DefaultStartPoint = new(0, 0.5f);

    /// <summary>The default end point, the middle of the right edge.</summary>
    public static readonly LvcPoint DefaultEndPoint = new(1, 0.5f);

    /// <summary>Initializes a new instance of the <see cref="LinearGradientPaint"/> class.</summary>
    public LinearGradientPaint(LvcColor[] gradientStops)
        : this(gradientStops, DefaultStartPoint, DefaultEndPoint) { }

    /// <summary>Initializes a new instance of the <see cref="LinearGradientPaint"/> class.</summary>
    public LinearGradientPaint(LvcColor startColor, LvcColor endColor, LvcPoint startPoint, LvcPoint endPoint)
        : this([startColor, endColor], startPoint, endPoint) { }

    /// <summary>Initializes a new instance of the <see cref="LinearGradientPaint"/> class.</summary>
    public LinearGradientPaint(LvcColor start, LvcColor end)
        : this(start, end, DefaultStartPoint, DefaultEndPoint) { }

    /// <inheritdoc cref="Paint.CloneTask" />
    public override Paint CloneTask()
    {
        var clone = new LinearGradientPaint(
            (LvcColor[])GradientStops.Clone(), StartPoint, EndPoint, (float[]?)ColorPos?.Clone(), tileMode);
        Map(this, clone);
        return clone;
    }

    internal override void OnPaintStarted(DrawingContext drawingContext, IDrawnElement? drawnElement)
    {
        var context = (MikoDrawingContext)drawingContext;
        var paint = UpdateGraphicsPaint(context, drawnElement);

        // the gradient spans the area being drawn: the clip at the time the paint starts
        var clip = context.Context.ClipBounds;
        if (!_stale && clip.Equals(_appliedClip) && paint.HasGradient) return;

        var xf = clip.X;
        var yf = clip.Y;
        var w = clip.Width;
        var h = clip.Height;

        var colors = Array.ConvertAll(GradientStops, c => new Color(c.R, c.G, c.B, c.A));
        paint.SetLinearGradient(
            xf + w * StartPoint.X, yf + h * StartPoint.Y,
            xf + w * EndPoint.X, yf + h * EndPoint.Y,
            colors, ColorPos, tileMode);

        _appliedClip = clip;
        _stale = false;
    }

    internal override Paint Transitionate(float progress, Paint target)
    {
        if (target is not LinearGradientPaint to) return target;

        if (to.GradientStops.Length != GradientStops.Length)
            throw new NotImplementedException(
                $"Transitions between {nameof(GradientStops)} must be of the same length.");

        for (var i = 0; i < GradientStops.Length; i++)
        {
            GradientStops[i] = new LvcColor(
                (byte)(GradientStops[i].R + progress * (to.GradientStops[i].R - GradientStops[i].R)),
                (byte)(GradientStops[i].G + progress * (to.GradientStops[i].G - GradientStops[i].G)),
                (byte)(GradientStops[i].B + progress * (to.GradientStops[i].B - GradientStops[i].B)),
                (byte)(GradientStops[i].A + progress * (to.GradientStops[i].A - GradientStops[i].A)));
        }

        StartPoint = new LvcPoint(
            StartPoint.X + progress * (to.StartPoint.X - StartPoint.X),
            StartPoint.Y + progress * (to.StartPoint.Y - StartPoint.Y));

        EndPoint = new LvcPoint(
            EndPoint.X + progress * (to.EndPoint.X - EndPoint.X),
            EndPoint.Y + progress * (to.EndPoint.Y - EndPoint.Y));

        if (ColorPos is not null && to.ColorPos is not null)
        {
            if (ColorPos.Length != to.ColorPos.Length)
                throw new NotImplementedException(
                    $"Transitions between {nameof(ColorPos)} must be of the same length.");

            for (var i = 0; i < ColorPos.Length; i++)
                ColorPos[i] = ColorPos[i] + progress * (to.ColorPos[i] - ColorPos[i]);
        }

        _stale = true;
        return this;
    }
}
