using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Miko.Hosting;
using Miko.Native;
using Miko.Native.App;
using Miko.Native.Browser;
using Miko.Native.Clipboard;
using Miko.Native.Device;
using Miko.Native.Geolocation;
using Miko.Native.Haptics;
using Miko.Native.Keyboard;
using Miko.Native.Network;
using Miko.Native.ScreenReader;
using Miko.Platform;

namespace Miko.Browser.Native;

internal sealed class BrowserAppService : NativeServiceBase, IAppService
{
    private readonly MikoAppOptions _options;
    private Action<AppState>? _stateChanged;
    private Action? _pause;
    private Action? _resume;

    public BrowserAppService(IOptions<MikoAppOptions> options)
    {
        _options = options.Value;
        BrowserNativeEvents.VisibilityChanged += OnVisibility;
    }

    public Task ExitAppAsync() => UnsupportedAsync();

    public Task<AppInfo> GetInfoAsync()
    {
        var assembly = Assembly.GetEntryAssembly()?.GetName();
        var version = assembly?.Version ?? new Version(1, 0, 0);
        return Task.FromResult(new AppInfo(
            _options.Title, assembly?.Name ?? "Miko.Browser", version.Build.ToString(), version.ToString()));
    }

    public Task<AppState> GetStateAsync() => Task.FromResult(new AppState(BrowserNativeInterop.IsAppActive()));
    public Task<AppLaunchUrl?> GetLaunchUrlAsync()
        => Task.FromResult<AppLaunchUrl?>(new(BrowserNativeInterop.GetLaunchUrl()));
    public Task MinimizeAppAsync() => UnsupportedAsync();

    public Task<AppLanguageCode> GetAppLanguageAsync()
    {
        var language = BrowserNativeInterop.GetLanguage();
        return Task.FromResult(new AppLanguageCode(language.Split('-', 2)[0]));
    }

    public Task ToggleBackButtonHandlerAsync(ToggleBackButtonHandlerOptions options) => UnsupportedAsync();

    public event Action<AppState>? OnAppStateChange { add => _stateChanged += value; remove => _stateChanged -= value; }
    public event Action? OnPause { add => _pause += value; remove => _pause -= value; }
    public event Action? OnResume { add => _resume += value; remove => _resume -= value; }
    public event Action<UrlOpenEvent>? OnAppUrlOpen { add { } remove { } }
    public event Action<RestoredResultEvent>? OnAppRestoredResult { add { } remove { } }
    public event Action<BackButtonEvent>? OnBackButton { add { } remove { } }

    private void OnVisibility(bool visible)
    {
        _stateChanged?.Invoke(new AppState(visible));
        if (visible) _resume?.Invoke();
        else _pause?.Invoke();
    }
}

internal sealed class BrowserBrowserService : NativeServiceBase, IBrowserService
{
    public Task OpenAsync(OpenOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Uri.TryCreate(options.Url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
            throw new ArgumentException("The browser requires an absolute HTTP(S) URL.", nameof(options));
        if (!BrowserNativeInterop.OpenBrowser(uri.AbsoluteUri, options.WindowName ?? "_blank"))
            throw new InvalidOperationException("The browser blocked the new window. Open it from a user gesture or allow pop-ups.");
        return Task.CompletedTask;
    }

    public Task CloseAsync() => BrowserNativeInterop.CloseBrowser()
        ? Task.CompletedTask
        : UnsupportedAsync();

    // A cross-origin popup's load and close notifications are not available to the opener.
    public event Action? OnBrowserFinished { add { } remove { } }
    public event Action? OnBrowserPageLoaded { add { } remove { } }
}

internal sealed class BrowserClipboardService : IClipboardService
{
    public async Task WriteAsync(ClipboardWriteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.String != null) await BrowserNativeInterop.WriteClipboardText(options.String).ConfigureAwait(false);
        else if (options.Image != null) await BrowserNativeInterop.WriteClipboardImage(options.Image).ConfigureAwait(false);
        else if (options.Url != null) await BrowserNativeInterop.WriteClipboardText(options.Url).ConfigureAwait(false);
        else throw new ArgumentException("ClipboardWriteOptions must set String, Image, or Url.", nameof(options));
    }

    public async Task<ClipboardReadResult> ReadAsync()
    {
        var json = await BrowserNativeInterop.ReadClipboard().ConfigureAwait(false);
        var value = JsonSerializer.Deserialize(json, BrowserJsonContext.Default.ClipboardJson)
            ?? throw new InvalidOperationException("The browser returned an empty clipboard response.");
        return new ClipboardReadResult(value.Value, value.Type);
    }
}

internal sealed class BrowserDeviceService : NativeServiceBase, IDeviceService
{
    public Task<DeviceId> GetIdAsync() => Task.FromResult(new DeviceId(BrowserNativeInterop.GetDeviceId()));

