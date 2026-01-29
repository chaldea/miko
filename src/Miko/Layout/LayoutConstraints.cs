namespace Miko.Layout;

/// <summary>
/// 布局约束
/// </summary>
public class LayoutConstraints
{
    /// <summary>
    /// 可用宽度
    /// </summary>
    public float? AvailableWidth { get; set; }

    /// <summary>
    /// 可用高度
    /// </summary>
    public float? AvailableHeight { get; set; }

    /// <summary>
    /// 是否为无限宽度
    /// </summary>
    public bool IsInfiniteWidth => !AvailableWidth.HasValue;

    /// <summary>
    /// 是否为无限高度
    /// </summary>
    public bool IsInfiniteHeight => !AvailableHeight.HasValue;

    public LayoutConstraints() { }

    public LayoutConstraints(float? width, float? height)
    {
        AvailableWidth = width;
        AvailableHeight = height;
    }

    public override string ToString() => $"Constraints(W: {AvailableWidth?.ToString() ?? "∞"}, H: {AvailableHeight?.ToString() ?? "∞"})";
}
