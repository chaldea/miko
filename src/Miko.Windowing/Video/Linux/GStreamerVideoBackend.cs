using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;
using static Miko.Windowing.Video.Linux.GStreamerInterop;

namespace Miko.Windowing.Video.Linux;

/// <summary>
/// Linux 系统原生视频后端，基于 GStreamer。<c>uridecodebin</c> 自动协商解码器，
/// 系统装有 <c>gstreamer1.0-vaapi</c> 时走 VAAPI GPU 硬解，否则退到软解插件。
/// <para>
/// GStreamer 是发行版标准组件而非随包分发的第三方二进制，因此不增加应用体积。
/// 运行时缺失时降级为无视频（poster）。
/// </para>
/// <para><b>未经真机运行验证</b>：本机为 Windows，此实现仅通过编译验证。</para>
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class GStreamerVideoBackend : IVideoBackend
{
    private readonly ILogger _logger;

    public GStreamerVideoBackend(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// 探测 GStreamer 运行时是否可加载。<c>gst_init</c> 幂等且可重复调用，
    /// 因此这里直接初始化一次作为探测。
    /// </summary>
    internal static bool IsAvailable
    {
        get
        {
            if (!OperatingSystem.IsLinux()) return false;

            try
            {
                if (!gst_is_initialized())
                    gst_init(IntPtr.Zero, IntPtr.Zero);
                return true;
            }
            catch (DllNotFoundException)
            {
                return false;   // 未安装 GStreamer 运行时
            }
            catch (EntryPointNotFoundException)
            {
                return false;   // 版本过旧，缺少所需符号
            }
        }
    }

    public VideoBackendCapabilities Capabilities { get; } = new(
        // uridecodebin 在装有 VAAPI 插件时使用硬解；无法在不打开具体媒体时确知，
        // 这里按「系统具备硬解能力」上报，实际协商由 GStreamer 完成。
        HardwareDecode: true,
        Hdr: false,
        SupportedMimeTypes: ["video/mp4", "video/webm", "video/x-matroska", "video/ogg", "video/quicktime", "application/vnd.apple.mpegurl", "application/x-mpegURL"]);

    public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
    {
        _logger.LogInformation("Creating GStreamer video session for {Uri}", source.Uri);
        var session = new GStreamerVideoSession(source, options, _logger);
        session.Start();
        return session;
    }
}
