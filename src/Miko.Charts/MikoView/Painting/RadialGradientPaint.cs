// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Rewritten to draw through Miko.Graphics instead of SkiaSharp.

using LiveChartsCore.Drawing;
using LiveChartsCore.MikoView.Drawing;
using LiveChartsCore.Painting;
using Miko.Common;
using Miko.Graphics;

namespace LiveChartsCore.MikoView.Painting;

/// <summary>
/// Paints with a radial gradient. The center and radius are fractions of the area being drawn.
/// </summary>
public class RadialGradientPaint : MikoPaint
{
    private readonly LvcColor[] _gradientStops;
    private LvcPoint _center;
    private float _radius;
    private readonly float[]? _colorPos;
    private readonly GradientTileMode _tileMode;
    private RectF _appliedClip = new(float.NaN, 0, 0, 0);
    private bool _stale = true;

    /// <summary>Initializes a new instance of the <see cref="RadialGradientPaint"/> class.</summary>
    /// <param name="gradientStops">The gradient colors.</param>
    /// <param name="center">The center as a fraction of the area, the middle by default.</param>
    /// <param name="radius">The radius as a fraction of the shorter side.</param>
    /// <param name="colorPos">The position of each color, evenly spaced when null.</param>
    /// <param name="tileMode">What happens beyond the radius.</param>
    public RadialGradientPaint(
        LvcColor[] gradientStops,
        LvcPoint? center = null,
        float radius = 0.5f,
        float[]? colorPos = null,
        GradientTileMode tileMode = GradientTileMode.Clamp)
    {
        _gradientStops = gradientStops;
        _center = center ?? new LvcPoint(0.5f, 0.5f);
        _radius = radius;
        _colorPos = colorPos;
        _tileMode = tileMode;
    }

    /// <summary>Initializes a new instance of the <see cref="RadialGradientPaint"/> class.</summary>
    public RadialGradientPaint(LvcColor centerColor, LvcColor outerColor)
        : this([centerColor, outerColor]) { }

    /// <inheritdoc cref="Paint.CloneTask" />
    public override Paint CloneTask()
    {
        var clone = new RadialGradientPaint(
            (LvcColor[])_gradientStops.Clone(), _center, _radius, (float[]?)_colorPos?.Clone(), _tileMode);
        Map(this, clone);
        return clone;
    }

    internal override void OnPaintStarted(DrawingContext drawingContext, IDrawnElement? drawnElement)
    {
        var context = (MikoDrawingContext)drawingContext;
        var paint = UpdateGraphicsPaint(context, drawnElement);

        var clip = context.Context.ClipBounds;
        if (!_stale && clip.Equals(_appliedClip) && paint.HasGradient) return;

        var center = new LvcPoint(clip.X + _center.X * clip.Width, clip.Y + _center.Y * clip.Height);
        var radius = Math.Min(clip.Width, clip.Height) * _radius;

        var colors = Array.ConvertAll(_gradientStops, c => new Color(c.R, c.G, c.B, c.A));
        paint.SetRadialGradient(center.X, center.Y, radius, colors, _colorPos, _tileMode);

        _appliedClip = clip;
        _stale = false;
    }

    internal override Paint Transitionate(float progress, Paint target)
    {
        if (target is not RadialGradientPaint to) return target;

        if (to._gradientStops.Length != _gradientStops.Length)
            throw new ArgumentException("The gradient stops must be the same length.");

        for (var i = 0; i < _gradientStops.Length; i++)
        {
            _gradientStops[i] = new LvcColor(
                (byte)(_gradientStops[i].R + progress * (to._gradientStops[i].R - _gradientStops[i].R)),
                (byte)(_gradientStops[i].G + progress * (to._gradientStops[i].G - _gradientStops[i].G)),
                (byte)(_gradientStops[i].B + progress * (to._gradientStops[i].B - _gradientStops[i].B)),
                (byte)(_gradientStops[i].A + progress * (to._gradientStops[i].A - _gradientStops[i].A)));
        }

        _center = new LvcPoint(
            _center.X + progress * (to._center.X - _center.X),
            _center.Y + progress * (to._center.Y - _center.Y));
        _radius += progress * (to._radius - _radius);

        if (_colorPos is not null && to._colorPos is not null)
        {
            if (_colorPos.Length != to._colorPos.Length)
                throw new ArgumentException("The color positions must be the same length.");

            for (var i = 0; i < _colorPos.Length; i++)
                _colorPos[i] += progress * (to._colorPos[i] - _colorPos[i]);
        }

        _stale = true;
        return this;
    }
}
