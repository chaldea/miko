using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;
using static Miko.Windowing.Video.MacOS.AVFoundationInterop;

namespace Miko.Windowing.Video.MacOS;

/// <summary>
/// macOS 系统原生视频后端，基于 AVFoundation（内部走 VideoToolbox 硬解）。
/// AVFoundation 是系统框架，不增加应用体积；容器/编码支持随系统提供
/// （H.264/HEVC/ProRes、MP4/MOV/HLS）。
/// <para><b>未经真机运行验证</b>：本机为 Windows，此实现仅通过编译验证。</para>
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class AVFoundationVideoBackend : IVideoBackend
{
    private readonly ILogger _logger;

    public AVFoundationVideoBackend(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// 探测 AVFoundation 是否可用：确认能加载框架并解析出 <c>AVPlayer</c> 类。
    /// </summary>
    internal static bool IsAvailable
    {
        get
        {
            if (!OperatingSystem.IsMacOS()) return false;

            try
            {
                // AVFoundation 需显式加载后其类才注册进 Objective-C runtime。
                NativeLibrary.Load("/System/Library/Frameworks/AVFoundation.framework/AVFoundation");
                return GetClass("AVPlayer") != IntPtr.Zero;
            }
            catch (DllNotFoundException)
            {
                return false;
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
        SupportedMimeTypes:
        [
            "video/mp4", "video/quicktime", "video/x-m4v",
            "application/vnd.apple.mpegurl",   // HLS，由 AVPlayer 原生支持
        ]);

    public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
    {
        _logger.LogInformation("Creating AVFoundation video session for {Uri}", source.Uri);
        var session = new AVFoundationVideoSession(source, options, _logger);
        session.Start();
        return session;
    }
}
