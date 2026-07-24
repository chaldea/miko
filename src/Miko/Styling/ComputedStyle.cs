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
    // CSS 初始值为 normal：justify-content 表现为 flex-start，align-items 表现为 stretch，
    // 由布局算法在各消费点映射（见 FlexLayout/GridLayout）。
    public new JustifyContent JustifyContent { get; set; } = Common.JustifyContent.Normal;
    public new AlignItems AlignItems { get; set; } = Common.AlignItems.Normal;

    // 书写方向与书写模式（CSS 可继承，见 ISSUE-103）。计算样式以逻辑值
    // （margin-inline-start、inline-size 等）为标准存储；物理属性（MarginLeft、Width 等）
    // 是按这两个值映射到逻辑存储的门面，布局/绘制代码读取物理属性时无需感知书写模式。
    public new Direction Direction { get; set; } = Common.Direction.Ltr;
    public new WritingMode WritingMode { get; set; } = Common.WritingMode.HorizontalTb;

    // Flex 子元素属性（CSS 默认值）
    public new float FlexGrow { get; set; } = 0f;
    public new float FlexShrink { get; set; } = 1f;
    public new Length FlexBasis { get; set; } = Length.Auto;

    // 尺寸：逻辑尺寸（inline-size / block-size）为标准存储，Width/Height 等物理属性为
    // 按 writing-mode 映射的门面（horizontal-tb：inline 轴 = 水平；垂直书写：inline 轴 = 垂直）。
    public new Length InlineSize { get; set; } = Length.Auto;
    public new Length BlockSize { get; set; } = Length.Auto;
    public new Length MinInlineSize { get; set; } = Length.Px(0);
    public new Length MinBlockSize { get; set; } = Length.Px(0);
    public new Length MaxInlineSize { get; set; } = Length.Auto;
    public new Length MaxBlockSize { get; set; } = Length.Auto;

    public new Length Width
    {
        get => WritingMode == Common.WritingMode.HorizontalTb ? InlineSize : BlockSize;
        set
        {
            if (WritingMode == Common.WritingMode.HorizontalTb) InlineSize = value;
            else BlockSize = value;
        }
    }

    public new Length Height
    {
        get => WritingMode == Common.WritingMode.HorizontalTb ? BlockSize : InlineSize;
        set
        {
            if (WritingMode == Common.WritingMode.HorizontalTb) BlockSize = value;
            else InlineSize = value;
        }
    }

    public new Length MinWidth
    {
        get => WritingMode == Common.WritingMode.HorizontalTb ? MinInlineSize : MinBlockSize;
        set
        {
            if (WritingMode == Common.WritingMode.HorizontalTb) MinInlineSize = value;
            else MinBlockSize = value;
        }
    }

    public new Length MinHeight
    {
        get => WritingMode == Common.WritingMode.HorizontalTb ? MinBlockSize : MinInlineSize;
        set
        {
            if (WritingMode == Common.WritingMode.HorizontalTb) MinBlockSize = value;
            else MinInlineSize = value;
        }
    }

    public new Length MaxWidth
    {
        get => WritingMode == Common.WritingMode.HorizontalTb ? MaxInlineSize : MaxBlockSize;
        set
        {
            if (WritingMode == Common.WritingMode.HorizontalTb) MaxInlineSize = value;
            else MaxBlockSize = value;
        }
    }

    public new Length MaxHeight
    {
        get => WritingMode == Common.WritingMode.HorizontalTb ? MaxBlockSize : MaxInlineSize;
        set
        {
            if (WritingMode == Common.WritingMode.HorizontalTb) MaxBlockSize = value;
            else MaxInlineSize = value;
        }
    }

    // 内边距：逻辑边为标准存储，物理边为按书写模式/方向映射的门面（见 LogicalEdgeMap）。
    public new Length PaddingInlineStart { get; set; } = Length.Px(0);
    public new Length PaddingInlineEnd { get; set; } = Length.Px(0);
    public new Length PaddingBlockStart { get; set; } = Length.Px(0);
    public new Length PaddingBlockEnd { get; set; } = Length.Px(0);

    public new Length PaddingTop
    {
        get => GetPaddingEdge(ToLogical(PhysicalEdge.Top));
        set => SetPaddingEdge(ToLogical(PhysicalEdge.Top), value);
    }

    public new Length PaddingRight
    {
        get => GetPaddingEdge(ToLogical(PhysicalEdge.Right));
        set => SetPaddingEdge(ToLogical(PhysicalEdge.Right), value);
    }

    public new Length PaddingBottom
    {
        get => GetPaddingEdge(ToLogical(PhysicalEdge.Bottom));
        set => SetPaddingEdge(ToLogical(PhysicalEdge.Bottom), value);
    }

    public new Length PaddingLeft
    {
        get => GetPaddingEdge(ToLogical(PhysicalEdge.Left));
        set => SetPaddingEdge(ToLogical(PhysicalEdge.Left), value);
    }

    // 外边距：同内边距，逻辑边为标准存储。
    public new Length MarginInlineStart { get; set; } = Length.Px(0);
    public new Length MarginInlineEnd { get; set; } = Length.Px(0);
    public new Length MarginBlockStart { get; set; } = Length.Px(0);
    public new Length MarginBlockEnd { get; set; } = Length.Px(0);

    public new Length MarginTop
    {
        get => GetMarginEdge(ToLogical(PhysicalEdge.Top));
        set => SetMarginEdge(ToLogical(PhysicalEdge.Top), value);
    }

    public new Length MarginRight
    {
        get => GetMarginEdge(ToLogical(PhysicalEdge.Right));
        set => SetMarginEdge(ToLogical(PhysicalEdge.Right), value);
    }

    public new Length MarginBottom
    {
        get => GetMarginEdge(ToLogical(PhysicalEdge.Bottom));
        set => SetMarginEdge(ToLogical(PhysicalEdge.Bottom), value);
    }

    public new Length MarginLeft
    {
        get => GetMarginEdge(ToLogical(PhysicalEdge.Left));
        set => SetMarginEdge(ToLogical(PhysicalEdge.Left), value);
    }

    // 定位偏移：逻辑 inset 为标准存储。
    public new Length InsetInlineStart { get; set; } = Length.Auto;
    public new Length InsetInlineEnd { get; set; } = Length.Auto;
    public new Length InsetBlockStart { get; set; } = Length.Auto;
    public new Length InsetBlockEnd { get; set; } = Length.Auto;

    public new Length Top
    {
        get => GetInsetEdge(ToLogical(PhysicalEdge.Top));
        set => SetInsetEdge(ToLogical(PhysicalEdge.Top), value);
    }

    public new Length Right
    {
        get => GetInsetEdge(ToLogical(PhysicalEdge.Right));
        set => SetInsetEdge(ToLogical(PhysicalEdge.Right), value);
    }

    public new Length Bottom
    {
        get => GetInsetEdge(ToLogical(PhysicalEdge.Bottom));
        set => SetInsetEdge(ToLogical(PhysicalEdge.Bottom), value);
    }

    public new Length Left
    {
        get => GetInsetEdge(ToLogical(PhysicalEdge.Left));
        set => SetInsetEdge(ToLogical(PhysicalEdge.Left), value);
    }

    /// <summary>当前书写模式/方向下物理边对应的逻辑边。</summary>
    private LogicalEdge ToLogical(PhysicalEdge edge) => LogicalEdgeMap.ToLogical(edge, WritingMode, Direction);

    private Length GetPaddingEdge(LogicalEdge edge) => edge switch
    {
        LogicalEdge.InlineStart => PaddingInlineStart,
        LogicalEdge.InlineEnd => PaddingInlineEnd,
        LogicalEdge.BlockStart => PaddingBlockStart,
        _ => PaddingBlockEnd,
    };

    private void SetPaddingEdge(LogicalEdge edge, Length value)
    {
        switch (edge)
        {
            case LogicalEdge.InlineStart: PaddingInlineStart = value; break;
            case LogicalEdge.InlineEnd: PaddingInlineEnd = value; break;
            case LogicalEdge.BlockStart: PaddingBlockStart = value; break;
            default: PaddingBlockEnd = value; break;
        }
    }

    private Length GetMarginEdge(LogicalEdge edge) => edge switch
    {
        LogicalEdge.InlineStart => MarginInlineStart,
        LogicalEdge.InlineEnd => MarginInlineEnd,
        LogicalEdge.BlockStart => MarginBlockStart,
        _ => MarginBlockEnd,
    };

    private void SetMarginEdge(LogicalEdge edge, Length value)
    {
        switch (edge)
        {
            case LogicalEdge.InlineStart: MarginInlineStart = value; break;
            case LogicalEdge.InlineEnd: MarginInlineEnd = value; break;
            case LogicalEdge.BlockStart: MarginBlockStart = value; break;
            default: MarginBlockEnd = value; break;
        }
    }

    private Length GetInsetEdge(LogicalEdge edge) => edge switch
    {
        LogicalEdge.InlineStart => InsetInlineStart,
        LogicalEdge.InlineEnd => InsetInlineEnd,
        LogicalEdge.BlockStart => InsetBlockStart,
        _ => InsetBlockEnd,
    };

    private void SetInsetEdge(LogicalEdge edge, Length value)
    {
        switch (edge)
        {
            case LogicalEdge.InlineStart: InsetInlineStart = value; break;
            case LogicalEdge.InlineEnd: InsetInlineEnd = value; break;
            case LogicalEdge.BlockStart: InsetBlockStart = value; break;
            default: InsetBlockEnd = value; break;
        }
    }

    // 边框：逻辑边（inline/block × start/end）的宽度/颜色/样式为标准存储，
    // 物理边属性为按书写模式/方向映射的门面（见 ISSUE-103）。
    public new Length BorderInlineStartWidth { get; set; } = Length.Px(0);
    public new Length BorderInlineEndWidth { get; set; } = Length.Px(0);
    public new Length BorderBlockStartWidth { get; set; } = Length.Px(0);
    public new Length BorderBlockEndWidth { get; set; } = Length.Px(0);

    public new Color BorderInlineStartColor { get; set; } = Color.Black;
    public new Color BorderInlineEndColor { get; set; } = Color.Black;
    public new Color BorderBlockStartColor { get; set; } = Color.Black;
    public new Color BorderBlockEndColor { get; set; } = Color.Black;

    public new BorderStyle BorderInlineStartStyle { get; set; } = Common.BorderStyle.None;
    public new BorderStyle BorderInlineEndStyle { get; set; } = Common.BorderStyle.None;
    public new BorderStyle BorderBlockStartStyle { get; set; } = Common.BorderStyle.None;
    public new BorderStyle BorderBlockEndStyle { get; set; } = Common.BorderStyle.None;

    public new Length BorderTopWidth
    {
        get => GetBorderWidthEdge(ToLogical(PhysicalEdge.Top));
        set => SetBorderWidthEdge(ToLogical(PhysicalEdge.Top), value);
    }

    public new Length BorderRightWidth
    {
        get => GetBorderWidthEdge(ToLogical(PhysicalEdge.Right));
        set => SetBorderWidthEdge(ToLogical(PhysicalEdge.Right), value);
    }

    public new Length BorderBottomWidth
    {
        get => GetBorderWidthEdge(ToLogical(PhysicalEdge.Bottom));
        set => SetBorderWidthEdge(ToLogical(PhysicalEdge.Bottom), value);
    }

    public new Length BorderLeftWidth
    {
        get => GetBorderWidthEdge(ToLogical(PhysicalEdge.Left));
        set => SetBorderWidthEdge(ToLogical(PhysicalEdge.Left), value);
    }

    public new Color BorderTopColor
    {
        get => GetBorderColorEdge(ToLogical(PhysicalEdge.Top));
        set => SetBorderColorEdge(ToLogical(PhysicalEdge.Top), value);
    }

    public new Color BorderRightColor
    {
        get => GetBorderColorEdge(ToLogical(PhysicalEdge.Right));
        set => SetBorderColorEdge(ToLogical(PhysicalEdge.Right), value);
    }

    public new Color BorderBottomColor
    {
        get => GetBorderColorEdge(ToLogical(PhysicalEdge.Bottom));
        set => SetBorderColorEdge(ToLogical(PhysicalEdge.Bottom), value);
    }

    public new Color BorderLeftColor
    {
        get => GetBorderColorEdge(ToLogical(PhysicalEdge.Left));
        set => SetBorderColorEdge(ToLogical(PhysicalEdge.Left), value);
    }

    public new BorderStyle BorderTopStyle
    {
        get => GetBorderStyleEdge(ToLogical(PhysicalEdge.Top));
        set => SetBorderStyleEdge(ToLogical(PhysicalEdge.Top), value);
    }

    public new BorderStyle BorderRightStyle
    {
        get => GetBorderStyleEdge(ToLogical(PhysicalEdge.Right));
        set => SetBorderStyleEdge(ToLogical(PhysicalEdge.Right), value);
    }

    public new BorderStyle BorderBottomStyle
    {
        get => GetBorderStyleEdge(ToLogical(PhysicalEdge.Bottom));
        set => SetBorderStyleEdge(ToLogical(PhysicalEdge.Bottom), value);
    }

    public new BorderStyle BorderLeftStyle
    {
        get => GetBorderStyleEdge(ToLogical(PhysicalEdge.Left));
        set => SetBorderStyleEdge(ToLogical(PhysicalEdge.Left), value);
    }

    private Length GetBorderWidthEdge(LogicalEdge edge) => edge switch
    {
        LogicalEdge.InlineStart => BorderInlineStartWidth,
        LogicalEdge.InlineEnd => BorderInlineEndWidth,
        LogicalEdge.BlockStart => BorderBlockStartWidth,
        _ => BorderBlockEndWidth,
    };

    private void SetBorderWidthEdge(LogicalEdge edge, Length value)
    {
        switch (edge)
        {
            case LogicalEdge.InlineStart: BorderInlineStartWidth = value; break;
            case LogicalEdge.InlineEnd: BorderInlineEndWidth = value; break;
            case LogicalEdge.BlockStart: BorderBlockStartWidth = value; break;
            default: BorderBlockEndWidth = value; break;
        }
    }

    private Color GetBorderColorEdge(LogicalEdge edge) => edge switch
    {
        LogicalEdge.InlineStart => BorderInlineStartColor,
        LogicalEdge.InlineEnd => BorderInlineEndColor,
        LogicalEdge.BlockStart => BorderBlockStartColor,
        _ => BorderBlockEndColor,
    };

    private void SetBorderColorEdge(LogicalEdge edge, Color value)
    {
        switch (edge)
        {
            case LogicalEdge.InlineStart: BorderInlineStartColor = value; break;
            case LogicalEdge.InlineEnd: BorderInlineEndColor = value; break;
            case LogicalEdge.BlockStart: BorderBlockStartColor = value; break;
            default: BorderBlockEndColor = value; break;
        }
    }

    private BorderStyle GetBorderStyleEdge(LogicalEdge edge) => edge switch
    {
        LogicalEdge.InlineStart => BorderInlineStartStyle,
        LogicalEdge.InlineEnd => BorderInlineEndStyle,
        LogicalEdge.BlockStart => BorderBlockStartStyle,
        _ => BorderBlockEndStyle,
    };

    private void SetBorderStyleEdge(LogicalEdge edge, BorderStyle value)
    {
        switch (edge)
        {
            case LogicalEdge.InlineStart: BorderInlineStartStyle = value; break;
            case LogicalEdge.InlineEnd: BorderInlineEndStyle = value; break;
            case LogicalEdge.BlockStart: BorderBlockStartStyle = value; break;
            default: BorderBlockEndStyle = value; break;
        }
    }

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
    // 定位偏移 Top/Right/Bottom/Left 为逻辑 inset 存储的物理门面，已在上方与尺寸/边距一并定义。

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
    public new AlignContent AlignContent { get; set; } = Common.AlignContent.Normal;
    // Grid inline 轴对齐（CSS 初始值：justify-items normal → 表现为 stretch；justify-self auto → 回退容器）。
    public new JustifyItems JustifyItems { get; set; } = Common.JustifyItems.Normal;
    public new JustifySelf JustifySelf { get; set; } = Common.JustifySelf.Auto;
    public new int Order { get; set; } = 0;

    // Gap 默认 0；RowGap/ColumnGap 默认 Auto，表示"未单独设置"，回退到 Gap。
    public new Length Gap { get; set; } = Length.Px(0);
    public new Length RowGap { get; set; } = Length.Auto;
    public new Length ColumnGap { get; set; } = Length.Auto;

    // Grid 容器属性（见 ISSUE-097）。模板列表默认 null = 无显式模板；
    // 隐式轨道默认 auto；放置线默认 0 = 自动放置。
    public new List<GridTrackSize>? GridTemplateColumns { get; set; }
    public new List<GridTrackSize>? GridTemplateRows { get; set; }
    public new GridTrackSize GridAutoRows { get; set; } = GridTrackSize.Auto;
    public new GridTrackSize GridAutoColumns { get; set; } = GridTrackSize.Auto;
    public new int GridColumnStart { get; set; }
    public new int GridColumnEnd { get; set; }
    public new int GridRowStart { get; set; }
    public new int GridRowEnd { get; set; }

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

    // Transitions / Animations 默认懒初始化（ISSUE-096）：绝大多数元素没有动画，
    // 不为它们支付每次样式解析两个 List 的分配。读取方应使用 *OrNull 判空。
    private List<Transition>? _transitions;
    private List<KeyframeAnimation>? _animations;

    public new List<Transition> Transitions { get => _transitions ??= new(); set => _transitions = value; }
    public new List<KeyframeAnimation> Animations { get => _animations ??= new(); set => _animations = value; }

    /// <summary>已定义的过渡列表（无过渡时为 null，热路径读取请用此属性避免懒分配）。</summary>
    internal List<Transition>? TransitionsOrNull => _transitions;

    /// <summary>已定义的动画列表（无动画时为 null，热路径读取请用此属性避免懒分配）。</summary>
    internal List<KeyframeAnimation>? AnimationsOrNull => _animations;

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

        // calc 表达式：对当前变量作用域求值；引用的变量未解析时返回 false（保留默认值，
        // 与未解析的 Var(...) 行为一致）。
        if (property.IsCalc)
            return property.TryEvaluateCalc(Vars, out value);

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
    /// <param name="viewport">
    /// 视口尺寸。用于折算 font-size 中的 vw/vh 视窗单位（须先于其 em 解析）。
    /// null 时 font-size 的视窗分量按 0 计（缺少视口上下文）。其余属性的 vw/vh 由
    /// <see cref="ResolveViewport"/> 在样式计算阶段统一折算。
    /// </param>
    public static ComputedStyle FromStyle(Style? style, float? parentFontSizePx = null,
        Dictionary<string, VarValue>? varScope = null, ComputedStyle? parent = null,
        ViewportInfo? viewport = null)
    {
        var computed = new ComputedStyle();
        // 先设作用域与父上下文：ApplyStylePropertiesGenerated 与下方特例都会经
        // TryResolveStyleProperty 读取它们来解析变量引用与 inherit/unset 关键词。
        computed.Vars = varScope;
        computed._keywordResolutionParent = parent;

        if (style != null)
        {
            // direction / writing-mode 必须先于其余属性解析：物理盒属性（MarginLeft、Width 等）
            // 是按书写模式映射到逻辑存储的门面，后续生成的赋值代码经门面读写，依赖这两个值（见 ISSUE-103）。
            if (style.Direction is { } directionProp &&
                computed.TryResolveStyleProperty(directionProp, parent?.Direction ?? default, inheritable: true, out var direction))
                computed.Direction = direction;
            if (style.WritingMode is { } writingModeProp &&
                computed.TryResolveStyleProperty(writingModeProp, parent?.WritingMode ?? default, inheritable: true, out var writingMode))
                computed.WritingMode = writingMode;

            // 调用生成的通用属性赋值（内部对每个属性解析变量引用与全局关键词）
            computed.ApplyStylePropertiesGenerated(style);

            // 特殊处理：font-size 的 em 单位需要相对父元素解析。font-size 可继承。
            if (style.FontSize is { } fontSizeProp &&
                computed.TryResolveStyleProperty(fontSizeProp, parent?.FontSize ?? default, inheritable: true, out var fontSize))
            {
                // vw/vh 相对视口解析，须先于 em（否则视窗分量会被 ToPixels 按 0 折算而丢失）。
                if (viewport != null)
                    fontSize = fontSize.ResolveViewport(viewport.Width, viewport.Height);
                computed.FontSize = Length.Px(fontSize.ToPixels(0, parentFontSizePx));
            }

            // 特殊处理：边框宽度/颜色/样式的统一属性回退逻辑。边框类简写不可继承。
            // 回退逐逻辑边进行：该逻辑边的逻辑槽位与对应物理槽位（按书写模式映射）均未设置时，
            // 才用统一值填充该逻辑边（物理边门面与逻辑槽位最终落在同一存储上）。
            var wm = computed.WritingMode;
            var dir = computed.Direction;
            if (style.BorderWidth is { } borderWidthProp &&
                computed.TryResolveStyleProperty(borderWidthProp, parent?.BorderTopWidth ?? default, inheritable: false, out var borderWidth))
            {
                if (style.BorderInlineStartWidth == null && GetPhysicalBorderWidth(style, LogicalEdgeMap.ToPhysical(LogicalEdge.InlineStart, wm, dir)) == null)
                    computed.BorderInlineStartWidth = borderWidth;
                if (style.BorderInlineEndWidth == null && GetPhysicalBorderWidth(style, LogicalEdgeMap.ToPhysical(LogicalEdge.InlineEnd, wm, dir)) == null)
                    computed.BorderInlineEndWidth = borderWidth;
                if (style.BorderBlockStartWidth == null && GetPhysicalBorderWidth(style, LogicalEdgeMap.ToPhysical(LogicalEdge.BlockStart, wm, dir)) == null)
                    computed.BorderBlockStartWidth = borderWidth;
                if (style.BorderBlockEndWidth == null && GetPhysicalBorderWidth(style, LogicalEdgeMap.ToPhysical(LogicalEdge.BlockEnd, wm, dir)) == null)
                    computed.BorderBlockEndWidth = borderWidth;
            }

            // 特殊处理：边框颜色的统一属性回退逻辑
            if (style.BorderColor is { } borderColorProp &&
                computed.TryResolveStyleProperty(borderColorProp, parent?.BorderTopColor ?? default, inheritable: false, out var borderColor))
            {
                if (style.BorderInlineStartColor == null && GetPhysicalBorderColor(style, LogicalEdgeMap.ToPhysical(LogicalEdge.InlineStart, wm, dir)) == null)
                    computed.BorderInlineStartColor = borderColor;
                if (style.BorderInlineEndColor == null && GetPhysicalBorderColor(style, LogicalEdgeMap.ToPhysical(LogicalEdge.InlineEnd, wm, dir)) == null)
                    computed.BorderInlineEndColor = borderColor;
                if (style.BorderBlockStartColor == null && GetPhysicalBorderColor(style, LogicalEdgeMap.ToPhysical(LogicalEdge.BlockStart, wm, dir)) == null)
                    computed.BorderBlockStartColor = borderColor;
                if (style.BorderBlockEndColor == null && GetPhysicalBorderColor(style, LogicalEdgeMap.ToPhysical(LogicalEdge.BlockEnd, wm, dir)) == null)
                    computed.BorderBlockEndColor = borderColor;
            }

            // 特殊处理：边框样式的统一属性回退逻辑
            if (style.BorderStyle is { } borderStyleProp &&
                computed.TryResolveStyleProperty(borderStyleProp, parent?.BorderTopStyle ?? default, inheritable: false, out var borderStyle))
            {
                if (style.BorderInlineStartStyle == null && GetPhysicalBorderStyle(style, LogicalEdgeMap.ToPhysical(LogicalEdge.InlineStart, wm, dir)) == null)
                    computed.BorderInlineStartStyle = borderStyle;
                if (style.BorderInlineEndStyle == null && GetPhysicalBorderStyle(style, LogicalEdgeMap.ToPhysical(LogicalEdge.InlineEnd, wm, dir)) == null)
                    computed.BorderInlineEndStyle = borderStyle;
                if (style.BorderBlockStartStyle == null && GetPhysicalBorderStyle(style, LogicalEdgeMap.ToPhysical(LogicalEdge.BlockStart, wm, dir)) == null)
                    computed.BorderBlockStartStyle = borderStyle;
                if (style.BorderBlockEndStyle == null && GetPhysicalBorderStyle(style, LogicalEdgeMap.ToPhysical(LogicalEdge.BlockEnd, wm, dir)) == null)
                    computed.BorderBlockEndStyle = borderStyle;
            }
        }

        return computed;
    }

    /// <summary>读取 <paramref name="style"/> 中某物理边的边框宽度槽位（统一边框回退的判空用）。</summary>
    private static StyleProperty<Length>? GetPhysicalBorderWidth(Style style, PhysicalEdge edge) => edge switch
    {
        PhysicalEdge.Top => style.BorderTopWidth,
        PhysicalEdge.Right => style.BorderRightWidth,
        PhysicalEdge.Bottom => style.BorderBottomWidth,
        _ => style.BorderLeftWidth,
    };

    /// <summary>读取 <paramref name="style"/> 中某物理边的边框颜色槽位。</summary>
    private static StyleProperty<Color>? GetPhysicalBorderColor(Style style, PhysicalEdge edge) => edge switch
    {
        PhysicalEdge.Top => style.BorderTopColor,
        PhysicalEdge.Right => style.BorderRightColor,
        PhysicalEdge.Bottom => style.BorderBottomColor,
        _ => style.BorderLeftColor,
    };

    /// <summary>读取 <paramref name="style"/> 中某物理边的边框样式槽位。</summary>
    private static StyleProperty<BorderStyle>? GetPhysicalBorderStyle(Style style, PhysicalEdge edge) => edge switch
    {
        PhysicalEdge.Top => style.BorderTopStyle,
        PhysicalEdge.Right => style.BorderRightStyle,
        PhysicalEdge.Bottom => style.BorderBottomStyle,
        _ => style.BorderLeftStyle,
    };

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

        // 物理属性是逻辑存储的门面（ISSUE-103）：四边/两轴的物理枚举与逻辑存储一一对应，
        // 因此逐物理属性读写即可覆盖全部逻辑槽位，无需（也不能重复）再列逻辑属性。
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

        // Grid 轨道列表中的固定长度同样需要折算 env() 分量（见 ISSUE-097）。
        GridTemplateColumns = ResolveTracksSafeArea(GridTemplateColumns, insets);
        GridTemplateRows = ResolveTracksSafeArea(GridTemplateRows, insets);
        GridAutoRows = ResolveTrackSafeArea(GridAutoRows, insets);
        GridAutoColumns = ResolveTrackSafeArea(GridAutoColumns, insets);
    }

    /// <summary>折算轨道列表中各固定长度轨道的 env() 分量；无安全区分量时原样返回（不分配新列表）。</summary>
    private static List<GridTrackSize>? ResolveTracksSafeArea(List<GridTrackSize>? tracks, SafeAreaInsets insets)
    {
        if (tracks == null) return null;
        List<GridTrackSize>? resolved = null;
        for (int i = 0; i < tracks.Count; i++)
        {
            var t = tracks[i];
            if (!t.IsFixed || !t.Fixed.HasSafeAreaComponent) continue;
            resolved ??= new List<GridTrackSize>(tracks);
            resolved[i] = GridTrackSize.FromLength(t.Fixed.ResolveSafeArea(insets));
        }
        return resolved ?? tracks;
    }

    private static GridTrackSize ResolveTrackSafeArea(GridTrackSize track, SafeAreaInsets insets)
        => track.IsFixed && track.Fixed.HasSafeAreaComponent
            ? GridTrackSize.FromLength(track.Fixed.ResolveSafeArea(insets))
            : track;

    /// <summary>
    /// 折算所有长度属性中的视窗单位（vw/vh）分量为像素。由布局引擎在样式计算阶段、
    /// 已知视口尺寸时调用。仅影响显式使用了 vw/vh 的属性（其余长度原样返回），
    /// 因此对未使用视窗单位的元素完全无副作用。
    /// <para>
    /// vw/vh 始终相对整个视口解析（1vw = 视口宽度的 1%、1vh = 视口高度的 1%），
    /// 与包含块尺寸无关；因此可与 calc 复合长度共存（如 <c>calc(100vw - 240px)</c>）。
    /// font-size 中的 vw/vh 由 <see cref="FromStyle"/> 单独折算（须先于其 em 解析），此处不再处理。
    /// </para>
    /// </summary>
    public void ResolveViewport(ViewportInfo viewport)
    {
        float vw = viewport.Width;
        float vh = viewport.Height;

        // 与 ResolveSafeArea 同理：物理门面枚举已覆盖全部逻辑存储（ISSUE-103）。

        PaddingTop = PaddingTop.ResolveViewport(vw, vh);
        PaddingRight = PaddingRight.ResolveViewport(vw, vh);
        PaddingBottom = PaddingBottom.ResolveViewport(vw, vh);
        PaddingLeft = PaddingLeft.ResolveViewport(vw, vh);

        MarginTop = MarginTop.ResolveViewport(vw, vh);
        MarginRight = MarginRight.ResolveViewport(vw, vh);
        MarginBottom = MarginBottom.ResolveViewport(vw, vh);
        MarginLeft = MarginLeft.ResolveViewport(vw, vh);

        Top = Top.ResolveViewport(vw, vh);
        Right = Right.ResolveViewport(vw, vh);
        Bottom = Bottom.ResolveViewport(vw, vh);
        Left = Left.ResolveViewport(vw, vh);

        Width = Width.ResolveViewport(vw, vh);
        Height = Height.ResolveViewport(vw, vh);
        MinWidth = MinWidth.ResolveViewport(vw, vh);
        MinHeight = MinHeight.ResolveViewport(vw, vh);
        MaxWidth = MaxWidth.ResolveViewport(vw, vh);
        MaxHeight = MaxHeight.ResolveViewport(vw, vh);
        FlexBasis = FlexBasis.ResolveViewport(vw, vh);

        BorderTopWidth = BorderTopWidth.ResolveViewport(vw, vh);
        BorderRightWidth = BorderRightWidth.ResolveViewport(vw, vh);
        BorderBottomWidth = BorderBottomWidth.ResolveViewport(vw, vh);
        BorderLeftWidth = BorderLeftWidth.ResolveViewport(vw, vh);

        BorderTopLeftRadius = BorderTopLeftRadius.ResolveViewport(vw, vh);
        BorderTopRightRadius = BorderTopRightRadius.ResolveViewport(vw, vh);
        BorderBottomRightRadius = BorderBottomRightRadius.ResolveViewport(vw, vh);
        BorderBottomLeftRadius = BorderBottomLeftRadius.ResolveViewport(vw, vh);

        OutlineWidth = OutlineWidth.ResolveViewport(vw, vh);
        OutlineOffset = OutlineOffset.ResolveViewport(vw, vh);

        LineHeight = LineHeight.ResolveViewport(vw, vh);
        LetterSpacing = LetterSpacing.ResolveViewport(vw, vh);

        Gap = Gap.ResolveViewport(vw, vh);
        RowGap = RowGap.ResolveViewport(vw, vh);
        ColumnGap = ColumnGap.ResolveViewport(vw, vh);

        // Grid 轨道列表中的固定长度同样需要折算 vw/vh 分量（见 ISSUE-097）。
        GridTemplateColumns = ResolveTracksViewport(GridTemplateColumns, vw, vh);
        GridTemplateRows = ResolveTracksViewport(GridTemplateRows, vw, vh);
        GridAutoRows = ResolveTrackViewport(GridAutoRows, vw, vh);
        GridAutoColumns = ResolveTrackViewport(GridAutoColumns, vw, vh);
    }

    /// <summary>折算轨道列表中各固定长度轨道的 vw/vh 分量；无视窗分量时原样返回（不分配新列表）。</summary>
    private static List<GridTrackSize>? ResolveTracksViewport(List<GridTrackSize>? tracks, float vw, float vh)
    {
        if (tracks == null) return null;
        List<GridTrackSize>? resolved = null;
        for (int i = 0; i < tracks.Count; i++)
        {
            var t = tracks[i];
            if (!t.IsFixed || !t.Fixed.HasViewportComponent) continue;
            resolved ??= new List<GridTrackSize>(tracks);
            resolved[i] = GridTrackSize.FromLength(t.Fixed.ResolveViewport(vw, vh));
        }
        return resolved ?? tracks;
    }

    private static GridTrackSize ResolveTrackViewport(GridTrackSize track, float vw, float vh)
        => track.IsFixed && track.Fixed.HasViewportComponent
            ? GridTrackSize.FromLength(track.Fixed.ResolveViewport(vw, vh))
            : track;
}
