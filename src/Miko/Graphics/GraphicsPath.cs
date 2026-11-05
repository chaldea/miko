using Miko.Common;
using SkiaSharp;

namespace Miko.Graphics;

/// <summary>
/// A vector outline built from lines, curves and arcs, drawn with <see cref="IDrawingContext.DrawPath"/>.
/// The backend path is native: dispose a path you build, or keep it for the life of the drawing that uses it.
/// </summary>
public sealed class GraphicsPath : IDisposable
{
    private SKPath? _path;

    public GraphicsPath()
    {
        _path = new SKPath();
    }

    private GraphicsPath(SKPath path)
    {
        _path = path;
    }

    internal SKPath Native => _path ?? throw new ObjectDisposedException(nameof(GraphicsPath));

    /// <summary>True when nothing has been added to the path.</summary>
    public bool IsEmpty => Native.IsEmpty;

    /// <summary>The exact bounds of the geometry, not including control points.</summary>
    public RectF TightBounds
    {
        get
        {
            Native.GetTightBounds(out var bounds);
            return new RectF(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        }
    }

    /// <summary>
    /// Parses SVG path data (the <c>d</c> attribute of a <c>&lt;path&gt;</c>).
    /// Returns <c>null</c> when the data cannot be parsed.
    /// </summary>
    public static GraphicsPath? FromSvgPathData(string svgPathData)
    {
        ArgumentNullException.ThrowIfNull(svgPathData);
        var path = SKPath.ParseSvgPathData(svgPathData);
        return path is null ? null : new GraphicsPath(path);
    }

    public void MoveTo(float x, float y) => Native.MoveTo(x, y);

    public void LineTo(float x, float y) => Native.LineTo(x, y);

    public void QuadTo(float x1, float y1, float x2, float y2) => Native.QuadTo(x1, y1, x2, y2);

    public void CubicTo(float x1, float y1, float x2, float y2, float x3, float y3)
        => Native.CubicTo(x1, y1, x2, y2, x3, y3);

    /// <summary>
    /// Appends an arc of the ellipse inscribed in <paramref name="oval"/>. Angles are in degrees, 0 points right
    /// and positive sweeps run clockwise. With <paramref name="forceMoveTo"/> the arc starts a new contour
    /// instead of connecting to the current point with a line.
    /// </summary>
    public void ArcTo(RectF oval, float startAngle, float sweepAngle, bool forceMoveTo)
        => Native.ArcTo(oval.ToSKRect(), startAngle, sweepAngle, forceMoveTo);

    /// <summary>Appends an SVG-style elliptical arc from the current point to (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool clockwise, float x, float y)
        => Native.ArcTo(
            new SKPoint(radiusX, radiusY),
            rotation,
            largeArc ? SKPathArcSize.Large : SKPathArcSize.Small,
            clockwise ? SKPathDirection.Clockwise : SKPathDirection.CounterClockwise,
            new SKPoint(x, y));

    /// <summary>Appends a circular arc of <paramref name="radius"/> tangent to the lines through the three points.</summary>
    public void ArcToTangent(float x1, float y1, float x2, float y2, float radius)
        => Native.ArcTo(new SKPoint(x1, y1), new SKPoint(x2, y2), radius);

    public void Close() => Native.Close();

    /// <summary>Removes everything from the path.</summary>
    public void Reset() => Native.Reset();

    public void Dispose()
    {
        _path?.Dispose();
        _path = null;
    }
}
