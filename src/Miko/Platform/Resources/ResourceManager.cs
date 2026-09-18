using System.Collections.Concurrent;
using Miko.Common;
using SkiaSharp;
using Svg.Skia;

namespace Miko.Platform.Resources;

/// <summary>
/// 统一资源管理器：默认的 <see cref="IImageLoader"/> 实现，处理 file:// / res:// / http(s):// / data: 四类协议。
/// <list type="bullet">
///   <item><c>file</c>：本地文件（含裸路径），<see cref="SKBitmap.Decode(string)"/> 或 SVG 渲染。</item>
///   <item><c>res</c>：嵌入资源，逻辑路径经 <see cref="EmbeddedResources"/> 换算为清单资源名后按注册程序集顺序查找。</item>
///   <item><c>http(s)</c>：经 <see cref="HttpClient"/> 拉取字节后解码。</item>
///   <item><c>data</c>：内联 base64 解码。</item>
/// </list>
/// 解码结果按 <see cref="MediaSource.Raw"/> 缓存，去重并发与重复请求（如缩略图网格共享 URL）。
/// <para>同时实现 <see cref="IResourceDiagnostics"/>，把网络请求与缓存内容暴露给 DevTools 的资源面板。</para>
/// </summary>
public sealed class ResourceManager : IImageLoader, IImageIntrinsicSizeProvider, IResourceDiagnostics
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
    /// 缓存键 → 该键的资源源（协议来自它）。<see cref="_cache"/> 只按 <see cref="MediaSource.Raw"/>
    /// 索引任务，协议已被丢弃，而 <see cref="GetCachedResources"/> 要展示它。
    /// </summary>
    private readonly ConcurrentDictionary<string, MediaSource> _cachedSources = new();

    /// <summary>
    /// 本加载器发起过的网络请求记录（DevTools 资源面板）。有界：超出容量丢弃最旧的一条，
    /// 长会话（如缩略图无限滚动）下不会无界增长。
    /// </summary>
    private readonly Queue<NetworkResourceInfo> _networkLog = new();
    private readonly object _networkLogGate = new();

    /// <summary>网络请求记录的保留条数上限。</summary>
    private const int MaxNetworkLogEntries = 500;

    /// <summary><see cref="Version"/> 的后备字段。</summary>
    private long _diagnosticsVersion;

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

        // 记录源本身（协议在 _cache 的键里丢失了），供 DevTools 的缓存列表展示。
        _cachedSources[source.Raw] = source;

        bool added = false;
        var task = _cache.GetOrAdd(source.Raw, _ =>
        {
            added = true;
            return LoadCoreAsync(source, ct);
        });

        if (added)
        {
            // 新条目进缓存，以及它随后完成（Loading → Loaded/Failed）都是调试界面的可见变化。
            Interlocked.Increment(ref _diagnosticsVersion);
            task.ContinueWith(
                static (_, state) => Interlocked.Increment(ref ((ResourceManager)state!)._diagnosticsVersion),
                this,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        return task;
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

    private SKBitmap? DecodeResource(string resourcePath, string cacheKey)
    {
        // 逻辑路径（res://Assets/a.svg）→ 真实清单资源名，换算封装在 EmbeddedResources（ISSUE-139）。
        var located = EmbeddedResources.Locate(_assemblyProvider, resourcePath);
        if (located == null) return null;

        var (assembly, manifestName) = located.Value;
        using var stream = assembly.GetManifestResourceStream(manifestName);
        if (stream == null) return null;

        // 格式按解析出的清单名判定：逻辑路径与清单名的扩展名相同，但清单名是真正被打开的那个。
        return IsSvg(manifestName) ? RenderSvg(stream, cacheKey) : SKBitmap.Decode(stream);
    }

    private async Task<SKBitmap?> DecodeHttpAsync(string url, string cacheKey, CancellationToken ct)
    {
        // 只计「取字节」的耗时，不含解码：面板展示的是网络开销，解码是 CPU 开销。
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        byte[] bytes;
        try
        {
            bytes = await _http.GetByteArrayAsync(url, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RecordNetworkLoad(url, 0, System.Diagnostics.Stopwatch.GetElapsedTime(started), ex.Message);
            throw;
        }

        RecordNetworkLoad(url, bytes.Length, System.Diagnostics.Stopwatch.GetElapsedTime(started), null);

        if (IsSvg(url))
        {
            using var stream = new MemoryStream(bytes);
            return RenderSvg(stream, cacheKey);
        }
        return SKBitmap.Decode(bytes);
    }

    private void RecordNetworkLoad(string url, long byteCount, TimeSpan duration, string? error)
    {
        var entry = new NetworkResourceInfo(url, byteCount, duration, DateTime.Now, error);
        lock (_networkLogGate)
        {
            _networkLog.Enqueue(entry);
            while (_networkLog.Count > MaxNetworkLogEntries)
                _networkLog.Dequeue();
        }
        Interlocked.Increment(ref _diagnosticsVersion);
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

    // ---------------------------------------------------------------------
    // IResourceDiagnostics（DevTools 资源面板）
    // ---------------------------------------------------------------------

    /// <inheritdoc />
    public long Version => Interlocked.Read(ref _diagnosticsVersion);

    /// <inheritdoc />
    public IReadOnlyList<NetworkResourceInfo> GetNetworkResources()
    {
        lock (_networkLogGate)
        {
            return _networkLog.ToArray();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<CachedResourceInfo> GetCachedResources()
    {
        var result = new List<CachedResourceInfo>(_cache.Count);

        foreach (var (key, task) in _cache)
        {
            var source = _cachedSources.TryGetValue(key, out var s) ? s : MediaSource.Parse(key);

            // 不 await：面板是同步快照，在途的条目就报 Loading。
            if (!task.IsCompleted)
            {
                result.Add(new CachedResourceInfo(key, source.Scheme, 0, 0, 0, CachedResourceState.Loading));
                continue;
            }

            var bitmap = task.Status == TaskStatus.RanToCompletion ? task.Result : null;
            if (bitmap == null)
            {
                result.Add(new CachedResourceInfo(key, source.Scheme, 0, 0, 0, CachedResourceState.Failed));
                continue;
            }

            result.Add(new CachedResourceInfo(
                key,
                source.Scheme,
                bitmap.Width,
                bitmap.Height,
                bitmap.ByteCount,
                CachedResourceState.Loaded));
        }

        result.Sort(static (a, b) => string.CompareOrdinal(a.Source, b.Source));
        return result;
    }
}
