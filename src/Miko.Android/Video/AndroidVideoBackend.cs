using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;

namespace Miko.Android.Video;

/// <summary>
/// Android 系统原生视频后端：<c>MediaExtractor</c> 解复用 + <c>MediaCodec</c> 硬件解码。
/// 两者都是系统组件，不增加应用体积；支持的编码取决于设备解码器
/// （H.264/HEVC/VP8/VP9/AV1 视机型而定）。
/// <para>
/// 解码输出直接写入 <c>SurfaceTexture</c> 关联的 OES 外部纹理，因此帧从解码器到 Skia
/// 全程驻留 GPU —— 这是五个平台中最干净的零拷贝路径（见 <see cref="AndroidVideoFrameSource"/>）。
/// </para>
/// <para><b>未经真机运行验证</b>：本机无 Android 设备/模拟器，此实现仅通过编译验证。</para>
/// </summary>
public sealed class AndroidVideoBackend : IVideoBackend
{
    private readonly ILogger _logger;

    public AndroidVideoBackend(ILogger<AndroidVideoBackend>? logger = null)
    {
        _logger = logger ?? NullLogger<AndroidVideoBackend>.Instance;
    }

    public VideoBackendCapabilities Capabilities { get; } = new(
        HardwareDecode: true,
        Hdr: false,
        SupportedMimeTypes:
        [
            "video/mp4", "video/webm", "video/3gpp", "video/x-matroska", "application/vnd.apple.mpegurl", "application/x-mpegURL",
        ]);

    public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
    {
        _logger.LogInformation("Creating Android MediaCodec video session for {Uri}", source.Uri);
        var session = new AndroidMediaPlayerSession(source, options);
        session.Start();
        return session;
    }
}
