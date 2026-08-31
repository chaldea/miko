using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;

namespace Miko.iOS.Video;

/// <summary>
/// iOS 系统原生视频后端，基于 <c>AVPlayer</c> + <c>AVPlayerItemVideoOutput</c>
/// （内部走 VideoToolbox 硬解）。AVFoundation 是系统框架，不增加应用体积；
/// A/V 同步、HLS、网络缓冲与后台音频策略全部由系统负责。
/// <para>
/// 与桌面 macOS 后端的区别：iOS 有完整托管绑定，可直接使用 <c>AVFoundation</c> 命名空间，
/// 无需经 Objective-C runtime 手工发消息。
/// </para>
/// <para><b>未经真机运行验证</b>：本机为 Windows，无 iOS 设备/模拟器，此实现仅通过编译验证。</para>
/// </summary>
public sealed class IosVideoBackend : IVideoBackend
{
    private readonly ILogger _logger;

    public IosVideoBackend(ILogger<IosVideoBackend>? logger = null)
    {
        _logger = logger ?? NullLogger<IosVideoBackend>.Instance;
    }

    public VideoBackendCapabilities Capabilities { get; } = new(
        HardwareDecode: true,
        Hdr: false,
        SupportedMimeTypes:
        [
            "video/mp4", "video/quicktime", "video/x-m4v",
            "application/vnd.apple.mpegurl",   // HLS，AVPlayer 原生支持
        ]);

    public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
    {
        _logger.LogInformation("Creating iOS AVPlayer video session for {Uri}", source.Uri);
        var session = new IosVideoSession(source, options, _logger);
        session.Start();
        return session;
    }
}
