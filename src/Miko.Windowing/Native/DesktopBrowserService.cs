using System.Diagnostics;
using Miko.Native;
using Miko.Native.Browser;

namespace Miko.Windowing.Native;

/// <summary>
/// 桌面浏览器：用系统默认浏览器打开 URL。
/// <para>
/// 桌面没有「内嵌浏览器」（Miko 本身不含 WebView），因此这是 <c>Open</c> 的合理等价语义——
/// 页面确实被打开了。但由此产生两个真实差异，如实体现在实现里而不是假装支持：
/// <c>CloseAsync</c> 无法关闭一个独立的浏览器进程（抛异常），
/// <c>OnBrowserFinished</c>/<c>OnBrowserPageLoaded</c> 也无从得知（永不触发）。
/// </para>
/// </summary>
internal sealed class DesktopBrowserService : NativeServiceBase, IBrowserService
{
    public Task OpenAsync(OpenOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Url))
            throw new ArgumentException("OpenOptions.Url is required.", nameof(options));

        if (!Uri.TryCreate(options.Url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"OpenOptions.Url is not an absolute URI: {options.Url}", nameof(options));

        // 只放行 http/https/mailto 等常规 scheme。UseShellExecute 会把任意字符串交给系统
        // 解析，file:// 或自定义 scheme 可能被用来启动本地程序。
        if (uri.Scheme is not ("http" or "https" or "mailto"))
            throw new ArgumentException($"Unsupported URL scheme for the desktop browser: {uri.Scheme}", nameof(options));

        // UseShellExecute=true 是让系统挑选默认浏览器的方式；.NET Core 起它默认为 false。
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true })?.Dispose();

        return Task.CompletedTask;
    }

    /// <summary>桌面用的是独立的系统浏览器进程，应用无权关闭它。</summary>
    public Task CloseAsync() => UnsupportedAsync();

    /// <summary>系统浏览器不会回调应用，这些事件在桌面上永不触发。</summary>
    public event Action? OnBrowserFinished { add { } remove { } }
    public event Action? OnBrowserPageLoaded { add { } remove { } }
}
