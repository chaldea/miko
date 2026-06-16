using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Platform.Video;

namespace Miko.Windowing.Video;

/// <summary>
/// 视频后端的 DI 注册扩展。桌面应用在构建时调用 <see cref="UseFFmpegVideo"/> 启用
/// <c>&lt;video&gt;</c> 播放。未调用时 <c>&lt;video&gt;</c> 元素仅显示背景/poster。
/// </summary>
public static class VideoServiceExtensions
{
    /// <summary>
    /// 注册 FFmpeg 视频后端（软解基线）。控制器初始化时会从 DI 解析并注入引擎。
    /// </summary>
    public static MikoAppBuilder UseFFmpegVideo(this MikoAppBuilder builder)
    {
        builder.Services.AddSingleton<IVideoBackend, FFmpegVideoBackend>();
        return builder;
    }
}
