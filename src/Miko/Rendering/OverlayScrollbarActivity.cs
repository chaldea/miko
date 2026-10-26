using Miko.Core;

namespace Miko.Rendering;

/// <summary>
/// Overlay 滚动条的显隐时钟（ISSUE-156）。
///
/// <para>Overlay 滚动条（macOS 的默认样式）平时不可见：容器一滚动就显现，停止滚动后保持
/// <see cref="HoldSeconds"/>，再用 <see cref="FadeSeconds"/> 淡出。本类按滚动容器的<b>元素</b>
/// 记录上次滚动的时刻，绘制与命中测试都向它询问当前不透明度——淡出后的滚动条既不画，
/// 也不拦截点击，点在那里的鼠标落到下面的内容上。</para>
///
/// <para><b>保持期不出帧。</b>保持期内画面一成不变，若照常逐帧推进，每次滚动停下后宿主都要再白画
/// 一秒钟的相同画面，破坏 ISSUE-096 的零帧空闲。所以时间按真实时钟（<see cref="TimeProvider"/>）计，
/// 只有淡出期才需要出帧（<see cref="IsFading"/>）。轮询待办工作的宿主（桌面、模拟器、iOS 的
/// display link）到时自然会看到；按需绘制的宿主（Android）靠保持期结束时向调度器投递的唤醒
/// ——<see cref="Platform.MikoDispatcher.Posted"/> 正是它的唤醒信号。</para>
///
/// <para>按元素而非布局盒记录：重排会换掉布局盒实例，元素在重排间保持不变；组件重渲染换掉的
/// 元素则沿 <see cref="Element.ResolveSuperseded"/> 找到接替它的实例。</para>
/// </summary>
public sealed class OverlayScrollbarActivity
{
    /// <summary>停止滚动后滚动条保持完全可见的时长（秒）。</summary>
    public const float HoldSeconds = 1.0f;

    /// <summary>随后淡出的时长（秒）。</summary>
    public const float FadeSeconds = 0.3f;

    private static readonly TimeSpan HoldDuration = TimeSpan.FromSeconds(HoldSeconds);

    private sealed class Entry
    {
        public required Element Element;
        public long LastActivity;
    }

    // 同时可见的通常只有正在滚动的那一个，线性查找即可，也不必为它分配字典。
    private readonly List<Entry> _entries = new();
    private readonly TimeProvider _time;
    private readonly Action? _wake;
    private ITimer? _wakeTimer;

    // 本帧绘制所用的时刻（BeginFrame 采样）。命中测试也以它为准：点得到的就是画出来的。
    private long _frameTimestamp;

    /// <param name="timeProvider">时钟，默认 <see cref="TimeProvider.System"/>；测试注入手动时钟。</param>
    /// <param name="wake">
    /// 保持期结束、淡出开始时调用（在计时器线程上），让空闲的宿主出帧。必须线程安全。
    /// </param>
    public OverlayScrollbarActivity(TimeProvider? timeProvider = null, Action? wake = null)
    {
        _time = timeProvider ?? TimeProvider.System;
        _wake = wake;
    }

    /// <summary>是否有滚动条可见（含保持期与淡出中）。</summary>
    public bool HasVisible => _entries.Count > 0;

    /// <summary>
    /// 是否有滚动条过了保持期、正在淡出（或在等下一帧把它擦掉）——只有这时才需要逐帧绘制。
    /// </summary>
    public bool IsFading
    {
        get
        {
            if (_entries.Count == 0) return false;
            var now = _time.GetTimestamp();
            for (int i = 0; i < _entries.Count; i++)
            {
                if (Elapsed(_entries[i], now) >= HoldSeconds) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 记录 <paramref name="element"/> 这个滚动容器刚刚滚动过（或滚动条正被按住），使其滚动条完全显现
    /// 并重新开始计时。
    /// </summary>
    public void NoteActivity(Element element)
    {
        var now = _time.GetTimestamp();
        var entry = Find(element);
        if (entry != null)
            entry.LastActivity = now;
        else
            _entries.Add(new Entry { Element = element, LastActivity = now });

        // 保持期结束时唤醒宿主开始淡出。每次活动都把计时器往后推，只有最后一次滚动之后的那一次会触发。
        if (_wake == null) return;
        if (_wakeTimer == null)
        {
            // 计时器与引擎同寿，不能捕获创建时（某次滚动事件处理中）的 ExecutionContext：
            // 那会把当时的 AsyncLocal 值一直钉住。
            using (ExecutionContext.SuppressFlow())
            {
                _wakeTimer = _time.CreateTimer(static state => ((OverlayScrollbarActivity)state!)._wake!(),
                    this, HoldDuration, Timeout.InfiniteTimeSpan);
            }
        }
        else
        {
            _wakeTimer.Change(HoldDuration, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// 帧开始时调用：采样本帧的时刻，并丢弃已经完全淡出的条目——本帧就不再画它们，
    /// 之后也不再需要出帧。
    /// </summary>
    public void BeginFrame()
    {
        _frameTimestamp = _time.GetTimestamp();
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (Elapsed(_entries[i], _frameTimestamp) >= HoldSeconds + FadeSeconds)
                _entries.RemoveAt(i);
        }
    }

    /// <summary>
    /// <paramref name="element"/> 的滚动条在本帧的不透明度：0 为不可见，1 为完全可见。
    /// 本帧开始之后才发生的滚动按刚滚动处理（完全可见）。
    /// </summary>
    public float GetOpacity(Element element)
    {
        var entry = Find(element);
        if (entry == null) return 0f;
        float elapsed = Elapsed(entry, _frameTimestamp);
        if (elapsed <= HoldSeconds) return 1f;
        return Math.Clamp(1f - (elapsed - HoldSeconds) / FadeSeconds, 0f, 1f);
    }

    /// <summary>立即隐藏所有滚动条（换根树、改变滚动条样式时）。</summary>
    public void Clear() => _entries.Clear();

    private float Elapsed(Entry entry, long now)
        => (float)_time.GetElapsedTime(entry.LastActivity, now).TotalSeconds;

    private Entry? Find(Element element)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (ReferenceEquals(entry.Element, element))
                return entry;

            // 组件重渲染换掉了滚动容器的元素：把条目转给接替它的实例，滚动条不因重渲染而闪没。
            if (entry.Element.SupersededBy != null)
            {
                entry.Element = entry.Element.ResolveSuperseded();
                if (ReferenceEquals(entry.Element, element))
                    return entry;
            }
        }
        return null;
    }
}
