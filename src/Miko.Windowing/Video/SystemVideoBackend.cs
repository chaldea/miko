using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;
using Miko.Windowing.Video.Linux;
using Miko.Windowing.Video.MacOS;
using Miko.Windowing.Video.Windows;

namespace Miko.Windowing.Video;

/// <summary>
/// 桌面系统原生视频后端。按运行平台分派到系统自带的硬件解码栈，
/// 因此不携带任何第三方原生二进制（对比 <c>Miko.Video.FFmpeg</c> 的 &gt;80MB）。
///
/// <list type="table">
///   <item><term>Windows</term><description>Media Foundation（内部走 D3D11VA 硬解）</description></item>
///   <item><term>Linux</term><description>GStreamer（内部走 VAAPI 硬解）</description></item>
///   <item><term>macOS</term><description>AVFoundation（内部走 VideoToolbox 硬解）</description></item>
/// </list>
///
/// <para>
/// 平台栈不可用（缺 GStreamer 运行时、系统过旧）时记录警告并按「无后端」处理：
/// <c>CreateSession</c> 抛 <see cref="NotSupportedException"/>，引擎侧捕获后
/// <c>&lt;video&gt;</c> 退化为只显示 poster/背景，而不是让整个应用崩溃。
/// </para>
/// </summary>
public sealed class SystemVideoBackend : IVideoBackend
{
    private readonly ILogger<SystemVideoBackend> _logger;
    private readonly IVideoBackend? _platformBackend;

    public SystemVideoBackend(ILogger<SystemVideoBackend>? logger = null)
    {
        _logger = logger ?? NullLogger<SystemVideoBackend>.Instance;
        _platformBackend = CreatePlatformBackend(_logger);

        if (_platformBackend == null)
        {
            _logger.LogWarning(
                "No system video backend available on this platform; <video> will show poster/background only. " +
                "Reference Miko.Video.FFmpeg and call UseFFmpegVideo() for a portable software-decode fallback.");
        }
        else
        {
            _logger.LogInformation("System video backend: {Backend}", _platformBackend.GetType().Name);
        }
    }

    /// <summary>
    /// 能力取自实际平台后端；无可用后端时全部为假（等价于 <see cref="NullVideoBackend"/>）。
    /// </summary>
    public VideoBackendCapabilities Capabilities =>
        _platformBackend?.Capabilities
        ?? new VideoBackendCapabilities(HardwareDecode: false, Hdr: false, SupportedMimeTypes: []);

    /// <summary>本后端是否真的能播放（供宿主在启动时判定是否需要提示用户装扩展包）。</summary>
    public bool IsAvailable => _platformBackend != null;

    public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
    {
        if (_platformBackend == null)
            throw new NotSupportedException(
                "No system video backend is available on this platform. " +
                "Reference Miko.Video.FFmpeg and call UseFFmpegVideo() to enable software decoding.");

        return _platformBackend.CreateSession(source, options);
    }

    /// <summary>
    /// 探测并构造当前平台的后端。构造期即验证系统库可加载（各后端的 <c>IsAvailable</c>），
    /// 使「平台不支持」在启动时就暴露，而不是等到第一个 <c>&lt;video&gt;</c> 才失败。
    /// </summary>
    private static IVideoBackend? CreatePlatformBackend(ILogger logger)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                return MediaFoundationVideoBackend.IsAvailable ? new MediaFoundationVideoBackend(logger) : null;

            if (OperatingSystem.IsLinux())
                return GStreamerVideoBackend.IsAvailable ? new GStreamerVideoBackend(logger) : null;

            if (OperatingSystem.IsMacOS())
                return AVFoundationVideoBackend.IsAvailable ? new AVFoundationVideoBackend(logger) : null;
        }
        catch (Exception ex)
        {
            // 探测本身失败（缺 DLL、权限问题）不应阻止应用启动。
            logger.LogWarning(ex, "System video backend probe failed; falling back to no video support");
        }

        return null;
    }
}
