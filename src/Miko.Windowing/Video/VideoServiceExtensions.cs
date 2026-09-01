using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Platform.Video;

namespace Miko.Windowing.Video;

/// <summary>
/// 桌面视频后端的 DI 注册扩展。
/// </summary>
public static class VideoServiceExtensions
{
    /// <summary>
    /// 注册系统原生视频后端：Windows 走 Media Foundation、Linux 走 GStreamer、
    /// macOS 走 AVFoundation，解码均由系统硬件解码器完成。
    /// <para>
    /// 不携带任何第三方原生二进制。系统栈不可用时 <c>&lt;video&gt;</c> 自动降级为
    /// 只显示 poster/背景，并在日志中说明原因。
    /// </para>
    /// <para>
    /// 需要系统解码器不覆盖的格式时，改为引用 <c>Miko.Video.FFmpeg</c> 并调用
    /// <c>UseFFmpegVideo()</c>（代价是 &gt;80MB 原生库）。
    /// </para>
    /// </summary>
    public static MikoAppBuilder UseSystemVideo(this MikoAppBuilder builder)
    {
        builder.Services.AddSingleton<IVideoBackend, SystemVideoBackend>();
        return builder;
    }
}
