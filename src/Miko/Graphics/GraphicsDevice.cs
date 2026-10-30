using Miko.Graphics.Skia;
using Miko.Rendering;
using SkiaSharp;

namespace Miko.Graphics;

/// <summary>渲染后端使用的图形 API。</summary>
public enum GraphicsBackend
{
    /// <summary>CPU 光栅化，没有 GPU 上下文（无窗口渲染、测试）。</summary>
    Software,

    OpenGl,

    Metal,
}

/// <summary>
/// 一个 GPU 上下文（或 CPU 渲染的 <see cref="Software"/>）。平台宿主用原生句柄创建它，
/// 由它产出窗口与离屏的 <see cref="RenderSurface"/>，并交给引擎（<c>MikoEngine.GraphicsDevice</c>）
/// 供视频帧零拷贝包装 GPU 纹理（ISSUE-162）。
///
/// <para>宿主只传原生句柄（GL 函数加载器与帧缓冲 id、Metal 设备 / 命令队列 / 可绘制纹理），
/// 渲染后端的类型不出核心库。</para>
///
/// <para><b>线程</b>：GPU 设备不是线程安全的，除 <see cref="Software"/> 外只能在创建它的渲染线程使用。</para>
/// </summary>
public sealed class GraphicsDevice : IDisposable
{
    /// <summary>Miko 自建的 GPU 上下文默认的资源缓存上限（字节）。</summary>
    public const long DefaultResourceCacheLimit = GpuResourceCache.DefaultLimitBytes;

    private GRContext? _context;

    private GraphicsDevice(GRContext? context, GraphicsBackend backend)
    {
        _context = context;
        Backend = backend;
    }

    /// <summary>CPU 渲染：没有 GPU 上下文，纹理包装一律返回 <c>null</c>。进程内共享，释放它没有效果。</summary>
    public static GraphicsDevice Software { get; } = new(null, GraphicsBackend.Software);

    public GraphicsBackend Backend { get; }

    /// <summary>后端的 GPU 上下文；<see cref="Software"/> 与已释放的设备为 <c>null</c>。</summary>
    internal GRContext? Context => _context;

    /// <summary>
    /// 在当前线程的 OpenGL / GLES 上下文上创建设备（该上下文须已是当前上下文）。
    /// </summary>
    /// <param name="getProcAddress">GL 函数加载器；为 <c>null</c> 时用平台默认的加载方式（Android 的 EGL）。</param>
    /// <exception cref="InvalidOperationException">加载 GL 函数或创建上下文失败（驱动不支持所需的 GL 版本）。</exception>
    public static GraphicsDevice CreateOpenGl(Func<string, IntPtr>? getProcAddress = null)
    {
        var glInterface = getProcAddress == null
            ? GRGlInterface.Create()
            : GRGlInterface.Create(name => getProcAddress(name));
        if (glInterface == null)
            throw new InvalidOperationException("Skia could not load the OpenGL entry points.");

        var context = GRContext.CreateGl(glInterface)
            ?? throw new InvalidOperationException("Skia could not create an OpenGL context.");
        return new GraphicsDevice(context, GraphicsBackend.OpenGl);
    }

    /// <summary>用 Metal 设备与命令队列（<c>id&lt;MTLDevice&gt;</c> / <c>id&lt;MTLCommandQueue&gt;</c>）创建设备。</summary>
    /// <exception cref="InvalidOperationException">创建上下文失败。</exception>
    public static GraphicsDevice CreateMetal(IntPtr device, IntPtr commandQueue)
    {
        if (device == IntPtr.Zero) throw new ArgumentException("A Metal device is required.", nameof(device));
        if (commandQueue == IntPtr.Zero) throw new ArgumentException("A Metal command queue is required.", nameof(commandQueue));

        using var backend = new GRMtlBackendContext { DeviceHandle = device, QueueHandle = commandQueue };
        var context = GRContext.CreateMetal(backend)
            ?? throw new InvalidOperationException("Skia could not create a Metal context.");
        return new GraphicsDevice(context, GraphicsBackend.Metal);
    }

