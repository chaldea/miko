using Miko.Common;

namespace Miko.Utils;

/// <summary>
/// 几何计算工具
/// </summary>
public static class GeometryUtils
{
    /// <summary>
    /// 检查点是否在矩形内
    /// </summary>
    public static bool PointInRect(float x, float y, RectF rect)
    {
        return x >= rect.Left && x <= rect.Right &&
               y >= rect.Top && y <= rect.Bottom;
    }

    /// <summary>
    /// 计算两个矩形的交集
    /// </summary>
    public static RectF? Intersect(RectF a, RectF b)
    {
        float left = Math.Max(a.Left, b.Left);
        float top = Math.Max(a.Top, b.Top);
        float right = Math.Min(a.Right, b.Right);
        float bottom = Math.Min(a.Bottom, b.Bottom);

        if (right > left && bottom > top)
        {
            return new RectF(left, top, right - left, bottom - top);
        }

        return null;
    }

    /// <summary>
    /// 计算两个矩形的并集
    /// </summary>
    public static RectF Union(RectF a, RectF b)
    {
        float left = Math.Min(a.Left, b.Left);
        float top = Math.Min(a.Top, b.Top);
        float right = Math.Max(a.Right, b.Right);
        float bottom = Math.Max(a.Bottom, b.Bottom);

        return new RectF(left, top, right - left, bottom - top);
    }

    /// <summary>
    /// 计算矩形的面积
    /// </summary>
    public static float Area(RectF rect)
    {
        return rect.Width * rect.Height;
    }

    /// <summary>
    /// 检查矩形是否为空
    /// </summary>
    public static bool IsEmpty(RectF rect)
    {
        return rect.Width <= 0 || rect.Height <= 0;
    }

    /// <summary>
    /// 扩展矩形
    /// </summary>
    public static RectF Inflate(RectF rect, float dx, float dy)
    {
        return new RectF(
            rect.X - dx,
            rect.Y - dy,
            rect.Width + dx * 2,
            rect.Height + dy * 2
        );
    }

    /// <summary>
    /// 缩小矩形
    /// </summary>
    public static RectF Deflate(RectF rect, float dx, float dy)
    {
        return Inflate(rect, -dx, -dy);
    }

    /// <summary>
    /// 计算两点之间的距离
    /// </summary>
    public static float Distance(float x1, float y1, float x2, float y2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// 线性插值
    /// </summary>
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    /// <summary>
    /// 限制值在范围内
    /// </summary>
    public static float Clamp(float value, float min, float max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    /// <summary>
    /// 比较浮点数是否相等（带容差）
    /// </summary>
    public static bool FloatEquals(float a, float b, float epsilon = 0.0001f)
    {
        return Math.Abs(a - b) < epsilon;
    }
}
