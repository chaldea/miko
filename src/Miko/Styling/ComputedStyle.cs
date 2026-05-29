using Miko.Animation;
using Miko.Common;

namespace Miko.Styling;

/// <summary>
/// 计算后的样式（包含所有默认值）
/// </summary>
public class ComputedStyle : Style
{
    // 使用非空的默认值
    public new Display Display { get; set; } = Common.Display.Block;
    public new BoxSizing BoxSizing { get; set; } = Common.BoxSizing.ContentBox;
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
    public new BackgroundImage? BackgroundImage { get; set; }
    public new BackgroundRepeat BackgroundRepeat { get; set; } = Common.BackgroundRepeat.Repeat;
    public new BackgroundSize BackgroundSize { get; set; } = Common.BackgroundSize.Auto;
    public new BackgroundPosition BackgroundPosition { get; set; } = Common.BackgroundPosition.LeftTop;
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

    public new TextDecoration TextDecoration { get; set; } = Common.TextDecoration.None;
    public new TextTransform TextTransform { get; set; } = Common.TextTransform.None;
    public new FontStyle FontStyle { get; set; } = Common.FontStyle.Normal;
    public new WhiteSpace WhiteSpace { get; set; } = Common.WhiteSpace.Normal;
    public new Visibility Visibility { get; set; } = Common.Visibility.Visible;
    public new FlexWrap FlexWrap { get; set; } = Common.FlexWrap.Nowrap;
    public new AlignSelf AlignSelf { get; set; } = Common.AlignSelf.Auto;
    public new AlignContent AlignContent { get; set; } = Common.AlignContent.FlexStart;

    public new float Opacity { get; set; } = 1.0f;
    public new int ZIndex { get; set; } = 0;

    public new Overflow OverflowX { get; set; } = Overflow.Visible;
    public new Overflow OverflowY { get; set; } = Overflow.Visible;

    public new Transform Transform { get; set; } = Transform.None;
    public new TransformOrigin TransformOrigin { get; set; } = TransformOrigin.Center;
    public new List<Transition> Transitions { get; set; } = new();
    public new List<KeyframeAnimation> Animations { get; set; } = new();

    public new string? Content { get; set; }

    internal CustomPropertyScope? CustomPropertyScope { get; set; }

