using Miko.Common;
using SkiaSharp;

namespace Miko.Graphics;

/// <summary>How a stroke ends.</summary>
public enum StrokeCap
{
    Butt,
    Round,
    Square,
}

/// <summary>How two stroked segments are joined.</summary>
public enum StrokeJoin
{
    Miter,
    Round,
    Bevel,
}

/// <summary>What a gradient does outside its start and end.</summary>
public enum GradientTileMode
{
    /// <summary>Extends the edge colors.</summary>
    Clamp,
    Repeat,
    Mirror,
}

/// <summary>
/// A reusable description of how to fill or stroke a shape: color or gradient, stroke geometry, dash
/// pattern and blur or shadow. Pass it to the <c>Draw*</c> methods of <see cref="IDrawingContext"/>.
/// Change a property and draw again; the backend object behind it is updated only when something changed.
/// Dispose a paint when you no longer draw with it.
/// </summary>
public sealed class GraphicsPaint : IDisposable
{
    private SKPaint? _paint;
    private SKShader? _shader;
    private SKPathEffect? _pathEffect;
    private SKImageFilter? _imageFilter;
    private SKColorFilter? _colorFilter;
    private bool _shaderDirty, _effectDirty, _filterDirty;
    private Color _color;
    private float _opacity = 1;
    private float _strokeWidth = 1;
    private bool _stroke, _antialias = true;
    private StrokeCap _cap;
    private StrokeJoin _join;
    private float _miter = 4;
    private GradientSpec? _gradient;
    private float[]? _dash;
    private float _dashPhase;
    private FilterSpec? _filter;
    private bool _propertiesDirty = true;

    public GraphicsPaint()
    {
    }

    public GraphicsPaint(Color color)
    {
        _color = color;
    }

    /// <summary>Solid color. Ignored while a gradient is set, except for its alpha.</summary>
    public Color Color
    {
        get => _color;
        set { _color = value; _propertiesDirty = true; }
    }

    /// <summary>Extra transparency from 0 to 1, multiplied into the color and gradient alpha.</summary>
    public float Opacity
    {
        get => _opacity;
        set { _opacity = Math.Clamp(value, 0f, 1f); _propertiesDirty = true; }
    }

    /// <summary>True to draw outlines with <see cref="StrokeWidth"/>, false to fill.</summary>
    public bool IsStroke
    {
        get => _stroke;
        set { _stroke = value; _propertiesDirty = true; }
    }

    public float StrokeWidth
    {
        get => _strokeWidth;
        set { _strokeWidth = value; _propertiesDirty = true; }
    }

    public StrokeCap StrokeCap
    {
        get => _cap;
        set { _cap = value; _propertiesDirty = true; }
    }

    public StrokeJoin StrokeJoin
    {
        get => _join;
        set { _join = value; _propertiesDirty = true; }
    }

    /// <summary>Limit of a mitered join before it turns into a bevel.</summary>
    public float StrokeMiter
    {
        get => _miter;
        set { _miter = value; _propertiesDirty = true; }
    }

    public bool IsAntialias
    {
        get => _antialias;
        set { _antialias = value; _propertiesDirty = true; }
    }

    /// <summary>True while a gradient replaces the solid color.</summary>
    public bool HasGradient => _gradient is not null;

    /// <summary>
    /// Fills with a linear gradient between two points, in the coordinates of the shape being drawn.
    /// <paramref name="positions"/> are offsets from 0 to 1 and may be null for even spacing.
    /// </summary>
    public void SetLinearGradient(
        float x0, float y0, float x1, float y1, ReadOnlySpan<Color> colors, ReadOnlySpan<float> positions = default,
        GradientTileMode tileMode = GradientTileMode.Clamp)
    {
        _gradient = new GradientSpec(false, x0, y0, x1, y1, 0, colors.ToArray(), positions.IsEmpty ? null : positions.ToArray(), tileMode);
        _shaderDirty = true;
        _propertiesDirty = true;
    }

    /// <summary>Fills with a radial gradient around a center point.</summary>
    public void SetRadialGradient(
        float centerX, float centerY, float radius, ReadOnlySpan<Color> colors, ReadOnlySpan<float> positions = default,
        GradientTileMode tileMode = GradientTileMode.Clamp)
    {
        _gradient = new GradientSpec(true, centerX, centerY, 0, 0, radius, colors.ToArray(), positions.IsEmpty ? null : positions.ToArray(), tileMode);
        _shaderDirty = true;
        _propertiesDirty = true;
    }

    public void ClearGradient()
    {
        if (_gradient is null) return;
        _gradient = null;
        _shaderDirty = true;
        _propertiesDirty = true;
    }

