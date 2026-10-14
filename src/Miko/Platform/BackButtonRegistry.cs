namespace Miko.Platform;

/// <summary>
/// 返回处理者的优先级（ISSUE-138）。数值与 Ionic 的硬件返回键优先级一致：覆盖层 &gt; 菜单 &gt; 导航。
///
/// <para>
/// 必须用优先级而不是"后注册者优先"来决定谁先响应：嵌套组件在父组件每次渲染时都会重建，
/// 注册顺序因此跟随<b>构建</b>顺序而不是屏幕上的层叠。例如页头重渲染后，页面的返回按钮会排到
/// 已打开的模态框之后注册——若按注册顺序，按返回就会在模态框还开着时把页面导航走。
/// 同一优先级内仍按后注册者优先。
/// </para>
/// </summary>
public static class BackButtonPriority
{
    /// <summary>页面导航（返回上一页）。最低：只有没有任何覆盖层与菜单打开时才轮到它。</summary>
    public const int Navigation = 0;

    /// <summary>侧边菜单。</summary>
    public const int Menu = 99;

    /// <summary>覆盖层（模态框、动作面板等）。经 portal 绘制在页面与菜单之上，因此最先响应。</summary>
    public const int Overlay = 100;
}

/// <summary>
/// 系统返回键的分发链（ISSUE-138）。
///
/// <para>
/// 平台宿主把硬件返回键（Android）、返回手势（iOS）、Escape（桌面）或模拟器面板的返回按钮经
/// <c>MikoInteractionController.OnBackButton</c> 交给 <see cref="Dispatch"/>。本类按
/// <see cref="BackButtonPriority">优先级</see>从高到低、同优先级内后注册者优先地询问处理者，
/// 第一个返回 <c>true</c> 的处理者消费本次返回。全部未消费时返回 <c>false</c>，宿主据此把返回
/// 交还系统（Android 的默认行为是退出应用）。
/// </para>
///
/// <para>同步、无分配；处理者必须快速返回，不得阻塞。</para>
/// </summary>
public sealed class BackButtonRegistry
{
    private readonly List<Entry> _entries = new();

    // 组件注册/释放在渲染线程，移动端宿主的分发与 Changed 订阅在 UI 线程，故加锁。
    private readonly object _sync = new();

    // 单调递增的注册序号：同优先级内用它判定谁更晚注册（更晚 = 更先响应）。
    private long _sequence;

    /// <summary>
    /// 处理者集合发生变化（注册或注销）时触发，在变更所在线程上同步调用、且在锁外。
    /// Android 宿主据此只在确有处理者时才向系统认领返回键——认领后系统不再执行默认返回，
    /// 也不再播放预测性返回动画。
    /// </summary>
    public event Action? Changed;

    /// <summary>当前已注册的处理者数量。</summary>
    public int Count
    {
        get { lock (_sync) return _entries.Count; }
    }

    /// <summary>
    /// 注册一个返回处理者。处理者返回 <c>true</c> 表示它消费了本次返回（例如关闭了自己）。
    /// 释放返回的句柄即注销；重复释放是幂等的。
    /// </summary>
    /// <param name="handler">处理者。</param>
    /// <param name="priority">优先级，见 <see cref="BackButtonPriority"/>；越大越先响应。</param>
    public IDisposable Register(Func<bool> handler, int priority = BackButtonPriority.Navigation)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var entry = new Entry(handler, priority);
        lock (_sync)
        {
            entry.Sequence = ++_sequence;
            _entries.Add(entry);
        }

        Changed?.Invoke();
        return new Registration(this, entry);
    }

    /// <summary>分发一次返回，返回是否被某个处理者消费。</summary>
    public bool Dispatch()
    {
        // 先取有序快照再遍历：处理者可能在回调中关闭自己（从而注销），直接遍历活表会漏掉或踩空。
        // 快照也让分发不必长时间持锁——处理者里可能触发组件重建。
        Entry[] snapshot;
        lock (_sync)
        {
            if (_entries.Count == 0) return false;
            snapshot = _entries
                .OrderByDescending(e => e.Priority)
                .ThenByDescending(e => e.Sequence)
                .ToArray();
        }

        foreach (var entry in snapshot)
        {
            // 处理者可能已在本次分发中被撤销（例如被更高层的处理者关闭），撤销后不应再被调用。
            if (entry.Removed) continue;
            if (entry.Handler()) return true;
        }

        return false;
    }

    private void Remove(Entry entry)
    {
        lock (_sync)
        {
            if (entry.Removed) return;
            entry.Removed = true;
            _entries.Remove(entry);
        }

        Changed?.Invoke();
    }

    private sealed class Entry(Func<bool> handler, int priority)
    {
        public Func<bool> Handler { get; } = handler;
        public int Priority { get; } = priority;
        public long Sequence { get; set; }

        // 只在锁内写入；分发时在锁外读取——读到旧值的最坏结果是多调用一次已撤销的处理者，
        // 而组件的处理者自身都会再核对一次自己的状态。
        public volatile bool Removed;
    }

    private sealed class Registration(BackButtonRegistry registry, Entry entry) : IDisposable
    {
        public void Dispose() => registry.Remove(entry);
    }
}
