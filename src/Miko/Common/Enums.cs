namespace Miko.Common;

/// <summary>
/// Display 显示类型
/// </summary>
public enum Display
{
    Block,
    Inline,
    InlineBlock,
    Flex,
    None,
    Table,
    TableRow,
    TableCell
}

/// <summary>
/// Position 定位方式
/// </summary>
public enum Position
{
    Static,
    Relative,
    Absolute,
    Fixed
}

/// <summary>
/// Flex 方向
/// </summary>
public enum FlexDirection
{
    Row,
    RowReverse,
    Column,
    ColumnReverse
}

/// <summary>
/// 主轴对齐方式
/// </summary>
public enum JustifyContent
{
    FlexStart,
    FlexEnd,
    Center,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly
}

/// <summary>
/// 交叉轴对齐方式
/// </summary>
public enum AlignItems
{
    FlexStart,
    FlexEnd,
    Center,
    Stretch,
    Baseline
}

/// <summary>
/// 边框样式
/// </summary>
public enum BorderStyle
{
    None,
    Solid,
    Dotted,
    Dashed,
    Double
}

/// <summary>
/// 字体粗细
/// </summary>
public enum FontWeight
{
    Thin = 100,
    ExtraLight = 200,
    Light = 300,
    Normal = 400,
    Medium = 500,
    SemiBold = 600,
    Bold = 700,
    ExtraBold = 800,
    Black = 900,
    Lighter = 300,
    Bolder = 900
}

/// <summary>
/// 文本对齐
/// </summary>
public enum TextAlign
{
    Left,
    Right,
    Center,
    Justify
}

/// <summary>
/// 布局类型
/// </summary>
public enum LayoutType
{
    Block,
    Inline,
    InlineBlock,
    Flex,
    FlexItem,
    Anonymous,
    Table,
    TableRow,
    TableCell
}

/// <summary>
/// 长度单位
/// </summary>
public enum LengthUnit
{
    Px,
    Percent,
    Auto,
    Rem,
    Em,
    /// <summary>无单位数值。用于无单位 line-height：实际像素 = 该系数 × 字体大小。</summary>
    Number
}

/// <summary>
/// 输入框类型
/// </summary>
public enum InputType
{
    Text,
    Password,
    Checkbox,
    Radio,
    Range,
    Search
}

/// <summary>
/// 溢出处理方式
/// </summary>
public enum Overflow
{
    Visible,
    Hidden,
    Scroll,
    Auto
}

/// <summary>
/// 文本装饰
/// </summary>
public enum TextDecoration
{
    None,
    Underline,
    Overline,
    LineThrough
}

public enum TextTransform { None, Uppercase, Lowercase, Capitalize }
public enum FontStyle { Normal, Italic, Oblique }
public enum WhiteSpace { Normal, Nowrap, Pre, PreWrap, PreLine }
public enum Cursor { Default, Pointer, Text, Wait, NotAllowed, Move, Help }
public enum Visibility { Visible, Hidden, Collapse }
/// <summary>
/// Whether an element is a hit-test target. <see cref="None"/> makes the element transparent
/// to pointer hits (taps pass through to whatever is behind it), while its descendants remain
/// hittable. Inherited, like CSS <c>pointer-events</c>.
/// </summary>
public enum PointerEvents { Auto, None }
public enum FlexWrap { Nowrap, Wrap, WrapReverse }
public enum AlignSelf { Auto, FlexStart, FlexEnd, Center, Stretch, Baseline }
public enum AlignContent { FlexStart, FlexEnd, Center, SpaceBetween, SpaceAround, Stretch }
public enum UserSelect { Auto, None, Text, All }
public enum VerticalAlign { Baseline, Top, Middle, Bottom, TextTop, TextBottom }
public enum BoxSizing { ContentBox, BorderBox }
public enum BackgroundRepeat { Repeat, RepeatX, RepeatY, NoRepeat }
public enum BackgroundSizeMode { Auto, Cover, Contain, Explicit }
public enum BackgroundPosition { LeftTop, CenterTop, RightTop, LeftCenter, Center, RightCenter, LeftBottom, CenterBottom, RightBottom }

/// <summary>
/// 表格布局算法（CSS table-layout 属性）
/// </summary>
public enum TableLayoutAlgorithm
{
    /// <summary>自动布局（默认）：根据内容计算列宽。</summary>
    Auto,
    /// <summary>固定布局：根据表格首行/colgroup 平均分配列宽，性能更好。</summary>
    Fixed
}
