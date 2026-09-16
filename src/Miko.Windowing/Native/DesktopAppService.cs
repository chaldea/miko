using System.Globalization;
using System.Reflection;
using Miko.Native;
using Miko.Native.App;

namespace Miko.Windowing.Native;

/// <summary>
/// 桌面应用信息与生命周期。
/// <para>
/// 应用信息取自入口程序集的元数据。<c>ExitApp</c>/<c>Minimize</c>/<c>ToggleBackButtonHandler</c>
/// 在原始文档中即标注为 Android 专有，桌面上抛 <see cref="PlatformNotSupportedException"/>。
/// </para>
/// </summary>
internal sealed class DesktopAppService : NativeServiceBase, IAppService
{
    /// <summary>Android 专有。</summary>
    public Task ExitAppAsync() => UnsupportedAsync();

    /// <summary>Android 专有。</summary>
    public Task MinimizeAppAsync() => UnsupportedAsync();

    /// <summary>Android 专有（桌面没有硬件返回键）。</summary>
    public Task ToggleBackButtonHandlerAsync(ToggleBackButtonHandlerOptions options) => UnsupportedAsync();

    public Task<AppInfo> GetInfoAsync()
    {
        var assembly = Assembly.GetEntryAssembly();
        var name = assembly?.GetName();

        // InformationalVersion 是 <Version> 的直接映射；退回 AssemblyVersion 保证非空。
        var informational = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = informational ?? name?.Version?.ToString() ?? "0.0.0";

        // 桌面没有 build number 的原生概念；用 AssemblyVersion 的 Revision 位作为近似。
        var build = name?.Version?.Revision.ToString(CultureInfo.InvariantCulture) ?? "0";

        return Task.FromResult(new AppInfo(
            Name: name?.Name ?? AppDomain.CurrentDomain.FriendlyName,
            Id: assembly?.GetName().Name ?? AppDomain.CurrentDomain.FriendlyName,
            Build: build,
            Version: version));
    }

    /// <summary>
    /// 桌面窗口应用只要在运行就视为活动状态：Silk 宿主没有暴露窗口焦点的可靠跨平台查询，
    /// 谎报一个「不活动」比返回恒 true 更容易误导调用方。
    /// </summary>
    public Task<AppState> GetStateAsync() => Task.FromResult(new AppState(IsActive: true));

    /// <summary>桌面通过命令行参数而非 URL scheme 启动；首个看起来像 URI 的参数视为启动 URL。</summary>
    public Task<AppLaunchUrl?> GetLaunchUrlAsync()
    {
        var launchUrl = Environment.GetCommandLineArgs()
            .Skip(1)
            .FirstOrDefault(arg => Uri.TryCreate(arg, UriKind.Absolute, out _));

        return Task.FromResult(launchUrl is null ? null : new AppLaunchUrl(launchUrl));
    }

    public Task<AppLanguageCode> GetAppLanguageAsync()
        => Task.FromResult(new AppLanguageCode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));

    /// <summary>桌面宿主不上报前后台切换，这些事件永不触发。</summary>
    public event Action<AppState>? OnAppStateChange { add { } remove { } }
    public event Action? OnPause { add { } remove { } }
    public event Action? OnResume { add { } remove { } }
    public event Action<UrlOpenEvent>? OnAppUrlOpen { add { } remove { } }

    /// <summary>Android 专有。</summary>
    public event Action<RestoredResultEvent>? OnAppRestoredResult { add { } remove { } }

    /// <summary>Android 专有。</summary>
    public event Action<BackButtonEvent>? OnBackButton { add { } remove { } }
}
