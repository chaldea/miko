using SkiaSharp;

namespace Miko.Common;

/// <summary>
/// 浮点矩形
/// </summary>
public struct RectF
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    public RectF(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public float Left => X;
    public float Top => Y;
    public float Right => X + Width;
    public float Bottom => Y + Height;

    public SKRect ToSKRect() => new SKRect(X, Y, X + Width, Y + Height);

    public bool IntersectsWith(RectF other)
    {
        return Left < other.Right && Right > other.Left &&
               Top < other.Bottom && Bottom > other.Top;
    }

    public bool Contains(float x, float y)
    {
        return x >= Left && x <= Right && y >= Top && y <= Bottom;
    }

    public static RectF Union(RectF a, RectF b)
    {
        float left = Math.Min(a.Left, b.Left);
        float top = Math.Min(a.Top, b.Top);
        float right = Math.Max(a.Right, b.Right);
        float bottom = Math.Max(a.Bottom, b.Bottom);

        return new RectF(left, top, right - left, bottom - top);
    }

    public static RectF Intersect(RectF a, RectF b)
    {
        float left = Math.Max(a.Left, b.Left);
        float top = Math.Max(a.Top, b.Top);
        float right = Math.Min(a.Right, b.Right);
        float bottom = Math.Min(a.Bottom, b.Bottom);

        if (right > left && bottom > top)
        {
            return new RectF(left, top, right - left, bottom - top);
        }

        return new RectF(0, 0, 0, 0);
    }

    public override string ToString() => $"RectF({X}, {Y}, {Width}, {Height})";
}

/// <summary>
/// 边距尺寸
/// </summary>
public struct EdgeSizes
{
    public float Top;
    public float Right;
    public float Bottom;
    public float Left;

    public EdgeSizes(float all)
    {
        Top = Right = Bottom = Left = all;
    }

    public EdgeSizes(float vertical, float horizontal)
    {
        Top = Bottom = vertical;
        Left = Right = horizontal;
    }

    public EdgeSizes(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;

    public override string ToString() => $"EdgeSizes({Top}, {Right}, {Bottom}, {Left})";
}
