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
    TableCell,
    // 行内级 flex 容器：外层按 inline-block 参与行内流（shrink-to-fit），内层按 flex 布局子项。
    InlineFlex,
    // 块级 grid 容器：子项按显式/自动放置进网格轨道。
    Grid
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
    /// <summary>CSS 初始值 normal：不在主轴分配剩余空间，行为等同 <see cref="FlexStart"/>。</summary>
    Normal,
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
    /// <summary>CSS 初始值 normal：行为等同 <see cref="Stretch"/>。</summary>
    Normal,
    FlexStart,
    FlexEnd,
    Center,
    Stretch,
    Baseline
}

/// <summary>
/// Grid 容器内所有子项在各自 grid area 内的 inline（行内/水平）轴对齐（CSS <c>justify-items</c>）。
/// 仅作用于 grid 布局；命名沿用 <see cref="AlignItems"/> 的 <c>FlexStart</c>/<c>FlexEnd</c> 约定
/// （对应 CSS 的 <c>start</c>/<c>end</c>）。见 ISSUE-101。
/// </summary>
public enum JustifyItems
{
    /// <summary>CSS 初始值 normal：grid 子项行为等同 <see cref="Stretch"/>。</summary>
    Normal,
    FlexStart,
    FlexEnd,
    Center,
    Stretch
}

/// <summary>
/// 单个 grid 子项在自身 grid area 内的 inline 轴对齐（CSS <c>justify-self</c>），
/// 覆盖容器的 <see cref="JustifyItems"/>；<c>Auto</c> 回退到容器设置。见 ISSUE-101。
/// </summary>
public enum JustifySelf { Auto, FlexStart, FlexEnd, Center, Stretch }

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
    TableCell,
    // 匿名文本节点（TextNode）：仅测量文本外接矩形写入 Content，无 padding/border/margin。
    Text,
    // 行内级 flex 容器：外层行内级（参与行内行流），内层由 FlexLayout 布局（见 ISSUE-097）。
    InlineFlex,
    // grid 容器：由 GridLayout 布局（见 ISSUE-097）。
    Grid
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
    /// <summary>视窗宽度单位：<c>1vw</c> = 视口宽度的 1%。</summary>
    Vw,
    /// <summary>视窗高度单位：<c>1vh</c> = 视口高度的 1%。</summary>
    Vh,
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

/// <summary>
/// CSS <c>overflow-wrap</c>（旧称 <c>word-wrap</c>）：控制是否允许在单词内部断行以避免溢出。
/// </summary>
public enum OverflowWrap
{
    /// <summary>仅在正常的换行点（如空格）断行，长单词允许溢出。</summary>
    Normal,
    /// <summary>当单词整体放不下一行时，允许在其内部断行。</summary>
    BreakWord,
    /// <summary>可在任意字符间断行（软换行机会，包括收缩容器时）。</summary>
    Anywhere
}

/// <summary>
/// CSS <c>word-break</c>：控制断行时如何处理单词内部的断点。
/// </summary>
public enum WordBreak
{
    /// <summary>默认换行规则（仅在允许的断点处断行）。</summary>
    Normal,
    /// <summary>允许在任意字符间断行（等价于强制长单词逐字符断行）。</summary>
    BreakAll,
    /// <summary>对 CJK 文本不在字符间断行（对本引擎的拉丁文本等同 Normal）。</summary>
    KeepAll
}

/// <summary>
/// CSS <c>text-overflow</c>：单行文本溢出容器时的呈现方式（需配合 overflow: hidden + white-space: nowrap）。
/// </summary>
public enum TextOverflow
{
    /// <summary>在内容盒边缘裁剪溢出文本。</summary>
    Clip,
    /// <summary>用省略号（…）表示被裁剪的溢出文本。</summary>
    Ellipsis
}
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
/// <summary>多行/多列 flex 容器或 grid 容器的行/列级交叉轴分布。</summary>
public enum AlignContent
{
    /// <summary>CSS 初始值 normal：行为等同 <see cref="Stretch"/>（等分剩余空间增大各行/列交叉尺寸）。</summary>
    Normal,
    FlexStart,
    FlexEnd,
    Center,
    SpaceBetween,
    SpaceAround,
    Stretch
}
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
