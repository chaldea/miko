namespace Miko.Platform.Resources;

/// <summary>
/// 资源加载器的可观测面：供 DevTools 的资源面板展示「拉过什么、花了多久、缓存里还有什么」。
///
/// <para>与 <see cref="IImageIntrinsicSizeProvider"/> 同样是 <see cref="IImageLoader"/> 的<b>可选</b>
/// 附加接口——自定义加载器不实现它时，面板只显示嵌入资源清单（那部分来自程序集元数据，
/// 与加载器无关）。</para>
/// </summary>
public interface IResourceDiagnostics
{
    /// <summary>
    /// 单调递增的版本号：每次记录一条网络请求、或缓存新增一条目时递增。
    ///
    /// <para>供调试界面判断「是否有新内容」而无需每帧快照两份列表。必须是单调序号而非
    /// 条目数——网络日志有界，裁剪后条目数可能不变却已有新内容（与
    /// <c>DevTools.Logging.LogBuffer.Sequence</c> 同样的理由）。</para>
    /// </summary>
    long Version { get; }

    /// <summary>
    /// 本加载器发起过的网络请求。<b>仅包含经由 Miko 加载的资源</b>——应用自己用
    /// <c>HttpClient</c> 拉的字节不经过这里，也就无从记录。
    /// </summary>
    IReadOnlyList<NetworkResourceInfo> GetNetworkResources();

    /// <summary>当前解码缓存中的条目（按资源源去重后的已解码位图）。</summary>
    IReadOnlyList<CachedResourceInfo> GetCachedResources();
}

/// <summary>
/// 一次由 Miko 发起的网络资源加载。
/// </summary>
/// <param name="Url">请求地址。</param>
/// <param name="ByteCount">收到的字节数；失败为 0。</param>
/// <param name="Duration">从发起到收完字节的耗时（不含解码）。</param>
/// <param name="CompletedAt">完成时刻（本地时间）。</param>
/// <param name="Error">失败原因；成功为 <c>null</c>。</param>
public sealed record NetworkResourceInfo(
    string Url,
    long ByteCount,
    TimeSpan Duration,
    DateTime CompletedAt,
    string? Error)
{
    /// <summary>是否加载成功。</summary>
    public bool Succeeded => Error == null;
}

/// <summary>
/// 解码缓存中的一条资源。
/// </summary>
/// <param name="Source">原始书写串，如 <c>res://Assets/logo.svg</c>。</param>
/// <param name="Scheme">识别出的协议。</param>
/// <param name="PixelWidth">已解码位图的像素宽；未就绪或失败为 0。</param>
/// <param name="PixelHeight">已解码位图的像素高；未就绪或失败为 0。</param>
/// <param name="ByteCount">位图占用的字节数（像素数据，非源文件大小）。</param>
/// <param name="State">缓存条目状态。</param>
public sealed record CachedResourceInfo(
    string Source,
    Common.MediaSourceScheme Scheme,
    int PixelWidth,
    int PixelHeight,
    long ByteCount,
    CachedResourceState State);

/// <summary>解码缓存条目的状态。</summary>
public enum CachedResourceState
{
    /// <summary>仍在加载/解码中。</summary>
    Loading,
    /// <summary>已解码，位图可用。</summary>
    Loaded,
    /// <summary>加载或解码失败（缓存了失败结果，不会重试）。</summary>
    Failed,
}
