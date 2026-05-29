using System.Collections.Concurrent;
using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Styling.Selectors;

namespace Miko.Styling;

/// <summary>
/// 样式对象
/// </summary>
public class Style
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
    public StyleProperty<BoxSizing>? BoxSizing { get; set; }
    public StyleProperty<Length>? Width { get; set; }
    public StyleProperty<Length>? Height { get; set; }
    public StyleProperty<Length>? MinWidth { get; set; }
    public StyleProperty<Length>? MinHeight { get; set; }
    public StyleProperty<Length>? MaxWidth { get; set; }
    public StyleProperty<Length>? MaxHeight { get; set; }

    // 内边距
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
            PaddingTop?.Value ?? Length.Px(0),
            PaddingRight?.Value ?? Length.Px(0),
            PaddingBottom?.Value ?? Length.Px(0),
            PaddingLeft?.Value ?? Length.Px(0));
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
            MarginTop?.Value ?? Length.Px(0),
            MarginRight?.Value ?? Length.Px(0),
            MarginBottom?.Value ?? Length.Px(0),
            MarginLeft?.Value ?? Length.Px(0));
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
            BorderWidth?.Value ?? Length.Px(0),
            BorderStyle?.Value ?? Common.BorderStyle.None,
            BorderColor?.Value ?? Common.Color.Transparent);
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
            BorderTopWidth?.Value ?? BorderWidth?.Value ?? Length.Px(0),
            BorderTopStyle?.Value ?? BorderStyle?.Value ?? Common.BorderStyle.None,
            BorderTopColor?.Value ?? BorderColor?.Value ?? Common.Color.Transparent);
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
            BorderRightWidth?.Value ?? BorderWidth?.Value ?? Length.Px(0),
            BorderRightStyle?.Value ?? BorderStyle?.Value ?? Common.BorderStyle.None,
            BorderRightColor?.Value ?? BorderColor?.Value ?? Common.Color.Transparent);
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
            BorderBottomWidth?.Value ?? BorderWidth?.Value ?? Length.Px(0),
            BorderBottomStyle?.Value ?? BorderStyle?.Value ?? Common.BorderStyle.None,
            BorderBottomColor?.Value ?? BorderColor?.Value ?? Common.Color.Transparent);
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
            BorderLeftWidth?.Value ?? BorderWidth?.Value ?? Length.Px(0),
            BorderLeftStyle?.Value ?? BorderStyle?.Value ?? Common.BorderStyle.None,
            BorderLeftColor?.Value ?? BorderColor?.Value ?? Common.Color.Transparent);
        set
        {
            BorderLeftWidth = value.Width;
            BorderLeftStyle = value.Style;
            BorderLeftColor = value.Color;
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
            BorderTopLeftRadius?.Value ?? 0,
            BorderTopRightRadius?.Value ?? 0,
            BorderBottomLeftRadius?.Value ?? 0,
            BorderBottomRightRadius?.Value ?? 0);
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
    public BackgroundImage? BackgroundImage { get; set; }
    public StyleProperty<BackgroundRepeat>? BackgroundRepeat { get; set; }
    public StyleProperty<BackgroundSize>? BackgroundSize { get; set; }
    public StyleProperty<BackgroundPosition>? BackgroundPosition { get; set; }
    public StyleProperty<Color>? Color { get; set; }
    public string? FontFamily { get; set; }
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

    public StyleProperty<float>? Opacity { get; set; }
    public StyleProperty<int>? ZIndex { get; set; }
    public StyleProperty<Visibility>? Visibility { get; set; }
    public StyleProperty<Cursor>? Cursor { get; set; }
    public StyleProperty<UserSelect>? UserSelect { get; set; }

    // Flex extras
    public StyleProperty<FlexWrap>? FlexWrap { get; set; }
    public StyleProperty<AlignSelf>? AlignSelf { get; set; }
    public StyleProperty<AlignContent>? AlignContent { get; set; }
    public StyleProperty<Length>? Gap { get; set; }
    public StyleProperty<Length>? RowGap { get; set; }
    public StyleProperty<Length>? ColumnGap { get; set; }

    public BoxShadow? BoxShadow { get; set; }

    // 溢出
    public StyleProperty<Overflow>? OverflowX { get; set; }
    public StyleProperty<Overflow>? OverflowY { get; set; }

    // 动画与过渡
    public Transform? Transform { get; set; }
    public StyleProperty<TransformOrigin>? TransformOrigin { get; set; }
    public List<Transition>? Transitions { get; set; }
    public List<KeyframeAnimation>? Animations { get; set; }

    // 伪元素
    public string? Content { get; set; }

    // 自定义属性（CSS 变量定义）
    public Dictionary<string, StyleValue>? CustomProperties { get; set; }

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

        if (other.CustomProperties != null)
        {
            CustomProperties ??= new();
            foreach (var (key, value) in other.CustomProperties)
                CustomProperties.TryAdd(key, value);
        }
    }

    /// <summary>
    /// 克隆样式
    /// </summary>
    public Style Clone()
    {
        var clone = (Style)MemberwiseClone();
        if (CustomProperties != null)
            clone.CustomProperties = new Dictionary<string, StyleValue>(CustomProperties);
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
