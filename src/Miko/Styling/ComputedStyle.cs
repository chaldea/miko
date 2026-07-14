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

    // 轮廓（outline）计算值。默认无轮廓（style=None、width=0）。
    public new Length OutlineWidth { get; set; } = Length.Px(0);
    public new Color OutlineColor { get; set; } = Color.Black;
    public new BorderStyle OutlineStyle { get; set; } = Common.BorderStyle.None;
    public new Length OutlineOffset { get; set; } = Length.Px(0);

    /// <summary>轮廓是否可见（有非 None 线型、正宽度、非全透明颜色）。</summary>
    public bool HasVisibleOutline =>
        OutlineStyle != Common.BorderStyle.None && OutlineWidth.Value > 0 && OutlineColor.A > 0;

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
    public new Length LetterSpacing { get; set; } = Length.Px(0);  // normal = 0
    public new OverflowWrap OverflowWrap { get; set; } = Common.OverflowWrap.Normal;
    public new WordBreak WordBreak { get; set; } = Common.WordBreak.Normal;
    public new TextOverflow TextOverflow { get; set; } = Common.TextOverflow.Clip;
    public new Visibility Visibility { get; set; } = Common.Visibility.Visible;
    public new UserSelect UserSelect { get; set; } = Common.UserSelect.Auto;
    public new Cursor Cursor { get; set; } = Common.Cursor.Default;
    public new PointerEvents PointerEvents { get; set; } = Common.PointerEvents.Auto;
    public new FlexWrap FlexWrap { get; set; } = Common.FlexWrap.Nowrap;
    public new AlignSelf AlignSelf { get; set; } = Common.AlignSelf.Auto;
    public new AlignContent AlignContent { get; set; } = Common.AlignContent.FlexStart;
    public new int Order { get; set; } = 0;

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
    /// 本元素生效的完整自定义变量作用域（继承自祖先 + 本元素定义）。
    /// 由 <see cref="StyleResolver"/> 在计算前注入，供本元素解析 <c>Var(...)</c> 引用，
    /// 并供后代继承（读取父 <see cref="ComputedStyle"/> 的该字段）。
    /// <para>刻意遮蔽基类 <see cref="Style.Vars"/>：基类存“本节点声明的变量”，
    /// 此处存“级联继承后本节点生效的完整作用域”。</para>
    /// </summary>
    public new Dictionary<string, VarValue>? Vars { get; set; }

    /// <summary>
    /// 本元素解析样式时的父元素计算样式。用于解析 CSS 全局关键词
    /// <see cref="StyleKeyword.Inherit"/> / <see cref="StyleKeyword.Unset"/> 等（读取父属性值）。
    /// 由 <see cref="FromStyle"/> 在应用属性前注入；根元素为 null。
    /// </summary>
    private ComputedStyle? _keywordResolutionParent;

    /// <summary>
    /// 解析一个 <see cref="StyleProperty{T}"/>：具体值直接返回；变量引用则查当前
    /// <see cref="Vars"/> 作用域，未命中时用引用自带的 fallback，仍未命中返回 false
    /// （调用方据此保持默认/继承值）。
    /// <para>不含关键词消解——仅用于内部“变量/具体值”两路场景（如无父上下文的解析路径）。</para>
    /// </summary>
    internal bool TryResolveStyleProperty<T>(StyleProperty<T> property, out T value)
    {
        if (property.TryGetValue(out value))
            return true;

        // 关键词但无“父属性值 + 是否可继承”上下文：无法就地消解，视为未解析。
        // （带上下文的重载会正确处理；此重载仅供不涉及关键词的调用点复用。）
        if (property.IsKeyword)
        {
            value = default!;
            return false;
        }

        var reference = property.VarRef;
        if (Vars != null && Vars.TryGetValue(reference.Name, out var resolved) && resolved.TryGet(out value))
            return true;

        if (reference.Fallback is { } fallback && fallback.TryGet(out value))
            return true;

        value = default!;
        return false;
    }

    /// <summary>
    /// 解析一个 <see cref="StyleProperty{T}"/>，含 CSS 全局关键词消解。
    /// 具体值/变量引用走 <see cref="TryResolveStyleProperty{T}(StyleProperty{T}, out T)"/>；关键词按语义解析：
    /// <list type="bullet">
    /// <item><c>initial</c> → 返回 false（保留 ComputedStyle 的初始/默认值）。</item>
    /// <item><c>inherit</c> → 取 <paramref name="parentValue"/>（父元素该属性计算值）。</item>
    /// <item><c>unset</c>/<c>revert</c>/<c>revert-layer</c> → 可继承属性等价于 inherit，否则等价于 initial。</item>
    /// </list>
    /// 无父元素（<see cref="_keywordResolutionParent"/> 为 null）时，inherit 类关键词退回默认（返回 false）。
    /// </summary>
    /// <param name="parentValue">父元素该属性的计算值（仅当需要继承时使用）。</param>
    /// <param name="inheritable">该属性是否为 CSS 可继承属性（决定 unset/revert 的走向）。</param>
    internal bool TryResolveStyleProperty<T>(StyleProperty<T> property, T parentValue, bool inheritable, out T value)
    {
        if (!property.IsKeyword)
            return TryResolveStyleProperty(property, out value);

        // 关键词消解为“是否继承父值”。
        bool inherit = property.Keyword switch
        {
            StyleKeyword.Inherit => true,
            StyleKeyword.Initial => false,
            // unset / revert / revert-layer：可继承属性继承父值，否则回退初始值。
            _ => inheritable,
        };

        if (inherit && _keywordResolutionParent != null)
        {
            value = parentValue;
            return true;
        }

        // initial，或 inherit/unset 但无父元素：不写入 → 保留 ComputedStyle 默认值。
        value = default!;
        return false;
    }

    /// <summary>
    /// 从样式对象创建计算样式
    /// </summary>
    /// <param name="style">级联后的样式。</param>
    /// <param name="parentFontSizePx">
    /// 父元素的计算字体大小（px），用于解析本元素 font-size 中的 em 分量
    /// （CSS 中 font-size 的 em 相对于父元素字体大小）。null 时回退到 RootFontSize。
    /// </param>
    /// <param name="varScope">
    /// 本元素生效的自定义变量作用域。用于解析 <paramref name="style"/> 中的 <c>Var(...)</c> 引用，
    /// 并记录到结果的 <see cref="Vars"/> 上供后代继承。
    /// </param>
    /// <param name="parent">
    /// 父元素的计算样式。用于解析 CSS 全局关键词 <c>inherit</c> / <c>unset</c> 等
    /// （读取父属性值）。根元素为 null。
    /// </param>
    public static ComputedStyle FromStyle(Style? style, float? parentFontSizePx = null,
        Dictionary<string, VarValue>? varScope = null, ComputedStyle? parent = null)
    {
        var computed = new ComputedStyle();
        // 先设作用域与父上下文：ApplyStylePropertiesGenerated 与下方特例都会经
        // TryResolveStyleProperty 读取它们来解析变量引用与 inherit/unset 关键词。
        computed.Vars = varScope;
        computed._keywordResolutionParent = parent;

        if (style != null)
        {
            // 调用生成的通用属性赋值（内部对每个属性解析变量引用与全局关键词）
            computed.ApplyStylePropertiesGenerated(style);

            // 特殊处理：font-size 的 em 单位需要相对父元素解析。font-size 可继承。
            if (style.FontSize is { } fontSizeProp &&
                computed.TryResolveStyleProperty(fontSizeProp, parent?.FontSize ?? default, inheritable: true, out var fontSize))
            {
                computed.FontSize = Length.Px(fontSize.ToPixels(0, parentFontSizePx));
            }

            // 特殊处理：边框宽度的统一属性回退逻辑。边框类简写不可继承。
            if (style.BorderWidth is { } borderWidthProp &&
                computed.TryResolveStyleProperty(borderWidthProp, parent?.BorderTopWidth ?? default, inheritable: false, out var borderWidth))
            {
                if (style.BorderTopWidth == null) computed.BorderTopWidth = borderWidth;
                if (style.BorderRightWidth == null) computed.BorderRightWidth = borderWidth;
                if (style.BorderBottomWidth == null) computed.BorderBottomWidth = borderWidth;
                if (style.BorderLeftWidth == null) computed.BorderLeftWidth = borderWidth;
            }

            // 特殊处理：边框颜色的统一属性回退逻辑
            if (style.BorderColor is { } borderColorProp &&
                computed.TryResolveStyleProperty(borderColorProp, parent?.BorderTopColor ?? default, inheritable: false, out var borderColor))
            {
                if (style.BorderTopColor == null) computed.BorderTopColor = borderColor;
                if (style.BorderRightColor == null) computed.BorderRightColor = borderColor;
                if (style.BorderBottomColor == null) computed.BorderBottomColor = borderColor;
                if (style.BorderLeftColor == null) computed.BorderLeftColor = borderColor;
            }

            // 特殊处理：边框样式的统一属性回退逻辑
            if (style.BorderStyle is { } borderStyleProp &&
                computed.TryResolveStyleProperty(borderStyleProp, parent?.BorderTopStyle ?? default, inheritable: false, out var borderStyle))
            {
                if (style.BorderTopStyle == null) computed.BorderTopStyle = borderStyle;
                if (style.BorderRightStyle == null) computed.BorderRightStyle = borderStyle;
                if (style.BorderBottomStyle == null) computed.BorderBottomStyle = borderStyle;
                if (style.BorderLeftStyle == null) computed.BorderLeftStyle = borderStyle;
            }
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
