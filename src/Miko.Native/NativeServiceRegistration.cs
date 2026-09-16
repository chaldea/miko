using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Miko.Native;

/// <summary>
/// 供**平台包**注册自身实现使用的辅助扩展。
/// </summary>
public static class NativeServiceRegistration
{
    /// <summary>
    /// 以平台实现覆盖某个 Native 能力接口。
    /// <para>
    /// 用 <c>Replace</c> 而非 <c>Add</c>：Microsoft DI 对单服务解析取**最后一条**注册，而
    /// <c>AddMikoNative()</c> 的 <c>Null*</c> 默认实现可能在平台注册**之后**才执行
    /// （例如应用先调 <c>UseDesktopNative()</c> 再调 <c>AddMikoNative()</c>）。用 Add 会让默认实现
    /// 后注册并盖掉平台实现——正是 ISSUE-129 记录的坑。
    /// </para>
    /// <para>
    /// 注意 <c>Replace</c> 覆盖的是**任何**已有注册，不只是 <c>Null*</c> 默认实现。因此应用若要
    /// 自定义某个能力（例如换掉 <c>IFilesystemService</c>），必须注册在 <c>UseXxxNative()</c>
    /// **之后**——「顺序无关」只对 <c>AddMikoNative()</c> 的默认实现成立。
    /// </para>
    /// </summary>
    public static IServiceCollection ReplaceNative<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.Replace(ServiceDescriptor.Singleton<TService, TImplementation>());
        return services;
    }

    /// <summary>
    /// 以工厂方式覆盖某个 Native 能力接口，供需要构造参数的平台实现使用。
    /// </summary>
    public static IServiceCollection ReplaceNative<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> factory)
        where TService : class
    {
        services.Replace(ServiceDescriptor.Singleton(factory));
        return services;
    }
}
