using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Miko.Platform.Resources;

namespace Miko.Hosting;

/// <summary>
/// 图片加载的 DI 注册扩展。引擎需要注入 <see cref="IImageLoader"/> 才能使 <c>&lt;img&gt;</c> 标签工作。
/// 默认使用 <see cref="ResourceManager"/>（复用 DI 中的 HttpClient）。
/// </summary>
public static class ImageLoadingExtensions
{
    /// <summary>
    /// 注册默认的图片加载服务（使用 <see cref="ResourceManager"/>）。
    /// 默认仅支持入口程序集的 res:// 资源，可通过 <see cref="AddResourceAssembly"/> 添加更多程序集。
    /// </summary>
    public static IServiceCollection AddImageLoader(this IServiceCollection service)
    {
        // 注册配置选项
        service.AddOptions<ResourceAssemblyOptions>();

        // 注册资源程序集提供器为单例
        service.TryAddSingleton<IResourceAssemblyProvider, ResourceAssemblyProvider>();

        // 注册 ResourceManager 作为 IImageLoader，从 DI 解析依赖
        // TryAdd：默认实现绝不覆盖调用方已注册的自定义 IImageLoader（ISSUE-129）。
        service.TryAddSingleton<IImageLoader, ResourceManager>();
        return service;
    }

    /// <summary>
    /// 添加资源程序集，使 res:// 协议能够解析指定程序集中的嵌入资源。
    /// 例如：builder.AddResourceAssembly(typeof(MyApp).Assembly)
    /// </summary>
    public static MikoAppBuilder AddResourceAssembly(this MikoAppBuilder builder, Assembly assembly)
    {
        builder.Services.Configure<ResourceAssemblyOptions>(options =>
        {
            options.Assemblies.Add(assembly);
        });
        return builder;
    }

    /// <summary>
    /// 添加多个资源程序集
    /// </summary>
    public static MikoAppBuilder AddResourceAssemblies(this MikoAppBuilder builder, params Assembly[] assemblies)
    {
        builder.Services.Configure<ResourceAssemblyOptions>(options =>
        {
            foreach (var assembly in assemblies)
            {
                options.Assemblies.Add(assembly);
            }
        });
        return builder;
    }

    /// <summary>
    /// 注册自定义的 <see cref="IImageLoader"/> 实现。
    /// </summary>
    public static MikoAppBuilder UseImageLoading<TLoader>(this MikoAppBuilder builder)
        where TLoader : class, IImageLoader
    {
        // Replace 而非 Add：无论默认实现是先注册还是后注册，自定义实现都确定生效（ISSUE-129）。
        builder.Services.Replace(ServiceDescriptor.Singleton<IImageLoader, TLoader>());
        return builder;
    }
}
