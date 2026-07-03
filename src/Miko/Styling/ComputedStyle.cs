using Miko.Animation;
using Miko.Common;

namespace Miko.Styling;

/// <summary>
/// 计算后的样式（包含所有默认值）
/// </summary>
public partial class ComputedStyle : Style
{
    // 使用非空的默认值
    public new Display Display { get; set; } = Common.Display.Block;
    public new BoxSizing BoxSizing { get; set; } = Common.BoxSizing.ContentBox;
    public new FlexDirection FlexDirection { get; set; } = Common.FlexDirection.Row;
    public new JustifyContent JustifyContent { get; set; } = Common.JustifyContent.FlexStart;
    public new AlignItems AlignItems { get; set; } = Common.AlignItems.Stretch;

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
    public new Cursor Cursor { get; set; } = Common.Cursor.Default;
    public new PointerEvents PointerEvents { get; set; } = Common.PointerEvents.Auto;
    public new FlexWrap FlexWrap { get; set; } = Common.FlexWrap.Nowrap;
    public new AlignSelf AlignSelf { get; set; } = Common.AlignSelf.Auto;
    public new AlignContent AlignContent { get; set; } = Common.AlignContent.FlexStart;

    // Gap 默认 0；RowGap/ColumnGap 默认 Auto，表示"未单独设置"，回退到 Gap。
    public new Length Gap { get; set; } = Length.Px(0);
    public new Length RowGap { get; set; } = Length.Auto;
    public new Length ColumnGap { get; set; } = Length.Auto;

    public new float Opacity { get; set; } = 1.0f;
    public new int ZIndex { get; set; } = 0;

    public new Overflow OverflowX { get; set; } = Overflow.Visible;
    public new Overflow OverflowY { get; set; } = Overflow.Visible;

    // vertical-align 默认 baseline（与 CSS 一致）。
    public new VerticalAlign VerticalAlign { get; set; } = Common.VerticalAlign.Baseline;

    /// <summary>
    /// 表格布局算法（默认 auto，与浏览器行为一致）。
    /// </summary>
    public new TableLayoutAlgorithm TableLayout { get; set; } = TableLayoutAlgorithm.Auto;

    public new Transform Transform { get; set; } = Transform.None;
    public new TransformOrigin TransformOrigin { get; set; } = TransformOrigin.Center;
    public new List<Transition> Transitions { get; set; } = new();
    public new List<KeyframeAnimation> Animations { get; set; } = new();

    public new string? Content { get; set; }


    /// <summary>
    /// 从样式对象创建计算样式
    /// </summary>
    /// <param name="style">级联后的样式。</param>
    /// <param name="parentFontSizePx">
    /// 父元素的计算字体大小（px），用于解析本元素 font-size 中的 em 分量
    /// （CSS 中 font-size 的 em 相对于父元素字体大小）。null 时回退到 RootFontSize。
    /// </param>
    public static ComputedStyle FromStyle(Style? style, float? parentFontSizePx = null)
    {
        var computed = new ComputedStyle();

        if (style != null)
        {
            // 调用生成的通用属性赋值
            computed.ApplyStylePropertiesGenerated(style);

            // 特殊处理：font-size 的 em 单位需要相对父元素解析
            if (style.FontSize.HasValue)
            {
                computed.FontSize = Length.Px(style.FontSize.Value.ToPixels(0, parentFontSizePx));
            }

            // 特殊处理：边框宽度的统一属性回退逻辑
            if (!style.BorderTopWidth.HasValue && style.BorderWidth.HasValue)
                computed.BorderTopWidth = style.BorderWidth.Value;
            if (!style.BorderRightWidth.HasValue && style.BorderWidth.HasValue)
                computed.BorderRightWidth = style.BorderWidth.Value;
            if (!style.BorderBottomWidth.HasValue && style.BorderWidth.HasValue)
                computed.BorderBottomWidth = style.BorderWidth.Value;
            if (!style.BorderLeftWidth.HasValue && style.BorderWidth.HasValue)
                computed.BorderLeftWidth = style.BorderWidth.Value;

            // 特殊处理：边框颜色的统一属性回退逻辑
            if (!style.BorderTopColor.HasValue && style.BorderColor.HasValue)
                computed.BorderTopColor = style.BorderColor.Value;
            if (!style.BorderRightColor.HasValue && style.BorderColor.HasValue)
                computed.BorderRightColor = style.BorderColor.Value;
            if (!style.BorderBottomColor.HasValue && style.BorderColor.HasValue)
                computed.BorderBottomColor = style.BorderColor.Value;
            if (!style.BorderLeftColor.HasValue && style.BorderColor.HasValue)
                computed.BorderLeftColor = style.BorderColor.Value;

            // 特殊处理：边框样式的统一属性回退逻辑
            if (!style.BorderTopStyle.HasValue && style.BorderStyle.HasValue)
                computed.BorderTopStyle = style.BorderStyle.Value;
            if (!style.BorderRightStyle.HasValue && style.BorderStyle.HasValue)
                computed.BorderRightStyle = style.BorderStyle.Value;
            if (!style.BorderBottomStyle.HasValue && style.BorderStyle.HasValue)
                computed.BorderBottomStyle = style.BorderStyle.Value;
            if (!style.BorderLeftStyle.HasValue && style.BorderStyle.HasValue)
                computed.BorderLeftStyle = style.BorderStyle.Value;
        }

        return computed;
    }

    /// <summary>
    /// 由 Source Generator 实现的样式属性应用逻辑
    /// </summary>
    partial void ApplyStylePropertiesGenerated(Style style);


    /// <summary>
    /// 折算所有长度属性中的 env(safe-area-inset-*) 分量为像素。由布局引擎在样式计算阶段、
    /// 已知平台安全区边距时调用。仅影响显式使用了 env() 的属性（其余长度原样返回），
    /// 因此对桌面（零安全区）或未使用 env() 的元素完全无副作用。
    /// <para>
    /// 注意：这只解析“内容元素主动声明的”安全区内边距等；不会内缩整个视口，故全屏浮层
    /// （未使用 env() 的 absolute 100%×100% 元素）仍覆盖整个屏幕（见 ISSUE-054）。
    /// </para>
    /// </summary>
    public void ResolveSafeArea(SafeAreaInsets insets)
    {
        if (insets.IsZero) return;

        PaddingTop = PaddingTop.ResolveSafeArea(insets);
        PaddingRight = PaddingRight.ResolveSafeArea(insets);
        PaddingBottom = PaddingBottom.ResolveSafeArea(insets);
        PaddingLeft = PaddingLeft.ResolveSafeArea(insets);

        MarginTop = MarginTop.ResolveSafeArea(insets);
        MarginRight = MarginRight.ResolveSafeArea(insets);
        MarginBottom = MarginBottom.ResolveSafeArea(insets);
        MarginLeft = MarginLeft.ResolveSafeArea(insets);

        Top = Top.ResolveSafeArea(insets);
        Right = Right.ResolveSafeArea(insets);
        Bottom = Bottom.ResolveSafeArea(insets);
        Left = Left.ResolveSafeArea(insets);

        Width = Width.ResolveSafeArea(insets);
        Height = Height.ResolveSafeArea(insets);
        MinWidth = MinWidth.ResolveSafeArea(insets);
        MinHeight = MinHeight.ResolveSafeArea(insets);
        MaxWidth = MaxWidth.ResolveSafeArea(insets);
        MaxHeight = MaxHeight.ResolveSafeArea(insets);
        FlexBasis = FlexBasis.ResolveSafeArea(insets);
    }
}
