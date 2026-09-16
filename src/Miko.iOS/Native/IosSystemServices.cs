using CoreLocation;
using CoreMotion;
using Foundation;
using Miko.Native;
using Miko.Native.Keyboard;
using Miko.Native.Motion;
using Miko.Native.Network;
using Miko.Native.StatusBar;
using Miko.Platform;
using Network;
using UIKit;
using NativePosition = Miko.Native.Geolocation.Position;

namespace Miko.iOS.Native;

/// <summary>
/// iOS 键盘。
/// <para>
/// Miko 自绘 UI、不使用 WebView，因此 <c>SetScroll</c>/<c>SetResizeMode</c> 这类针对
/// WKWebView 的 API 无对应实现；键盘显隐事件则来自系统通知，是真实可用的。
/// </para>
/// </summary>
internal sealed class IosKeyboardService : NativeServiceBase, IKeyboardService, IDisposable
{
    private readonly IInputMethodService _inputMethod;
    private readonly Lock _gate = new();
    private readonly List<NSObject> _observers = new();

    private Action<KeyboardInfo>? _willShow;
    private Action<KeyboardInfo>? _didShow;
    private Action? _willHide;
    private Action? _didHide;

    public IosKeyboardService(IInputMethodService inputMethod) => _inputMethod = inputMethod;

    /// <summary>iOS 的软键盘由第一响应者驱动；转发给 IME 端点以呈现键盘。</summary>
    public Task ShowAsync()
    {
        _inputMethod.ShowKeyboard();
        return Task.CompletedTask;
    }

    public Task HideAsync()
    {
        UIApplication.SharedApplication.InvokeOnMainThread(() =>
            // 让当前第一响应者辞职即收起键盘，不必知道具体是哪个视图。
            UIApplication.SharedApplication.KeyWindow?.EndEditing(true));

        // 同时清空引擎的文本客户端，避免下一帧又把键盘唤起来。
        _inputMethod.SetState(null);
        return Task.CompletedTask;
    }

    /// <summary>辅助栏属于 WKWebView 的输入视图，Miko 自绘 UI 无对应实现。</summary>
    public Task SetAccessoryBarVisibleAsync(bool isVisible) => UnsupportedAsync();

    /// <summary>针对 WKWebView 的滚动开关；Miko 不使用 WebView。</summary>
    public Task SetScrollAsync(bool isDisabled) => UnsupportedAsync();

    /// <summary>键盘外观由文本输入视图的 <c>KeyboardAppearance</c> 决定，未经由本服务暴露。</summary>
    public Task SetStyleAsync(KeyboardStyleOptions options) => UnsupportedAsync();

    /// <summary>调整模式描述 WebView 容器如何随键盘改变尺寸；Miko 不使用 WebView。</summary>
    public Task SetResizeModeAsync(KeyboardResizeOptions options) => UnsupportedAsync();
    public Task<KeyboardResizeOptions> GetResizeModeAsync() => UnsupportedAsync<KeyboardResizeOptions>();

    public event Action<KeyboardInfo>? OnKeyboardWillShow
    {
        add { if (value is not null) { lock (_gate) { _willShow += value; EnsureObservers(); } } }
        remove { if (value is not null) { lock (_gate) { _willShow -= value; StopIfIdle(); } } }
    }

    public event Action<KeyboardInfo>? OnKeyboardDidShow
    {
        add { if (value is not null) { lock (_gate) { _didShow += value; EnsureObservers(); } } }
        remove { if (value is not null) { lock (_gate) { _didShow -= value; StopIfIdle(); } } }
    }

    public event Action? OnKeyboardWillHide
    {
        add { if (value is not null) { lock (_gate) { _willHide += value; EnsureObservers(); } } }
        remove { if (value is not null) { lock (_gate) { _willHide -= value; StopIfIdle(); } } }
    }

    public event Action? OnKeyboardDidHide
    {
        add { if (value is not null) { lock (_gate) { _didHide += value; EnsureObservers(); } } }
        remove { if (value is not null) { lock (_gate) { _didHide -= value; StopIfIdle(); } } }
    }

