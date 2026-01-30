using Miko.Common;

namespace Miko.Styling;

/// <summary>
/// 样式对象
/// </summary>
public class Style
{
    // 布局属性
    public Display? Display { get; set; }
    public FlexDirection? FlexDirection { get; set; }
    public JustifyContent? JustifyContent { get; set; }
    public AlignItems? AlignItems { get; set; }

    // Flex 子元素属性
    public float? FlexGrow { get; set; }
    public float? FlexShrink { get; set; }
    public Length? FlexBasis { get; set; }

    // 盒子模型
    public Length? Width { get; set; }
    public Length? Height { get; set; }
    public Length? MinWidth { get; set; }
    public Length? MinHeight { get; set; }
    public Length? MaxWidth { get; set; }
    public Length? MaxHeight { get; set; }

    // 内边距
    public Length? PaddingTop { get; set; }
    public Length? PaddingRight { get; set; }
    public Length? PaddingBottom { get; set; }
    public Length? PaddingLeft { get; set; }

    /// <summary>
    /// 内边距简写属性
    /// </summary>
    public Padding Padding
    {
        get => new Padding(
            PaddingTop ?? Length.Px(0),
            PaddingRight ?? Length.Px(0),
            PaddingBottom ?? Length.Px(0),
            PaddingLeft ?? Length.Px(0));
        set
        {
            PaddingTop = value.Top;
            PaddingRight = value.Right;
            PaddingBottom = value.Bottom;
            PaddingLeft = value.Left;
        }
    }

    // 外边距
    public Length? MarginTop { get; set; }
    public Length? MarginRight { get; set; }
    public Length? MarginBottom { get; set; }
    public Length? MarginLeft { get; set; }

    /// <summary>
    /// 外边距简写属性
    /// </summary>
    public Margin Margin
    {
        get => new Margin(
            MarginTop ?? Length.Px(0),
            MarginRight ?? Length.Px(0),
            MarginBottom ?? Length.Px(0),
            MarginLeft ?? Length.Px(0));
        set
        {
            MarginTop = value.Top;
            MarginRight = value.Right;
            MarginBottom = value.Bottom;
            MarginLeft = value.Left;
        }
    }

    // 边框
    public Length? BorderWidth { get; set; }
    public Color? BorderColor { get; set; }
    public BorderStyle? BorderStyle { get; set; }

    /// <summary>
    /// 边框简写属性
    /// </summary>
    public Border Border
    {
        get => new Border(
            BorderWidth ?? Length.Px(0),
            BorderStyle ?? Common.BorderStyle.None,
            BorderColor ?? Common.Color.Transparent);
        set
        {
            BorderWidth = value.Width;
            BorderColor = value.Color;
            BorderStyle = value.Style;
        }
    }

    // 圆角
    public Length? BorderTopLeftRadius { get; set; }
    public Length? BorderTopRightRadius { get; set; }
    public Length? BorderBottomRightRadius { get; set; }
    public Length? BorderBottomLeftRadius { get; set; }

    public BorderRadius BorderRadius
    {
        get => new BorderRadius(
            BorderTopLeftRadius ?? 0,
            BorderTopRightRadius ?? 0,
            BorderBottomLeftRadius ?? 0,
            BorderBottomRightRadius ?? 0);
        set
        {
            BorderTopLeftRadius = value.TopLeft;
            BorderTopRightRadius = value.TopRight;
            BorderBottomLeftRadius = value.BottomLeft;
            BorderBottomRightRadius = value.BottomRight;
        }
    }

    // 视觉属性
    public Color? BackgroundColor { get; set; }
    public Color? Color { get; set; }
    public string? FontFamily { get; set; }
    public Length? FontSize { get; set; }
    public FontWeight? FontWeight { get; set; }
    public TextAlign? TextAlign { get; set; }
    public Length? LineHeight { get; set; }

    // 定位
    public Position? Position { get; set; }
    public Length? Top { get; set; }
    public Length? Right { get; set; }
    public Length? Bottom { get; set; }
    public Length? Left { get; set; }

    public float? Opacity { get; set; }
    public int? ZIndex { get; set; }

    /// <summary>
    /// 合并样式（用于级联）
    /// </summary>
    public void Merge(Style other)
    {
        Display ??= other.Display;
        FlexDirection ??= other.FlexDirection;
        JustifyContent ??= other.JustifyContent;
        AlignItems ??= other.AlignItems;

        FlexGrow ??= other.FlexGrow;
        FlexShrink ??= other.FlexShrink;
        FlexBasis ??= other.FlexBasis;

        Width ??= other.Width;
        Height ??= other.Height;
        MinWidth ??= other.MinWidth;
        MinHeight ??= other.MinHeight;
        MaxWidth ??= other.MaxWidth;
        MaxHeight ??= other.MaxHeight;

        PaddingTop ??= other.PaddingTop;
        PaddingRight ??= other.PaddingRight;
        PaddingBottom ??= other.PaddingBottom;
        PaddingLeft ??= other.PaddingLeft;

        MarginTop ??= other.MarginTop;
        MarginRight ??= other.MarginRight;
        MarginBottom ??= other.MarginBottom;
        MarginLeft ??= other.MarginLeft;

        BorderWidth ??= other.BorderWidth;
        BorderColor ??= other.BorderColor;
        BorderStyle ??= other.BorderStyle;

        BorderTopLeftRadius ??= other.BorderTopLeftRadius;
        BorderTopRightRadius ??= other.BorderTopRightRadius;
        BorderBottomRightRadius ??= other.BorderBottomRightRadius;
        BorderBottomLeftRadius ??= other.BorderBottomLeftRadius;

        BackgroundColor ??= other.BackgroundColor;
        Color ??= other.Color;
        FontFamily ??= other.FontFamily;
        FontSize ??= other.FontSize;
        FontWeight ??= other.FontWeight;
        TextAlign ??= other.TextAlign;
        LineHeight ??= other.LineHeight;

        Position ??= other.Position;
        Top ??= other.Top;
        Right ??= other.Right;
        Bottom ??= other.Bottom;
        Left ??= other.Left;

        Opacity ??= other.Opacity;
        ZIndex ??= other.ZIndex;
    }

    /// <summary>
    /// 克隆样式
    /// </summary>
    public Style Clone()
    {
        return (Style)MemberwiseClone();
    }
}
