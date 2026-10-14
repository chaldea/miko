using Miko.Platform;

namespace Miko.Windowing.Native;

/// <summary>
/// 桌面的屏幕方向 / 息屏服务（ISSUE-138）。
///
/// <para>
/// 桌面窗口没有可编程的"屏幕方向"——窗口由用户自由缩放，不存在旋转这一动作。因此
/// <see cref="Lock"/> / <see cref="SetAsync"/> 记录状态但不改变窗口，这是**刻意的静默降级**：
/// 与 <c>Miko.Native</c> 的桌面实现抛 <see cref="PlatformNotSupportedException"/> 不同，
/// 本服务面向组件库，一个视频组件不该因为跑在桌面上就崩。
/// </para>
///
/// <para>
/// 锁定状态与息屏请求仍按引用计数维护，使嵌套调用（组件重入、页面切换）不会互相抵消，
/// 也便于 Simulator 与测试观察。公开类型：Simulator 复用同一实现，因为"桌面尺寸的窗口"
/// 与"模拟器里的设备画面"在宿主层面同样没有可编程方向。
/// </para>
/// </summary>
public sealed class DesktopOrientationService : IOrientationService
{
    private int _lockDepth;
    private int _awakeDepth;
    private ScreenOrientation? _lockedOrientation;

    /// <summary>当前方向锁的嵌套深度（0 表示未锁定）。</summary>
    public int LockDepth => _lockDepth;

    /// <summary>当前屏幕常亮的嵌套深度（0 表示允许息屏）。</summary>
    public int AwakeDepth => _awakeDepth;

    /// <summary>被锁定的方向；<c>null</c> 表示仅禁止旋转、未指定方向，或未锁定。</summary>
    public ScreenOrientation? LockedOrientation => _lockedOrientation;

    public void Lock(ScreenOrientation? orientation = null)
    {
        _lockDepth++;
        // 首次锁定才记录方向：嵌套锁定中，内层传 null 表示"保持外层的方向"，不该把外层
        // 已经指定的方向抹掉（那样最外层解锁后方向就丢了）。
        if (_lockDepth == 1) _lockedOrientation = orientation;
    }

    public void Unlock()
    {
        if (_lockDepth == 0) return;
        _lockDepth--;
        if (_lockDepth == 0) _lockedOrientation = null;
    }

    public Task SetAsync(ScreenOrientation orientation)
    {
        // 未锁定时记录期望方向（桌面上没有窗口旋转，仅作状态）；已锁定时忽略——
        // 与 iOS/Android 上"锁定期间 Set 不生效"的语义一致。
        if (_lockDepth == 0) _lockedOrientation = orientation;
        return Task.CompletedTask;
    }

    public void KeepAwake() => _awakeDepth++;

    public void AllowSleep()
    {
        if (_awakeDepth > 0) _awakeDepth--;
    }
}
