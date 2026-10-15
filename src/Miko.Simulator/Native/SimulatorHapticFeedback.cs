using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform;

namespace Miko.Simulator.Native;

/// <summary>
/// 模拟器的组件触觉反馈（ISSUE-149）。桌面没有触觉硬件，能做的只有把每次反馈记进日志并计数——
/// 这已经足以在桌面上验证「组件什么时候触发触觉」这条调用链，例如滚轮每跳一格是否恰好一次。
///
/// <para>
/// 公开类型与计数属性，便于测试与 MCP/DevTools 这类观察者读取，与
/// <c>DesktopOrientationService</c> 的定位一致。计数在任意线程上递增（组件也会从计时器线程
/// 结束选择会话），因此用原子操作。
/// </para>
/// </summary>
public sealed class SimulatorHapticFeedback : IHapticFeedback
{
    private readonly ILogger _logger;
    private int _selectionStarts;
    private int _selectionChanges;
    private int _selectionEnds;
    private int _impacts;

    public SimulatorHapticFeedback(ILogger<SimulatorHapticFeedback>? logger = null)
        => _logger = logger ?? (ILogger)NullLogger.Instance;

    /// <summary><see cref="SelectionStart"/> 的调用次数。</summary>
    public int SelectionStartCount => Volatile.Read(ref _selectionStarts);

    /// <summary><see cref="SelectionChanged"/> 的调用次数——即真机上"咔"的次数。</summary>
    public int SelectionChangedCount => Volatile.Read(ref _selectionChanges);

    /// <summary><see cref="SelectionEnd"/> 的调用次数。</summary>
    public int SelectionEndCount => Volatile.Read(ref _selectionEnds);

    /// <summary><see cref="Impact"/> 的调用次数。</summary>
    public int ImpactCount => Volatile.Read(ref _impacts);

    public void SelectionStart() => Record(ref _selectionStarts, nameof(SelectionStart));

    public void SelectionChanged() => Record(ref _selectionChanges, nameof(SelectionChanged));

    public void SelectionEnd() => Record(ref _selectionEnds, nameof(SelectionEnd));

    public void Impact(HapticImpactStyle style = HapticImpactStyle.Medium)
        => Record(ref _impacts, nameof(Impact), style.ToString());

    private void Record(ref int counter, string member, string? detail = null)
    {
        Interlocked.Increment(ref counter);
        _logger.LogInformation("[Simulator.Haptics] {Member} {Detail}", member, detail ?? string.Empty);
    }
}
