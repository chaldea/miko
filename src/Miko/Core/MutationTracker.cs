namespace Miko.Core;

/// <summary>
/// 单个引擎实例的 DOM/样式变更版本号（ISSUE-129）。
///
/// <para>任何影响样式匹配或布局结果的修改（结构、文本、class/id、行内样式替换、元素状态、
/// 图片内禀尺寸等）都会使其递增。<see cref="Layout.LayoutEngine"/> 据此判断上一次布局结果
/// 是否仍然有效——版本未变且视口/样式表未变时整棵布局树可直接复用（ISSUE-096）。</para>
///
/// <para>本类型取代了原先挂在 <see cref="Element"/> 上的进程级全局静态计数。全局计数使
/// <b>任一</b>引擎的 DOM 变更都击穿<b>所有</b>引擎的布局缓存：次级引擎（DevTools 独立窗口、
/// 模拟器设置面板）的 <c>IsLayoutCurrent</c> 被主窗口活动持续击穿而恒为 false，空闲跳帧
/// 完全失效（ISSUE-117）。改为按引擎实例计数后，各引擎的布局缓存互不干扰。</para>
///
/// <para>元素通过 <see cref="Element.Owner"/> 找到自己所属的计数器；尚未挂入任何引擎的
/// 游离元素没有归属，其变更静默不计数——没有引擎在观察它，也就没有布局缓存需要失效。</para>
/// </summary>
public sealed class MutationTracker
{
    private long _version;

    /// <summary>当前变更版本号（单调递增）。</summary>
    public long Version => Interlocked.Read(ref _version);

    /// <summary>
    /// 递增变更版本号。由元素自身的变更入口自动调用；引擎在元素外完成的布局相关写入
    /// （动画帧值、图片/视频内禀尺寸等）也应调用，否则下一帧可能复用过期布局。
    /// </summary>
    internal void Bump() => Interlocked.Increment(ref _version);
}
