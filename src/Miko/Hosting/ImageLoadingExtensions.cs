using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Miko.Platform.Resources;

namespace Miko.Hosting;

/// <summary>
/// 图片加载的 DI 注册扩展。引擎默认已注入内置 <see cref="ResourceManager"/>（复用 DI 中的 HttpClient），
/// 因此 <c>&lt;img&gt;</c> 开箱即用；仅在需要自定义 res:// 解析程序集或替换加载器时调用本扩展。
/// </summary>
public static class ImageLoadingExtensions
{
    /// <summary>
    /// 注册内置 <see cref="ResourceManager"/> 作为 <see cref="IImageLoader"/>，并指定 res:// 解析时
    /// 查找的程序集（默认仅入口程序集）。HttpClient 从 DI 解析（应用可自行注册带 BaseAddress 的实例）。
    /// </summary>
    public static MikoAppBuilder UseImageLoading(this MikoAppBuilder builder, params Assembly[] resourceAssemblies)
    {
        builder.Services.AddSingleton<IImageLoader>(sp =>
            new ResourceManager(sp.GetService<HttpClient>(), resourceAssemblies));
        return builder;
    }

    /// <summary>注册自定义的 <see cref="IImageLoader"/> 实现。</summary>
    public static MikoAppBuilder UseImageLoading<TLoader>(this MikoAppBuilder builder)
        where TLoader : class, IImageLoader
    {
        builder.Services.AddSingleton<IImageLoader, TLoader>();
        return builder;
    }
}
