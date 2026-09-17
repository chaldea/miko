namespace Miko.Native.App;

/// <summary>应用名称、标识、版本与构建号。</summary>
/// <param name="Name">应用显示名称。</param>
/// <param name="Id">应用标识（Android package name / iOS bundle id）。</param>
/// <param name="Build">构建号（Android versionCode / iOS CFBundleVersion）。</param>
/// <param name="Version">版本号（Android versionName / iOS CFBundleShortVersionString）。</param>
public sealed record AppInfo(string Name, string Id, string Build, string Version);

/// <summary>应用前后台状态。</summary>
/// <param name="IsActive">应用当前是否处于活动（前台）状态。</param>
public sealed record AppState(bool IsActive);

/// <summary>启动应用所使用的 URL。</summary>
/// <param name="Url">启动 URL。</param>
public sealed record AppLaunchUrl(string Url);

/// <summary>应用语言代码。</summary>
/// <param name="Value">2 或 3 位语言代码，如 <c>zh</c>、<c>en</c>。</param>
public sealed record AppLanguageCode(string Value);

/// <summary>是否启用默认返回键处理。</summary>
/// <param name="Enabled">为 <c>true</c> 时由系统/框架处理返回键，为 <c>false</c> 时交给应用。</param>
public sealed record ToggleBackButtonHandlerOptions(bool Enabled);

/// <summary>通过自定义 Scheme、Universal Link 或 App Link 打开应用的事件。</summary>
/// <param name="Url">打开应用的 URL。</param>
/// <param name="IosSourceApplication">iOS 上发起打开的源应用标识。</param>
/// <param name="IosOpenInPlace">iOS 上文件是否就地打开。</param>
public sealed record UrlOpenEvent(string Url, string? IosSourceApplication = null, bool IosOpenInPlace = false);

/// <summary>外部 Activity 结束后恢复的插件调用结果（Android）。</summary>
/// <param name="PluginId">发起调用的插件标识。</param>
/// <param name="MethodName">被恢复的方法名。</param>
/// <param name="Data">调用结果数据。</param>
/// <param name="Success">调用是否成功。</param>
/// <param name="ErrorMessage">失败时的错误信息。</param>
public sealed record RestoredResultEvent(
    string PluginId,
    string MethodName,
    object? Data,
    bool Success,
    string? ErrorMessage = null);

/// <summary>硬件返回键事件（Android）。</summary>
/// <param name="CanGoBack">应用内是否还有可回退的历史。</param>
public sealed record BackButtonEvent(bool CanGoBack);
