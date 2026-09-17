using System.Globalization;
using Foundation;
using Miko.Native;
using Miko.Native.App;
using Miko.Native.Browser;
using Miko.Native.Clipboard;
using Miko.Native.Device;
using Miko.Native.Haptics;
using Miko.Native.ScreenReader;
using Miko.Native.SplashScreen;
using Miko.Native.Toast;
using SafariServices;
using UIKit;
using DeviceOperatingSystem = Miko.Native.Device.OperatingSystem;

namespace Miko.iOS.Native;

/// <summary>iOS 剪贴板，基于 <see cref="UIPasteboard"/>。</summary>
internal sealed class IosClipboardService : NativeServiceBase, IClipboardService
{
    public Task WriteAsync(ClipboardWriteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var pasteboard = UIPasteboard.General;

        if (options.Image is { } image)
        {
            // Data URL 的 base64 载荷在逗号之后；纯 base64 时整串即载荷。
            var comma = image.IndexOf(',', StringComparison.Ordinal);
            var payload = comma >= 0 ? image[(comma + 1)..] : image;

            using var data = new NSData(payload, NSDataBase64DecodingOptions.None);
            using var uiImage = UIImage.LoadFromData(data)
                ?? throw new ArgumentException("ClipboardWriteOptions.Image is not a decodable image.", nameof(options));

            pasteboard.Image = uiImage;
            return Task.CompletedTask;
        }

        if (options.Url is { } url && options.String is null)
        {
            pasteboard.Url = new NSUrl(url);
            return Task.CompletedTask;
        }

        pasteboard.String = options.String
            ?? throw new ArgumentException("ClipboardWriteOptions must set String, Image or Url.", nameof(options));

        return Task.CompletedTask;
    }

    public Task<ClipboardReadResult> ReadAsync()
    {
        var pasteboard = UIPasteboard.General;

        if (pasteboard.Image is { } image)
        {
            // 与写入对称：图片以 Data URL 形式返回，调用方可直接用于 <img>。
            using var png = image.AsPNG();
            var base64 = png?.GetBase64EncodedString(NSDataBase64EncodingOptions.None) ?? string.Empty;
            return Task.FromResult(new ClipboardReadResult($"data:image/png;base64,{base64}", "image/png"));
        }

        if (pasteboard.Url is { } url)
            return Task.FromResult(new ClipboardReadResult(url.AbsoluteString ?? string.Empty, "text/uri-list"));

        return Task.FromResult(new ClipboardReadResult(pasteboard.String ?? string.Empty, "text/plain"));
    }
}

/// <summary>iOS 设备信息，基于 <see cref="UIDevice"/>。</summary>
internal sealed class IosDeviceService : NativeServiceBase, IDeviceService
{
    public Task<DeviceId> GetIdAsync()
        // identifierForVendor：同一开发者的应用共享，卸载全部应用后会重置——非硬件 ID。
        => Task.FromResult(new DeviceId(
            UIDevice.CurrentDevice.IdentifierForVendor?.AsString() ?? Guid.NewGuid().ToString("n")));

    public Task<DeviceInfo> GetInfoAsync()
    {
        var device = UIDevice.CurrentDevice;
        var systemVersion = device.SystemVersion ?? "0";

        // SystemVersion 形如 "17.4.1"，取主版本号。
        int? majorVersion = int.TryParse(systemVersion.Split('.')[0], out var major) ? major : null;

        return Task.FromResult(new DeviceInfo(
            Name: device.Name ?? "iPhone",
            Model: device.Model ?? "iPhone",
            Platform: DevicePlatform.Ios,
            OperatingSystem: DeviceOperatingSystem.Ios,
            OsVersion: systemVersion,
            IosVersion: majorVersion,
            AndroidSdkVersion: null,
            Manufacturer: "Apple",
            // 模拟器上 SIMULATOR_DEVICE_NAME 环境变量存在，这是判断 iOS 模拟器的常用手法。
            IsVirtual: Environment.GetEnvironmentVariable("SIMULATOR_DEVICE_NAME") is not null,
            MemUsed: Environment.WorkingSet,
            WebViewVersion: "Miko.iOS"));
    }