    public Task<DeviceInfo> GetInfoAsync()
    {
        var json = BrowserNativeInterop.GetDeviceInfo();
        var value = JsonSerializer.Deserialize(json, BrowserJsonContext.Default.DeviceJson)
            ?? throw new InvalidOperationException("The browser returned empty device information.");
        return Task.FromResult(BrowserJsonContext.ToDeviceInfo(value));
    }

    public async Task<BatteryInfo> GetBatteryInfoAsync()
    {
        var json = await BrowserNativeInterop.GetBatteryInfo().ConfigureAwait(false);
        if (string.IsNullOrEmpty(json) || json == "null") throw Unsupported();
        var value = JsonSerializer.Deserialize(json, BrowserJsonContext.Default.BatteryJson)!;
        return new BatteryInfo(value.Level, value.Charging);
    }

    public Task<LanguageCode> GetLanguageCodeAsync()
        => Task.FromResult(new LanguageCode(BrowserNativeInterop.GetLanguage().Split('-', 2)[0]));
    public Task<LanguageTag> GetLanguageTagAsync()
        => Task.FromResult(new LanguageTag(BrowserNativeInterop.GetLanguage()));
}

internal sealed class BrowserNetworkService : INetworkService
{
    private Action<ConnectionStatus>? _changed;
    private int _subscribers;

    public BrowserNetworkService() => BrowserNativeEvents.NetworkChanged += OnNetworkChanged;

    public Task<ConnectionStatus> GetStatusAsync()
    {
        var json = BrowserNativeInterop.GetNetworkStatus();
        var value = JsonSerializer.Deserialize(json, BrowserJsonContext.Default.NetworkJson)!;
        return Task.FromResult(BrowserJsonContext.ToConnectionStatus(value));
    }

    public event Action<ConnectionStatus>? OnNetworkStatusChange
    {
        add
        {
            if (value is null) return;
            _changed += value;
            if (++_subscribers == 1) BrowserNativeInterop.SubscribeNetwork();
        }
        remove
        {
            if (value is null || _changed is null || !_changed.GetInvocationList().Contains(value)) return;
            _changed -= value;
            _subscribers = _changed?.GetInvocationList().Length ?? 0;
            if (_subscribers == 0) BrowserNativeInterop.UnsubscribeNetwork();
        }
    }

    private void OnNetworkChanged(string json)
    {
        var value = JsonSerializer.Deserialize(json, BrowserJsonContext.Default.NetworkJson)!;
        _changed?.Invoke(BrowserJsonContext.ToConnectionStatus(value));
    }
}

internal sealed class BrowserScreenReaderService : NativeServiceBase, IScreenReaderService
{
    // The web platform intentionally exposes no screen-reader detection API.
    public Task<ScreenReaderState> IsEnabledAsync() => UnsupportedAsync<ScreenReaderState>();

    public Task SpeakAsync(SpeakOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return BrowserNativeInterop.Speak(options.Value, options.Language)
            ? Task.CompletedTask
            : UnsupportedAsync();
    }

    public event Action<ScreenReaderState>? OnStateChange { add { } remove { } }
}

internal sealed class BrowserHapticsService : NativeServiceBase, IHapticsService
{
    public Task ImpactAsync(ImpactOptions? options = null) => Vibrate((options?.Style ?? ImpactStyle.Medium) switch
    {
        ImpactStyle.Heavy => 40,
        ImpactStyle.Light => 10,
        _ => 20,
    });

    public Task NotificationAsync(NotificationOptions? options = null) => (options?.Type ?? NotificationType.Success) switch
    {
        NotificationType.Warning => Vibrate("30,40,30"),
        NotificationType.Error => Vibrate("50,40,50"),
        _ => Vibrate("15,30,15"),
    };

    public Task VibrateAsync(VibrateOptions? options = null) => Vibrate(Math.Max(0, options?.Duration ?? 300));
    public Task SelectionStartAsync() => Vibrate(5);
    public Task SelectionChangedAsync() => Vibrate(5);
    public Task SelectionEndAsync() => Task.CompletedTask;

    private Task Vibrate(int milliseconds) => Vibrate(milliseconds.ToString());

    private Task Vibrate(string pattern)
    {
        if (!BrowserNativeInterop.SupportsVibration()) return UnsupportedAsync();
        BrowserNativeInterop.Vibrate(pattern);
        return Task.CompletedTask;
    }
}

/// <summary>Component-facing haptics are deliberately silent where vibration is unavailable.</summary>
internal sealed class BrowserHapticFeedback : IHapticFeedback
{
    public void SelectionStart() => Vibrate("5");
    public void SelectionChanged() => Vibrate("5");
    public void SelectionEnd() { }
    public void Impact(HapticImpactStyle style = HapticImpactStyle.Medium) => Vibrate(style switch
    {
        HapticImpactStyle.Heavy => "40",
        HapticImpactStyle.Light => "10",
        _ => "20",
    });

    private static void Vibrate(string pattern)
    {
        if (BrowserNativeInterop.SupportsVibration()) BrowserNativeInterop.Vibrate(pattern);
    }
}

