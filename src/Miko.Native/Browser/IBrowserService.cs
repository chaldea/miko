namespace Miko.Native.Browser;

/// <summary>
/// 系统浏览器 / 内嵌浏览器。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/v8/native/browser#api">Browser API</see>。
/// 支持平台：Android、iOS、Windowing。
/// </summary>
public interface IBrowserService
{
    /// <summary>打开内嵌浏览器页面。桌面上退化为用系统默认浏览器打开。</summary>
    Task OpenAsync(OpenOptions options);

    /// <summary>关闭当前浏览器。</summary>
    Task CloseAsync();

    /// <summary>用户关闭浏览器（Android / iOS）。</summary>
    event Action? OnBrowserFinished;

    /// <summary>初始 URL 加载完成（Android / iOS）。</summary>
    event Action? OnBrowserPageLoaded;
}
