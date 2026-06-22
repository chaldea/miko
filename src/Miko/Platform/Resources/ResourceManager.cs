using System.Collections.Concurrent;
using System.Reflection;
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
public sealed class ResourceManager : IImageLoader
{
    private readonly HttpClient _http;
    private readonly Assembly[] _resourceAssemblies;
    private readonly ConcurrentDictionary<string, Task<SKBitmap?>> _cache = new();

    /// <param name="httpClient">用于网络源的 HttpClient；为空时新建一个默认实例。</param>
    /// <param name="resourceAssemblies">res:// 源解析时查找的程序集；为空时回退到入口程序集。</param>
    public ResourceManager(HttpClient? httpClient = null, params Assembly[]? resourceAssemblies)
    {
        _http = httpClient ?? new HttpClient();
        _resourceAssemblies = resourceAssemblies is { Length: > 0 }
            ? resourceAssemblies
            : DefaultResourceAssemblies();
    }

    private static Assembly[] DefaultResourceAssemblies()
    {
        var entry = Assembly.GetEntryAssembly();
        return entry != null ? new[] { entry } : Array.Empty<Assembly>();
    }

    /// <inheritdoc />
    public Task<SKBitmap?> LoadAsync(MediaSource source, CancellationToken ct = default)
    {
        if (source.IsEmpty)
            return Task.FromResult<SKBitmap?>(null);

        return _cache.GetOrAdd(source.Raw, _ => LoadCoreAsync(source, ct));
    }

    private async Task<SKBitmap?> LoadCoreAsync(MediaSource source, CancellationToken ct)
    {
        try
        {
            return source.Scheme switch
            {
                MediaSourceScheme.File => await Task.Run(() => DecodeFile(source.Value), ct),
                MediaSourceScheme.Resource => DecodeResource(source.Value),
                MediaSourceScheme.Http or MediaSourceScheme.Https => await DecodeHttpAsync(source.Value, ct),
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

    private static SKBitmap? DecodeFile(string path)
    {
        if (!File.Exists(path)) return null;
        if (IsSvg(path))
        {
            using var stream = File.OpenRead(path);
            return RenderSvg(stream);
        }
        return SKBitmap.Decode(path);
    }

    private SKBitmap? DecodeResource(string resourceName)
    {
        foreach (var assembly in _resourceAssemblies)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;
            return IsSvg(resourceName) ? RenderSvg(stream) : SKBitmap.Decode(stream);
        }
        return null;
    }

    private async Task<SKBitmap?> DecodeHttpAsync(string url, CancellationToken ct)
    {
        var bytes = await _http.GetByteArrayAsync(url, ct).ConfigureAwait(false);
        if (IsSvg(url))
        {
            using var stream = new MemoryStream(bytes);
            return RenderSvg(stream);
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

    private static SKBitmap? RenderSvg(Stream stream)
    {
        var svg = new SKSvg();
        svg.Load(stream);
        if (svg.Picture == null) return null;

        int w = (int)svg.Picture.CullRect.Width;
        int h = (int)svg.Picture.CullRect.Height;
        if (w <= 0) w = 16;
        if (h <= 0) h = 16;

        var bitmap = new SKBitmap(w, h);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawPicture(svg.Picture);
        return bitmap;
    }

    private static bool IsSvg(string nameOrUrl) =>
        nameOrUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
}
