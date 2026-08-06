using Miko.Layout;
using Miko.Routing;

namespace Miko.Core;

/// <summary>
/// 按路由路径保存的滚动快照存储（ISSUE-118）。
///
/// <para>ISSUE-092 的滚动恢复只能在<b>上一帧布局树</b>与新布局树之间搬运偏移，因此只覆盖
/// 「跨重建仍然存在的稳定容器」（如分栏布局中始终在场的侧栏）。当被滚动的是<b>页面自身</b>
/// （Ionic 的 <c>IonContent</c>）时，push 到下一页那一刻旧页面树连同其偏移一起被丢弃；
/// 返回时上一帧布局树是「详情页」，根本不存在可搬运的来源。</para>
///
/// <para>因此这里按<b>历史栈路径</b>持久化偏移，对齐浏览器/Ionic 的 scroll restoration 模型：
/// 离开某路径时为它拍一张快照，<see cref="NavigationDirection.Back"/> 出栈返回该路径时再回放。</para>
///
/// <para>快照只在<b>返回</b>方向回放：<see cref="NavigationDirection.Forward"/> 压栈进入的是
/// 一次新的页面访问，按浏览器语义应从顶部开始；<see cref="NavigationDirection.Root"/>
/// （如 Tab 切换）会清空历史栈，其上的快照随之全部作废。</para>
/// </summary>
internal sealed class ScrollSnapshotStore
{
    /// <summary>
    /// 一个可滚动盒子的偏移快照。
    /// <para><paramref name="IndexPath"/> 是从<b>布局树根</b>逐层下行到该盒子的子节点索引路径。
    /// 之所以记录在布局树而非 DOM 树上：偏移本身存放于 <see cref="LayoutBox"/>，且
    /// <c>display:none</c> 的元素会被布局树过滤掉，两棵树的形状并不一致。同一路径的页面重建
    /// 会产出同形的布局树，因此索引路径在这一用途下是稳定的；回放时再以
    /// <paramref name="TagName"/> 校验末端盒子的身份，不匹配就保守放弃该条目。</para>
    /// </summary>
    internal readonly record struct Entry(int[] IndexPath, string TagName, float ScrollTop, float ScrollLeft);

    // 键为路由路径；值为该页面离开时所有非零偏移的可滚动盒子。
    private readonly Dictionary<string, List<Entry>> _snapshots = new();

    /// <summary>当前保存了快照的路径数（测试与诊断用）。</summary>
    internal int Count => _snapshots.Count;

    /// <summary>
    /// 为 <paramref name="path"/> 拍一张快照：收集 <paramref name="layout"/> 中所有存在非零滚动
    /// 偏移的盒子。整棵树都没有滚动过时不产生任何条目，并清除该路径的旧快照（页面已回到顶部，
    /// 旧快照不再代表它的状态）。
    /// </summary>
    internal void Capture(string path, LayoutBox? layout)
    {
        if (layout == null) return;

        List<Entry>? entries = null;
        CaptureRecursive(layout, new List<int>(), ref entries);

        if (entries == null)
        {
            _snapshots.Remove(path);
            return;
        }

        _snapshots[path] = entries;
    }

    private static void CaptureRecursive(LayoutBox box, List<int> pathBuffer, ref List<Entry>? entries)
    {
        if (box.ScrollTop != 0f || box.ScrollLeft != 0f)
        {
            entries ??= new List<Entry>();
            entries.Add(new Entry(pathBuffer.ToArray(), box.Element.TagName, box.ScrollTop, box.ScrollLeft));
        }

        for (int i = 0; i < box.Children.Count; i++)
        {
            pathBuffer.Add(i);
            CaptureRecursive(box.Children[i], pathBuffer, ref entries);
            pathBuffer.RemoveAt(pathBuffer.Count - 1);
        }
    }

    /// <summary>
    /// 把 <paramref name="path"/> 的快照回放到 <paramref name="layout"/> 上。<b>不消费</b>快照，
    /// 因此一次导航中若因 transition 触发而重新布局，可以对新布局树再回放一次；消费由
    /// <see cref="Forget"/> 在该次导航收尾时完成。
    /// <para>回放沿索引路径下行，任一层越界或末端标签名不符即跳过该条目——页面结构确实变了时，
    /// 宁可从顶部开始，也不要把偏移写到不相干的盒子上。写回的偏移按该盒子当前的可滚动范围夹取，
    /// 因此返回页比离开时更短（如图片尚未加载）也不会越界。</para>
    /// </summary>
    /// <returns>实际恢复的盒子数量。</returns>
    internal int Apply(string path, LayoutBox? layout)
    {
        if (layout == null) return 0;
        if (!_snapshots.TryGetValue(path, out var entries)) return 0;

        int restored = 0;
        foreach (var entry in entries)
        {
            var box = Resolve(layout, entry);
            if (box == null) continue;

            box.ScrollTop = ClampScroll(entry.ScrollTop, box.ScrollableContentHeight, box.BoxModel.PaddingBox.Height);
            box.ScrollLeft = ClampScroll(entry.ScrollLeft, box.ScrollableContentWidth, box.BoxModel.PaddingBox.Width);
            restored++;
        }
        return restored;
    }

    /// <summary>
    /// 丢弃某个路径的快照。返回该路径即出栈，其历史条目已被消费——与
    /// <see cref="NavigationManager"/> 弹出历史条目的语义一致，同时避免快照无限增长。
    /// </summary>
    internal void Forget(string path) => _snapshots.Remove(path);

    /// <summary>丢弃全部快照（历史栈被清空时，如 <see cref="NavigationDirection.Root"/> 切换）。</summary>
    internal void Clear() => _snapshots.Clear();

    /// <summary>沿索引路径下行定位盒子；越界或末端标签名不符时返回 null。</summary>
    private static LayoutBox? Resolve(LayoutBox root, in Entry entry)
    {
        var current = root;
        foreach (int index in entry.IndexPath)
        {
            if (index < 0 || index >= current.Children.Count) return null;
            current = current.Children[index];
        }
        return current.Element.TagName == entry.TagName ? current : null;
    }

    /// <summary>
    /// 与 <c>MikoEngine.ScrollBy</c> 同款的夹取：可滚动内容尺寸与 padding box 视口尺寸之差即为上限。
    /// </summary>
    private static float ClampScroll(float saved, float scrollableContentSize, float viewportSize)
    {
        if (saved <= 0f) return 0f;
        float max = Math.Max(0f, scrollableContentSize - viewportSize);
        return Math.Clamp(saved, 0f, max);
    }
}
