using Miko.Common;

namespace Miko.Styling;

/// <summary>
/// 计算后的样式（包含所有默认值）
/// </summary>
public class ComputedStyle : Style
{
    // 使用非空的默认值
    public new Display Display { get; set; } = Common.Display.Block;
    public new FlexDirection FlexDirection { get; set; } = Common.FlexDirection.Row;
    public new JustifyContent JustifyContent { get; set; } = Common.JustifyContent.FlexStart;
    public new AlignItems AlignItems { get; set; } = Common.AlignItems.FlexStart;

    // Flex 子元素属性（CSS 默认值）
    public new float FlexGrow { get; set; } = 0f;
    public new float FlexShrink { get; set; } = 1f;
    public new Length FlexBasis { get; set; } = Length.Auto;

    public new Length Width { get; set; } = Length.Auto;
    public new Length Height { get; set; } = Length.Auto;
    public new Length MinWidth { get; set; } = Length.Px(0);
    public new Length MinHeight { get; set; } = Length.Px(0);
    public new Length MaxWidth { get; set; } = Length.Auto;
    public new Length MaxHeight { get; set; } = Length.Auto;

    public new Length PaddingTop { get; set; } = Length.Px(0);
    public new Length PaddingRight { get; set; } = Length.Px(0);
    public new Length PaddingBottom { get; set; } = Length.Px(0);
    public new Length PaddingLeft { get; set; } = Length.Px(0);

    public new Length MarginTop { get; set; } = Length.Px(0);
    public new Length MarginRight { get; set; } = Length.Px(0);
    public new Length MarginBottom { get; set; } = Length.Px(0);
    public new Length MarginLeft { get; set; } = Length.Px(0);

    // 边框宽度（每边单独计算）
    public new Length BorderTopWidth { get; set; } = Length.Px(0);
    public new Length BorderRightWidth { get; set; } = Length.Px(0);
    public new Length BorderBottomWidth { get; set; } = Length.Px(0);
    public new Length BorderLeftWidth { get; set; } = Length.Px(0);

    // 边框颜色（每边单独计算）
    public new Color BorderTopColor { get; set; } = Color.Black;
    public new Color BorderRightColor { get; set; } = Color.Black;
    public new Color BorderBottomColor { get; set; } = Color.Black;
    public new Color BorderLeftColor { get; set; } = Color.Black;

    // 边框样式（每边单独计算）
    public new BorderStyle BorderTopStyle { get; set; } = Common.BorderStyle.None;
    public new BorderStyle BorderRightStyle { get; set; } = Common.BorderStyle.None;
    public new BorderStyle BorderBottomStyle { get; set; } = Common.BorderStyle.None;
    public new BorderStyle BorderLeftStyle { get; set; } = Common.BorderStyle.None;

    // 便捷属性：获取完整的边框侧
    public BorderSide ComputedBorderTop => new(BorderTopWidth, BorderTopStyle, BorderTopColor);
    public BorderSide ComputedBorderRight => new(BorderRightWidth, BorderRightStyle, BorderRightColor);
    public BorderSide ComputedBorderBottom => new(BorderBottomWidth, BorderBottomStyle, BorderBottomColor);
    public BorderSide ComputedBorderLeft => new(BorderLeftWidth, BorderLeftStyle, BorderLeftColor);

    public new Length BorderTopLeftRadius { get; set; } = Length.Px(0);
    public new Length BorderTopRightRadius { get; set; } = Length.Px(0);
    public new Length BorderBottomRightRadius { get; set; } = Length.Px(0);
    public new Length BorderBottomLeftRadius { get; set; } = Length.Px(0);

    public new Color BackgroundColor { get; set; } = Color.Transparent;
    public new Color Color { get; set; } = Color.Black;
    public new string FontFamily { get; set; } = "Arial";
    public new Length FontSize { get; set; } = Length.Px(16);
    public new FontWeight FontWeight { get; set; } = Common.FontWeight.Normal;
    public new TextAlign TextAlign { get; set; } = Common.TextAlign.Left;
    public new Length LineHeight { get; set; } = Length.Px(0);  // 0 = auto/normal

    public new Position Position { get; set; } = Common.Position.Static;
    public new Length Top { get; set; } = Length.Auto;
    public new Length Right { get; set; } = Length.Auto;
    public new Length Bottom { get; set; } = Length.Auto;
    public new Length Left { get; set; } = Length.Auto;

    public new float Opacity { get; set; } = 1.0f;
    public new int ZIndex { get; set; } = 0;