    /// <summary>
    /// 从样式对象创建计算样式
    /// </summary>
    public static ComputedStyle FromStyle(Style? style)
    {
        var computed = new ComputedStyle();

        if (style != null)
        {
            if (style.Display?.TryGetValue(out var display) == true) computed.Display = display;
            if (style.BoxSizing?.TryGetValue(out var boxSizing) == true) computed.BoxSizing = boxSizing;
            if (style.FlexDirection?.TryGetValue(out var flexDirection) == true) computed.FlexDirection = flexDirection;
            if (style.JustifyContent?.TryGetValue(out var justifyContent) == true) computed.JustifyContent = justifyContent;
            if (style.AlignItems?.TryGetValue(out var alignItems) == true) computed.AlignItems = alignItems;

            if (style.FlexGrow?.TryGetValue(out var flexGrow) == true) computed.FlexGrow = flexGrow;
            if (style.FlexShrink?.TryGetValue(out var flexShrink) == true) computed.FlexShrink = flexShrink;
            if (style.FlexBasis?.TryGetValue(out var flexBasis) == true) computed.FlexBasis = flexBasis;

            if (style.Width?.TryGetValue(out var width) == true) computed.Width = width;
            if (style.Height?.TryGetValue(out var height) == true) computed.Height = height;
            if (style.MinWidth?.TryGetValue(out var minWidth) == true) computed.MinWidth = minWidth;
            if (style.MinHeight?.TryGetValue(out var minHeight) == true) computed.MinHeight = minHeight;
            if (style.MaxWidth?.TryGetValue(out var maxWidth) == true) computed.MaxWidth = maxWidth;
            if (style.MaxHeight?.TryGetValue(out var maxHeight) == true) computed.MaxHeight = maxHeight;

            if (style.PaddingTop?.TryGetValue(out var paddingTop) == true) computed.PaddingTop = paddingTop;
            if (style.PaddingRight?.TryGetValue(out var paddingRight) == true) computed.PaddingRight = paddingRight;
            if (style.PaddingBottom?.TryGetValue(out var paddingBottom) == true) computed.PaddingBottom = paddingBottom;
            if (style.PaddingLeft?.TryGetValue(out var paddingLeft) == true) computed.PaddingLeft = paddingLeft;

            if (style.MarginTop?.TryGetValue(out var marginTop) == true) computed.MarginTop = marginTop;
            if (style.MarginRight?.TryGetValue(out var marginRight) == true) computed.MarginRight = marginRight;
            if (style.MarginBottom?.TryGetValue(out var marginBottom) == true) computed.MarginBottom = marginBottom;
            if (style.MarginLeft?.TryGetValue(out var marginLeft) == true) computed.MarginLeft = marginLeft;

            // 边框宽度：单边属性 > 统一属性 > 默认值
            if (style.BorderTopWidth?.TryGetValue(out var borderTopWidth) == true)
                computed.BorderTopWidth = borderTopWidth;
            else if (style.BorderWidth?.TryGetValue(out var bwTop) == true)
                computed.BorderTopWidth = bwTop;

            if (style.BorderRightWidth?.TryGetValue(out var borderRightWidth) == true)
                computed.BorderRightWidth = borderRightWidth;
            else if (style.BorderWidth?.TryGetValue(out var bwRight) == true)
                computed.BorderRightWidth = bwRight;

            if (style.BorderBottomWidth?.TryGetValue(out var borderBottomWidth) == true)
                computed.BorderBottomWidth = borderBottomWidth;
            else if (style.BorderWidth?.TryGetValue(out var bwBottom) == true)
                computed.BorderBottomWidth = bwBottom;

            if (style.BorderLeftWidth?.TryGetValue(out var borderLeftWidth) == true)
                computed.BorderLeftWidth = borderLeftWidth;
            else if (style.BorderWidth?.TryGetValue(out var bwLeft) == true)
                computed.BorderLeftWidth = bwLeft;

            // 边框颜色：单边属性 > 统一属性 > 默认值
            if (style.BorderTopColor?.TryGetValue(out var borderTopColor) == true)
                computed.BorderTopColor = borderTopColor;
            else if (style.BorderColor?.TryGetValue(out var bcTop) == true)
                computed.BorderTopColor = bcTop;

            if (style.BorderRightColor?.TryGetValue(out var borderRightColor) == true)
                computed.BorderRightColor = borderRightColor;
            else if (style.BorderColor?.TryGetValue(out var bcRight) == true)
                computed.BorderRightColor = bcRight;

            if (style.BorderBottomColor?.TryGetValue(out var borderBottomColor) == true)
                computed.BorderBottomColor = borderBottomColor;
            else if (style.BorderColor?.TryGetValue(out var bcBottom) == true)
                computed.BorderBottomColor = bcBottom;

            if (style.BorderLeftColor?.TryGetValue(out var borderLeftColor) == true)
                computed.BorderLeftColor = borderLeftColor;
            else if (style.BorderColor?.TryGetValue(out var bcLeft) == true)
                computed.BorderLeftColor = bcLeft;

            // 边框样式：单边属性 > 统一属性 > 默认值
            if (style.BorderTopStyle?.TryGetValue(out var borderTopStyle) == true)
                computed.BorderTopStyle = borderTopStyle;
            else if (style.BorderStyle?.TryGetValue(out var bsTop) == true)
                computed.BorderTopStyle = bsTop;

            if (style.BorderRightStyle?.TryGetValue(out var borderRightStyle) == true)
                computed.BorderRightStyle = borderRightStyle;
            else if (style.BorderStyle?.TryGetValue(out var bsRight) == true)
                computed.BorderRightStyle = bsRight;

            if (style.BorderBottomStyle?.TryGetValue(out var borderBottomStyle) == true)
                computed.BorderBottomStyle = borderBottomStyle;
            else if (style.BorderStyle?.TryGetValue(out var bsBottom) == true)
                computed.BorderBottomStyle = bsBottom;

            if (style.BorderLeftStyle?.TryGetValue(out var borderLeftStyle) == true)
                computed.BorderLeftStyle = borderLeftStyle;
            else if (style.BorderStyle?.TryGetValue(out var bsLeft) == true)
                computed.BorderLeftStyle = bsLeft;

            if (style.BorderTopLeftRadius?.TryGetValue(out var btlr) == true) computed.BorderTopLeftRadius = btlr;
            if (style.BorderTopRightRadius?.TryGetValue(out var btrr) == true) computed.BorderTopRightRadius = btrr;
            if (style.BorderBottomRightRadius?.TryGetValue(out var bbrr) == true) computed.BorderBottomRightRadius = bbrr;
            if (style.BorderBottomLeftRadius?.TryGetValue(out var bblr) == true) computed.BorderBottomLeftRadius = bblr;

            if (style.BackgroundColor?.TryGetValue(out var bgColor) == true) computed.BackgroundColor = bgColor;
            if (style.BackgroundImage != null) computed.BackgroundImage = style.BackgroundImage;
            if (style.BackgroundRepeat?.TryGetValue(out var bgRepeat) == true) computed.BackgroundRepeat = bgRepeat;
            if (style.BackgroundSize?.TryGetValue(out var bgSize) == true) computed.BackgroundSize = bgSize;
            if (style.BackgroundPosition?.TryGetValue(out var bgPos) == true) computed.BackgroundPosition = bgPos;
            if (style.Color?.TryGetValue(out var color) == true) computed.Color = color;
            if (style.FontFamily != null) computed.FontFamily = style.FontFamily;
            if (style.FontSize?.TryGetValue(out var fontSize) == true) computed.FontSize = Length.Px(fontSize.ToPixels(0));
            if (style.FontWeight?.TryGetValue(out var fontWeight) == true) computed.FontWeight = fontWeight;
            if (style.TextAlign?.TryGetValue(out var textAlign) == true) computed.TextAlign = textAlign;
            if (style.LineHeight?.TryGetValue(out var lineHeight) == true) computed.LineHeight = lineHeight;

            if (style.Position?.TryGetValue(out var position) == true) computed.Position = position;
            if (style.Top?.TryGetValue(out var top) == true) computed.Top = top;
            if (style.Right?.TryGetValue(out var right) == true) computed.Right = right;
            if (style.Bottom?.TryGetValue(out var bottom) == true) computed.Bottom = bottom;
            if (style.Left?.TryGetValue(out var left) == true) computed.Left = left;

            if (style.TextDecoration?.TryGetValue(out var textDecoration) == true) computed.TextDecoration = textDecoration;
            if (style.TextTransform?.TryGetValue(out var textTransform) == true) computed.TextTransform = textTransform;
            if (style.FontStyle?.TryGetValue(out var fontStyle) == true) computed.FontStyle = fontStyle;
            if (style.WhiteSpace?.TryGetValue(out var whiteSpace) == true) computed.WhiteSpace = whiteSpace;
            if (style.Visibility?.TryGetValue(out var visibility) == true) computed.Visibility = visibility;
            if (style.FlexWrap?.TryGetValue(out var flexWrap) == true) computed.FlexWrap = flexWrap;
            if (style.AlignSelf?.TryGetValue(out var alignSelf) == true) computed.AlignSelf = alignSelf;
            if (style.AlignContent?.TryGetValue(out var alignContent) == true) computed.AlignContent = alignContent;

            if (style.Opacity?.TryGetValue(out var opacity) == true) computed.Opacity = opacity;
            if (style.ZIndex?.TryGetValue(out var zIndex) == true) computed.ZIndex = zIndex;

            if (style.OverflowX?.TryGetValue(out var overflowX) == true) computed.OverflowX = overflowX;
            if (style.OverflowY?.TryGetValue(out var overflowY) == true) computed.OverflowY = overflowY;

            if (style.Transform != null) computed.Transform = style.Transform;
            if (style.TransformOrigin?.TryGetValue(out var transformOrigin) == true) computed.TransformOrigin = transformOrigin;
            if (style.Transitions != null) computed.Transitions = style.Transitions;
            if (style.Animations != null) computed.Animations = style.Animations;

            if (style.Content != null) computed.Content = style.Content;
        }

        return computed;
    }
}
