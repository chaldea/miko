using Miko.Common;
using Miko.Core;
using Miko.Diagnostics;
using Miko.Styling;

namespace Miko.Layout;

/// <summary>
/// 布局盒子
/// </summary>
public class LayoutBox
{
    public LayoutBox() => LayoutAllocationDiagnostics.RecordLayoutBoxCreated();

    public Element Element { get; set; } = null!;
    public ComputedStyle ComputedStyle { get; set; } = null!;

    // 盒子维度
    public BoxModel BoxModel { get; set; } = new();

    // 子盒子
    public List<LayoutBox> Children { get; set; } = new();

    // 布局类型
    public LayoutType Type { get; set; }

    // 滚动状态
    public float ScrollTop { get; set; }
    public float ScrollLeft { get; set; }

    // 内容实际尺寸（可能超出 Content 区域）
    public float ScrollableContentWidth { get; set; }
    public float ScrollableContentHeight { get; set; }

    // ---- 行内盒的逐行片段（ISSUE-126）----
    // 非替换 inline 盒在行内格式化上下文中是「透明」的：其内容参与父级行内流、可跨多行断开，
    // 因此它自身的可视几何不是单个矩形，而是每条行盒上一段矩形（浏览器的 inline box fragment）。
    // 元素存**绝对坐标的 border box** 矩形（不像 TextNode.LayoutFragments 那样相对内容盒原点——
    // 跨行 inline 盒没有单一内容盒可作基准）；BoxModel.Content 仍是全部片段的并集，
    // 供 ScrollableContent* 度量、绝对定位包含块等既有消费者使用。
    // 为 null 表示未经行内断行（inline-block / flex 项 / 块级盒等），按 BorderBox 单矩形绘制。
    internal List<RectF>? InlineFragments;

    // 是否需要显示滚动条
    // ScrollableContent* 表示包含内边距的滚动区域，因此与 padding box 的视口尺寸比较。
    public bool HasVerticalScrollbar => ComputedStyle.OverflowY == Overflow.Scroll ||
        (ComputedStyle.OverflowY == Overflow.Auto && ScrollableContentHeight > BoxModel.PaddingBox.Height + 0.01f);
    public bool HasHorizontalScrollbar => ComputedStyle.OverflowX == Overflow.Scroll ||
        (ComputedStyle.OverflowX == Overflow.Auto && ScrollableContentWidth > BoxModel.PaddingBox.Width + 0.01f);

    // scrollbar-width controls presentation only. A hidden scrollbar remains a scroll container.
    public bool ShowsVerticalScrollbar => HasVerticalScrollbar && ComputedStyle.ScrollbarWidth != ScrollbarWidth.None;
    public bool ShowsHorizontalScrollbar => HasHorizontalScrollbar && ComputedStyle.ScrollbarWidth != ScrollbarWidth.None;

    public float VerticalScrollbarThickness => ShowsVerticalScrollbar ? GetScrollbarThickness(ComputedStyle) : 0f;
    public float HorizontalScrollbarThickness => ShowsHorizontalScrollbar ? GetScrollbarThickness(ComputedStyle) : 0f;

    // Prevent scroll-state restoration from overwriting a component's explicit initial position.
    internal bool InitialScrollTopApplied { get; set; }

    // ---- 内在尺寸测量缓存（ISSUE-132）----
    //
    // Flex 布局要知道每个子项的「内容自然尺寸」（flex-basis: auto / width: auto），做法是以
    // (null, null) 约束把该子项**整棵子树**预排一遍，再读它的内容盒——见
    // FlexLayout.ComputeFlexBasis。问题在于同一个子项在一次布局里会被这样测量**多次**：
    // PartitionIntoLines 分行时一次、LayoutLine 求解主轴尺寸时又一次，随后才是真正的排布。
    //
    // 每一层 flex 容器都这样对下一层做 2 次以上的完整子树预排，于是派发次数**逐层翻倍**：
    // 实测嵌套 n 层 flex、总共 n+2 个节点时，一次布局的派发数是 2^(n+1)
    // （depth 1→4 次、3→22、5→94、7→382）。真实的 Ionic RangePage 里
    // ion-page → item-native → item-inner → input-wrapper → ion-range → range-wrapper →
    // native-wrapper → range-knob-handle 共 7 层 flex 嵌套，87 个元素的页面一帧要派发
    // 12905 次、同一个盒子被排布 1276 遍，约 2 MB 分配——这才是拖动 IonRange 时 G2 暴涨的根因。
    //
    // 修复：内在尺寸只取决于子树自身内容，与外部约束无关，因此在**一次布局遍历内**测一次就够。
    // 这里按「测量代号」缓存结果：代号由 LayoutDispatcher 在每次顶层布局开始时递增，
    // 因此缓存天然限定在单次布局内，跨帧不会漏掉 DOM 变化。
    internal long IntrinsicMeasureStamp = -1;
    internal float IntrinsicContentWidth;
    internal float IntrinsicContentHeight;

    // Classic 滚动条宽度（占用布局空间）
    public const float ScrollbarThickness = 12f;
    public const float ThinScrollbarThickness = 6f;

    public static float GetScrollbarThickness(ComputedStyle style) => style.ScrollbarWidth switch
    {
        ScrollbarWidth.None => 0f,
        ScrollbarWidth.Thin => ThinScrollbarThickness,
        _ => ScrollbarThickness,
    };

    public override string ToString() => $"LayoutBox({Element.TagName}, Type: {Type})";
}