    /// <summary>
    /// 从样式对象创建计算样式
    /// </summary>
    public static ComputedStyle FromStyle(Style? style)
    {
        var computed = new ComputedStyle();

        if (style != null)
        {
            if (style.Display.HasValue) computed.Display = style.Display.Value;
            if (style.FlexDirection.HasValue) computed.FlexDirection = style.FlexDirection.Value;
            if (style.JustifyContent.HasValue) computed.JustifyContent = style.JustifyContent.Value;
            if (style.AlignItems.HasValue) computed.AlignItems = style.AlignItems.Value;

            if (style.FlexGrow.HasValue) computed.FlexGrow = style.FlexGrow.Value;
            if (style.FlexShrink.HasValue) computed.FlexShrink = style.FlexShrink.Value;
            if (style.FlexBasis.HasValue) computed.FlexBasis = style.FlexBasis.Value;

            if (style.Width.HasValue) computed.Width = style.Width.Value;
            if (style.Height.HasValue) computed.Height = style.Height.Value;
            if (style.MinWidth.HasValue) computed.MinWidth = style.MinWidth.Value;
            if (style.MinHeight.HasValue) computed.MinHeight = style.MinHeight.Value;
            if (style.MaxWidth.HasValue) computed.MaxWidth = style.MaxWidth.Value;
            if (style.MaxHeight.HasValue) computed.MaxHeight = style.MaxHeight.Value;

            if (style.PaddingTop.HasValue) computed.PaddingTop = style.PaddingTop.Value;
            if (style.PaddingRight.HasValue) computed.PaddingRight = style.PaddingRight.Value;
            if (style.PaddingBottom.HasValue) computed.PaddingBottom = style.PaddingBottom.Value;
            if (style.PaddingLeft.HasValue) computed.PaddingLeft = style.PaddingLeft.Value;

            if (style.MarginTop.HasValue) computed.MarginTop = style.MarginTop.Value;
            if (style.MarginRight.HasValue) computed.MarginRight = style.MarginRight.Value;
            if (style.MarginBottom.HasValue) computed.MarginBottom = style.MarginBottom.Value;
            if (style.MarginLeft.HasValue) computed.MarginLeft = style.MarginLeft.Value;

            // 边框宽度：单边属性 > 统一属性 > 默认值
            if (style.BorderTopWidth.HasValue)
                computed.BorderTopWidth = style.BorderTopWidth.Value;
            else if (style.BorderWidth.HasValue)
                computed.BorderTopWidth = style.BorderWidth.Value;

            if (style.BorderRightWidth.HasValue)
                computed.BorderRightWidth = style.BorderRightWidth.Value;
            else if (style.BorderWidth.HasValue)
                computed.BorderRightWidth = style.BorderWidth.Value;

            if (style.BorderBottomWidth.HasValue)
                computed.BorderBottomWidth = style.BorderBottomWidth.Value;
            else if (style.BorderWidth.HasValue)
                computed.BorderBottomWidth = style.BorderWidth.Value;

            if (style.BorderLeftWidth.HasValue)
                computed.BorderLeftWidth = style.BorderLeftWidth.Value;
            else if (style.BorderWidth.HasValue)
                computed.BorderLeftWidth = style.BorderWidth.Value;

            // 边框颜色：单边属性 > 统一属性 > 默认值
            if (style.BorderTopColor.HasValue)
                computed.BorderTopColor = style.BorderTopColor.Value;
            else if (style.BorderColor.HasValue)
                computed.BorderTopColor = style.BorderColor.Value;

            if (style.BorderRightColor.HasValue)
                computed.BorderRightColor = style.BorderRightColor.Value;
            else if (style.BorderColor.HasValue)
                computed.BorderRightColor = style.BorderColor.Value;

            if (style.BorderBottomColor.HasValue)
                computed.BorderBottomColor = style.BorderBottomColor.Value;
            else if (style.BorderColor.HasValue)
                computed.BorderBottomColor = style.BorderColor.Value;

            if (style.BorderLeftColor.HasValue)
                computed.BorderLeftColor = style.BorderLeftColor.Value;
            else if (style.BorderColor.HasValue)
                computed.BorderLeftColor = style.BorderColor.Value;

            // 边框样式：单边属性 > 统一属性 > 默认值
            if (style.BorderTopStyle.HasValue)
                computed.BorderTopStyle = style.BorderTopStyle.Value;
            else if (style.BorderStyle.HasValue)
                computed.BorderTopStyle = style.BorderStyle.Value;

            if (style.BorderRightStyle.HasValue)
                computed.BorderRightStyle = style.BorderRightStyle.Value;
            else if (style.BorderStyle.HasValue)
                computed.BorderRightStyle = style.BorderStyle.Value;

            if (style.BorderBottomStyle.HasValue)
                computed.BorderBottomStyle = style.BorderBottomStyle.Value;
            else if (style.BorderStyle.HasValue)
                computed.BorderBottomStyle = style.BorderStyle.Value;

            if (style.BorderLeftStyle.HasValue)
                computed.BorderLeftStyle = style.BorderLeftStyle.Value;
            else if (style.BorderStyle.HasValue)
                computed.BorderLeftStyle = style.BorderStyle.Value;

            if (style.BorderTopLeftRadius.HasValue) computed.BorderTopLeftRadius = style.BorderTopLeftRadius.Value;
            if (style.BorderTopRightRadius.HasValue) computed.BorderTopRightRadius = style.BorderTopRightRadius.Value;
            if (style.BorderBottomRightRadius.HasValue) computed.BorderBottomRightRadius = style.BorderBottomRightRadius.Value;
            if (style.BorderBottomLeftRadius.HasValue) computed.BorderBottomLeftRadius = style.BorderBottomLeftRadius.Value;

            if (style.BackgroundColor.HasValue) computed.BackgroundColor = style.BackgroundColor.Value;
            if (style.Color.HasValue) computed.Color = style.Color.Value;
            if (style.FontFamily != null) computed.FontFamily = style.FontFamily;
            if (style.FontSize.HasValue) computed.FontSize = style.FontSize.Value;
            if (style.FontWeight.HasValue) computed.FontWeight = style.FontWeight.Value;
            if (style.TextAlign.HasValue) computed.TextAlign = style.TextAlign.Value;
            if (style.LineHeight.HasValue) computed.LineHeight = style.LineHeight.Value;

            if (style.Position.HasValue) computed.Position = style.Position.Value;
            if (style.Top.HasValue) computed.Top = style.Top.Value;
            if (style.Right.HasValue) computed.Right = style.Right.Value;
            if (style.Bottom.HasValue) computed.Bottom = style.Bottom.Value;
            if (style.Left.HasValue) computed.Left = style.Left.Value;

            if (style.Opacity.HasValue) computed.Opacity = style.Opacity.Value;
            if (style.ZIndex.HasValue) computed.ZIndex = style.ZIndex.Value;
        }

        return computed;
    }
}
