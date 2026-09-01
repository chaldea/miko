using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Platform.Video;

namespace Miko.Android.Video;

/// <summary>Android 视频后端的 DI 注册扩展。</summary>
public static class AndroidVideoServiceExtensions
{
    /// <summary>
    /// 注册 Android 系统原生视频后端（<c>MediaExtractor</c> + <c>MediaCodec</c> 硬解，
    /// 解码直出 <c>SurfaceTexture</c> 零拷贝）。不引入任何第三方原生库。
    /// </summary>
    public static MikoAppBuilder UseAndroidVideo(this MikoAppBuilder builder)
    {
        builder.Services.AddSingleton<IVideoBackend, AndroidVideoBackend>();
        return builder;
    }
}
