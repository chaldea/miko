using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Platform.Video;

namespace Miko.iOS.Video;

/// <summary>iOS 视频后端的 DI 注册扩展。</summary>
public static class IosVideoServiceExtensions
{
    /// <summary>
    /// 注册 iOS 系统原生视频后端（<c>AVPlayer</c> + VideoToolbox 硬解，
    /// <c>CVOpenGLESTextureCache</c> 零拷贝映射）。不引入任何第三方原生库。
    /// </summary>
    public static MikoAppBuilder UseIosVideo(this MikoAppBuilder builder)
    {
        builder.Services.AddSingleton<IVideoBackend, IosVideoBackend>();
        return builder;
    }
}
