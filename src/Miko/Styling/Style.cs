using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Styling.Selectors;
using System.Collections.Concurrent;

namespace Miko.Styling;

/// <summary>
/// 样式对象
/// </summary>
public partial class Style
{
    // 布局属性
    public StyleProperty<Display>? Display { get; set; }
    public StyleProperty<FlexDirection>? FlexDirection { get; set; }
    public StyleProperty<JustifyContent>? JustifyContent { get; set; }
    public StyleProperty<AlignItems>? AlignItems { get; set; }

    // Flex 子元素属性
    public StyleProperty<float>? FlexGrow { get; set; }
    public StyleProperty<float>? FlexShrink { get; set; }
    public StyleProperty<Length>? FlexBasis { get; set; }

    /// <summary>
    /// flex 简写属性，展开为 flex-grow / flex-shrink / flex-basis。
    /// 支持 <see cref="Flex"/> 的多种写法，并保留 <c>Flex = 1</c> 的单数值兼容
    /// （通过 <see cref="Flex"/> 的隐式转换，等价于 <c>N 1 0%</c>）。
    /// </summary>
    public Flex Flex
    {
        set
        {
            FlexGrow = value.Grow;
            FlexShrink = value.Shrink;
            FlexBasis = value.Basis;
        }
    }

    // 盒子模型
    public StyleProperty<BoxSizing>? BoxSizing { get; set; }
    public StyleProperty<Length>? Width { get; set; }
    public StyleProperty<Length>? Height { get; set; }
    public StyleProperty<Length>? MinWidth { get; set; }
    public StyleProperty<Length>? MinHeight { get; set; }
    public StyleProperty<Length>? MaxWidth { get; set; }
    public StyleProperty<Length>? MaxHeight { get; set; }

    // 内边
    public StyleProperty<Length>? PaddingTop { get; set; }
    public StyleProperty<Length>? PaddingRight { get; set; }
    public StyleProperty<Length>? PaddingBottom { get; set; }
    public StyleProperty<Length>? PaddingLeft { get; set; }

    /// <summary>
    /// 内边距简写属性
    /// </summary>
    public Padding Padding
    {
        get => new Padding(
            PaddingTop.ValueOrNull() ?? Length.Px(0),
            PaddingRight.ValueOrNull() ?? Length.Px(0),
            PaddingBottom.ValueOrNull() ?? Length.Px(0),
            PaddingLeft.ValueOrNull() ?? Length.Px(0));
        set
        {
            PaddingTop = value.Top;
            PaddingRight = value.Right;
            PaddingBottom = value.Bottom;
            PaddingLeft = value.Left;
        }
    }

    // 外边距
    public StyleProperty<Length>? MarginTop { get; set; }
    public StyleProperty<Length>? MarginRight { get; set; }
    public StyleProperty<Length>? MarginBottom { get; set; }
    public StyleProperty<Length>? MarginLeft { get; set; }

    /// <summary>
    /// 外边距简写属性
    /// </summary>
    public Margin Margin
    {
        get => new Margin(
            MarginTop.ValueOrNull() ?? Length.Px(0),
            MarginRight.ValueOrNull() ?? Length.Px(0),
            MarginBottom.ValueOrNull() ?? Length.Px(0),
            MarginLeft.ValueOrNull() ?? Length.Px(0));
        set
        {
            MarginTop = value.Top;
            MarginRight = value.Right;
            MarginBottom = value.Bottom;
            MarginLeft = value.Left;
        }
    }

    // 边框（统一属性，作为各边的后备值）
    public StyleProperty<Length>? BorderWidth { get; set; }
    public StyleProperty<Color>? BorderColor { get; set; }
    public StyleProperty<BorderStyle>? BorderStyle { get; set; }

    // 边框宽度（每边单独设置）
    public StyleProperty<Length>? BorderTopWidth { get; set; }
    public StyleProperty<Length>? BorderRightWidth { get; set; }
    public StyleProperty<Length>? BorderBottomWidth { get; set; }
    public StyleProperty<Length>? BorderLeftWidth { get; set; }

    // 边框颜色（每边单独设置）
    public StyleProperty<Color>? BorderTopColor { get; set; }
    public StyleProperty<Color>? BorderRightColor { get; set; }
    public StyleProperty<Color>? BorderBottomColor { get; set; }
    public StyleProperty<Color>? BorderLeftColor { get; set; }

