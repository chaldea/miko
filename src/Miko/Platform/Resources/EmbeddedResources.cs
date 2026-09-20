using System.Collections.Concurrent;
using System.Reflection;

namespace Miko.Platform.Resources;

/// <summary>
/// 嵌入资源的<b>路径解析</b>与<b>清单枚举</b>。
///
/// <para>用户书写的 <c>res://</c> 路径与 <c>file://</c> 同构、且不含程序集名：
/// <c>res://Assets/thumbnail.svg</c>。而 .NET 的清单资源名是 MSBuild 按
/// <c>&lt;RootNamespace&gt;.&lt;目录&gt;.&lt;文件名&gt;</c> 拼出来的（<c>IonicDemo.Assets.thumbnail.svg</c>）。
/// 两者的换算只发生在这里，不外泄到用户端——把程序集名写进资源路径会让「换程序集名/挪项目」
/// 变成全仓改路径（ISSUE-139）。</para>
///
/// <para>解析策略：先把逻辑路径归一为点分形式（<c>Assets.thumbnail.svg</c>），在每个注册程序集的
/// 清单里<b>先精确匹配、再按点边界后缀匹配</b>。后缀匹配同时让旧写法
/// （<c>res://IonicDemo.Assets.thumbnail.svg</c>，已含程序集名）继续可用——它对自己的程序集
/// 恰好是精确匹配，因此新旧写法可以共存，无需为兼容单独开一条分支。</para>
///
/// <para>点边界是必须的：裸后缀比较会让 <c>res://avatar.svg</c> 命中
/// <c>App.Assets.my-avatar.svg</c>。比较 <c>"." + 归一路径</c> 即可排除。</para>
/// </summary>
public static class EmbeddedResources
{
    /// <summary>
    /// 每个程序集的清单资源名。<see cref="Assembly.GetManifestResourceNames"/> 每次调用都新建数组，
    /// 而解析发生在图片/图标加载路径上（一个列表页可能解析几十次），故缓存。
    /// 清单是程序集的静态元数据，进程内不会变，缓存无失效问题。
    /// </summary>
    private static readonly ConcurrentDictionary<Assembly, string[]> _manifestNames = new();

    /// <summary>
    /// 把 <c>res://</c> 的逻辑路径归一为清单资源名的点分形式：
    /// 目录分隔符（<c>/</c> 与 <c>\</c>）转 <c>.</c>，去掉前导的 <c>./</c> 与 <c>/</c>。
    /// 已是点分形式（旧写法）的路径原样返回。
    /// </summary>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;

        var span = path.AsSpan();
        // 去掉前导 "./" 与 "/"，使 res://./Assets/a.svg 与 res:///Assets/a.svg 等价于 res://Assets/a.svg。
        while (span.Length > 0)
        {
            if (span[0] == '/' || span[0] == '\\') { span = span[1..]; continue; }
            if (span.Length >= 2 && span[0] == '.' && (span[1] == '/' || span[1] == '\\')) { span = span[2..]; continue; }
            break;
        }

