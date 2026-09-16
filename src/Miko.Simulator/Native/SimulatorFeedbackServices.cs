using Microsoft.Extensions.Logging;
using Miko.Native;
using Miko.Native.Haptics;
using Miko.Native.ScreenReader;
using Miko.Native.SplashScreen;
using Miko.Native.StatusBar;
using Miko.Native.Toast;

namespace Miko.Simulator.Native;

/// <summary>
/// 模拟器触觉反馈。桌面没有振动马达，能做的只有把每次反馈记进日志——
/// 这已经足以验证「什么时候该触发触觉」这条业务逻辑。
/// </summary>
internal sealed class SimulatorHapticsService : SimulatorNativeServiceBase, IHapticsService
{
    public SimulatorHapticsService(INativeHostContext hostContext, ILogger<SimulatorHapticsService>? logger)
        : base(hostContext, logger) { }

    public Task ImpactAsync(ImpactOptions? options = null)
    {
        Log(nameof(ImpactAsync), (options?.Style ?? ImpactStyle.Medium).ToString());
        return Task.CompletedTask;
    }

    public Task NotificationAsync(NotificationOptions? options = null)
    {
        Log(nameof(NotificationAsync), (options?.Type ?? NotificationType.Success).ToString());
        return Task.CompletedTask;
    }

    public Task VibrateAsync(VibrateOptions? options = null)
    {
        Log(nameof(VibrateAsync), $"{options?.Duration ?? 300}ms");
        return Task.CompletedTask;
    }

    public Task SelectionStartAsync() { Log(nameof(SelectionStartAsync)); return Task.CompletedTask; }
    public Task SelectionChangedAsync() { Log(nameof(SelectionChangedAsync)); return Task.CompletedTask; }
    public Task SelectionEndAsync() { Log(nameof(SelectionEndAsync)); return Task.CompletedTask; }
}

/// <summary>模拟器 Toast：记录日志。文本与时长可从日志核对。</summary>
internal sealed class SimulatorToastService : SimulatorNativeServiceBase, IToastService
{
    public SimulatorToastService(INativeHostContext hostContext, ILogger<SimulatorToastService>? logger)
        : base(hostContext, logger) { }

    public Task ShowAsync(ToastOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Log(nameof(ShowAsync), $"\"{options.Text}\" {options.Duration}/{options.Position}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 模拟器状态栏。模拟器把设备安全区画在离屏画面上，状态栏本身不可由应用显隐，
/// 因此这里维护一份内存状态并如实回读——应用的状态栏逻辑得以完整验证。
/// </summary>
internal sealed class SimulatorStatusBarService : SimulatorNativeServiceBase, IStatusBarService
{
    private readonly Lock _gate = new();
    private bool _visible = true;
    private StatusBarStyle _style = StatusBarStyle.Default;
    private string _color = "#00000000";
    private bool _overlays = true;

    public SimulatorStatusBarService(INativeHostContext hostContext, ILogger<SimulatorStatusBarService>? logger)
        : base(hostContext, logger) { }

    public Task SetStyleAsync(StatusBarStyleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        lock (_gate) _style = options.Style;
        Log(nameof(SetStyleAsync), options.Style.ToString());
        return Task.CompletedTask;
    }

    public Task SetBackgroundColorAsync(BackgroundColorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        lock (_gate) _color = options.Color;
        Log(nameof(SetBackgroundColorAsync), options.Color);
        return Task.CompletedTask;
    }

    public Task ShowAsync(AnimationOptions? options = null)
    {
        SetVisible(true);
        Log(nameof(ShowAsync));
        return Task.CompletedTask;
    }

    public Task HideAsync(AnimationOptions? options = null)
    {
        SetVisible(false);
        Log(nameof(HideAsync));
        return Task.CompletedTask;
    }

    public Task<StatusBarInfo> GetInfoAsync() => Task.FromResult(Snapshot());

    public Task SetOverlaysWebViewAsync(SetOverlaysWebViewOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        StatusBarInfo snapshot;
        lock (_gate)
        {
            _overlays = options.Overlay;
            snapshot = SnapshotLocked();
        }

        Log(nameof(SetOverlaysWebViewAsync), options.Overlay.ToString());
        OnOverlayChanged?.Invoke(snapshot);
        return Task.CompletedTask;
    }

    public event Action<StatusBarInfo>? OnVisibilityChanged;
    public event Action<StatusBarInfo>? OnOverlayChanged;

    private void SetVisible(bool visible)
    {
        StatusBarInfo snapshot;
        lock (_gate)
        {
            if (_visible == visible) return;
            _visible = visible;
            snapshot = SnapshotLocked();
        }

        // 在锁外触发：处理器可能同步回调 GetInfoAsync。
        OnVisibilityChanged?.Invoke(snapshot);
    }

    private StatusBarInfo Snapshot()
    {
        lock (_gate) return SnapshotLocked();
    }

    private StatusBarInfo SnapshotLocked()
        // 高度取当前设备的顶部安全区——模拟器就是用它来画刘海/状态栏区域的。
        => new(_visible, _style, _color, _overlays, Device.SafeArea.Top);
}

/// <summary>
/// 模拟器启动画面。模拟器不绘制启动画面，维护内存状态并记录日志，
/// 让应用的「隐藏启动画面」时序逻辑可被验证。
/// </summary>
internal sealed class SimulatorSplashScreenService : SimulatorNativeServiceBase, ISplashScreenService
{
    public SimulatorSplashScreenService(INativeHostContext hostContext, ILogger<SimulatorSplashScreenService>? logger)
        : base(hostContext, logger) { }

    public Task ShowAsync(ShowSplashOptions? options = null)
    {
        Log(nameof(ShowAsync), options is null ? null : $"autoHide={options.AutoHide} show={options.ShowDuration}ms");
        return Task.CompletedTask;
    }

    public Task HideAsync(HideSplashOptions? options = null)
    {
        Log(nameof(HideAsync), options is null ? null : $"fadeOut={options.FadeOutDuration}ms");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 模拟器屏幕阅读器。模拟器不接系统读屏，<c>IsEnabled</c> 恒为 <c>false</c>，
/// 朗读内容记入日志——可验证应用在什么时机朗读了什么。
/// </summary>
internal sealed class SimulatorScreenReaderService : SimulatorNativeServiceBase, IScreenReaderService
{
    public SimulatorScreenReaderService(INativeHostContext hostContext, ILogger<SimulatorScreenReaderService>? logger)
        : base(hostContext, logger) { }

    public Task<ScreenReaderState> IsEnabledAsync() => Task.FromResult(new ScreenReaderState(false));

    public Task SpeakAsync(SpeakOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Log(nameof(SpeakAsync), $"\"{options.Value}\" lang={options.Language ?? "-"}");
        return Task.CompletedTask;
    }

    /// <summary>模拟器的读屏状态恒定，永不触发。</summary>
    public event Action<ScreenReaderState>? OnStateChange { add { } remove { } }
}