    /// <summary>
    /// GPU 资源缓存的上限（字节）。新建的设备保持后端的默认值；Miko 的桌面与 iOS 宿主设为
    /// <see cref="DefaultResourceCacheLimit"/>。<see cref="Software"/> 恒为 0。
    /// </summary>
    public long ResourceCacheLimit
    {
        get => _context?.GetResourceCacheLimit() ?? 0;
        set
        {
            if (_context != null) GpuResourceCache.Configure(_context, value);
        }
    }

    /// <summary>GPU 资源缓存当前保留的资源。</summary>
    public GpuResourceCacheUsage ResourceUsage
        => _context == null ? default : GpuResourceCache.GetUsage(_context);

    /// <summary>提交已记录的 GPU 命令。</summary>
    public void Flush() => _context?.Flush();

    /// <summary>
    /// 通知后端 GPU 状态已被外部改动（例如渲染到另一张离屏表面之后再绘制窗口），下次绘制前重新同步。
    /// </summary>
    public void ResetContext() => _context?.ResetContext();

    /// <summary>释放缓存中未被引用的 GPU 资源（内存告警时）。</summary>
    public void PurgeUnlockedResources() => GpuResourceCache.PurgeUnlockedResources(_context);

    /// <summary>释放缓存中的全部 GPU 资源。</summary>
    public void PurgeAllResources() => GpuResourceCache.PurgeAllResources(_context);

    /// <summary>
    /// 原生上下文已经失效（如 Android 的 EGL 上下文丢失）时调用：放弃设备而不再发出任何 GPU 命令。
    /// 之后设备不可再用。
    /// </summary>
    public void Abandon()
    {
        if (_context == null) return;
        _context.AbandonContext(releaseResources: false);
        _context.Dispose();
        _context = null;
    }

    /// <summary>
    /// 为 OpenGL 窗口的帧缓冲创建渲染目标。目标按尺寸与帧缓冲复用包装表面（见 <see cref="WindowRenderTarget"/>）。
    /// </summary>
    public WindowRenderTarget CreateWindowRenderTarget()
        => new(RequireContext(GraphicsBackend.OpenGl));

    /// <summary>
    /// 把一张 Metal 纹理（<c>id&lt;MTLTexture&gt;</c>，BGRA8，如 <c>CAMetalLayer</c> 的可绘制纹理）
    /// 包装为本帧的绘制表面。调用方在呈现后释放它（可绘制纹理每帧轮换，表面随帧新建）。
    /// 包装失败时返回 <c>null</c>。
    /// </summary>
    public RenderSurface? CreateMetalSurface(IntPtr texture, int width, int height)
    {
        var context = RequireContext(GraphicsBackend.Metal);
        if (texture == IntPtr.Zero || width <= 0 || height <= 0) return null;

        var target = new GRBackendRenderTarget(width, height, new GRMtlTextureInfo(texture));
        var surface = SKSurface.Create(context, target, GRSurfaceOrigin.TopLeft, SKColorType.Bgra8888);
        if (surface == null)
        {
            target.Dispose();
            return null;
        }
        return new RenderSurface(surface, width, height, target);
    }