    public Task<BatteryInfo> GetBatteryInfoAsync()
    {
        var device = UIDevice.CurrentDevice;

        // 不打开监控时 BatteryLevel 恒为 -1；用完恢复原状，避免影响宿主应用的设置。
        var wasEnabled = device.BatteryMonitoringEnabled;
        device.BatteryMonitoringEnabled = true;

        try
        {
            var level = device.BatteryLevel;
            var state = device.BatteryState;

            if (level < 0)
                throw new InvalidOperationException("Battery level is unavailable (unsupported on the simulator).");

            return Task.FromResult(new BatteryInfo(
                BatteryLevel: level,
                IsCharging: state is UIDeviceBatteryState.Charging or UIDeviceBatteryState.Full));
        }
        finally
        {
            device.BatteryMonitoringEnabled = wasEnabled;
        }
    }

    public Task<LanguageCode> GetLanguageCodeAsync()
        => Task.FromResult(new LanguageCode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));

    public Task<LanguageTag> GetLanguageTagAsync()
        => Task.FromResult(new LanguageTag(CultureInfo.CurrentUICulture.IetfLanguageTag));
}

/// <summary>iOS 应用信息，基于主 Bundle 的 Info.plist。</summary>
internal sealed class IosAppService : NativeServiceBase, IAppService
{
    private readonly INativeHostContext _hostContext;

    public IosAppService(INativeHostContext hostContext) => _hostContext = hostContext;

    /// <summary>iOS 不允许应用主动退出（主动调用会被 App Store 审核拒绝）。</summary>
    public Task ExitAppAsync() => UnsupportedAsync();

    /// <summary>iOS 没有「最小化」的公开 API。</summary>
    public Task MinimizeAppAsync() => UnsupportedAsync();

    /// <summary>iOS 没有硬件返回键。</summary>
    public Task ToggleBackButtonHandlerAsync(ToggleBackButtonHandlerOptions options) => UnsupportedAsync();

    public Task<AppInfo> GetInfoAsync()
    {
        var info = NSBundle.MainBundle.InfoDictionary;

        string Read(string key) => info?[key]?.ToString() ?? string.Empty;

        return Task.FromResult(new AppInfo(
            Name: Read("CFBundleDisplayName") is { Length: > 0 } display ? display : Read("CFBundleName"),
            Id: NSBundle.MainBundle.BundleIdentifier ?? string.Empty,
            Build: Read("CFBundleVersion"),
            Version: Read("CFBundleShortVersionString")));
    }

    public Task<AppState> GetStateAsync()
        => Task.FromResult(new AppState(UIApplication.SharedApplication.ApplicationState == UIApplicationState.Active));

    /// <summary>
    /// 启动 URL 由 <c>AppDelegate</c> 的 <c>OpenUrl</c>/<c>FinishedLaunching</c> 收到，
    /// 宿主未把它转发给 Native 层，因此无从得知。
    /// </summary>
    public Task<AppLaunchUrl?> GetLaunchUrlAsync() => Task.FromResult<AppLaunchUrl?>(null);

    public Task<AppLanguageCode> GetAppLanguageAsync()
        => Task.FromResult(new AppLanguageCode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));

    /// <summary>生命周期事件需要 <c>AppDelegate</c> 主动转发；当前宿主未接线。</summary>
    public event Action<AppState>? OnAppStateChange { add { } remove { } }
    public event Action? OnPause { add { } remove { } }
    public event Action? OnResume { add { } remove { } }
    public event Action<UrlOpenEvent>? OnAppUrlOpen { add { } remove { } }

    /// <summary>Android 专有。</summary>
    public event Action<RestoredResultEvent>? OnAppRestoredResult { add { } remove { } }
    public event Action<BackButtonEvent>? OnBackButton { add { } remove { } }
}

/// <summary>iOS 内嵌浏览器，基于 <see cref="SFSafariViewController"/>。</summary>
internal sealed class IosBrowserService : NativeServiceBase, IBrowserService
{
    private readonly INativeHostContext _hostContext;
    private SFSafariViewController? _current;

    public IosBrowserService(INativeHostContext hostContext) => _hostContext = hostContext;

