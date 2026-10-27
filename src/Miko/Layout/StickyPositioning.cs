using Miko.Common;

namespace Miko.Layout;

/// <summary>
/// <c>position: sticky</c> 的定位算法（ISSUE-159，CSS Positioned Layout 3 §3.4）。
///
/// <para>粘性盒在常规流中占位，布局阶段与 static 盒完全相同；它的偏移取决于最近的滚动容器
/// <b>此刻</b>滚到了哪里。而 Miko 的滚动不触发重排——<see cref="LayoutBox.ScrollTop"/> 变了，
/// 布局缓存照旧命中（ISSUE-096），绘制与命中测试才把滚动量作为平移计入。因此粘性偏移不能在
/// 布局里一次算定，而是由 <see cref="Update"/> 在<b>每次绘制与命中测试之前</b>按当前滚动量重算，
/// 直接平移盒子的几何：绘制、命中、输入法候选框定位等一切从 <see cref="BoxModel"/> 派生的消费者
/// 因此自动看到粘住后的位置，无需各自再算一遍。</para>
///
/// <para>平移是增量的：每个盒子记住上次施加的偏移（<see cref="LayoutBox.StickyOffsetX"/> /
/// <see cref="LayoutBox.StickyOffsetY"/>），新偏移按「当前几何 − 上次偏移」即常规流位置求出，
/// 只平移差值。布局缓存复用同一棵盒树时因此可以反复调用；滚动量不变时为空操作。</para>
///
/// <para>布局阶段（<see cref="LayoutEngine"/> 的定位 pass）按先序收集粘性盒，连同其最近的滚动
/// 容器与包含块一起挂在布局根上（<see cref="LayoutBox.StickyBoxes"/>）；没有粘性盒的树上
/// <see cref="Update"/> 只是一次 null 判断。先序保证祖先先于后代平移：嵌套的粘性盒读到的
/// 包含块与常规流位置都已包含祖先本帧的偏移。</para>
/// </summary>
internal static class StickyPositioning
{
    /// <summary>一个粘性盒及其定位参照。</summary>
    /// <param name="Box">粘性盒。</param>
    /// <param name="ScrollContainer">最近的滚动容器（任一轴 overflow 非 visible）；null 表示视口。</param>
    /// <param name="ContainingBlock">包含块所在的盒（最近的块容器祖先），粘性盒的 margin box 不越出其内容盒。</param>
    internal readonly record struct Entry(LayoutBox Box, LayoutBox? ScrollContainer, LayoutBox ContainingBlock);

    /// <summary>一棵布局树上的全部粘性盒（文档先序）与视口矩形。</summary>
    internal sealed class BoxSet(List<Entry> entries, RectF viewport)
    {
        public List<Entry> Entries { get; } = entries;

        /// <summary>没有滚动容器祖先的粘性盒以视口为可视区。</summary>
        public RectF Viewport { get; } = viewport;
    }

    /// <summary>
    /// 按各滚动容器当前的滚动量重新施加 <paramref name="layoutRoot"/> 上所有粘性盒的偏移。
    /// 由渲染引擎在每次绘制前、由 <see cref="Core.MikoEngine"/> 在每次命中测试前调用。
    /// </summary>
    public static void Update(LayoutBox? layoutRoot)
    {
        var set = layoutRoot?.StickyBoxes;
        if (set == null) return;

        var entries = set.Entries;
        for (int i = 0; i < entries.Count; i++)
            Apply(entries[i], set.Viewport);
    }

    private static void Apply(in Entry entry, RectF viewport)
    {
        var box = entry.Box;
        var style = box.ComputedStyle;
        float fs = style.FontSize.Value;

        // 可视区（scrollport），用布局坐标表达：布局坐标不含滚动，滚动容器的可视窗口在内容中
        // 的位置 = padding box 原点 + 滚动量。只扣 Classic 滚动条占的位（与裁剪区一致，ISSUE-156）。
        // 边界拉伸（OverscrollX/Y）不计入：它是绘制级位移，粘住的盒子随内容一起被拉动，
        // 与 iOS 上粘性标题跟着橡皮筋下移的表现一致。
        RectF port;
        if (entry.ScrollContainer is { } scroller)
        {
            var pb = scroller.BoxModel.PaddingBox;
            port = new RectF(
                pb.X + scroller.ScrollLeft,
                pb.Y + scroller.ScrollTop,
                Math.Max(0f, pb.Width - scroller.VerticalScrollbarGutter),
                Math.Max(0f, pb.Height - scroller.HorizontalScrollbarGutter));
        }
        else
        {
            port = viewport;
        }

        var cb = ContainingRect(entry);

        // 常规流位置 = 当前几何 − 上次施加的偏移。
        float appliedX = box.StickyOffsetX;
        float appliedY = box.StickyOffsetY;
        var border = box.BoxModel.BorderBox;
        var margin = box.BoxModel.MarginBox;

        float dy = ResolveAxis(
            style.Top, style.Bottom, port.Top, port.Height,
            border.Top - appliedY, border.Bottom - appliedY,
            margin.Top - appliedY, margin.Bottom - appliedY,
            cb.Top, cb.Bottom, fs);
        float dx = ResolveAxis(
            style.Left, style.Right, port.Left, port.Width,
            border.Left - appliedX, border.Right - appliedX,
            margin.Left - appliedX, margin.Right - appliedX,
            cb.Left, cb.Right, fs);

        if (dx == appliedX && dy == appliedY) return;

        OffsetSubtree(box, dx - appliedX, dy - appliedY);
        box.StickyOffsetX = dx;
        box.StickyOffsetY = dy;
    }