    // 边框样式（每边单独设置）
    public StyleProperty<BorderStyle>? BorderTopStyle { get; set; }
    public StyleProperty<BorderStyle>? BorderRightStyle { get; set; }
    public StyleProperty<BorderStyle>? BorderBottomStyle { get; set; }
    public StyleProperty<BorderStyle>? BorderLeftStyle { get; set; }

    /// <summary>
    /// 边框简写属性（设置所有边）
    /// </summary>
    public Border Border
    {
        get => new Border(
            BorderWidth.ValueOrNull() ?? Length.Px(0),
            BorderStyle.ValueOrNull() ?? Common.BorderStyle.None,
            BorderColor.ValueOrNull() ?? Common.Color.Transparent);
        set
        {
            BorderWidth = value.Width;
            BorderColor = value.Color;
            BorderStyle = value.Style;
            BorderTopWidth = BorderRightWidth = BorderBottomWidth = BorderLeftWidth = null;
            BorderTopColor = BorderRightColor = BorderBottomColor = BorderLeftColor = null;
            BorderTopStyle = BorderRightStyle = BorderBottomStyle = BorderLeftStyle = null;
        }
    }

    /// <summary>
    /// 上边框简写属性
    /// </summary>
    public BorderSide BorderTop
    {
        get => new BorderSide(
            BorderTopWidth.ValueOrNull() ?? BorderWidth.ValueOrNull() ?? Length.Px(0),
            BorderTopStyle.ValueOrNull() ?? BorderStyle.ValueOrNull() ?? Common.BorderStyle.None,
            BorderTopColor.ValueOrNull() ?? BorderColor.ValueOrNull() ?? Common.Color.Transparent);
        set
        {
            BorderTopWidth = value.Width;
            BorderTopStyle = value.Style;
            BorderTopColor = value.Color;
        }
    }

    /// <summary>
    /// 右边框简写属性
    /// </summary>
    public BorderSide BorderRight
    {
        get => new BorderSide(
            BorderRightWidth.ValueOrNull() ?? BorderWidth.ValueOrNull() ?? Length.Px(0),
            BorderRightStyle.ValueOrNull() ?? BorderStyle.ValueOrNull() ?? Common.BorderStyle.None,
            BorderRightColor.ValueOrNull() ?? BorderColor.ValueOrNull() ?? Common.Color.Transparent);
        set
        {
            BorderRightWidth = value.Width;
            BorderRightStyle = value.Style;
            BorderRightColor = value.Color;
        }
    }

    /// <summary>
    /// 下边框简写属性
    /// </summary>
    public BorderSide BorderBottom
    {
        get => new BorderSide(
            BorderBottomWidth.ValueOrNull() ?? BorderWidth.ValueOrNull() ?? Length.Px(0),
            BorderBottomStyle.ValueOrNull() ?? BorderStyle.ValueOrNull() ?? Common.BorderStyle.None,
            BorderBottomColor.ValueOrNull() ?? BorderColor.ValueOrNull() ?? Common.Color.Transparent);
        set
        {
            BorderBottomWidth = value.Width;
            BorderBottomStyle = value.Style;
            BorderBottomColor = value.Color;
        }
    }

    /// <summary>
    /// 左边框简写属性
    /// </summary>
    public BorderSide BorderLeft
    {
        get => new BorderSide(
            BorderLeftWidth.ValueOrNull() ?? BorderWidth.ValueOrNull() ?? Length.Px(0),
            BorderLeftStyle.ValueOrNull() ?? BorderStyle.ValueOrNull() ?? Common.BorderStyle.None,
            BorderLeftColor.ValueOrNull() ?? BorderColor.ValueOrNull() ?? Common.Color.Transparent);
        set
        {
            BorderLeftWidth = value.Width;
            BorderLeftStyle = value.Style;
            BorderLeftColor = value.Color;
        }
    }

    // 轮廓（outline）：绘制在边框盒之外，不占据布局空间。复用 BorderStyle 表示线型。
    public StyleProperty<Length>? OutlineWidth { get; set; }
    public StyleProperty<Color>? OutlineColor { get; set; }
    public StyleProperty<BorderStyle>? OutlineStyle { get; set; }
    public StyleProperty<Length>? OutlineOffset { get; set; }

    /// <summary>
    /// 轮廓简写属性。设置宽度/线型/颜色（不含 outline-offset，需单独设置）。
    /// </summary>
    public Outline Outline
    {
        get => new Outline(
            OutlineWidth.ValueOrNull() ?? Length.Px(0),
            OutlineStyle.ValueOrNull() ?? Common.BorderStyle.None,
            OutlineColor.ValueOrNull() ?? Common.Color.Transparent);
        set
        {
            OutlineWidth = value.Width;
            OutlineStyle = value.Style;
            OutlineColor = value.Color;
        }
    }