    private void EnsureObservers()
    {
        if (_observers.Count > 0) return;

        var center = NSNotificationCenter.DefaultCenter;

        _observers.Add(center.AddObserver(UIKeyboard.WillShowNotification,
            n => Invoke(() => _willShow, KeyboardHeight(n))));
        _observers.Add(center.AddObserver(UIKeyboard.DidShowNotification,
            n => Invoke(() => _didShow, KeyboardHeight(n))));
        _observers.Add(center.AddObserver(UIKeyboard.WillHideNotification, _ => InvokeVoid(() => _willHide)));
        _observers.Add(center.AddObserver(UIKeyboard.DidHideNotification, _ => InvokeVoid(() => _didHide)));
    }

    private void StopIfIdle()
    {
        if (_willShow is not null || _didShow is not null || _willHide is not null || _didHide is not null) return;

        // 最后一个订阅者移除后注销系统观察者（要求第 2 条）。
        foreach (var observer in _observers)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
            observer.Dispose();
        }

        _observers.Clear();
    }

    private void Invoke(Func<Action<KeyboardInfo>?> selector, double height)
    {
        Action<KeyboardInfo>? handlers;
        lock (_gate) handlers = selector();

        handlers?.Invoke(new KeyboardInfo(height));
    }

    private void InvokeVoid(Func<Action?> selector)
    {
        Action? handlers;
        lock (_gate) handlers = selector();

        handlers?.Invoke();
    }

    private static double KeyboardHeight(NSNotification notification)
        => UIKeyboard.FrameEndFromNotification(notification).Height;

    public void Dispose()
    {
        lock (_gate)
        {
            _willShow = null;
            _didShow = null;
            _willHide = null;
            _didHide = null;
            StopIfIdle();
        }
    }
}

/// <summary>
/// iOS 网络状态，基于 Network.framework 的 <see cref="NWPathMonitor"/>。
/// <para>
/// 用 NWPathMonitor 而不是老的 SCNetworkReachability：前者能直接区分 Wi-Fi 与蜂窝
/// （<c>UsesInterfaceType</c>），后者只能告诉你「可达」与「需要蜂窝」。
/// </para>
/// </summary>
internal sealed class IosNetworkService : NativeServiceBase, INetworkService, IDisposable
{
    private readonly Lock _gate = new();
    private Action<ConnectionStatus>? _statusChanged;
    private NWPathMonitor? _monitor;
    private ConnectionStatus _latest = new(false, ConnectionType.Unknown);

    public Task<ConnectionStatus> GetStatusAsync()
    {
        // 已在监听时直接用最新快照；否则起一个一次性 monitor 取当前状态。
        lock (_gate)
        {
            if (_monitor is not null) return Task.FromResult(_latest);
        }

        var tcs = new TaskCompletionSource<ConnectionStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        var monitor = new NWPathMonitor();

        monitor.SnapshotHandler = path =>
        {
            if (!tcs.TrySetResult(Describe(path))) return;

            monitor.Cancel();
            monitor.Dispose();
        };

        monitor.SetQueue(CoreFoundation.DispatchQueue.DefaultGlobalQueue);
        monitor.Start();

        return tcs.Task;
    }

    public event Action<ConnectionStatus>? OnNetworkStatusChange
    {
        add
        {
            if (value is null) return;

            lock (_gate)
            {
                _statusChanged += value;
                if (_monitor is not null) return;

                _monitor = new NWPathMonitor();
                _monitor.SnapshotHandler = path =>
                {
                    var status = Describe(path);

                    Action<ConnectionStatus>? handlers;
                    lock (_gate)
                    {
                        _latest = status;
                        handlers = _statusChanged;
                    }

                    handlers?.Invoke(status);
                };

                _monitor.SetQueue(CoreFoundation.DispatchQueue.DefaultGlobalQueue);
                _monitor.Start();
            }
        }
        remove
        {
            if (value is null) return;

            lock (_gate)
            {
                _statusChanged -= value;
                if (_statusChanged is not null || _monitor is null) return;

                // 最后一个订阅者移除后停掉 monitor（要求第 2 条）。
                _monitor.Cancel();
                _monitor.Dispose();
                _monitor = null;
            }
        }
    }

