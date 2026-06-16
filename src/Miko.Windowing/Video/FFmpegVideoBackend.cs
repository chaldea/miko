using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;

namespace Miko.Windowing.Video;

/// <summary>
/// 桌面（当前仅 Windows）FFmpeg 视频后端。Phase 1 为软解基线（CPU 解码 + 纹理上传），
/// 架构上为 Phase 2 的 D3D11VA 零拷贝硬解预留（见 ISSUE-058 §2.1）。
/// <para>
/// 原生 FFmpeg 二进制由 <c>Sdcb.FFmpeg.runtime.windows-x64</c> 提供并随输出复制，
/// 因此无需用户单独安装 FFmpeg。
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
