using Miko.Common;

namespace Miko.Layout.LayoutAlgorithms;

/// <summary>
/// 布局调度器 - 根据 LayoutBox 的类型选择正确的布局算法
/// </summary>
public static class LayoutDispatcher
{
    private static readonly BlockLayout _blockLayout = new();
    private static readonly InlineLayout _inlineLayout = new();
    private static readonly FlexLayout _flexLayout = new();
    private static readonly GridLayout _gridLayout = new();
    private static readonly TableLayout _tableLayout = new();
    private static readonly TextLayout _textLayout = new();

    /// <summary>
    /// 当前测量代号（ISSUE-132）。每次<b>顶层</b>布局开始时递增，用作
    /// <see cref="LayoutBox.IntrinsicMeasureStamp"/> 的键，使内在尺寸缓存天然限定在单次布局内
    /// ——跨帧自动失效，不会漏掉 DOM 变化。
    ///
    /// <para><see cref="ThreadStaticAttribute"/>：调度器是静态的，而多个引擎可能在各自线程上
    /// 并发布局（DevTools 独立窗口、模拟器设置面板，见 ISSUE-129），按线程隔离避免互相串扰。</para>
    /// </summary>
    [ThreadStatic]
    private static long _measureStamp;

    /// <summary>开启一次新的布局遍历，作废上一遍的内在尺寸缓存。由 <see cref="LayoutEngine"/> 调用。</summary>
    internal static void BeginLayoutPass() => _measureStamp++;

    /// <summary>
    /// 测量子树的<b>内在尺寸</b>（内容自然尺寸），必要时才真正预排一遍（ISSUE-132）。
    ///
    /// <para>Flex 需要子项的内容自然尺寸来求 flex-basis，做法是以无限约束把子树预排一遍
    /// （<c>FlexLayout.ComputeFlexBasis</c>）。同一个子项在一次布局里会被这样测量多次
    /// ——<c>PartitionIntoLines</c> 分行时一次、<c>LayoutLine</c> 求解主轴尺寸时又一次——
    /// 而每层 flex 容器都对下一层这么做，派发次数于是<b>逐层翻倍</b>。</para>
    ///
    /// <para>实测（嵌套 n 层 flex、共 n+2 个节点，改动前）：一次布局的派发数为 2^(n+1)
    /// ——depth 1→4、3→22、5→94、7→382。真实的 Ionic RangePage 有 7 层 flex 嵌套
    /// （ion-page → item-native → item-inner → input-wrapper → ion-range → range-wrapper →
    /// native-wrapper → range-knob-handle），87 个元素的页面<b>一帧</b>要派发 12905 次、
    /// 同一个盒子被排布 1276 遍，约 2 MB 分配。拖动 IonRange 时 Gen0 被瞬间打满、对象被提升进
    /// Gen2，于是 G2 单调上涨——这才是 ISSUE-132 现场的真正根因（issue 原本归因于布局缓存
    /// 未命中，方向是错的）。</para>
    ///
    /// <para>内在尺寸只取决于子树自身内容、与外部约束无关，因此一次布局遍历内测一次就够。
    /// 缓存后派发数从指数回落到线性（depth 7：382 → 43），真实页面 12905 → 673。</para>
    /// </summary>
    internal static (float Width, float Height) MeasureIntrinsic(LayoutBox box)
    {
        if (box.IntrinsicMeasureStamp == _measureStamp)
            return (box.IntrinsicContentWidth, box.IntrinsicContentHeight);

        Dispatch(box, new LayoutConstraints(null, null), 0, 0);

        box.IntrinsicContentWidth = box.BoxModel.Content.Width;
        box.IntrinsicContentHeight = box.BoxModel.Content.Height;
        box.IntrinsicMeasureStamp = _measureStamp;
        return (box.IntrinsicContentWidth, box.IntrinsicContentHeight);
    }

    /// <summary>
    /// 根据盒子类型执行相应的布局算法
    /// </summary>
    public static void Dispatch(LayoutBox box, LayoutConstraints constraints, float x, float y)
    {
        switch (box.Type)
        {
            case LayoutType.Block:
                _blockLayout.Layout(box, constraints, x, y);
                break;

            case LayoutType.Inline:
            case LayoutType.InlineBlock:
                _inlineLayout.Layout(box, constraints, x, y);
                break;

            case LayoutType.Flex:
            case LayoutType.InlineFlex:
                _flexLayout.Layout(box, constraints, x, y);
                break;

            case LayoutType.Grid:
                _gridLayout.Layout(box, constraints, x, y);
                break;

            case LayoutType.Table:
                _tableLayout.Layout(box, constraints, x, y);
                break;

            case LayoutType.TableRow:
            case LayoutType.TableCell:
                // TableRow 和 TableCell 由 TableLayout 直接布局
                // 如果单独调用，使用 Block 布局作为后备
                _blockLayout.Layout(box, constraints, x, y);
                break;

            case LayoutType.Text:
                _textLayout.Layout(box, constraints, x, y);
                break;
        }
    }
}
