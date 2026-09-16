namespace Miko.Native.App;

/// <summary>
/// 应用 Host 与生命周期。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/v8/native/app#api">App API</see>。
/// 支持平台：Android、iOS、Windowing。
/// </summary>
public interface IAppService
{
    /// <summary>
    /// 退出应用。通常只在 Android 上、且应用自行处理完返回键之后调用。
    /// 其他平台不支持（iOS 主动退出会被 App Store 拒绝）。
    /// </summary>
    Task ExitAppAsync();

    /// <summary>获取应用名称、标识、版本和构建号。</summary>
    Task<AppInfo> GetInfoAsync();

    /// <summary>获取应用当前是否处于活动状态。</summary>
    Task<AppState> GetStateAsync();

    /// <summary>获取启动应用的 URL；不是通过 URL 启动时返回 <c>null</c>。</summary>
    Task<AppLaunchUrl?> GetLaunchUrlAsync();

    /// <summary>最小化应用（Android）。</summary>
    Task MinimizeAppAsync();

    /// <summary>获取应用语言代码。</summary>
    Task<AppLanguageCode> GetAppLanguageAsync();

    /// <summary>启用或禁用默认返回键处理（Android）。</summary>
    Task ToggleBackButtonHandlerAsync(ToggleBackButtonHandlerOptions options);

    /// <summary>应用前后台状态变化。</summary>
    event Action<AppState>? OnAppStateChange;

    /// <summary>应用暂停（进入后台）。</summary>
    event Action? OnPause;

    /// <summary>应用恢复（回到前台）。</summary>
    event Action? OnResume;

    /// <summary>通过自定义 Scheme、Universal Link 或 App Link 打开应用。</summary>
    event Action<UrlOpenEvent>? OnAppUrlOpen;

    /// <summary>外部 Activity 结束后恢复插件调用结果（Android）。</summary>
    event Action<RestoredResultEvent>? OnAppRestoredResult;

    /// <summary>硬件返回键（Android）。</summary>
    event Action<BackButtonEvent>? OnBackButton;
}
