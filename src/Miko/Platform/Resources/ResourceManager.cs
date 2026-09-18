using System.Collections.Concurrent;
using Miko.Common;
using SkiaSharp;
using Svg.Skia;

namespace Miko.Platform.Resources;

/// <summary>
/// 统一资源管理器：默认的 <see cref="IImageLoader"/> 实现，处理 file:// / res:// / http(s):// / data: 四类协议。
/// <list type="bullet">
///   <item><c>file</c>：本地文件（含裸路径），<see cref="SKBitmap.Decode(string)"/> 或 SVG 渲染。</item>
///   <item><c>res</c>：嵌入资源，按注册程序集顺序 <c>GetManifestResourceStream</c>。</item>
///   <item><c>http(s)</c>：经 <see cref="HttpClient"/> 拉取字节后解码。</item>
///   <item><c>data</c>：内联 base64 解码。</item>
/// </list>
/// 解码结果按 <see cref="MediaSource.Raw"/> 缓存，去重并发与重复请求（如缩略图网格共享 URL）。
/// </summary>
public sealed class ResourceManager : IImageLoader, IImageIntrinsicSizeProvider
{
    /// <summary>
    /// SVG 栅格化的分辨率下限（长边像素）。
    ///
    /// <para>SVG 没有固有分辨率，按 <c>viewBox</c> 栅格化会在高密度屏上被画布放大插值，
    /// 边缘发虚（ISSUE-137）。<c>&lt;img&gt;</c> 的显示尺寸在加载时还未知（布局尚未发生），
    /// 因此这里无法像背景图那样按实际绘制尺寸栅格化，改为一次性过采样到足够大的分辨率，
    /// 之后无论布局到多大、设备密度多高，Skia 都是在<b>缩小</b>采样（无损）而非放大插值。</para>
    ///
    /// <para>512 覆盖了 4× 密度下 128 逻辑像素的图片（头像、插画的常见上限）；
    /// 已经不小于该值的 <c>viewBox</c>（如 512×512 的头像）保持原样，不额外放大。</para>
    /// </summary>
    private const int MinSvgRasterSize = 512;

    private readonly HttpClient _http;
    private readonly IResourceAssemblyProvider _assemblyProvider;
    private readonly ConcurrentDictionary<string, Task<SKBitmap?>> _cache = new();

    /// <summary>
    /// 过采样栅格化的矢量源 → 其 CSS 内禀（<c>viewBox</c>）尺寸。
    /// 供 <see cref="GetIntrinsicSize"/> 让引擎的内禀尺寸不受过采样影响（ISSUE-137）。
    /// 只记录矢量源；位图源不入表，引擎按位图像素尺寸处理。
    /// </summary>
    private readonly ConcurrentDictionary<string, (int Width, int Height)> _logicalSizes = new();

    /// <summary>
    /// 创建 ResourceManager 实例（推荐通过 DI 容器注入）
    /// </summary>
    /// <param name="httpClient">用于网络源的 HttpClient；为空时新建一个默认实例。</param>
    /// <param name="assemblyProvider">资源程序集提供器；为空时使用默认提供器（仅包含入口程序集）。</param>
    public ResourceManager(HttpClient? httpClient = null, IResourceAssemblyProvider? assemblyProvider = null)
    {
        _http = httpClient ?? new HttpClient();
        _assemblyProvider = assemblyProvider ?? new ResourceAssemblyProvider();
    }

    /// <inheritdoc />
    public Task<SKBitmap?> LoadAsync(MediaSource source, CancellationToken ct = default)
    {
        if (source.IsEmpty)
            return Task.FromResult<SKBitmap?>(null);

        return _cache.GetOrAdd(source.Raw, _ => LoadCoreAsync(source, ct));
    }

    /// <inheritdoc />
    public (int Width, int Height)? GetIntrinsicSize(MediaSource source)
    {
        if (source.IsEmpty) return null;
        return _logicalSizes.TryGetValue(source.Raw, out var size) ? size : null;
    }

    private async Task<SKBitmap?> LoadCoreAsync(MediaSource source, CancellationToken ct)
    {
        try
        {
            // 矢量源的过采样尺寸要记录到 source.Raw 名下（GetIntrinsicSize 的键）。
            var key = source.Raw;
            return source.Scheme switch
            {
                MediaSourceScheme.File => await Task.Run(() => DecodeFile(source.Value, key), ct),
                MediaSourceScheme.Resource => DecodeResource(source.Value, key),
                MediaSourceScheme.Http or MediaSourceScheme.Https => await DecodeHttpAsync(source.Value, key, ct),
                MediaSourceScheme.Data => DecodeData(source.Value),
                _ => null,
            };
        }
        catch
        {
            // 加载失败不应中断渲染：返回 null，上层回退到占位图/背景。
            return null;
        }
    }