    // 圆角
    public StyleProperty<Length>? BorderTopLeftRadius { get; set; }
    public StyleProperty<Length>? BorderTopRightRadius { get; set; }
    public StyleProperty<Length>? BorderBottomRightRadius { get; set; }
    public StyleProperty<Length>? BorderBottomLeftRadius { get; set; }

    public BorderRadius BorderRadius
    {
        get => new BorderRadius(
            BorderTopLeftRadius.ValueOrNull() ?? 0,
            BorderTopRightRadius.ValueOrNull() ?? 0,
            BorderBottomLeftRadius.ValueOrNull() ?? 0,
            BorderBottomRightRadius.ValueOrNull() ?? 0);
        set
        {
            BorderTopLeftRadius = value.TopLeft;
            BorderTopRightRadius = value.TopRight;
            BorderBottomLeftRadius = value.BottomLeft;
            BorderBottomRightRadius = value.BottomRight;
        }
    }

    // 视觉属性
    public StyleProperty<Color>? BackgroundColor { get; set; }
    public StyleProperty<BackgroundImage>? BackgroundImage { get; set; }
    public StyleProperty<BackgroundRepeat>? BackgroundRepeat { get; set; }
    public StyleProperty<BackgroundSize>? BackgroundSize { get; set; }
    public StyleProperty<BackgroundPosition>? BackgroundPosition { get; set; }
    public StyleProperty<Color>? Color { get; set; }
    public StyleProperty<string>? FontFamily { get; set; }
    public StyleProperty<Length>? FontSize { get; set; }
    public StyleProperty<FontWeight>? FontWeight { get; set; }
    public StyleProperty<TextAlign>? TextAlign { get; set; }
    public StyleProperty<Length>? LineHeight { get; set; }

    // 定位
    public StyleProperty<Position>? Position { get; set; }
    public StyleProperty<Length>? Top { get; set; }
    public StyleProperty<Length>? Right { get; set; }
    public StyleProperty<Length>? Bottom { get; set; }
    public StyleProperty<Length>? Left { get; set; }

    public StyleProperty<TextDecoration>? TextDecoration { get; set; }
    public StyleProperty<TextTransform>? TextTransform { get; set; }
    public StyleProperty<FontStyle>? FontStyle { get; set; }
    public StyleProperty<WhiteSpace>? WhiteSpace { get; set; }
    public StyleProperty<Length>? LetterSpacing { get; set; }
    public StyleProperty<VerticalAlign>? VerticalAlign { get; set; }

    // 文本换行与溢出
    public StyleProperty<OverflowWrap>? OverflowWrap { get; set; }
    public StyleProperty<WordBreak>? WordBreak { get; set; }
    public StyleProperty<TextOverflow>? TextOverflow { get; set; }

    // 表格布局算法（仅对 Display.Table 生效）
    public StyleProperty<TableLayoutAlgorithm>? TableLayout { get; set; }

    public StyleProperty<float>? Opacity { get; set; }
    public StyleProperty<int>? ZIndex { get; set; }
    public StyleProperty<Visibility>? Visibility { get; set; }
    public StyleProperty<Cursor>? Cursor { get; set; }
    public StyleProperty<PointerEvents>? PointerEvents { get; set; }
    public StyleProperty<UserSelect>? UserSelect { get; set; }

    // Flex extras
    public StyleProperty<FlexWrap>? FlexWrap { get; set; }
    public StyleProperty<AlignSelf>? AlignSelf { get; set; }
    public StyleProperty<AlignContent>? AlignContent { get; set; }
    public StyleProperty<int>? Order { get; set; }
    public StyleProperty<Length>? Gap { get; set; }
    public StyleProperty<Length>? RowGap { get; set; }
    public StyleProperty<Length>? ColumnGap { get; set; }

    /// <summary>
    /// Box shadow layers. Multiple shadows are applied in order (first shadow is on top).
    /// Matches CSS box-shadow which supports comma-separated shadow definitions.
    /// </summary>
    public StyleProperty<List<BoxShadow>>? BoxShadow { get; set; }

    // 溢出
    public StyleProperty<Overflow>? OverflowX { get; set; }
    public StyleProperty<Overflow>? OverflowY { get; set; }

    // 动画与过渡
    public StyleProperty<Transform>? Transform { get; set; }
    public StyleProperty<TransformOrigin>? TransformOrigin { get; set; }
    public StyleProperty<List<Transition>>? Transitions { get; set; }
    public StyleProperty<List<KeyframeAnimation>>? Animations { get; set; }

