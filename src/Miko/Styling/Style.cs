using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Styling.Selectors;
using System.Collections.Concurrent;

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

    /// <summary>
    /// flex 简写属性 (flex: N → flex-grow: N; flex-shrink: 1; flex-basis: 0%)
    /// </summary>
    public float Flex
    {
        set
        {
            FlexGrow = value;
            FlexShrink = 1;
            FlexBasis = Length.Percent(0);
        }
    }

    // 盒子模型
    public BoxSizing? BoxSizing { get; set; }
    public Length? Width { get; set; }
    public Length? Height { get; set; }
    public Length? MinWidth { get; set; }
    public Length? MinHeight { get; set; }
    public Length? MaxWidth { get; set; }
    public Length? MaxHeight { get; set; }

    // 内边
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

    // 边框（统一属性，作为各边的后备值）
    public Length? BorderWidth { get; set; }
    public Color? BorderColor { get; set; }
    public BorderStyle? BorderStyle { get; set; }

    // 边框宽度（每边单独设置）
    public Length? BorderTopWidth { get; set; }
    public Length? BorderRightWidth { get; set; }
    public Length? BorderBottomWidth { get; set; }
    public Length? BorderLeftWidth { get; set; }

    // 边框颜色（每边单独设置）
    public Color? BorderTopColor { get; set; }
    public Color? BorderRightColor { get; set; }
    public Color? BorderBottomColor { get; set; }
    public Color? BorderLeftColor { get; set; }

    // 边框样式（每边单独设置）
    public BorderStyle? BorderTopStyle { get; set; }
    public BorderStyle? BorderRightStyle { get; set; }
    public BorderStyle? BorderBottomStyle { get; set; }
    public BorderStyle? BorderLeftStyle { get; set; }

    /// <summary>
    /// 边框简写属性（设置所有边）
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
            BorderTopWidth ?? BorderWidth ?? Length.Px(0),
            BorderTopStyle ?? BorderStyle ?? Common.BorderStyle.None,
            BorderTopColor ?? BorderColor ?? Common.Color.Transparent);
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
            BorderRightWidth ?? BorderWidth ?? Length.Px(0),
            BorderRightStyle ?? BorderStyle ?? Common.BorderStyle.None,
            BorderRightColor ?? BorderColor ?? Common.Color.Transparent);
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
            BorderBottomWidth ?? BorderWidth ?? Length.Px(0),
            BorderBottomStyle ?? BorderStyle ?? Common.BorderStyle.None,
            BorderBottomColor ?? BorderColor ?? Common.Color.Transparent);
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
            BorderLeftWidth ?? BorderWidth ?? Length.Px(0),
            BorderLeftStyle ?? BorderStyle ?? Common.BorderStyle.None,
            BorderLeftColor ?? BorderColor ?? Common.Color.Transparent);
        set
        {
            BorderLeftWidth = value.Width;
            BorderLeftStyle = value.Style;
            BorderLeftColor = value.Color;
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
    public BackgroundImage? BackgroundImage { get; set; }
    public BackgroundRepeat? BackgroundRepeat { get; set; }
    public BackgroundSize? BackgroundSize { get; set; }
    public BackgroundPosition? BackgroundPosition { get; set; }
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

    public TextDecoration? TextDecoration { get; set; }
    public TextTransform? TextTransform { get; set; }
    public FontStyle? FontStyle { get; set; }
    public WhiteSpace? WhiteSpace { get; set; }
    public Length? LetterSpacing { get; set; }
    public VerticalAlign? VerticalAlign { get; set; }

    // 表格布局算法（仅对 Display.Table 生效）
    public TableLayoutAlgorithm? TableLayout { get; set; }

    public float? Opacity { get; set; }
    public int? ZIndex { get; set; }
    public Visibility? Visibility { get; set; }
    public Cursor? Cursor { get; set; }
    public UserSelect? UserSelect { get; set; }

    // Flex extras
    public FlexWrap? FlexWrap { get; set; }
    public AlignSelf? AlignSelf { get; set; }
    public AlignContent? AlignContent { get; set; }
    public Length? Gap { get; set; }
    public Length? RowGap { get; set; }
    public Length? ColumnGap { get; set; }

    public BoxShadow? BoxShadow { get; set; }

    // 溢出
    public Overflow? OverflowX { get; set; }
    public Overflow? OverflowY { get; set; }

    // 动画与过渡
    public Transform? Transform { get; set; }
    public TransformOrigin? TransformOrigin { get; set; }
    public List<Transition>? Transitions { get; set; }
    public List<KeyframeAnimation>? Animations { get; set; }

    // 伪元素
    public string? Content { get; set; }

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
        Display ??= other.Display;
        FlexDirection ??= other.FlexDirection;
        JustifyContent ??= other.JustifyContent;
        AlignItems ??= other.AlignItems;

        FlexGrow ??= other.FlexGrow;
        FlexShrink ??= other.FlexShrink;
        FlexBasis ??= other.FlexBasis;

        BoxSizing ??= other.BoxSizing;
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

        BorderTopWidth ??= other.BorderTopWidth;
        BorderRightWidth ??= other.BorderRightWidth;
        BorderBottomWidth ??= other.BorderBottomWidth;
        BorderLeftWidth ??= other.BorderLeftWidth;

        BorderTopColor ??= other.BorderTopColor;
        BorderRightColor ??= other.BorderRightColor;
        BorderBottomColor ??= other.BorderBottomColor;
        BorderLeftColor ??= other.BorderLeftColor;

        BorderTopStyle ??= other.BorderTopStyle;
        BorderRightStyle ??= other.BorderRightStyle;
        BorderBottomStyle ??= other.BorderBottomStyle;
        BorderLeftStyle ??= other.BorderLeftStyle;

        BorderTopLeftRadius ??= other.BorderTopLeftRadius;
        BorderTopRightRadius ??= other.BorderTopRightRadius;
        BorderBottomRightRadius ??= other.BorderBottomRightRadius;
        BorderBottomLeftRadius ??= other.BorderBottomLeftRadius;

        BackgroundColor ??= other.BackgroundColor;
        BackgroundImage ??= other.BackgroundImage;
        BackgroundRepeat ??= other.BackgroundRepeat;
        BackgroundSize ??= other.BackgroundSize;
        BackgroundPosition ??= other.BackgroundPosition;
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

        TextDecoration ??= other.TextDecoration;
        TextTransform ??= other.TextTransform;
        FontStyle ??= other.FontStyle;
        WhiteSpace ??= other.WhiteSpace;
        LetterSpacing ??= other.LetterSpacing;
        VerticalAlign ??= other.VerticalAlign;
        TableLayout ??= other.TableLayout;

        Opacity ??= other.Opacity;
        ZIndex ??= other.ZIndex;
        Visibility ??= other.Visibility;
        Cursor ??= other.Cursor;
        UserSelect ??= other.UserSelect;

        FlexWrap ??= other.FlexWrap;
        AlignSelf ??= other.AlignSelf;
        AlignContent ??= other.AlignContent;
        Gap ??= other.Gap;
        RowGap ??= other.RowGap;
        ColumnGap ??= other.ColumnGap;

        BoxShadow ??= other.BoxShadow;

        OverflowX ??= other.OverflowX;
        OverflowY ??= other.OverflowY;

        Transform ??= other.Transform;
        TransformOrigin ??= other.TransformOrigin;
        Transitions ??= other.Transitions;
        Animations ??= other.Animations;

        Content ??= other.Content;
    }

    /// <summary>
    /// 克隆样式
    /// </summary>
    public Style Clone()
    {
        return (Style)MemberwiseClone();
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
