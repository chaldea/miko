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
///
/// <para><b>失败、取消与共享（ISSUE-154 BUG-05）。</b>只有成功解码的位图会一直留在缓存里：</para>
/// <list type="bullet">
/// <item>加载失败（文件不存在、网络错误、解码失败）的条目仍出现在诊断列表里（状态 Failed），但
/// 同一路径的下一次请求会重新加载——文件出现、网络恢复之后图片就能加载出来，而不是永远拿到缓存的 null。</item>
/// <item>同一路径的并发请求共享一次加载，这次加载不受任何单个调用方的 <see cref="CancellationToken"/>
/// 控制：某个调用方取消只让<b>它自己</b>得到已取消的任务，其它调用方照常拿到位图。只有当所有在等的
/// 调用方都取消了，共享加载才被取消，其条目随即移出缓存。</item>
/// </list>
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
    private readonly ConcurrentDictionary<string, LoadEntry> _cache = new();

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
    /// <remarks>
    /// 失败返回 <c>null</c>，且不会被永久缓存：之后的同一路径请求会重新加载。<paramref name="ct"/>
    /// 只作用于本次调用——取消时本次调用得到已取消的任务，同一路径上其它调用方共享的加载不受影响。
    /// </remarks>
    public Task<SKBitmap?> LoadAsync(MediaSource source, CancellationToken ct = default)
    {
        if (source.IsEmpty)
            return Task.FromResult<SKBitmap?>(null);
        if (ct.IsCancellationRequested)
            return Task.FromCanceled<SKBitmap?>(ct);

        // 记录源本身（协议在 _cache 的键里丢失了），供 DevTools 的缓存列表展示。
        _cachedSources[source.Raw] = source;

        while (true)
        {
            if (_cache.TryGetValue(source.Raw, out var existing))
            {
                // 成功的条目、在途的条目直接共享；失败的条目（或所有等待者都已取消、正在作废的条目）
                // 换成一次新的加载——这正是「文件出现/网络恢复后能重新加载」的入口。
                if (!existing.IsFailed && existing.TryJoin(ct, out var shared))
                    return shared;

                var retry = new LoadEntry();
                if (_cache.TryUpdate(source.Raw, retry, existing))
                    return StartLoad(source, retry, ct);
                continue;
            }

            var created = new LoadEntry();
            if (_cache.TryAdd(source.Raw, created))
                return StartLoad(source, created, ct);
        }
    }

    /// <summary>
    /// 开始 <paramref name="entry"/>（已发布进缓存）的共享加载，并把本次调用登记为它的第一个等待者。
    /// </summary>
    private Task<SKBitmap?> StartLoad(MediaSource source, LoadEntry entry, CancellationToken ct)
    {
        // 新条目进缓存，以及它随后完成（Loading → Loaded/Failed）都是调试界面的可见变化。
        Interlocked.Increment(ref _diagnosticsVersion);
        entry.Task.ContinueWith(
            static (_, state) =>
            {
                var (manager, key, completed) = ((ResourceManager, string, LoadEntry))state!;
                // 因所有等待者取消而停下的加载什么也没留下，不该继续占着缓存（也不该显示成 Failed）。
                if (completed.IsCanceled)
                    manager._cache.TryRemove(new KeyValuePair<string, LoadEntry>(key, completed));
                Interlocked.Increment(ref manager._diagnosticsVersion);
            },
            (this, source.Raw, entry),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        // 先登记等待者再开始加载：同步完成的源（嵌入资源、data:）在 Run 里就会完成。
        entry.TryJoin(ct, out var waiter);
        entry.Run(token => LoadCoreAsync(source, token));
        return waiter;
    }

    /// <summary>
    /// 同一路径的一次共享加载。加载本身只听从内部的取消源：调用方各自的
    /// <see cref="CancellationToken"/> 只结束各自的等待，最后一个可取消的等待者离开且没有不可取消的
    /// 等待者时，才取消共享加载。
    /// </summary>
    private sealed class LoadEntry
    {
        private readonly TaskCompletionSource<SKBitmap?> _completion = new();
        private readonly CancellationTokenSource _cancellation = new();
        private readonly object _gate = new();

        /// <summary>带可取消令牌、仍在等待的调用方数。</summary>
        private int _waiters;

        /// <summary>有过不可取消的调用方：它会一直等到底，共享加载因此不能被取消。</summary>
        private bool _pinned;

        /// <summary>所有等待者都已取消，共享加载被放弃（随后取消）。</summary>
        private volatile bool _abandoned;

        /// <summary>共享加载的结果：成功为位图，失败或被取消为 null。从不出错、从不取消。</summary>
        public Task<SKBitmap?> Task => _completion.Task;

        /// <summary>加载已结束但没有得到位图（失败或被取消）。这样的条目由下一次请求替换。</summary>
        public bool IsFailed => Task.IsCompleted && Task.Result is null;

        /// <summary>共享加载已因所有等待者取消而被放弃。</summary>
        public bool IsCanceled => _abandoned;

        public void Run(Func<CancellationToken, Task<SKBitmap?>> load) => _ = RunAsync(load);

        private async Task RunAsync(Func<CancellationToken, Task<SKBitmap?>> load)
        {
            SKBitmap? result = null;
            try
            {
                result = await load(_cancellation.Token).ConfigureAwait(false);
            }
            catch
            {
                // LoadCoreAsync 已吞掉加载错误；这里只是不让任何意外让等待者永远挂起。
            }
            _completion.TrySetResult(result);
        }

        /// <summary>
        /// 登记一个等待者。返回 false 表示本条目已经作废（所有等待者都取消了），调用方应换一个新条目。
        /// </summary>
        public bool TryJoin(CancellationToken ct, out Task<SKBitmap?> waiter)
        {
            lock (_gate)
            {
                if (_abandoned)
                {
                    waiter = null!;
                    return false;
                }
                if (Task.IsCompleted || !ct.CanBeCanceled)
                {
                    if (!Task.IsCompleted) _pinned = true;
                    waiter = Task;
                    return true;
                }
                _waiters++;
            }

            waiter = Task.WaitAsync(ct);
            waiter.ContinueWith(
                static (t, state) => ((LoadEntry)state!).Leave(t.IsCanceled),
                this,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
            return true;
        }

        private void Leave(bool canceled)
        {
            lock (_gate)
            {
                _waiters--;
                if (!canceled || _waiters > 0 || _pinned || Task.IsCompleted) return;
                // 在锁内标记作废，使随后的 TryJoin 立即改用新条目；取消回调（中断 HTTP 等）放到锁外执行。
                _abandoned = true;
            }
            _cancellation.Cancel();
        }
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

        foreach (var (key, entry) in _cache)
        {
            var task = entry.Task;
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
