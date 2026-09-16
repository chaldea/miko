using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using Miko.Native;
using Miko.Native.Haptics;
using Miko.Native.ScreenReader;
using Miko.Native.StatusBar;
using Miko.Native.Toast;
using AndroidToast = Android.Widget.Toast;
using AndroidView = Android.Views.View;

namespace Miko.Android.Native;

/// <summary>Android Toast，基于 <see cref="AndroidToast"/>。</summary>
internal sealed class AndroidToastService : NativeServiceBase, IToastService
{
    private readonly INativeHostContext _hostContext;

    public AndroidToastService(INativeHostContext hostContext) => _hostContext = hostContext;

    public Task ShowAsync(ToastOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var host = _hostContext.RequireHost<AndroidNativeHost>();
        var length = options.Duration == ToastDuration.Long ? ToastLength.Long : ToastLength.Short;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Toast 必须在 UI 线程创建/显示。
        RunOnUiThread(host, () =>
        {
            try
            {
                var toast = AndroidToast.MakeText(host.Context, options.Text, length);

                // Android 11 (API 30) 起 setGravity 对文本 Toast 无效，系统固定显示在底部。
                // 低版本上仍按请求定位。
                if (toast is not null && !OperatingSystem.IsAndroidVersionAtLeast(30))
                {
                    var gravity = options.Position switch
                    {
                        ToastPosition.Top => GravityFlags.Top | GravityFlags.CenterHorizontal,
                        ToastPosition.Center => GravityFlags.Center,
                        _ => GravityFlags.Bottom | GravityFlags.CenterHorizontal,
                    };
                    toast.SetGravity(gravity, 0, 0);
                }

                toast?.Show();
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    // 参数类型写全限定：本文件 using 了 Android.Views.Accessibility，其中也有一个 Action 类型。
    internal static void RunOnUiThread(AndroidNativeHost host, System.Action action)
    {
        if (host.Activity is { } activity) activity.RunOnUiThread(action);
        else new Handler(Looper.MainLooper!).Post(action);
    }
}

/// <summary>Android 触觉与振动，基于 <see cref="Vibrator"/>。</summary>
internal sealed class AndroidHapticsService : NativeServiceBase, IHapticsService
{
    private readonly INativeHostContext _hostContext;

    public AndroidHapticsService(INativeHostContext hostContext) => _hostContext = hostContext;

    private AndroidNativeHost Host => _hostContext.RequireHost<AndroidNativeHost>();

    public Task ImpactAsync(ImpactOptions? options = null)
    {
        // Android 没有 iOS 那套 impact generator，用不同时长的振动近似三档强度。
        var duration = (options?.Style ?? ImpactStyle.Medium) switch
        {
            ImpactStyle.Heavy => 40,
            ImpactStyle.Light => 10,
            _ => 20,
        };

        return VibrateForAsync(duration);
    }

    public Task NotificationAsync(NotificationOptions? options = null)
    {
        // 通知触觉用节奏区分类型：成功一下、警告两下、错误三下。
        var pattern = (options?.Type ?? NotificationType.Success) switch
        {
            NotificationType.Warning => new long[] { 0, 30, 80, 30 },
            NotificationType.Error => new long[] { 0, 30, 80, 30, 80, 30 },
            _ => new long[] { 0, 30 },
        };

        return VibratePatternAsync(pattern);
    }

    public Task VibrateAsync(VibrateOptions? options = null) => VibrateForAsync(options?.Duration ?? 300);

    /// <summary>选择反馈在 Android 上是一次极短的轻振。</summary>
    public Task SelectionStartAsync() => VibrateForAsync(10);
    public Task SelectionChangedAsync() => VibrateForAsync(10);

    /// <summary>选择结束无反馈（与 Android 系统控件一致）。</summary>
    public Task SelectionEndAsync() => Task.CompletedTask;

    private Task VibrateForAsync(int milliseconds)
    {
        var vibrator = GetVibrator();
        if (vibrator is null || !vibrator.HasVibrator) return Task.CompletedTask;

        vibrator.Vibrate(VibrationEffect.CreateOneShot(milliseconds, VibrationEffect.DefaultAmplitude)!);
        return Task.CompletedTask;
    }

    private Task VibratePatternAsync(long[] pattern)
    {
        var vibrator = GetVibrator();
        if (vibrator is null || !vibrator.HasVibrator) return Task.CompletedTask;

        // repeat = -1：只播一遍，不循环。
        vibrator.Vibrate(VibrationEffect.CreateWaveform(pattern, -1)!);
        return Task.CompletedTask;
    }

    private Vibrator? GetVibrator()
    {
        var context = Host.Context;

        // API 31 起 Vibrator 要经 VibratorManager 获取；旧路径在新系统上已过时。
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            return (context.GetSystemService(Context.VibratorManagerService) as VibratorManager)?.DefaultVibrator;
        }

#pragma warning disable CA1422 // 低版本系统上这是唯一入口
        return context.GetSystemService(Context.VibratorService) as Vibrator;
#pragma warning restore CA1422
    }
}

/// <summary>
/// Android 屏幕阅读器（TalkBack）状态与朗读。
/// 朗读走无障碍事件公告，而不是引入 TextToSpeech 引擎——后者需要额外的初始化与生命周期管理，
/// 且公告正是「让读屏念一句话」的标准做法。
/// </summary>
internal sealed class AndroidScreenReaderService : NativeServiceBase, IScreenReaderService
{
    private readonly INativeHostContext _hostContext;

    public AndroidScreenReaderService(INativeHostContext hostContext) => _hostContext = hostContext;

    private AndroidNativeHost Host => _hostContext.RequireHost<AndroidNativeHost>();

    private AccessibilityManager? Manager =>
        Host.Context.GetSystemService(Context.AccessibilityService) as AccessibilityManager;

    public Task<ScreenReaderState> IsEnabledAsync()
    {
        var manager = Manager;

        // 必须同时满足「无障碍开启」和「有朗读型服务」——只看 IsEnabled 会把放大镜之类
        // 的非朗读服务也算成读屏。
        var enabled = manager is { IsEnabled: true, IsTouchExplorationEnabled: true };

        return Task.FromResult(new ScreenReaderState(enabled));
    }

    public Task SpeakAsync(SpeakOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var manager = Manager;
        if (manager is null || !manager.IsEnabled) return Task.CompletedTask;

        var host = Host;

        AndroidToastService.RunOnUiThread(host, () =>
        {
            // 公告事件由当前窗口发出；没有 Activity 时无从发送。
            var root = host.Activity?.Window?.DecorView;
            if (root is null) return;

            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                root.AnnounceForAccessibility(options.Value);
            }
            else
            {
#pragma warning disable CA1422 // 低版本回退路径
                var e = AccessibilityEvent.Obtain(EventTypes.Announcement);
                if (e is null) return;
                e.Text?.Add(new Java.Lang.String(options.Value));
                manager.SendAccessibilityEvent(e);
#pragma warning restore CA1422
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// TalkBack 开关变化需要注册 <c>AccessibilityStateChangeListener</c>；
    /// 当前实现只提供查询，不订阅系统变化。
    /// </summary>
    public event Action<ScreenReaderState>? OnStateChange { add { } remove { } }
}

/// <summary>
/// Android 状态栏。
/// <para>
/// 注意 Android 15 (API 35) 起强制 edge-to-edge：<c>SetStatusBarColor</c> 与
/// 「是否覆盖内容」都不再生效，系统栏永远覆盖在内容之上。这与原始文档中的标注一致。
/// </para>
/// </summary>
internal sealed class AndroidStatusBarService : NativeServiceBase, IStatusBarService
{
    private readonly INativeHostContext _hostContext;
    private StatusBarStyle _style = StatusBarStyle.Default;
    private string _color = "#00000000";

    public AndroidStatusBarService(INativeHostContext hostContext) => _hostContext = hostContext;

    private AndroidNativeHost Host => _hostContext.RequireHost<AndroidNativeHost>();

    public Task SetStyleAsync(StatusBarStyleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var host = Host;
        _style = options.Style;

        AndroidToastService.RunOnUiThread(host, () =>
        {
            var window = host.Activity?.Window;
            if (window is null) return;

            // Capacitor 的 Dark/Light 指的是**内容**（文字/图标）颜色。
            // Android 的 APPEARANCE_LIGHT_STATUS_BARS 表示「浅色背景 → 深色图标」，
            // 因此 Style.Dark（深色文字）对应 light-bars = true。
            var lightBars = options.Style == StatusBarStyle.Dark;

            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                window.InsetsController?.SetSystemBarsAppearance(
                    lightBars ? (int)WindowInsetsControllerAppearance.LightStatusBars : 0,
                    (int)WindowInsetsControllerAppearance.LightStatusBars);
            }
            else
            {
#pragma warning disable CA1422 // 低版本回退路径
                // 用 SystemUiFlags 而非已过时的 SystemUiVisibility：后者的枚举类型不对，
                // 迫使每次读写都在 int 与 StatusBarVisibility 之间来回强转。
                var decor = window.DecorView;
                decor.SystemUiFlags = lightBars
                    ? decor.SystemUiFlags | SystemUiFlags.LightStatusBar
                    : decor.SystemUiFlags & ~SystemUiFlags.LightStatusBar;
#pragma warning restore CA1422
            }
        });

        return Task.CompletedTask;
    }

    public Task SetBackgroundColorAsync(BackgroundColorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Android 15+ 强制 edge-to-edge，状态栏背景色不再可设；如实抛出而不是假装成功。
        if (OperatingSystem.IsAndroidVersionAtLeast(35))
            throw new PlatformNotSupportedException(
                "Android 15 (API 35) enforces edge-to-edge; the status bar background colour can no longer be set.");

        var host = Host;
        _color = options.Color;

        AndroidToastService.RunOnUiThread(host, () =>
        {
            var window = host.Activity?.Window;
            if (window is null) return;

#pragma warning disable CA1422 // API 35 起过时，此分支只在更低版本执行
            window.SetStatusBarColor(global::Android.Graphics.Color.ParseColor(options.Color));
#pragma warning restore CA1422
        });

        return Task.CompletedTask;
    }

    public Task ShowAsync(AnimationOptions? options = null) => SetStatusBarVisibleAsync(true);

    public Task HideAsync(AnimationOptions? options = null) => SetStatusBarVisibleAsync(false);

    private Task SetStatusBarVisibleAsync(bool visible)
    {
        var host = Host;

        AndroidToastService.RunOnUiThread(host, () =>
        {
            var window = host.Activity?.Window;
            if (window is null) return;

            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var controller = window.InsetsController;
                if (visible) controller?.Show(WindowInsets.Type.StatusBars());
                else controller?.Hide(WindowInsets.Type.StatusBars());
            }
            else
            {
#pragma warning disable CA1422 // 低版本回退路径
                var decor = window.DecorView;
                decor.SystemUiFlags = visible
                    ? decor.SystemUiFlags & ~SystemUiFlags.Fullscreen
                    : decor.SystemUiFlags | SystemUiFlags.Fullscreen;
#pragma warning restore CA1422
            }

            OnVisibilityChanged?.Invoke(BuildInfo(visible));
        });

        return Task.CompletedTask;
    }

    public Task<StatusBarInfo> GetInfoAsync()
    {
        var host = Host;
        var window = host.Activity?.Window;
        var visible = true;
        double height = 0;

        if (window is not null && OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var insets = window.DecorView.RootWindowInsets;
            if (insets is not null)
            {
                var bars = insets.GetInsets(WindowInsets.Type.StatusBars());
                var density = host.Context.Resources?.DisplayMetrics?.Density ?? 1f;
                height = bars.Top / density;
                visible = bars.Top > 0;
            }
        }

        return Task.FromResult(new StatusBarInfo(visible, _style, _color, Overlays: true, Height: height));
    }

    public Task SetOverlaysWebViewAsync(SetOverlaysWebViewOptions options)
    {
        // Miko 始终 edge-to-edge 绘制（宿主用安全区内缩内容），系统栏本就覆盖在内容之上；
        // Android 15+ 更是强制如此。该开关无法实现，明确抛出。
        throw new PlatformNotSupportedException(
            "Miko always draws edge-to-edge on Android; the status bar always overlays the content.");
    }

    public event Action<StatusBarInfo>? OnVisibilityChanged;

    /// <summary>覆盖状态不可变（始终覆盖），永不触发。</summary>
    public event Action<StatusBarInfo>? OnOverlayChanged { add { } remove { } }

    private StatusBarInfo BuildInfo(bool visible) => new(visible, _style, _color, Overlays: true, Height: 0);
}
