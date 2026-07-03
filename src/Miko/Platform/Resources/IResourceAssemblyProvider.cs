using System.Reflection;

namespace Miko.Platform.Resources;

/// <summary>
/// 资源程序集提供器接口。用于向 <see cref="ResourceManager"/> 注册包含嵌入资源的程序集，
/// 使 res:// 协议能够解析自定义程序集中的资源。
/// </summary>
public interface IResourceAssemblyProvider
{
    /// <summary>
    /// 获取所有注册的资源程序集。ResourceManager 将按顺序查找 res:// 资源。
    /// </summary>
    IEnumerable<Assembly> GetResourceAssemblies();
}