    /// <summary>
    /// 创建一张离屏表面（像素尺寸）：GPU 设备上在显存中，<see cref="Software"/> 上在内存中。
    /// 调用方拥有返回值；创建失败时返回 <c>null</c>。
    /// </summary>
    /// <param name="subpixelText">
    /// 文本按水平 RGB 子像素（LCD）抗锯齿。只适合最终以原尺寸显示在 RGB 条纹屏上的内容。
    /// </param>
    public RenderSurface? CreateSurface(int width, int height, bool subpixelText = false)
    {
        if (width <= 0 || height <= 0) return null;

        var props = subpixelText ? new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal) : null;
        SKSurface? surface = _context != null
            ? SKSurface.Create(_context, budgeted: true,
                new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul), 0, props)
            : SKSurface.Create(new SKImageInfo(width, height), props);
        return surface == null ? null : new RenderSurface(surface, width, height);
    }

    /// <summary>
    /// 以像素数据创建图像（拷贝一份，CPU 内存）。<paramref name="rowBytes"/> 为 0 表示紧凑排列；
    /// 行带填充（硬解器输出常见）时必须给出真实的行字节数，否则画面会斜切。
    /// 尺寸非正或数据不足时返回 <c>null</c>。
    /// </summary>
    public Image? CreateImage(int width, int height, ImagePixelFormat format, ReadOnlySpan<byte> pixels, int rowBytes = 0)
        => SkiaImage.FromPixels(width, height, format, pixels, rowBytes);

    /// <summary>
    /// 以非托管内存中的像素创建图像（拷贝一份）。用于把平台缓冲（如锁定的 <c>CVPixelBuffer</c> 平面）上传。
    /// </summary>
    public Image? CreateImage(int width, int height, ImagePixelFormat format, IntPtr pixels, int rowBytes)
        => SkiaImage.FromPixels(width, height, format, pixels, rowBytes);

    /// <summary>
    /// 把一张 GL 纹理包装为图像，不拷贝像素（视频零拷贝）。纹理仍归调用方所有，图像释放时不删除它。
    /// 设备不是 OpenGL 时返回 <c>null</c>。
    /// </summary>
    /// <param name="target">纹理目标，如 <c>GL_TEXTURE_2D</c>、<c>GL_TEXTURE_EXTERNAL_OES</c>。</param>
    /// <param name="id">纹理对象 id。</param>
    /// <param name="glFormat">GL 内部格式，如 <c>GL_RGBA8</c>、<c>GL_R8</c>。</param>
    /// <param name="format">采样结果的通道含义。</param>
    public Image? WrapGlTexture(uint target, uint id, uint glFormat, int width, int height, ImagePixelFormat format)
    {
        if (Backend != GraphicsBackend.OpenGl || _context == null || width <= 0 || height <= 0) return null;

        using var texture = new GRBackendTexture(width, height, mipmapped: false, new GRGlTextureInfo(target, id, glFormat));
        return SkiaImage.FromTexture(_context, texture, format);
    }

    /// <summary>
    /// 把一张 Metal 纹理（<c>id&lt;MTLTexture&gt;</c>）包装为图像，不拷贝像素（视频零拷贝）。
    /// 纹理仍归调用方所有，且须活到本帧 GPU 命令执行完毕。设备不是 Metal 时返回 <c>null</c>。
    /// </summary>
    public Image? WrapMetalTexture(IntPtr texture, int width, int height, ImagePixelFormat format)
    {
        if (Backend != GraphicsBackend.Metal || _context == null || texture == IntPtr.Zero || width <= 0 || height <= 0)
            return null;

        using var backendTexture = new GRBackendTexture(width, height, mipmapped: false, new GRMtlTextureInfo(texture));
        return SkiaImage.FromTexture(_context, backendTexture, format);
    }

    /// <summary>后端进程级共享缓存（CPU 光栅缓存、字形缓存）的占用，诊断用。</summary>
    public static GraphicsCacheUsage GetSharedCacheUsage()
        => new(SKGraphics.GetResourceCacheTotalBytesUsed(), SKGraphics.GetFontCacheUsed(), SKGraphics.GetFontCacheCountUsed());

    /// <summary>清空资源缓存后释放 GPU 上下文。之前应先释放引用它的表面与图像。<see cref="Software"/> 上无效果。</summary>
    public void Dispose()
    {
        if (_context == null) return;
        GpuResourceCache.PurgeAllResources(_context);
        _context.Dispose();
        _context = null;
    }

    private GRContext RequireContext(GraphicsBackend backend)
    {
        if (Backend != backend)
            throw new InvalidOperationException($"This operation needs a {backend} device; this device is {Backend}.");
        return _context ?? throw new ObjectDisposedException(nameof(GraphicsDevice));
    }
}

/// <summary>后端进程级共享缓存的占用（字节 / 条目数）。</summary>
public readonly record struct GraphicsCacheUsage(long RasterCacheBytes, long FontCacheBytes, int FontCacheCount);