    public Task OpenAsync(OpenOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Url))
            throw new ArgumentException("OpenOptions.Url is required.", nameof(options));

        var host = _hostContext.RequireHost<IosNativeHost>();
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // UIKit 只能在主线程操作。
        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            try
            {
                var safari = new SFSafariViewController(new NSUrl(options.Url));

                if (options.PresentationStyle == BrowserPresentationStyle.Popover)
                    safari.ModalPresentationStyle = UIModalPresentationStyle.Popover;

                if (options.ToolbarColor is { } color)
                    safari.PreferredBarTintColor = ParseColor(color);

                safari.Delegate = new SafariDelegate(this);

                _current = safari;
                host.TopViewController.PresentViewController(safari, animated: true, completionHandler: null);
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    public Task CloseAsync()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            var current = _current;
            _current = null;

            if (current is null) { tcs.TrySetResult(); return; }

            current.DismissViewController(animated: true, completionHandler: () => tcs.TrySetResult());
        });

        return tcs.Task;
    }

    public event Action? OnBrowserFinished;
    public event Action? OnBrowserPageLoaded;

    private static UIColor ParseColor(string hex)
    {
        var value = hex.TrimStart('#');
        if (value.Length is not (6 or 8))
            throw new ArgumentException($"Unsupported colour format: {hex}", nameof(hex));

        var r = Convert.ToByte(value[..2], 16) / 255f;
        var g = Convert.ToByte(value[2..4], 16) / 255f;
        var b = Convert.ToByte(value[4..6], 16) / 255f;
        var a = value.Length == 8 ? Convert.ToByte(value[6..8], 16) / 255f : 1f;

        return UIColor.FromRGBA(r, g, b, a);
    }

    /// <summary>把 Safari 视图的回调桥接为服务事件。</summary>
    private sealed class SafariDelegate : SFSafariViewControllerDelegate
    {
        private readonly IosBrowserService _owner;

        public SafariDelegate(IosBrowserService owner) => _owner = owner;

        public override void DidFinish(SFSafariViewController controller)
        {
            _owner._current = null;
            _owner.OnBrowserFinished?.Invoke();
        }

        public override void DidCompleteInitialLoad(SFSafariViewController controller, bool didLoadSuccessfully)
        {
            if (didLoadSuccessfully) _owner.OnBrowserPageLoaded?.Invoke();
        }
    }
}

/// <summary>iOS 触觉反馈，基于 UIKit 的 feedback generator。</summary>
internal sealed class IosHapticsService : NativeServiceBase, IHapticsService
{
    private UISelectionFeedbackGenerator? _selection;

    public Task ImpactAsync(ImpactOptions? options = null)
    {
        var style = (options?.Style ?? ImpactStyle.Medium) switch
        {
            ImpactStyle.Heavy => UIImpactFeedbackStyle.Heavy,
            ImpactStyle.Light => UIImpactFeedbackStyle.Light,
            _ => UIImpactFeedbackStyle.Medium,
        };

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            using var generator = new UIImpactFeedbackGenerator(style);
            // Prepare 让触觉引擎提前预热，否则首次触发会有明显延迟。
            generator.Prepare();
            generator.ImpactOccurred();
        });

        return Task.CompletedTask;
    }

    public Task NotificationAsync(NotificationOptions? options = null)
    {
        var type = (options?.Type ?? NotificationType.Success) switch
        {
            NotificationType.Warning => UINotificationFeedbackType.Warning,
            NotificationType.Error => UINotificationFeedbackType.Error,
            _ => UINotificationFeedbackType.Success,
        };

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            using var generator = new UINotificationFeedbackGenerator();
            generator.Prepare();
            generator.NotificationOccurred(type);
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// iOS 没有可控时长的振动 API；<c>AudioServicesPlaySystemSound(kSystemSoundID_Vibrate)</c>
    /// 的时长由系统固定。这里用重档 impact 近似，忽略 <c>Duration</c>。
    /// </summary>
    public Task VibrateAsync(VibrateOptions? options = null)
        => ImpactAsync(new ImpactOptions { Style = ImpactStyle.Heavy });

    public Task SelectionStartAsync()
    {
        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            // 选择反馈必须由**同一个** generator 贯穿 start/changed/end，
            // 否则 iOS 无法把连续的选择动作识别为一串，触感会失真。
            _selection?.Dispose();
            _selection = new UISelectionFeedbackGenerator();
            _selection.Prepare();
        });

        return Task.CompletedTask;
    }

    public Task SelectionChangedAsync()
    {
        UIApplication.SharedApplication.InvokeOnMainThread(() => _selection?.SelectionChanged());
        return Task.CompletedTask;
    }

    public Task SelectionEndAsync()
    {
        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            _selection?.Dispose();
            _selection = null;
        });

        return Task.CompletedTask;
    }
}

/// <summary>iOS 屏幕阅读器（VoiceOver）状态与朗读。</summary>
internal sealed class IosScreenReaderService : NativeServiceBase, IScreenReaderService
{
    private Action<ScreenReaderState>? _stateChanged;
    private NSObject? _observer;
    private readonly Lock _gate = new();

    public Task<ScreenReaderState> IsEnabledAsync()
        => Task.FromResult(new ScreenReaderState(UIAccessibility.IsVoiceOverRunning));

