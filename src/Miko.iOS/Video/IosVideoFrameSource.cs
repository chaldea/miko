using CoreVideo;
using Miko.Platform.Video;
using OpenGLES;
using SkiaSharp;

namespace Miko.iOS.Video;

/// <summary>
/// iOS 帧源：把 <c>CVPixelBuffer</c>（IOSurface 支撑）零拷贝映射为 GL ES 纹理，
/// 再由 SkSL shader 在 GPU 上完成 NV12→RGB。
///
/// <para>
/// **零拷贝路径**：<c>CVOpenGLESTextureCache</c> 把像素缓冲的 IOSurface 直接绑定为
/// GL 纹理，不产生像素拷贝。NV12 的两个平面各映射一张纹理
/// （平面 0 → <c>R8</c> 亮度，平面 1 → <c>RG8</c> 色度），交给
/// <see cref="Nv12FrameComposer"/> 合成。
/// </para>
/// <para>
/// 宿主用的是 <c>SKGLView</c>（OpenGL ES，非 Metal），因此走
/// <c>CVOpenGLESTextureCache</c> 而不是 ISSUE-058 设计中写的 <c>CVMetalTextureCache</c>。
/// 若宿主将来迁移到 Metal，此处需换为 Metal 纹理缓存。
/// </para>
/// <para>
/// 映射失败（纹理缓存不可用）时自动回退到 CPU 读取像素，因此任何设备都能出画面。
/// </para>
///
/// <para>
/// 线程约束：<see cref="PushPixelBuffer"/> 在轮询线程调用，
/// <see cref="AcquireCurrentFrame"/> 在渲染线程调用；纹理缓存与映射只在后者进行。
/// </para>
///
/// <para><b>未经真机运行验证</b>：本机无 iOS 设备/模拟器，此实现仅通过编译验证。</para>
/// </summary>
internal sealed class IosVideoFrameSource : IVideoFrameSource, IDisposable
{
    // GL 常量。CoreVideo 的纹理缓存按这些格式映射 NV12 的两个平面。
    private const int GlLuminance = 0x1909;         // GL_LUMINANCE
    private const int GlLuminanceAlpha = 0x190A;    // GL_LUMINANCE_ALPHA
    private const uint GlUnsignedByte = 0x1401;     // GL_UNSIGNED_BYTE
    private const uint GlR8 = 0x8229;               // GL_R8
    private const uint GlRg8 = 0x822B;              // GL_RG8

    private readonly object _lock = new();

    private CVPixelBuffer? _pendingBuffer;
    private TimeSpan _pendingPts;

    private CVOpenGLESTextureCache? _textureCache;
    private bool _textureCacheUnavailable;

    private SKImage? _currentImage;
    private bool _disposed;

    /// <summary>上一帧是否走了零拷贝纹理路径（供诊断）。</summary>
    internal bool LastFrameWasZeroCopy { get; private set; }

    /// <summary>
    /// 轮询线程推入一帧。只保留最近一帧，旧帧立即释放
    /// （渲染慢于解码时丢帧而非堆积）。
    /// </summary>
    internal void PushPixelBuffer(CVPixelBuffer buffer, TimeSpan pts)
    {
        CVPixelBuffer? dropped;
        lock (_lock)
        {
            if (_disposed)
            {
                buffer.Dispose();
                return;
            }

            dropped = _pendingBuffer;
            _pendingBuffer = buffer;
            _pendingPts = pts;
        }

        dropped?.Dispose();
    }

    /// <inheritdoc />
    public SKImage? AcquireCurrentFrame(GRContext? grContext)
    {
        CVPixelBuffer? buffer;
        TimeSpan pts;
        lock (_lock)
        {
            if (_disposed) return null;
            buffer = _pendingBuffer;
            pts = _pendingPts;
            _pendingBuffer = null;
        }

        if (buffer == null)
            return _currentImage;

        try
        {
            SKImage? image = WrapBuffer(buffer, grContext);
            if (image != null)
            {
                _currentImage?.Dispose();
                _currentImage = image;
            }
        }
        catch (Exception)
        {
            // 映射失败不应打断渲染：保留上一帧，下一帧再试。
        }
        finally
        {
            buffer.Dispose();
        }

        return _currentImage;
    }

    /// <inheritdoc />
    public void ReleaseCurrentFrame()
    {
        // 当前帧图像持有到下一帧替换为止。
    }

    /// <summary>
    /// 优先零拷贝映射为纹理；不可用时回退到 CPU 读像素。
    /// </summary>
    private SKImage? WrapBuffer(CVPixelBuffer buffer, GRContext? grContext)
    {
        int width = (int)buffer.Width;
        int height = (int)buffer.Height;
        if (width <= 0 || height <= 0) return null;

        if (grContext != null && buffer.PlaneCount >= 2)
        {
            SKImage? zeroCopy = TryWrapZeroCopy(buffer, grContext, width, height);
            if (zeroCopy != null)
            {
                LastFrameWasZeroCopy = true;
                return zeroCopy;
            }
        }

        LastFrameWasZeroCopy = false;
        return WrapViaCpu(buffer, grContext, width, height);
    }

