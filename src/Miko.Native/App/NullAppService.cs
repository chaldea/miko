namespace Miko.Native.App;

/// <summary>
/// 未注册平台实现时的 <see cref="IAppService"/> 占位实现。
/// <para>
/// 与 <c>NullVideoBackend</c> 同一思路：由 <c>AddMikoNative()</c> 以 <c>TryAdd</c> 注册，
/// 使 <c>[Inject] IAppService</c> 永远能解析成功；一旦真正调用则明确抛出
/// <see cref="PlatformNotSupportedException"/>，而不是返回一个假装可用的结果。
/// 平台包（<c>UseDesktopNative()</c> / <c>UseAndroidNative()</c> / <c>UseIosNative()</c> /
/// <c>UseSimulatorNative()</c>）用 <c>Replace</c> 覆盖它。
/// </para>
/// <para>事件订阅本身不抛异常（订阅一个永不触发的事件是无害的），只有调用方法时才失败。</para>
/// </summary>
public sealed class NullAppService : NativeServiceBase, IAppService
{
    public Task ExitAppAsync() => UnsupportedAsync();
    public Task<AppInfo> GetInfoAsync() => UnsupportedAsync<AppInfo>();
    public Task<AppState> GetStateAsync() => UnsupportedAsync<AppState>();
    public Task<AppLaunchUrl?> GetLaunchUrlAsync() => UnsupportedAsync<AppLaunchUrl?>();
    public Task MinimizeAppAsync() => UnsupportedAsync();
    public Task<AppLanguageCode> GetAppLanguageAsync() => UnsupportedAsync<AppLanguageCode>();
    public Task ToggleBackButtonHandlerAsync(ToggleBackButtonHandlerOptions options) => UnsupportedAsync();

    public event Action<AppState>? OnAppStateChange { add { } remove { } }
    public event Action? OnPause { add { } remove { } }
    public event Action? OnResume { add { } remove { } }
    public event Action<UrlOpenEvent>? OnAppUrlOpen { add { } remove { } }
    public event Action<RestoredResultEvent>? OnAppRestoredResult { add { } remove { } }
    public event Action<BackButtonEvent>? OnBackButton { add { } remove { } }
}