    private static ConnectionStatus Describe(NWPath path)
    {
        if (path.Status != NWPathStatus.Satisfied)
            return new ConnectionStatus(false, ConnectionType.None);

        if (path.UsesInterfaceType(NWInterfaceType.Wifi) || path.UsesInterfaceType(NWInterfaceType.Wired))
            return new ConnectionStatus(true, ConnectionType.Wifi);

        if (path.UsesInterfaceType(NWInterfaceType.Cellular))
            return new ConnectionStatus(true, ConnectionType.Cellular);

        return new ConnectionStatus(true, ConnectionType.Unknown);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _statusChanged = null;
            _monitor?.Cancel();
            _monitor?.Dispose();
            _monitor = null;
        }
    }
}

/// <summary>
/// iOS 状态栏。
/// <para>
/// iOS 的状态栏样式/显隐由**视图控制器**决定（<c>PreferredStatusBarStyle</c> /
/// <c>PrefersStatusBarHidden</c>），不能像 Android 那样直接设值。因此这里把请求记在
/// <see cref="MikoViewController"/> 上，再调 <c>SetNeedsStatusBarAppearanceUpdate</c>
/// 让系统回头来问。
/// </para>
/// </summary>
internal sealed class IosStatusBarService : NativeServiceBase, IStatusBarService
{
    private readonly INativeHostContext _hostContext;

    public IosStatusBarService(INativeHostContext hostContext) => _hostContext = hostContext;

    private IosNativeHost Host => _hostContext.RequireHost<IosNativeHost>();

    public Task SetStyleAsync(StatusBarStyleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var controller = Host.ViewController as MikoViewController
            ?? throw new InvalidOperationException(
                $"The status bar can only be controlled from a {nameof(MikoViewController)}.");

        // Capacitor 的 Dark/Light 指**内容**颜色：Dark = 深色文字（浅色背景）。
        // UIKit 的 DarkContent 正是「深色内容」，语义一致。
        var style = options.Style switch
        {
            StatusBarStyle.Dark => UIStatusBarStyle.DarkContent,
            StatusBarStyle.Light => UIStatusBarStyle.LightContent,
            _ => UIStatusBarStyle.Default,
        };

        UIApplication.SharedApplication.InvokeOnMainThread(() => controller.SetStatusBarStyle(style));
        return Task.CompletedTask;
    }

    /// <summary>iOS 的状态栏没有独立背景色——它显示在应用内容之上。</summary>
    public Task SetBackgroundColorAsync(BackgroundColorOptions options) => UnsupportedAsync();

    public Task ShowAsync(AnimationOptions? options = null) => SetHiddenAsync(false, options);

    public Task HideAsync(AnimationOptions? options = null) => SetHiddenAsync(true, options);

    private Task SetHiddenAsync(bool hidden, AnimationOptions? options)
    {
        var controller = Host.ViewController as MikoViewController
            ?? throw new InvalidOperationException(
                $"The status bar can only be controlled from a {nameof(MikoViewController)}.");

        var animation = (options?.Animation ?? StatusBarAnimation.None) switch
        {
            StatusBarAnimation.Slide => UIStatusBarAnimation.Slide,
            StatusBarAnimation.Fade => UIStatusBarAnimation.Fade,
            _ => UIStatusBarAnimation.None,
        };

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            controller.SetStatusBarHidden(hidden, animation);
            OnVisibilityChanged?.Invoke(BuildInfo(controller));
        });

        return Task.CompletedTask;
    }

    public Task<StatusBarInfo> GetInfoAsync()
    {
        var controller = Host.ViewController as MikoViewController
            ?? throw new InvalidOperationException(
                $"The status bar can only be inspected from a {nameof(MikoViewController)}.");

        return Task.FromResult(BuildInfo(controller));
    }

    /// <summary>iOS 的状态栏始终浮在内容之上，无此开关。</summary>
    public Task SetOverlaysWebViewAsync(SetOverlaysWebViewOptions options) => UnsupportedAsync();

    public event Action<StatusBarInfo>? OnVisibilityChanged;

    /// <summary>覆盖状态不可变（始终覆盖），永不触发。</summary>
    public event Action<StatusBarInfo>? OnOverlayChanged { add { } remove { } }

    private static StatusBarInfo BuildInfo(MikoViewController controller)
    {
        var style = controller.PreferredStatusBarStyle() switch
        {
            UIStatusBarStyle.DarkContent => StatusBarStyle.Dark,
            UIStatusBarStyle.LightContent => StatusBarStyle.Light,
            _ => StatusBarStyle.Default,
        };

        var height = controller.View?.SafeAreaInsets.Top ?? 0;

        return new StatusBarInfo(
            Visible: !controller.PrefersStatusBarHidden(),
            Style: style,
            Color: "#00000000",
            Overlays: true,
            Height: height);
    }
}

