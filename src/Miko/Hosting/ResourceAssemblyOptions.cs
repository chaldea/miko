using System.Reflection;

namespace Miko.Hosting;

/// <summary>
/// 资源程序集配置选项。用于配置 res:// 协议的程序集查找列表。
/// </summary>
public class ResourceAssemblyOptions
{
    /// <summary>
    /// 包含嵌入资源的程序集列表。ResourceManager 将按顺序查找 res:// 资源。
    /// </summary>
    public List<Assembly> Assemblies { get; } = new();
}
