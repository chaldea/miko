using System.Globalization;
using Android.Content;
using Android.Content.PM;
using Android.Views.InputMethods;
using Miko.Native;
using Miko.Native.App;
using Miko.Native.Browser;
using Miko.Native.Keyboard;
using Miko.Native.SplashScreen;
using AndroidUri = Android.Net.Uri;

namespace Miko.Android.Native;

/// <summary>Android 应用信息与生命周期。</summary>
internal sealed class AndroidAppService : NativeServiceBase, IAppService
{
    private readonly INativeHostContext _hostContext;

    public AndroidAppService(INativeHostContext hostContext) => _hostContext = hostContext;

    private AndroidNativeHost Host => _hostContext.RequireHost<AndroidNativeHost>();

    public Task ExitAppAsync()
    {
        var host = Host;

        AndroidToastService.RunOnUiThread(host, () => host.Activity?.FinishAffinity());
        return Task.CompletedTask;
    }

    public Task MinimizeAppAsync()
    {
        var host = Host;

        // MoveTaskToBack 把任务移到后台而不结束它——这才是「最小化」，
        // Finish 会销毁 Activity 并丢掉应用状态。
        AndroidToastService.RunOnUiThread(host, () => host.Activity?.MoveTaskToBack(true));
        return Task.CompletedTask;
    }

    public Task<AppInfo> GetInfoAsync()
    {
        var context = Host.Context;
        var packageName = context.PackageName ?? "unknown";
        var manager = context.PackageManager;

        var label = manager?.GetApplicationLabel(context.ApplicationInfo!) ?? packageName;

        string version = "0.0.0";
        string build = "0";

        if (manager is not null)
        {
            // 0L 消歧：PackageInfoFlags.Of 同时有 long 与 PackageInfoFlagsLong 重载。
            var info = OperatingSystem.IsAndroidVersionAtLeast(33)
                ? manager.GetPackageInfo(packageName, PackageManager.PackageInfoFlags.Of(0L)!)
                : GetPackageInfoLegacy(manager, packageName);

            if (info is not null)
            {
                version = info.VersionName ?? version;
                // LongVersionCode 需要 API 28+；低于此用已过时的 VersionCode。
                build = OperatingSystem.IsAndroidVersionAtLeast(28)
                    ? info.LongVersionCode.ToString(CultureInfo.InvariantCulture)
                    : LegacyVersionCode(info).ToString(CultureInfo.InvariantCulture);
            }
        }

        return Task.FromResult(new AppInfo(label, packageName, build, version));
    }

    [System.Runtime.Versioning.SupportedOSPlatform("android21.0")]
#pragma warning disable CA1422 // 仅在 API < 33 上调用
    private static PackageInfo? GetPackageInfoLegacy(PackageManager manager, string packageName)
        => manager.GetPackageInfo(packageName, 0);

    private static long LegacyVersionCode(PackageInfo info) => info.VersionCode;
#pragma warning restore CA1422

    /// <summary>Activity 存在且未结束即视为处于前台。</summary>
    public Task<AppState> GetStateAsync()
        => Task.FromResult(new AppState(Host.Activity is { IsFinishing: false }));

    public Task<AppLaunchUrl?> GetLaunchUrlAsync()
    {
        var data = Host.Activity?.Intent?.Data;

        return Task.FromResult(data is null ? null : new AppLaunchUrl(data.ToString() ?? string.Empty));
    }

    public Task<AppLanguageCode> GetAppLanguageAsync()
        => Task.FromResult(new AppLanguageCode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));

    /// <summary>
    /// Miko 的返回键由宿主 Activity 自行处理（<c>OnBackPressed</c>），
    /// 框架层没有可切换的「默认处理器」。
    /// </summary>
    public Task ToggleBackButtonHandlerAsync(ToggleBackButtonHandlerOptions options) => UnsupportedAsync();

    /// <summary>
    /// 生命周期与返回键事件需要宿主 Activity 主动转发（<c>OnPause</c>/<c>OnResume</c>/
    /// <c>OnNewIntent</c>/<c>OnBackPressed</c>）；当前宿主未接线，故不触发。
    /// </summary>
    public event Action<AppState>? OnAppStateChange { add { } remove { } }
    public event Action? OnPause { add { } remove { } }
    public event Action? OnResume { add { } remove { } }
    public event Action<UrlOpenEvent>? OnAppUrlOpen { add { } remove { } }
    public event Action<RestoredResultEvent>? OnAppRestoredResult { add { } remove { } }
    public event Action<BackButtonEvent>? OnBackButton { add { } remove { } }
}

/// <summary>
/// Android 浏览器。优先用 Chrome Custom Tabs（<c>ACTION_VIEW</c> + Custom Tabs extras），
/// 系统无 Custom Tabs 支持时自然退化为普通 <c>ACTION_VIEW</c>，由默认浏览器打开。
/// </summary>
internal sealed class AndroidBrowserService : NativeServiceBase, IBrowserService
{
    private const string ExtraSessionKey = "android.support.customtabs.extra.SESSION";
    private const string ExtraToolbarColorKey = "android.support.customtabs.extra.TOOLBAR_COLOR";