internal sealed class BrowserGeolocationService : NativeServiceBase, IGeolocationService
{
    private readonly Dictionary<string, Action<Position?, Exception?>> _watches = new();

    public BrowserGeolocationService() => BrowserNativeEvents.PositionChanged += OnPositionChanged;

    public async Task<Position> GetCurrentPositionAsync(PositionOptions? options = null)
    {
        if (!BrowserNativeInterop.SupportsGeolocation()) throw Unsupported();
        var json = await BrowserNativeInterop.GetCurrentPosition(SerializeOptions(options)).ConfigureAwait(false);
        return ParsePosition(json);
    }

    public Task<string> WatchPositionAsync(PositionOptions options, Action<Position?, Exception?> callback)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(callback);
        if (!BrowserNativeInterop.SupportsGeolocation()) return UnsupportedAsync<string>();
        var id = BrowserNativeInterop.WatchPosition(SerializeOptions(options));
        _watches.Add(id, callback);
        return Task.FromResult(id);
    }

    public Task ClearWatchAsync(string watchId)
    {
        if (!BrowserNativeInterop.SupportsGeolocation()) return UnsupportedAsync();
        BrowserNativeInterop.ClearPositionWatch(watchId);
        _watches.Remove(watchId);
        return Task.CompletedTask;
    }

    public async Task<GeolocationPermissionStatus> CheckPermissionsAsync()
    {
        if (!BrowserNativeInterop.SupportsGeolocation()) throw Unsupported();
        var state = await BrowserNativeInterop.CheckGeolocationPermission().ConfigureAwait(false);
        var permission = BrowserJsonContext.ToPermissionState(state);
        return new GeolocationPermissionStatus(permission, permission);
    }

    public async Task<GeolocationPermissionStatus> RequestPermissionsAsync(GeolocationPermissions? permissions = null)
    {
        if (!BrowserNativeInterop.SupportsGeolocation()) throw Unsupported();
        var state = await BrowserNativeInterop.RequestGeolocationPermission().ConfigureAwait(false);
        var permission = BrowserJsonContext.ToPermissionState(state);
        return new GeolocationPermissionStatus(permission, permission);
    }

    private void OnPositionChanged(string id, string? json, string? error)
    {
        if (!_watches.TryGetValue(id, out var callback)) return;
        if (error != null) callback(null, new InvalidOperationException(error));
        else if (json != null) callback(ParsePosition(json), null);
    }

    private static string SerializeOptions(PositionOptions? options)
        => JsonSerializer.Serialize(options ?? new PositionOptions(), BrowserJsonContext.Default.PositionOptions);

    private static Position ParsePosition(string json)
    {
        var value = JsonSerializer.Deserialize(json, BrowserJsonContext.Default.PositionJson)
            ?? throw new InvalidOperationException("The browser returned an empty position.");
        return BrowserJsonContext.ToPosition(value);
    }
}

internal sealed class BrowserKeyboardService : NativeServiceBase, IKeyboardService
{
    private Action<KeyboardInfo>? _willShow;
    private Action<KeyboardInfo>? _didShow;
    private Action? _willHide;
    private Action? _didHide;

    public BrowserKeyboardService() => BrowserNativeEvents.KeyboardInsetChanged += OnKeyboardInsetChanged;

    public Task ShowAsync()
    {
        BrowserInterop.ShowKeyboard();
        return Task.CompletedTask;
    }

    public Task HideAsync()
    {
        BrowserInterop.HideKeyboard();
        return Task.CompletedTask;
    }

    public Task SetAccessoryBarVisibleAsync(bool isVisible) => UnsupportedAsync();
    public Task SetScrollAsync(bool isDisabled) => UnsupportedAsync();
    public Task SetStyleAsync(KeyboardStyleOptions options) => UnsupportedAsync();
    public Task SetResizeModeAsync(KeyboardResizeOptions options) => options.Mode == KeyboardResize.Native
        ? Task.CompletedTask
        : UnsupportedAsync();
    public Task<KeyboardResizeOptions> GetResizeModeAsync()
        => Task.FromResult(new KeyboardResizeOptions { Mode = KeyboardResize.Native });

    public event Action<KeyboardInfo>? OnKeyboardWillShow { add => _willShow += value; remove => _willShow -= value; }
    public event Action<KeyboardInfo>? OnKeyboardDidShow { add => _didShow += value; remove => _didShow -= value; }
    public event Action? OnKeyboardWillHide { add => _willHide += value; remove => _willHide -= value; }
    public event Action? OnKeyboardDidHide { add => _didHide += value; remove => _didHide -= value; }

    private void OnKeyboardInsetChanged(double previous, double current)
    {
        if (previous <= 0 && current > 0)
        {
            var info = new KeyboardInfo(current);
            _willShow?.Invoke(info);
            _didShow?.Invoke(info);
        }
        else if (previous > 0 && current <= 0)
        {
            _willHide?.Invoke();
            _didHide?.Invoke();
        }
    }
}