    public Task SpeakAsync(SpeakOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
            // 公告通知是「让 VoiceOver 念一句话」的标准做法；VoiceOver 未开启时系统会忽略。
            UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement,
                new NSString(options.Value)));

        return Task.CompletedTask;
    }

    /// <summary>仅在有订阅者时监听系统通知，最后一个订阅者移除时注销（要求第 2 条）。</summary>
    public event Action<ScreenReaderState>? OnStateChange
    {
        add
        {
            if (value is null) return;

            lock (_gate)
            {
                _stateChanged += value;
                // 该通知常量挂在 UIApplication 上（不是 UIAccessibility）。
                _observer ??= NSNotificationCenter.DefaultCenter.AddObserver(
                    UIApplication.VoiceOverStatusDidChangeNotification,
                    _ => Publish());
            }
        }
        remove
        {
            if (value is null) return;

            lock (_gate)
            {
                _stateChanged -= value;
                if (_stateChanged is not null || _observer is null) return;

                NSNotificationCenter.DefaultCenter.RemoveObserver(_observer);
                _observer.Dispose();
                _observer = null;
            }
        }
    }

    private void Publish()
    {
        Action<ScreenReaderState>? handlers;
        lock (_gate) handlers = _stateChanged;

        handlers?.Invoke(new ScreenReaderState(UIAccessibility.IsVoiceOverRunning));
    }
}

/// <summary>
/// iOS Toast。iOS 没有系统 Toast 控件，这里在宿主视图上叠一层自绘的浮层，
/// 外观模仿 Android Toast（圆角深色胶囊 + 淡入淡出）。
/// </summary>
internal sealed class IosToastService : NativeServiceBase, IToastService
{
    private readonly INativeHostContext _hostContext;

    public IosToastService(INativeHostContext hostContext) => _hostContext = hostContext;

    public Task ShowAsync(ToastOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var host = _hostContext.RequireHost<IosNativeHost>();
        var seconds = options.Duration == ToastDuration.Long ? 3.5 : 2.0;

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            var parent = host.TopViewController.View;
            if (parent is null) return;

            var label = new UILabel
            {
                Text = options.Text,
                TextColor = UIColor.White,
                BackgroundColor = UIColor.FromWhiteAlpha(0f, 0.8f),
                TextAlignment = UITextAlignment.Center,
                Font = UIFont.SystemFontOfSize(14),
                Lines = 0,
                Alpha = 0,
                TranslatesAutoresizingMaskIntoConstraints = false,
            };

            label.Layer.CornerRadius = 8;
            label.Layer.MasksToBounds = true;
            // 文字与胶囊边缘之间留白：UILabel 没有 padding，用最小宽高约束近似。
            parent.AddSubview(label);

            var guide = parent.SafeAreaLayoutGuide;
            var vertical = options.Position switch
            {
                ToastPosition.Top => label.TopAnchor.ConstraintEqualTo(guide.TopAnchor, 24),
                ToastPosition.Center => label.CenterYAnchor.ConstraintEqualTo(parent.CenterYAnchor),
                _ => label.BottomAnchor.ConstraintEqualTo(guide.BottomAnchor, -64),
            };

            NSLayoutConstraint.ActivateConstraints(
            [
                label.CenterXAnchor.ConstraintEqualTo(parent.CenterXAnchor),
                vertical,
                label.WidthAnchor.ConstraintLessThanOrEqualTo(parent.WidthAnchor, 0.8f),
                label.HeightAnchor.ConstraintGreaterThanOrEqualTo(40),
            ]);

            UIView.Animate(0.25, () => label.Alpha = 1, () =>
                UIView.Animate(0.25, seconds, UIViewAnimationOptions.CurveEaseInOut,
                    () => label.Alpha = 0,
                    label.RemoveFromSuperview));
        });

        return Task.CompletedTask;
    }
}

/// <summary>
/// iOS 启动画面。
/// <para>
/// iOS 的启动画面是系统在进程启动时按 <c>LaunchScreen.storyboard</c> 显示的，
/// 应用无法在运行期显隐它——没有对应的公开 API。明确抛出，而不是返回一个什么都没做的成功。
/// </para>
/// </summary>
internal sealed class IosSplashScreenService : NativeServiceBase, ISplashScreenService
{
    public Task ShowAsync(ShowSplashOptions? options = null) => UnsupportedAsync();
    public Task HideAsync(HideSplashOptions? options = null) => UnsupportedAsync();
}
