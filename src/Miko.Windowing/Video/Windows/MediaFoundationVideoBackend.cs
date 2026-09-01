using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;
using static Miko.Windowing.Video.Windows.MediaFoundationInterop;

namespace Miko.Windowing.Video.Windows;

/// <summary>
/// Windows 系统原生视频后端，基于 Media Foundation。解码交由系统注册的硬件 MFT
/// （内部走 D3D11VA），因此无需任何第三方原生二进制。
/// <para>
/// 支持的容器/编码取决于系统已安装的解码器：Windows 10/11 默认覆盖 H.264/HEVC/VP9 与
/// MP4/ASF 容器；系统未装解码器的格式（如某些 WebM/VP8）会在创建会话时失败并降级到 poster。
/// </para>
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class MediaFoundationVideoBackend : IVideoBackend
{
    private readonly ILogger _logger;

    public MediaFoundationVideoBackend(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// 探测 Media Foundation 是否可用：尝试 <c>MFStartup</c>/<c>MFShutdown</c> 一次。
    /// Windows N/KN 版本可能缺失媒体功能包，此时 mfplat.dll 加载失败。
    /// </summary>
    internal static bool IsAvailable
    {
        get
        {
            if (!OperatingSystem.IsWindows()) return false;

            try
            {
                int hr = MFStartup(MF_VERSION, MFSTARTUP_NOSOCKET);
                if (hr < 0) return false;
                MFShutdown();
                return true;
            }
            catch (DllNotFoundException)
            {
                return false;   // 无媒体功能包（Windows N/KN）
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }
    }

    public VideoBackendCapabilities Capabilities { get; } = new(
        HardwareDecode: true,
        Hdr: false,
        SupportedMimeTypes: ["video/mp4", "video/quicktime", "video/x-ms-wmv", "video/3gpp", "video/x-m4v"]);

    public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
    {
        _logger.LogInformation("Creating Media Foundation video session for {Uri}", source.Uri);
        var session = new MediaFoundationVideoSession(source, options, _logger);
        session.Start();
        return session;
    }
}
