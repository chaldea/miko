using Android.Views;
using Miko.Native;
using Miko.Platform;
// Android 的平台枚举与 Miko 的 ScreenOrientation 同名，两侧都起别名消歧。
using AndroidScreenOrientation = Android.Content.PM.ScreenOrientation;
using MikoScreenOrientation = Miko.Platform.ScreenOrientation;

namespace Miko.Android.Native;

/// <summary>
/// Android 的屏幕方向 / 息屏控制（ISSUE-138），基于 <c>Activity.RequestedOrientation</c> 与
/// <c>FLAG_KEEP_SCREEN_ON</c>。
///
/// <para>
/// 与其它 Native 服务一样，<c>Activity</c> 在服务构造时还不存在，因此宿主
/// <see cref="MikoSurfaceView"/> 构造时才 <c>Attach</c>，本服务在**调用时**才读取它。
/// 宿主缺失时（视图未挂在 Activity 上）静默降级：方向/息屏是交互增强，不该让组件崩。
/// </para>
///
/// <para>
/// 所有写操作都切到 UI 线程——<c>RequestedOrientation</c> 与窗口标志只能在 UI 线程改。
/// </para>
/// </summary>
internal sealed class AndroidOrientationService : IOrientationService
{
    private readonly INativeHostContext _hostContext;
    private readonly object _sync = new();

    private int _lockDepth;
    private int _awakeDepth;
    private MikoScreenOrientation? _lockedOrientation;

    public AndroidOrientationService(INativeHostContext hostContext) => _hostContext = hostContext;

    public void Lock(MikoScreenOrientation? orientation = null)
    {
        MikoScreenOrientation? requested;
        lock (_sync)
        {
            _lockDepth++;
            // 首次锁定才记录方向：嵌套锁定中内层传 null 表示"保持外层方向"。
            if (_lockDepth == 1) _lockedOrientation = orientation;
            requested = _lockedOrientation;
        }

        ApplyOrientation(requested);
    }

    public void Unlock()
    {
        MikoScreenOrientation? requested;
        lock (_sync)
        {
            if (_lockDepth == 0) return;
            _lockDepth--;
            if (_lockDepth == 0) _lockedOrientation = null;
            requested = _lockedOrientation;
        }

        // 全部解锁 → 归还系统自动旋转（Unspecified）。
        ApplyOrientation(requested);
    }

    public Task SetAsync(MikoScreenOrientation orientation)
    {
        lock (_sync)
        {
            // 锁定期间忽略 Set（与 iOS/Android 的锁定语义一致）；未锁定时仅作为期望方向。
            if (_lockDepth == 0) _lockedOrientation = orientation;
            else return Task.CompletedTask;
        }

        ApplyOrientation(orientation);
        return Task.CompletedTask;
    }

    public void KeepAwake()
    {
        lock (_sync)
        {
            _awakeDepth++;
            if (_awakeDepth != 1) return;
        }

        ApplyKeepAwake(true);
    }

    public void AllowSleep()
    {
        lock (_sync)
        {
            if (_awakeDepth == 0) return;
            _awakeDepth--;
            if (_awakeDepth != 0) return;
        }

        ApplyKeepAwake(false);
    }

    private void ApplyOrientation(MikoScreenOrientation? orientation)
    {
        var target = ToAndroidOrientation(orientation);
        RunOnUiThread(activity =>
        {
            // 与 ActivityInfo.ScreenOrientation 同名，故显式限定。
            activity.RequestedOrientation = target;
        });
    }

    private void ApplyKeepAwake(bool keepAwake)
    {
        RunOnUiThread(activity =>
        {
            var window = activity.Window;
            if (window is null) return;
            if (keepAwake) window.AddFlags(WindowManagerFlags.KeepScreenOn);
            else window.ClearFlags(WindowManagerFlags.KeepScreenOn);
        });
    }

    private void RunOnUiThread(Action<global::Android.App.Activity> action)
    {
        // 宿主可能尚未 Attach（视图未挂载）；方向/息屏是增强能力，静默跳过而非抛错。
        var activity = (_hostContext.Host as AndroidNativeHost)?.Activity;
        if (activity is null) return;

        if (global::Android.OS.Looper.MyLooper() == global::Android.OS.Looper.MainLooper) action(activity);
        else activity.RunOnUiThread(() => action(activity));
    }

    /// <summary>
    /// 映射到 Android 的 <c>RequestedOrientation</c>。不指定方向时用 <c>Unspecified</c>
    /// 解除锁定、归还系统自动旋转；<c>Portrait</c>/<c>Landscape</c> 不区分正反，
    /// 用 <c>Sensor</c> 系列让设备在两个朝向间自由切换但仍受限于该轴。
    /// </summary>
    private static AndroidScreenOrientation ToAndroidOrientation(MikoScreenOrientation? orientation) => orientation switch
    {
        null => AndroidScreenOrientation.Unspecified,
        MikoScreenOrientation.Portrait => AndroidScreenOrientation.SensorPortrait,
        MikoScreenOrientation.Landscape => AndroidScreenOrientation.SensorLandscape,
        MikoScreenOrientation.PortraitPrimary => AndroidScreenOrientation.Portrait,
        MikoScreenOrientation.PortraitSecondary => AndroidScreenOrientation.ReversePortrait,
        MikoScreenOrientation.LandscapePrimary => AndroidScreenOrientation.Landscape,
        MikoScreenOrientation.LandscapeSecondary => AndroidScreenOrientation.ReverseLandscape,
        _ => AndroidScreenOrientation.Unspecified,
    };
}