        return span.ToString().Replace('/', '.').Replace('\\', '.');
    }

    /// <summary>
    /// 在单个程序集中把逻辑路径解析为真实的清单资源名。找不到返回 <c>null</c>。
    /// </summary>
    public static string? ResolveName(Assembly assembly, string path)
    {
        var normalized = NormalizePath(path);
        if (normalized.Length == 0) return null;

        var names = GetManifestNames(assembly);

        // 精确匹配优先：旧写法（含程序集名/根命名空间）走这条。
        foreach (var name in names)
        {
            if (string.Equals(name, normalized, StringComparison.Ordinal))
                return name;
        }

        // 再按点边界后缀匹配：新写法（不含程序集名）走这条。
        var suffix = "." + normalized;
        foreach (var name in names)
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal))
                return name;
        }

        return null;
    }

    /// <summary>
    /// 按注册顺序在 <paramref name="assemblyProvider"/> 的程序集中定位嵌入资源，
    /// 返回第一个命中的程序集与它的清单资源名。未命中返回 <c>null</c>。
    /// <para>分两步（先定位、再由调用方取流）是因为调用方对流的用法不同：
    /// <see cref="ResourceManager"/> 直接解码，<c>Miko.Ionic.IconResolver</c> 要把程序集
    /// 交给 <c>BackgroundImage.FromResource</c>。</para>
    /// </summary>
    public static (Assembly Assembly, string ManifestName)? Locate(
        IResourceAssemblyProvider? assemblyProvider, string path)
    {
        if (assemblyProvider == null) return null;

        foreach (var assembly in assemblyProvider.GetResourceAssemblies())
        {
            var name = ResolveName(assembly, path);
            if (name != null) return (assembly, name);
        }
        return null;
    }

    /// <summary>
    /// 枚举所有注册程序集的嵌入资源清单（供 DevTools 的资源面板展示）。
    /// 按程序集的注册顺序、每个程序集内按清单资源名排序，使展示稳定。
    /// </summary>
    public static IReadOnlyList<EmbeddedResourceInfo> Enumerate(IResourceAssemblyProvider? assemblyProvider)
    {
        if (assemblyProvider == null) return Array.Empty<EmbeddedResourceInfo>();

        var result = new List<EmbeddedResourceInfo>();
        var seen = new HashSet<Assembly>();

        foreach (var assembly in assemblyProvider.GetResourceAssemblies())
        {
            // 同一程序集可能被注册多次（入口程序集 + 显式 AddResourceAssembly）：只列一次。
            if (!seen.Add(assembly)) continue;

            var assemblyName = assembly.GetName().Name ?? assembly.FullName ?? "<unknown>";
            var names = GetManifestNames(assembly);
            var sorted = names.ToArray();
            Array.Sort(sorted, StringComparer.Ordinal);

            foreach (var name in sorted)
            {
                result.Add(new EmbeddedResourceInfo(
                    name,
                    ToLogicalPath(name, assemblyName),
                    assemblyName,
                    GetResourceSize(assembly, name)));
            }
        }

        return result;
    }

    /// <summary>
    /// 清单资源名 → 可书写的 <c>res://</c> 逻辑路径（展示用的反向映射）。
    ///
    /// <para>先剥掉程序集简名前缀（根命名空间与程序集名不一致时剥不掉，此时保留整名——
    /// 它对该程序集仍是能命中的精确匹配），再把「除文件名与扩展名之外」的点还原成 <c>/</c>。</para>
    ///
    /// <para>文件名自带点（<c>avatar.min.svg</c>）时无法与目录分隔区分，会显示成
    /// <c>Assets/avatar/min.svg</c>。面板同时展示原始清单名，故不会因此丢信息。</para>
    /// </summary>
    public static string ToLogicalPath(string manifestName, string assemblyName)
    {
        var name = manifestName;
        var prefix = assemblyName + ".";
        if (name.StartsWith(prefix, StringComparison.Ordinal))
            name = name.Substring(prefix.Length);

        // 末两段是「文件名 . 扩展名」，其前面的点才是目录分隔。
        var segments = name.Split('.');
        if (segments.Length <= 2) return name;

        var directories = string.Join('/', segments, 0, segments.Length - 2);
        return $"{directories}/{segments[^2]}.{segments[^1]}";
    }

    private static long GetResourceSize(Assembly assembly, string manifestName)
    {
        try
        {
            // 清单资源流由映射的 PE 映像支撑，Length 是 O(1)，不读取内容。
            using var stream = assembly.GetManifestResourceStream(manifestName);
            return stream?.Length ?? 0;
        }
        catch
        {
            // 非二进制清单资源（如 .resources 容器）在个别情形下取流会失败：不阻断枚举。
            return 0;
        }
    }

    private static string[] GetManifestNames(Assembly assembly) =>
        _manifestNames.GetOrAdd(assembly, static a =>
        {
            try
            {
                return a.GetManifestResourceNames();
            }
            catch
            {
                // 动态程序集没有清单。
                return Array.Empty<string>();
            }
        });
}

/// <summary>
/// 一条嵌入资源的元信息（DevTools 资源面板的行模型）。
/// </summary>
/// <param name="ManifestName">真实的清单资源名，如 <c>IonicDemo.Assets.thumbnail.svg</c>。</param>
/// <param name="LogicalPath">可书写的 <c>res://</c> 路径，如 <c>Assets/thumbnail.svg</c>。</param>
/// <param name="AssemblyName">所属程序集的简名。</param>
/// <param name="ByteCount">资源字节数。</param>
public sealed record EmbeddedResourceInfo(
    string ManifestName,
    string LogicalPath,
    string AssemblyName,
    long ByteCount);