/// <summary>
/// iOS 运动传感器，基于 <see cref="CMMotionManager"/>。
/// 只在有订阅者时启动更新，最后一个订阅者移除时停止——传感器常开是明显的耗电问题。
/// </summary>
internal sealed class IosMotionService : IMotionService, IDisposable
{
    private readonly Lock _gate = new();
    private readonly CMMotionManager _manager = new();

    private Action<AccelEvent>? _accel;
    private Action<RotationRate>? _orientation;

    public event Action<AccelEvent>? OnAccel
    {
        add { if (value is not null) { lock (_gate) { _accel += value; StartDeviceMotion(); } } }
        remove { if (value is not null) { lock (_gate) { _accel -= value; StopIfIdle(); } } }
    }

    public event Action<RotationRate>? OnOrientation
    {
        add { if (value is not null) { lock (_gate) { _orientation += value; StartDeviceMotion(); } } }
        remove { if (value is not null) { lock (_gate) { _orientation -= value; StopIfIdle(); } } }
    }

    private void StartDeviceMotion()
    {
        if (_manager.DeviceMotionActive || !_manager.DeviceMotionAvailable) return;

        // 60Hz：与渲染帧率同量级，足够驱动 UI 又不过度耗电。
        var interval = 1.0 / 60.0;
        _manager.DeviceMotionUpdateInterval = interval;

        _manager.StartDeviceMotionUpdates(NSOperationQueue.CurrentQueue ?? new NSOperationQueue(), (motion, _) =>
        {
            if (motion is null) return;

            Action<AccelEvent>? accelHandlers;
            Action<RotationRate>? orientationHandlers;
            lock (_gate)
            {
                accelHandlers = _accel;
                orientationHandlers = _orientation;
            }

            if (accelHandlers is not null)
            {
                // CoreMotion 的 UserAcceleration 单位是 G，接口约定 m/s²。
                const double g = 9.80665;
                var user = motion.UserAcceleration;
                var gravity = motion.Gravity;
                var rotation = motion.RotationRate;

                accelHandlers(new AccelEvent(
                    Acceleration: new Acceleration(user.X * g, user.Y * g, user.Z * g),
                    AccelerationIncludingGravity: new Acceleration(
                        (user.X + gravity.X) * g, (user.Y + gravity.Y) * g, (user.Z + gravity.Z) * g),
                    // RotationRate 是弧度/秒，接口约定度/秒。CMRotationRate 的字段是小写 x/y/z。
                    RotationRate: new RotationRate(
                        RadiansToDegrees(rotation.z), RadiansToDegrees(rotation.x), RadiansToDegrees(rotation.y)),
                    Interval: interval * 1000));
            }

            if (orientationHandlers is not null)
            {
                var attitude = motion.Attitude;
                orientationHandlers(new RotationRate(
                    RadiansToDegrees(attitude.Yaw), RadiansToDegrees(attitude.Pitch), RadiansToDegrees(attitude.Roll)));
            }
        });
    }

    private void StopIfIdle()
    {
        if (_accel is not null || _orientation is not null) return;
        if (_manager.DeviceMotionActive) _manager.StopDeviceMotionUpdates();
    }

    private static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;

    public void Dispose()
    {
        lock (_gate)
        {
            _accel = null;
            _orientation = null;
            StopIfIdle();
            _manager.Dispose();
        }
    }
}
