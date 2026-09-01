using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Platform.Video;

namespace Miko.Video.FFmpeg;

/// <summary>
/// FFmpeg 视频后端的 DI 注册扩展。
/// <para>
/// 本后端是**可选扩展**，不是任何平台的默认：各平台宿主默认注册系统原生硬解后端
/// （桌面 <c>UseSystemVideo()</c>、Android <c>UseAndroidVideo()</c>、iOS <c>UseIosVideo()</c>）。
/// 仅当系统解码器不覆盖所需容器/编码时才改用本包 —— 代价是随包引入 &gt;80MB 原生 FFmpeg 二进制。
/// </para>
/// </summary>
public static class FFmpegVideoServiceExtensions
{
    /// <summary>
    /// 注册 FFmpeg 视频后端（CPU 软解）。控制器初始化时会从 DI 解析并注入引擎。
    /// <para>
    /// 注册即覆盖系统原生后端：DI 中 <see cref="IVideoBackend"/> 只解析最后一个注册项，
    /// 因此本调用应放在平台宿主的 <c>UseSystemVideo()</c> 之后。
    /// </para>
    /// </summary>
    public static MikoAppBuilder UseFFmpegVideo(this MikoAppBuilder builder)
    {
        builder.Services.AddSingleton<IVideoBackend, FFmpegVideoBackend>();
        return builder;
    }
}