    /// <summary>Strokes with alternating on and off lengths. <paramref name="intervals"/> needs an even count of at least 2.</summary>
    public void SetDash(ReadOnlySpan<float> intervals, float phase = 0)
    {
        if (intervals.Length < 2 || intervals.Length % 2 != 0)
            throw new ArgumentException("A dash pattern needs an even number of lengths, two at least.", nameof(intervals));
        _dash = intervals.ToArray();
        _dashPhase = phase;
        _effectDirty = true;
        _propertiesDirty = true;
    }

    public void ClearDash()
    {
        if (_dash is null) return;
        _dash = null;
        _effectDirty = true;
        _propertiesDirty = true;
    }

    /// <summary>Blurs everything drawn with this paint.</summary>
    public void SetBlur(float sigmaX, float sigmaY)
    {
        _filter = new FilterSpec(false, 0, 0, sigmaX, sigmaY, default);
        _filterDirty = true;
        _propertiesDirty = true;
    }

    /// <summary>Draws a blurred, offset shadow beneath what is drawn with this paint.</summary>
    public void SetDropShadow(float dx, float dy, float sigmaX, float sigmaY, Color color)
    {
        _filter = new FilterSpec(true, dx, dy, sigmaX, sigmaY, color);
        _filterDirty = true;
        _propertiesDirty = true;
    }

    public void ClearFilter()
    {
        if (_filter is null) return;
        _filter = null;
        _filterDirty = true;
        _propertiesDirty = true;
    }

    /// <summary>The backend paint, brought up to date with the properties above.</summary>
    internal SKPaint Resolve()
    {
        var paint = _paint ??= new SKPaint();
        if (!_propertiesDirty && !_shaderDirty && !_effectDirty && !_filterDirty) return paint;

        paint.IsAntialias = _antialias;
        paint.Style = _stroke ? SKPaintStyle.Stroke : SKPaintStyle.Fill;
        paint.StrokeWidth = _strokeWidth;
        paint.StrokeCap = _cap switch { StrokeCap.Round => SKStrokeCap.Round, StrokeCap.Square => SKStrokeCap.Square, _ => SKStrokeCap.Butt };
        paint.StrokeJoin = _join switch { StrokeJoin.Round => SKStrokeJoin.Round, StrokeJoin.Bevel => SKStrokeJoin.Bevel, _ => SKStrokeJoin.Miter };
        paint.StrokeMiter = _miter;

        if (_shaderDirty)
        {
            _shader?.Dispose();
            _shader = _gradient is { } g ? CreateShader(g) : null;
            paint.Shader = _shader;
            _shaderDirty = false;
        }

        if (_effectDirty)
        {
            _pathEffect?.Dispose();
            _pathEffect = _dash is null ? null : SKPathEffect.CreateDash(_dash, _dashPhase);
            paint.PathEffect = _pathEffect;
            _effectDirty = false;
        }

        if (_filterDirty)
        {
            _imageFilter?.Dispose();
            _imageFilter = _filter is { } f
                ? f.IsShadow
                    ? SKImageFilter.CreateDropShadow(f.Dx, f.Dy, f.SigmaX, f.SigmaY, f.Color.ToSKColor())
                    : SKImageFilter.CreateBlur(f.SigmaX, f.SigmaY)
                : null;
            paint.ImageFilter = _imageFilter;
            _filterDirty = false;
        }

        // A shader supplies the color; the paint's alpha still modulates it, which carries the opacity.
        var alpha = (byte)Math.Round(_color.A * _opacity);
        paint.Color = _gradient is null
            ? new SKColor(_color.R, _color.G, _color.B, alpha)
            : new SKColor(255, 255, 255, (byte)Math.Round(255 * _opacity));

        _propertiesDirty = false;
        return paint;
    }

    private static SKShader CreateShader(GradientSpec g)
    {
        var colors = Array.ConvertAll(g.Colors, c => c.ToSKColor());
        var tile = g.TileMode switch
        {
            GradientTileMode.Repeat => SKShaderTileMode.Repeat,
            GradientTileMode.Mirror => SKShaderTileMode.Mirror,
            _ => SKShaderTileMode.Clamp,
        };
        return g.IsRadial
            ? SKShader.CreateRadialGradient(new SKPoint(g.X0, g.Y0), g.Radius, colors, g.Positions, tile)
            : SKShader.CreateLinearGradient(new SKPoint(g.X0, g.Y0), new SKPoint(g.X1, g.Y1), colors, g.Positions, tile);
    }

    public void Dispose()
    {
        _paint?.Dispose();
        _shader?.Dispose();
        _pathEffect?.Dispose();
        _imageFilter?.Dispose();
        _colorFilter?.Dispose();
        _paint = null;
        _shader = null;
        _pathEffect = null;
        _imageFilter = null;
        _colorFilter = null;
    }

    private sealed record GradientSpec(
        bool IsRadial, float X0, float Y0, float X1, float Y1, float Radius,
        Color[] Colors, float[]? Positions, GradientTileMode TileMode);

    private sealed record FilterSpec(bool IsShadow, float Dx, float Dy, float SigmaX, float SigmaY, Color Color);
}
