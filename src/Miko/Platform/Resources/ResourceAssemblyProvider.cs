using System.Reflection;
using Microsoft.Extensions.Options;
using Miko.Hosting;

namespace Miko.Platform.Resources;

/// <summary>
/// 默认的资源程序集提供器实现。支持动态添加资源程序集。
/// </summary>
internal sealed class ResourceAssemblyProvider : IResourceAssemblyProvider
{
    private readonly List<Assembly> _assemblies = new();

    public ResourceAssemblyProvider(IOptions<ResourceAssemblyOptions>? options = null)
    {
        // 默认包含入口程序集
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            _assemblies.Add(entryAssembly);
        }

        // 添加通过配置注册的程序集
        if (options?.Value != null)
        {
            AddAssemblies(options.Value.Assemblies.ToArray());
        }
    }

    /// <summary>
    /// 添加资源程序集
    /// </summary>
    public void AddAssembly(Assembly assembly)
    {
        if (assembly != null && !_assemblies.Contains(assembly))
        {
            _assemblies.Add(assembly);
        }
    }

    /// <summary>
    /// 添加多个资源程序集
    /// </summary>
    public void AddAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            AddAssembly(assembly);
        }
    }

    public IEnumerable<Assembly> GetResourceAssemblies() => _assemblies;
}