    // 伪元素
    public StyleProperty<string>? Content { get; set; }

    /// <summary>
    /// 自定义样式变量（CSS custom properties）：本 scope 上定义的变量名 → 值。
    /// 通过 <c>Var("--name")</c> 引用，沿 DOM 树级联与继承（见 <see cref="StyleResolver"/>）。
    /// 该属性不参与源生成器的通用合并/应用逻辑，按键手动合并（见 <see cref="Merge"/>）。
    /// </summary>
    public Dictionary<string, VarValue>? Vars { get; set; }

    /// <summary>
    /// 溢出简写属性（同时设置 X 和 Y）
    /// </summary>
    public Overflow Overflow
    {
        set
        {
            OverflowX = value;
            OverflowY = value;
        }
    }

    /// <summary>
    /// 合并样式（用于级联）
    /// </summary>
    public void Merge(Style other)
    {
        MergeGenerated(other);

        // 自定义变量按键合并：已存在的键保留（this 优先，与 ??= 语义一致），
        // 缺失的键从 other 补入。
        if (other.Vars != null)
        {
            Vars ??= new Dictionary<string, VarValue>();
            foreach (var kv in other.Vars)
            {
                if (!Vars.ContainsKey(kv.Key))
                    Vars[kv.Key] = kv.Value;
            }
        }
    }

    /// <summary>
    /// 由 Source Generator 实现的合并逻辑
    /// </summary>
    partial void MergeGenerated(Style other);

    /// <summary>
    /// 由 Source Generator 实现的属性清空逻辑
    /// </summary>
    partial void ResetGenerated();

    /// <summary>
    /// 清空所有属性与自定义变量，使实例回到初始状态。
    /// 供 <see cref="StyleResolver"/> 在每次样式解析后回收池化实例，避免每个元素
    /// 每次布局都新建 <see cref="Style"/>（见 ISSUE-096）。调用后不得再读取旧内容。
    /// </summary>
    public void Reset()
    {
        ResetGenerated();
        Vars = null;
    }

    /// <summary>
    /// 是否包含任意已设置的属性或自定义变量定义。用于判断一条规则是否需要产出
    /// （仅含 <see cref="Vars"/> 定义、无任何普通属性的规则也应产出）。
    /// </summary>
    public bool HasAnyPropertyOrVars() => HasAnyProperty() || (Vars?.Count > 0);

    /// <summary>
    /// 克隆样式
    /// </summary>
    public Style Clone()
    {
        var clone = (Style)MemberwiseClone();
        // MemberwiseClone 为浅拷贝：Vars 字典会被引用共享。变量在样式解析期被按键
        // 合并/读写，若共享会导致不同规则相互污染，故深拷贝。
        if (Vars != null)
            clone.Vars = new Dictionary<string, VarValue>(Vars);
        return clone;
    }

    private static readonly ConcurrentDictionary<Type, string> _tagNameCache = new();

    public static TypedStyleBuilder<T> For<T>() where T : Element, new()
    {
        var tagName = _tagNameCache.GetOrAdd(typeof(T), _ => new T().TagName);
        return new TypedStyleBuilder<T>(new TagSelector(tagName));
    }

    public static TypedStyleBuilder<Element> Class(string className)
        => new TypedStyleBuilder<Element>(new ClassSelector(className));

    public static TypedStyleBuilder<Element> Id(string id)
        => new TypedStyleBuilder<Element>(new IdSelector(id));

    public static CombinatorStyleBuilder<Element> Group(params Selector[] selectors)
        => new(new GroupSelector(selectors));

    /// <summary>
    /// 后代选择器 (ancestor descendant)
    /// </summary>
    public static CombinatorStyleBuilder<Element> Descendant(Selector ancestor, Selector descendant)
        => new(new DescendantSelector(ancestor, descendant));

    /// <summary>
    /// 子选择器 (parent > child)
    /// </summary>
    public static CombinatorStyleBuilder<Element> Child(Selector parent, Selector child)
        => new(new ChildSelector(parent, child));

    /// <summary>
    /// 相邻兄弟选择器 (previous + target)
    /// </summary>
    public static CombinatorStyleBuilder<Element> Adjacent(Selector previous, Selector target)
        => new(new AdjacentSiblingSelector(previous, target));

    /// <summary>
    /// 通用兄弟选择器 (previous ~ target)
    /// </summary>
    public static CombinatorStyleBuilder<Element> Sibling(Selector previous, Selector target)
        => new(new GeneralSiblingSelector(previous, target));
}