    private readonly INativeHostContext _hostContext;

    public AndroidBrowserService(INativeHostContext hostContext) => _hostContext = hostContext;

    public Task OpenAsync(OpenOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Url))
            throw new ArgumentException("OpenOptions.Url is required.", nameof(options));

        var host = _hostContext.RequireHost<AndroidNativeHost>();

        using var intent = new Intent(Intent.ActionView, AndroidUri.Parse(options.Url));

        // Custom Tabs 协议：带上 SESSION extra（可为 null）即请求以 Custom Tab 打开。
        // 不支持的浏览器会忽略这些 extra，退化为普通页面打开。
        intent.PutExtra(ExtraSessionKey, (global::Android.OS.Bundle?)null);

        if (options.ToolbarColor is { } color)
            intent.PutExtra(ExtraToolbarColorKey, global::Android.Graphics.Color.ParseColor(color).ToArgb());

        // 从非 Activity 上下文启动必须带 NewTask。
        if (host.Activity is null) intent.AddFlags(ActivityFlags.NewTask);

        (host.Activity ?? (Context)host.Context).StartActivity(intent);
        return Task.CompletedTask;
    }

    /// <summary>Custom Tab 在独立任务中运行，应用无法主动关闭它。</summary>
    public Task CloseAsync() => UnsupportedAsync();

    /// <summary>需要绑定 CustomTabsService 才能收到导航回调，当前未绑定。</summary>
    public event Action? OnBrowserFinished { add { } remove { } }
    public event Action? OnBrowserPageLoaded { add { } remove { } }
}

/// <summary>Android 键盘显隐，基于 <see cref="InputMethodManager"/>。</summary>
internal sealed class AndroidKeyboardService : NativeServiceBase, IKeyboardService
{
    private readonly INativeHostContext _hostContext;

    public AndroidKeyboardService(INativeHostContext hostContext) => _hostContext = hostContext;

    private AndroidNativeHost Host => _hostContext.RequireHost<AndroidNativeHost>();

    public Task ShowAsync()
    {
        var host = Host;

        AndroidToastService.RunOnUiThread(host, () =>
        {
            if (host.Context.GetSystemService(Context.InputMethodService) is not InputMethodManager imm) return;

            var view = host.Activity?.Window?.DecorView?.FindFocus() ?? host.Activity?.Window?.DecorView;
            if (view is null) return;

            imm.ShowSoftInput(view, ShowFlags.Implicit);
        });

        return Task.CompletedTask;
    }

    public Task HideAsync()
    {
        var host = Host;

        AndroidToastService.RunOnUiThread(host, () =>
        {
            if (host.Context.GetSystemService(Context.InputMethodService) is not InputMethodManager imm) return;

            var token = host.Activity?.Window?.DecorView?.WindowToken;
            if (token is null) return;

            imm.HideSoftInputFromWindow(token, HideSoftInputFlags.None);
        });

        return Task.CompletedTask;
    }

    /// <summary>iOS 专有。</summary>
    public Task SetAccessoryBarVisibleAsync(bool isVisible) => UnsupportedAsync();
    public Task SetScrollAsync(bool isDisabled) => UnsupportedAsync();
    public Task SetStyleAsync(KeyboardStyleOptions options) => UnsupportedAsync();
    public Task SetResizeModeAsync(KeyboardResizeOptions options) => UnsupportedAsync();
    public Task<KeyboardResizeOptions> GetResizeModeAsync() => UnsupportedAsync<KeyboardResizeOptions>();

    /// <summary>
    /// 键盘显隐事件需要监听 IME inset 变化（<c>WindowInsets.Type.ime()</c>）。
    /// <c>MikoSurfaceView</c> 已消费窗口 inset 用于安全区，但未把 IME inset 转发到本服务。
    /// </summary>
    public event Action<KeyboardInfo>? OnKeyboardWillShow { add { } remove { } }
    public event Action<KeyboardInfo>? OnKeyboardDidShow { add { } remove { } }
    public event Action? OnKeyboardWillHide { add { } remove { } }
    public event Action? OnKeyboardDidHide { add { } remove { } }
}

/// <summary>
/// Android 启动画面。
/// <para>
/// Android 12 (API 31) 起启动画面由系统的 SplashScreen API 管理，其显示时机固定在冷启动，
/// 应用只能延长（通过 <c>setKeepOnScreenCondition</c>），无法在运行期任意显隐。
/// Miko 的宿主未接入该 API，因此这里明确不支持，而不是返回一个什么都没做的成功。
/// </para>
/// </summary>
internal sealed class AndroidSplashScreenService : NativeServiceBase, ISplashScreenService
{
    public Task ShowAsync(ShowSplashOptions? options = null) => UnsupportedAsync();
    public Task HideAsync(HideSplashOptions? options = null) => UnsupportedAsync();
}