    private SKBitmap? DecodeFile(string path, string cacheKey)
    {
        // 智能路径解析：支持多种场景
        string? resolvedPath = ResolveFilePath(path);
        if (resolvedPath == null || !File.Exists(resolvedPath))
            return null;

        if (IsSvg(resolvedPath))
        {
            using var stream = File.OpenRead(resolvedPath);
            return RenderSvg(stream, cacheKey);
        }
        return SKBitmap.Decode(resolvedPath);
    }

    /// <summary>
    /// 智能解析文件路径，处理多种启动场景：
    /// 1. 绝对路径：直接使用
    /// 2. 相对路径：按照应用目录计算
    /// </summary>
    private static string? ResolveFilePath(string path)
    {
        // 1. 如果是绝对路径，直接返回
        if (Path.IsPathRooted(path))
        {
            return File.Exists(path) ? path : null;
        }

        var appBasePath = Path.Combine(AppContext.BaseDirectory, path);
        if (File.Exists(appBasePath))
            return appBasePath;

        return null;
    }

    private SKBitmap? DecodeResource(string resourceName, string cacheKey)
    {
        foreach (var assembly in _assemblyProvider.GetResourceAssemblies())
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;
            return IsSvg(resourceName) ? RenderSvg(stream, cacheKey) : SKBitmap.Decode(stream);
        }
        return null;
    }

    private async Task<SKBitmap?> DecodeHttpAsync(string url, string cacheKey, CancellationToken ct)
    {
        var bytes = await _http.GetByteArrayAsync(url, ct).ConfigureAwait(false);
        if (IsSvg(url))
        {
            using var stream = new MemoryStream(bytes);
            return RenderSvg(stream, cacheKey);
        }
        return SKBitmap.Decode(bytes);
    }

    private static SKBitmap? DecodeData(string dataUri)
    {
        // data:[<mime>][;base64],<payload>
        var comma = dataUri.IndexOf(',');
        if (comma < 0) return null;
        var meta = dataUri.Substring(0, comma);
        var payload = dataUri.Substring(comma + 1);

        if (meta.Contains("base64", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = Convert.FromBase64String(payload);
            return SKBitmap.Decode(bytes);
        }
        // 非 base64 的 data URI（URL 编码文本）当前不支持位图解码。
        return null;
    }

    /// <summary>
    /// 把 SVG 栅格化为位图。分辨率不低于 <see cref="MinSvgRasterSize"/>（长边），使图片在高密度
    /// 屏上始终走"缩小采样"而非"放大插值"（ISSUE-137）。过采样时把 <c>viewBox</c> 逻辑尺寸
    /// 记入 <see cref="_logicalSizes"/>，让 <c>&lt;img&gt;</c> 的内禀尺寸保持为 <c>viewBox</c>，
    /// 布局不受影响。
    /// </summary>
    private SKBitmap? RenderSvg(Stream stream, string cacheKey)
    {
        var svg = new SKSvg();
        svg.Load(stream);
        if (svg.Picture == null) return null;

        int logicalW = (int)svg.Picture.CullRect.Width;
        int logicalH = (int)svg.Picture.CullRect.Height;
        if (logicalW <= 0) logicalW = 16;
        if (logicalH <= 0) logicalH = 16;

        // 等比放大到长边至少 MinSvgRasterSize；已足够大的 viewBox 保持 1:1（factor = 1）。
        int longSide = Math.Max(logicalW, logicalH);
        int factor = Math.Max(1, (int)Math.Ceiling((double)MinSvgRasterSize / longSide));
        int w = logicalW * factor;
        int h = logicalH * factor;

        var bitmap = new SKBitmap(w, h);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        if (factor != 1)
            canvas.Scale(factor);

        // 使用之前在 ISSUE-073 中添加的抗锯齿设置
        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawPicture(svg.Picture, paint);

        if (factor != 1)
            _logicalSizes[cacheKey] = (logicalW, logicalH);

        return bitmap;
    }

    private static bool IsSvg(string nameOrUrl) =>
        nameOrUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
}
