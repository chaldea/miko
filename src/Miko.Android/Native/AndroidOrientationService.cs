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
/// <b>锁定与解锁（ISSUE-149）：</b>首次锁定时记下 Activity 原本的 <c>RequestedOrientation</c>，
/// 全部解锁时恢复它——而不是一律写 <c>Unspecified</c>，否则在清单里声明了竖屏的 Activity
/// 退出全屏后就变成了可以自由旋转。<see cref="Lock"/> 不指定方向时映射为
/// <c>Locked</c>（锁定在当前方向）。
/// </para>
///
/// <para>
/// <b>应用侧前提：</b>Activity 默认会在旋转时销毁重建，连同整个 Miko 视图和应用状态——
/// 锁定横屏会让正在全屏的播放器回到初始页面。需要旋转的应用要在 Activity 上声明
/// <c>ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | …</c>，
/// 由视图自己处理尺寸变化。
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

    // 首次锁定前 Activity 的 RequestedOrientation，全部解锁时恢复。只在 UI 线程读写。
    private AndroidScreenOrientation? _restoreOrientation;

    public AndroidOrientationService(INativeHostContext hostContext) => _hostContext = hostContext;

    public void Lock(MikoScreenOrientation? orientation = null)
    {
        lock (_sync)
        {
            _lockDepth++;
            // 只有最外层锁定生效：嵌套锁定中内层的方向被忽略，最外层解锁前方向保持不变。
            if (_lockDepth != 1) return;
        }

        var target = orientation is { } requested
            ? ToAndroidOrientation(requested)
            : AndroidScreenOrientation.Locked;

        RunOnUiThread(activity =>
        {
            _restoreOrientation ??= activity.RequestedOrientation;
            activity.RequestedOrientation = target;
        });
    }

    public void Unlock()
    {
        lock (_sync)
        {
            if (_lockDepth == 0) return;
            _lockDepth--;
            if (_lockDepth != 0) return;
        }

        RunOnUiThread(activity =>
        {
            activity.RequestedOrientation = _restoreOrientation ?? AndroidScreenOrientation.Unspecified;
            _restoreOrientation = null;
        });
    }

    public Task SetAsync(MikoScreenOrientation orientation)
    {
        lock (_sync)
        {
            // 锁定期间忽略 Set（与 iOS/Android 的锁定语义一致）。
            if (_lockDepth > 0) return Task.CompletedTask;
        }

        var target = ToAndroidOrientation(orientation);
        RunOnUiThread(activity => activity.RequestedOrientation = target);
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
    /// 映射到 Android 的 <c>RequestedOrientation</c>。<c>Portrait</c>/<c>Landscape</c> 不区分正反，
    /// 用 <c>Sensor</c> 系列让设备在两个朝向间自由切换但仍受限于该轴。
    /// </summary>
    private static AndroidScreenOrientation ToAndroidOrientation(MikoScreenOrientation orientation) => orientation switch
    {
        MikoScreenOrientation.Portrait => AndroidScreenOrientation.SensorPortrait,
        MikoScreenOrientation.Landscape => AndroidScreenOrientation.SensorLandscape,
        MikoScreenOrientation.PortraitPrimary => AndroidScreenOrientation.Portrait,
        MikoScreenOrientation.PortraitSecondary => AndroidScreenOrientation.ReversePortrait,
        MikoScreenOrientation.LandscapePrimary => AndroidScreenOrientation.Landscape,
        MikoScreenOrientation.LandscapeSecondary => AndroidScreenOrientation.ReverseLandscape,
        _ => AndroidScreenOrientation.Unspecified,
    };
}
