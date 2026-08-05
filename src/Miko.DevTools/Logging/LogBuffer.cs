using System.Collections.Concurrent;

namespace Miko.DevTools.Logging;

/// <summary>
/// DevTools 的日志缓冲：线程安全、有界，并对外暴露一个单调递增的内容版本号。
/// <para>取代裸 <see cref="ConcurrentQueue{T}"/>（见 ISSUE-117）：</para>
/// <list type="bullet">
/// <item>**有界**——Elements 面板活动时无人排空队列，无上限会持续增长。</item>
/// <item>**版本号**——UI 用 <see cref="Sequence"/> 而非条目数判断「是否有新日志」。
/// 裁剪会使条目数在有新内容时保持不变，按数量判断会漏掉更新。</item>
/// </list>
/// </summary>
public sealed class LogBuffer
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly int _maxEntries;
    private long _sequence;

    public LogBuffer(int maxEntries = 2000)
    {
        _maxEntries = Math.Max(1, maxEntries);
    }

    /// <summary>
    /// 单调递增的内容版本号：每入队一条日志递增一次，永不因裁剪回退。
    /// UI 据此判断自上次重建以来是否有新日志。
    /// </summary>
    public long Sequence => Interlocked.Read(ref _sequence);

    /// <summary>保留的最大条数。</summary>
    public int Capacity => _maxEntries;

    public void Enqueue(LogEntry entry)
    {
        _entries.Enqueue(entry);
        Interlocked.Increment(ref _sequence);

        // 裁剪到上限。并发写入下 Count 只是近似值，因此这里不追求精确——
        // 队列长度会稳定在上限附近，这对一个调试缓冲已足够。
        while (_entries.Count > _maxEntries && _entries.TryDequeue(out _))
        {
        }
    }

    public bool TryDequeue(out LogEntry? entry) => _entries.TryDequeue(out entry);

    public int Count => _entries.Count;
}