    /// <summary>
    /// 用 <c>CVOpenGLESTextureCache</c> 把两个平面映射为 GL 纹理并 shader 合成。
    /// </summary>
    private SKImage? TryWrapZeroCopy(CVPixelBuffer buffer, GRContext grContext, int width, int height)
    {
        var cache = EnsureTextureCache();
        if (cache == null) return null;

        CVOpenGLESTexture? yTexture = null;
        CVOpenGLESTexture? uvTexture = null;
        SKImage? yImage = null;
        SKImage? uvImage = null;

        try
        {
            // 平面 0 = 亮度（单通道），平面 1 = 交错色度（双通道）。
            // internalFormat/pixelFormat 用 GL_LUMINANCE(0x1909) 与 GL_LUMINANCE_ALPHA(0x190A)：
            // 这是 CVOpenGLESTextureCache 映射 NV12 双平面的标准组合（ES2 兼容，各代设备均支持）。
            yTexture = cache.CreateTexture(
                buffer, isTexture2d: true,
                internalFormat: GlLuminance, width: width, height: height,
                pixelFormat: GlLuminance, pixelType: GlUnsignedByte, planeIndex: 0,
                out CVReturn yStatus);
            if (yStatus != CVReturn.Success || yTexture == null) return null;

            int uvWidth = (width + 1) / 2;
            int uvHeight = (height + 1) / 2;
            uvTexture = cache.CreateTexture(
                buffer, isTexture2d: true,
                internalFormat: GlLuminanceAlpha, width: uvWidth, height: uvHeight,
                pixelFormat: GlLuminanceAlpha, pixelType: GlUnsignedByte, planeIndex: 1,
                out CVReturn uvStatus);
            if (uvStatus != CVReturn.Success || uvTexture == null) return null;

            // Skia 侧按单/双通道解释：LUMINANCE 对应 R8，LUMINANCE_ALPHA 对应 RG8。
            yImage = WrapTexture(grContext, yTexture, width, height, GlR8, SKColorType.R8Unorm);
            uvImage = WrapTexture(grContext, uvTexture, uvWidth, uvHeight, GlRg8, SKColorType.Rg88);
            if (yImage == null || uvImage == null) return null;

            return Nv12FrameComposer.Compose(
                yImage, uvImage, width, height, grContext,
                Nv12FrameComposer.SelectColorSpace(height));
        }
        finally
        {
            yImage?.Dispose();
            uvImage?.Dispose();
            yTexture?.Dispose();
            uvTexture?.Dispose();
        }
    }

    private static SKImage? WrapTexture(
        GRContext grContext, CVOpenGLESTexture texture,
        int width, int height, uint internalFormat, SKColorType colorType)
    {
        // Name 是 GL 纹理 id（绑定类型为 int），Target 已是 GL 枚举。
        var glInfo = new GRGlTextureInfo(texture.Target, (uint)texture.Name, internalFormat);
        var backendTexture = new GRBackendTexture(width, height, mipmapped: false, glInfo);

        return SKImage.FromTexture(
            grContext, backendTexture, GRSurfaceOrigin.TopLeft, colorType, SKAlphaType.Opaque);
    }

    /// <summary>CPU 回退：锁定像素缓冲并按平面读出，交由通用 NV12/BGRA 路径成像。</summary>
    private static SKImage? WrapViaCpu(CVPixelBuffer buffer, GRContext? grContext, int width, int height)
    {
        buffer.Lock(CVPixelBufferLock.ReadOnly);
        try
        {
            if (buffer.PlaneCount >= 2)
            {
                var yImage = UploadPlane(buffer, 0, SKColorType.R8Unorm);
                var uvImage = UploadPlane(buffer, 1, SKColorType.Rg88);

                try
                {
                    if (yImage == null || uvImage == null) return null;

                    return Nv12FrameComposer.Compose(
                        yImage, uvImage, width, height, grContext,
                        Nv12FrameComposer.SelectColorSpace(height));
                }
                finally
                {
                    yImage?.Dispose();
                    uvImage?.Dispose();
                }
            }

            // 非平面格式：按 BGRA 读取。
            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
            return SKImage.FromPixelCopy(info, buffer.BaseAddress, (int)buffer.BytesPerRow);
        }
        finally
        {
            buffer.Unlock(CVPixelBufferLock.ReadOnly);
        }
    }

    private static SKImage? UploadPlane(CVPixelBuffer buffer, nuint plane, SKColorType colorType)
    {
        IntPtr baseAddress = buffer.GetBaseAddress((nint)plane);
        if (baseAddress == IntPtr.Zero) return null;

        int planeWidth = (int)buffer.GetWidthOfPlane((nint)plane);
        int planeHeight = (int)buffer.GetHeightOfPlane((nint)plane);
        int stride = (int)buffer.GetBytesPerRowOfPlane((nint)plane);
        if (planeWidth <= 0 || planeHeight <= 0 || stride <= 0) return null;

        var info = new SKImageInfo(planeWidth, planeHeight, colorType, SKAlphaType.Opaque);
        return SKImage.FromPixelCopy(info, baseAddress, stride);
    }

    /// <summary>
    /// 惰性创建纹理缓存，绑定当前 GL ES 上下文。只在渲染线程调用。
    /// 失败后记录标志不再重试（避免每帧都做一次失败的系统调用）。
    /// </summary>
    private CVOpenGLESTextureCache? EnsureTextureCache()
    {
        if (_textureCache != null) return _textureCache;
        if (_textureCacheUnavailable) return null;

        var context = EAGLContext.CurrentContext;
        if (context == null)
        {
            _textureCacheUnavailable = true;
            return null;
        }

        _textureCache = CVOpenGLESTextureCache.FromEAGLContext(context);
        if (_textureCache == null)
            _textureCacheUnavailable = true;

        return _textureCache;
    }

    public void Dispose()
    {
        CVPixelBuffer? pending;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            pending = _pendingBuffer;
            _pendingBuffer = null;
            _currentImage?.Dispose();
            _currentImage = null;
        }

        pending?.Dispose();
        _textureCache?.Dispose();
        _textureCache = null;
    }
}
