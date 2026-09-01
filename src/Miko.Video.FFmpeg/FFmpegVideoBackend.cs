using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;

namespace Miko.Video.FFmpeg;

/// <summary>
/// FFmpeg 视频后端（CPU 软解）。作为**可选扩展**存在，用于系统原生硬解后端不覆盖的
/// 容器/编码；各平台的默认后端是系统硬解（见 <c>SystemVideoBackend</c>）。
/// <para>
/// 取舍：软解意味着 CPU 解码 + 每帧纹理上传，功耗与占用都高于系统硬解；
/// 换来的是不依赖系统解码器、跨平台行为完全一致。
/// </para>
/// <para>
/// 原生 FFmpeg 二进制由 <c>Sdcb.FFmpeg.runtime.windows-x64</c> 提供并随输出复制，
/// 因此 Windows 上无需用户单独安装 FFmpeg；其它平台需系统自带 FFmpeg 共享库。
/// </para>
/// </summary>
public sealed class FFmpegVideoBackend : IVideoBackend
{
    private readonly ILogger<FFmpegVideoBackend> _logger;

    public FFmpegVideoBackend(ILogger<FFmpegVideoBackend>? logger = null)
    {
        _logger = logger ?? NullLogger<FFmpegVideoBackend>.Instance;
    }

    public VideoBackendCapabilities Capabilities { get; } = new VideoBackendCapabilities(
        HardwareDecode: false,   // Phase 1 软解；Phase 2 切 D3D11VA 后置 true
        Hdr: false,
        SupportedMimeTypes: new[] { "video/mp4", "video/webm", "video/ogg", "video/x-matroska" });

    public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
    {
        _logger.LogInformation("Creating FFmpeg video session for {Uri}", source.Uri);
        var session = new FFmpegVideoSession(source, options, _logger);
        session.Start();
        return session;
    }
}
