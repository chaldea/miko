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
            if (style.Display.HasValue) computed.Display = style.Display.Value;
            if (style.BoxSizing.HasValue) computed.BoxSizing = style.BoxSizing.Value;
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
            if (style.BackgroundImage != null) computed.BackgroundImage = style.BackgroundImage;
            if (style.BackgroundRepeat.HasValue) computed.BackgroundRepeat = style.BackgroundRepeat.Value;
            if (style.BackgroundSize.HasValue) computed.BackgroundSize = style.BackgroundSize.Value;
            if (style.BackgroundPosition.HasValue) computed.BackgroundPosition = style.BackgroundPosition.Value;
            if (style.Color.HasValue) computed.Color = style.Color.Value;
            if (style.FontFamily != null) computed.FontFamily = style.FontFamily;
            // font-size 中的 em 相对于父元素字体大小解析；rem 相对于根字体大小。
            if (style.FontSize.HasValue) computed.FontSize = Length.Px(style.FontSize.Value.ToPixels(0, parentFontSizePx));
            if (style.FontWeight.HasValue) computed.FontWeight = style.FontWeight.Value;
            if (style.TextAlign.HasValue) computed.TextAlign = style.TextAlign.Value;
            if (style.LineHeight.HasValue) computed.LineHeight = style.LineHeight.Value;

            if (style.Position.HasValue) computed.Position = style.Position.Value;
            if (style.Top.HasValue) computed.Top = style.Top.Value;
            if (style.Right.HasValue) computed.Right = style.Right.Value;
            if (style.Bottom.HasValue) computed.Bottom = style.Bottom.Value;
            if (style.Left.HasValue) computed.Left = style.Left.Value;

            if (style.TextDecoration.HasValue) computed.TextDecoration = style.TextDecoration.Value;
            if (style.TextTransform.HasValue) computed.TextTransform = style.TextTransform.Value;
            if (style.FontStyle.HasValue) computed.FontStyle = style.FontStyle.Value;
            if (style.WhiteSpace.HasValue) computed.WhiteSpace = style.WhiteSpace.Value;
            if (style.Visibility.HasValue) computed.Visibility = style.Visibility.Value;
            if (style.Cursor.HasValue) computed.Cursor = style.Cursor.Value;
            if (style.PointerEvents.HasValue) computed.PointerEvents = style.PointerEvents.Value;
            if (style.FlexWrap.HasValue) computed.FlexWrap = style.FlexWrap.Value;
            if (style.AlignSelf.HasValue) computed.AlignSelf = style.AlignSelf.Value;
            if (style.AlignContent.HasValue) computed.AlignContent = style.AlignContent.Value;

            if (style.Gap.HasValue) computed.Gap = style.Gap.Value;
            if (style.RowGap.HasValue) computed.RowGap = style.RowGap.Value;
            if (style.ColumnGap.HasValue) computed.ColumnGap = style.ColumnGap.Value;

            if (style.Opacity.HasValue) computed.Opacity = style.Opacity.Value;
            if (style.ZIndex.HasValue) computed.ZIndex = style.ZIndex.Value;

            if (style.BoxShadow != null) computed.BoxShadow = style.BoxShadow;

            if (style.OverflowX.HasValue) computed.OverflowX = style.OverflowX.Value;
            if (style.OverflowY.HasValue) computed.OverflowY = style.OverflowY.Value;

            if (style.TableLayout.HasValue) computed.TableLayout = style.TableLayout.Value;

            if (style.Transform != null) computed.Transform = style.Transform;
            if (style.TransformOrigin.HasValue) computed.TransformOrigin = style.TransformOrigin.Value;
            if (style.Transitions != null) computed.Transitions = style.Transitions;
            if (style.Animations != null) computed.Animations = style.Animations;

            if (style.Content != null) computed.Content = style.Content;
        }

        return computed;
    }

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