    /// <summary>
    /// 粘性盒的 margin box 必须留在其中的矩形：包含块的内容盒。包含块恰好就是滚动容器本身时
    /// 改用其整个可滚动区域（与浏览器一致）——滚动容器的内容盒只有一屏高，按它约束的话，
    /// 列表顶部的粘性标题滚过一屏就会被推走，而它应当一直粘到列表末尾。
    /// </summary>
    private static RectF ContainingRect(in Entry entry)
    {
        var cbBox = entry.ContainingBlock;
        var content = cbBox.BoxModel.Content;
        if (!ReferenceEquals(cbBox, entry.ScrollContainer)) return content;

        // ScrollableContent* 含内边距，自 padding box 原点量起。
        var pb = cbBox.BoxModel.PaddingBox;
        var padding = cbBox.BoxModel.Padding;
        float right = Math.Max(content.Right, pb.Left + cbBox.ScrollableContentWidth - padding.Right);
        float bottom = Math.Max(content.Bottom, pb.Top + cbBox.ScrollableContentHeight - padding.Bottom);
        return new RectF(content.Left, content.Top, right - content.Left, bottom - content.Top);
    }

    /// <summary>
    /// 求单轴的粘性偏移（正值朝 end 方向）。<paramref name="startInset"/> / <paramref name="endInset"/>
    /// 为 top/bottom（或 left/right），auto 表示该侧不粘。
    /// <para>可视区按两侧 inset 内缩成「粘性视窗」；盒子越过视窗的 start 边就朝 end 推回、越过 end 边
    /// 就朝 start 推回，但推动量以 margin box 不越出包含块为限，且只朝把盒子拉回视窗的方向推。
    /// 视窗比盒子还小时 end 边向外让出（CSS：start 侧优先）。</para>
    /// </summary>
    private static float ResolveAxis(
        Length startInset, Length endInset, float portStart, float portSize,
        float boxStart, float boxEnd, float marginStart, float marginEnd,
        float cbStart, float cbEnd, float fs)
    {
        bool hasStart = !startInset.IsAuto;
        bool hasEnd = !endInset.IsAuto;
        if (!hasStart && !hasEnd) return 0f;

        // inset 百分比相对可视区尺寸解析。
        float viewStart = portStart + (hasStart ? startInset.ToPixels(portSize, fs) : 0f);
        float viewEnd = portStart + portSize - (hasEnd ? endInset.ToPixels(portSize, fs) : 0f);
        float boxSize = boxEnd - boxStart;
        if (viewEnd - viewStart < boxSize)
            viewEnd = viewStart + boxSize;

        if (hasStart && boxStart < viewStart)
            return Math.Min(viewStart - boxStart, Math.Max(0f, cbEnd - marginEnd));

        if (hasEnd && boxEnd > viewEnd)
            return Math.Max(viewEnd - boxEnd, Math.Min(0f, cbStart - marginStart));

        return 0f;
    }

    /// <summary>
    /// 平移盒子及其后代（含行内盒的逐行片段，与定位 pass 的 relative 平移同一做法）。
    /// fixed 后代以视口为包含块，不随粘性祖先移动，整棵跳过。
    /// </summary>
    private static void OffsetSubtree(LayoutBox box, float dx, float dy)
    {
        var content = box.BoxModel.Content;
        box.BoxModel.Content = new RectF(content.X + dx, content.Y + dy, content.Width, content.Height);

        var frags = box.InlineFragments;
        if (frags != null)
        {
            for (int i = 0; i < frags.Count; i++)
                frags[i] = new RectF(frags[i].X + dx, frags[i].Y + dy, frags[i].Width, frags[i].Height);
        }

        foreach (var child in box.Children)
        {
            if (child.ComputedStyle.Position == Position.Fixed) continue;
            OffsetSubtree(child, dx, dy);
        }
    }
}
