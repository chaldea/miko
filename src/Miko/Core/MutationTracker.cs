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
///
/// <para><b>变更的三种影响范围（ISSUE-146）</b>。<see cref="Version"/> 计入全部变更；
/// 布局引擎据另外两项判断样式阶段要做多少：</para>
/// <list type="bullet">
/// <item><see cref="Bump"/>：可能改变<b>任意</b>元素的计算样式（结构、class/id、状态……），
/// 递增 <see cref="StyleVersion"/>，整树重新级联。</item>
/// <item><see cref="BumpInlineStyle"/>：某个元素的行内样式对象被替换。选择器读不到行内样式，
/// 所以受影响的只有该元素<b>自身及其后代</b>（继承、em、变量作用域），只重算这棵子树。</item>
/// <item><see cref="BumpContent"/>：只影响布局（已有文字、内禀尺寸），任何计算样式都不变，
/// 不重新级联。</item>
/// </list>
/// <para>动机：详情页播放器每秒刷新时间文字、首帧按宽度写入 16:9 高度的行内样式，
/// 两者都曾让 340 个元素的整树级联重跑一遍——导航那一帧因此级联两次。</para>
/// </summary>
public sealed class MutationTracker
{
    private long _version;
    private long _styleVersion;

    // 自上次样式阶段以来行内样式被替换过的元素（见 BumpInlineStyle）。布局引擎在样式阶段开始时
    // 整体取走；在此之前只有下一帧布局会消费它，而任何一次登记都递增了 _version，下一帧必然布局，
    // 因此不会长期持有已离开 DOM 的元素。
    private readonly object _inlineGate = new();
    private List<Element>? _inlineStyleChanged;

    /// <summary>当前变更版本号（单调递增，计入全部变更）。</summary>
    public long Version => Interlocked.Read(ref _version);

    /// <summary>
    /// 可能影响<b>任意</b>元素计算样式的变更版本号（单调递增）。只有 <see cref="Bump"/> 递增它。
    /// </summary>
    public long StyleVersion => Interlocked.Read(ref _styleVersion);

    /// <summary>
    /// 递增变更版本号。由元素自身的变更入口自动调用；引擎在元素外完成的布局相关写入
    /// （动画帧值等）也应调用，否则下一帧可能复用过期布局。
    /// </summary>
    internal void Bump()
    {
        // 先样式后总数：任何读到这次总数递增的读者，随后读到的样式版本必然也已递增，
        // 不会出现「总数算进去了、样式没算进去」而把一次样式变更误当成局部变更。
        Interlocked.Increment(ref _styleVersion);
        Interlocked.Increment(ref _version);
    }

    /// <summary>
    /// 登记 <paramref name="element"/> 的行内样式对象被替换：下一次样式阶段只需重算它这棵子树。
    /// </summary>
    internal void BumpInlineStyle(Element element)
    {
        lock (_inlineGate)
            (_inlineStyleChanged ??= new List<Element>()).Add(element);
        Interlocked.Increment(ref _version);
    }

    /// <summary>
    /// 递增变更版本号，但声明本次变更<b>不可能</b>改变任何元素的计算样式——
    /// 只影响布局（文字内容、内禀尺寸）。调用方必须能证明这一点：选择器与级联都不读取
    /// 这次改动的数据。拿不准时一律用 <see cref="Bump"/>，多算一次样式只是慢，少算一次就是错。
    /// </summary>
    internal void BumpContent() => Interlocked.Increment(ref _version);

    /// <summary>取走并清空自上次调用以来登记的行内样式变更元素；没有时返回 null。</summary>
    internal List<Element>? TakeInlineStyleChanges()
    {
        lock (_inlineGate)
        {
            var changed = _inlineStyleChanged;
            _inlineStyleChanged = null;
            return changed;
        }
    }
}
