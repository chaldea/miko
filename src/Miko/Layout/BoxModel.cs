using Miko.Common;

namespace Miko.Layout;

/// <summary>
/// 盒子模型
/// </summary>
public class BoxModel
{
    // Content box
    public RectF Content { get; set; }

    // Padding box = Content + Padding
    public EdgeSizes Padding { get; set; }

    // Border box = Padding box + Border
    public EdgeSizes Border { get; set; }

    // Margin box = Border box + Margin
    public EdgeSizes Margin { get; set; }

    /// <summary>
    /// 计算 Padding Box
    /// </summary>
    public RectF PaddingBox => new RectF(
        Content.X - Padding.Left,
        Content.Y - Padding.Top,
        Content.Width + Padding.Horizontal,
        Content.Height + Padding.Vertical
    );

    /// <summary>
    /// 计算 Border Box
    /// </summary>
    public RectF BorderBox
    {
        get
        {
            var paddingBox = PaddingBox;
            return new RectF(
                paddingBox.X - Border.Left,
                paddingBox.Y - Border.Top,
                paddingBox.Width + Border.Horizontal,
                paddingBox.Height + Border.Vertical
            );
        }
    }

    /// <summary>
    /// 计算 Margin Box
    /// </summary>
    public RectF MarginBox
    {
        get
        {
            var borderBox = BorderBox;
            return new RectF(
                borderBox.X - Margin.Left,
                borderBox.Y - Margin.Top,
                borderBox.Width + Margin.Horizontal,
                borderBox.Height + Margin.Vertical
            );
        }
    }

    public override string ToString() => $"Content: {Content}, Padding: {Padding}, Border: {Border}, Margin: {Margin}";
}
