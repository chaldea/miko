using System.Runtime.InteropServices;
using Miko.Native;
using Miko.Native.ScreenReader;

namespace Miko.Windowing.Native;

/// <summary>
/// 桌面屏幕阅读器状态查询。
/// <para>
/// Windows 通过 <c>SystemParametersInfo(SPI_GETSCREENREADER)</c> 查询——这是系统暴露的
/// 「有屏幕阅读器在运行」标志（讲述人及第三方读屏软件会设置它）。Linux/macOS 无等价的
/// 免依赖查询，抛 <see cref="PlatformNotSupportedException"/>。
/// </para>
/// <para>
/// <c>SpeakAsync</c> 需要 TTS 引擎（Windows 需 SAPI COM 互操作、macOS 需 AVSpeechSynthesizer），
/// 桌面宿主不引入这些依赖，故不支持。
/// </para>
/// </summary>
internal sealed class DesktopScreenReaderService : NativeServiceBase, IScreenReaderService
{
    private const uint SPI_GETSCREENREADER = 0x0046;

    public Task<ScreenReaderState> IsEnabledAsync()
    {
        if (!System.OperatingSystem.IsWindows()) throw Unsupported();

        if (!SystemParametersInfo(SPI_GETSCREENREADER, 0, out var enabled, 0))
            throw new InvalidOperationException("SystemParametersInfo(SPI_GETSCREENREADER) failed.");

        return Task.FromResult(new ScreenReaderState(enabled));
    }

    /// <summary>桌面宿主不携带 TTS 引擎。</summary>
    public Task SpeakAsync(SpeakOptions options) => UnsupportedAsync();

    /// <summary>Windows 以 <c>WM_SETTINGCHANGE</c> 广播该变化，桌面宿主未监听窗口消息，故不触发。</summary>
    public event Action<ScreenReaderState>? OnStateChange { add { } remove { } }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(
        uint uiAction,
        uint uiParam,
        [MarshalAs(UnmanagedType.Bool)] out bool pvParam,
        uint fWinIni);
}
