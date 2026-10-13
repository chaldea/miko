using CoreVideo;
using Metal;
using Miko.Platform.Video;
using SkiaSharp;

namespace Miko.iOS.Video;

/// <summary>
/// iOS 帧源：把 <c>CVPixelBuffer</c>（IOSurface 支撑）零拷贝映射为 Metal 纹理，
/// 再由 SkSL shader 在 GPU 上完成 NV12→RGB。
///
/// <para>
/// **零拷贝路径**：<c>CVMetalTextureCache</c> 把像素缓冲的 IOSurface 直接包装为
/// Metal 纹理，不产生像素拷贝。NV12 的两个平面各映射一张纹理
/// （平面 0 → <c>R8Unorm</c> 亮度，平面 1 → <c>RG8Unorm</c> 色度），交给
/// <see cref="Nv12FrameComposer"/> 合成。宿主随 ISSUE-147 迁移到 Metal 后，
/// 这里也从 <c>CVOpenGLESTextureCache</c> 换成了 ISSUE-058 设计中的 <c>CVMetalTextureCache</c>。
/// </para>
/// <para>
/// **纹理寿命**：合成命令要到帧末才提交、之后才在 GPU 上执行，映射出的纹理与像素缓冲必须活到那时。
/// 因此映射成功后它们交给 <see cref="MetalFrameResources"/>，由宿主在本帧命令缓冲完成后释放。
/// </para>
/// <para>
/// 映射失败（纹理缓存不可用、GPU 上下文不是 Metal）时自动回退到 CPU 读取像素，因此任何设备都能出画面。
/// </para>
///
/// <para>
/// 线程约束：<see cref="PushPixelBuffer"/> 在轮询线程调用，
/// <see cref="AcquireCurrentFrame"/> 在渲染线程（主线程）调用；纹理缓存与映射只在后者进行。
/// </para>
/// </summary>
internal sealed class IosVideoFrameSource : IVideoFrameSource, IDisposable
{
    private readonly object _lock = new();

    private CVPixelBuffer? _pendingBuffer;
    private TimeSpan _pendingPts;

    private CVMetalTextureCache? _textureCache;
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

        bool bufferRetained = false;
        try
        {
            SKImage? image = WrapBuffer(buffer, grContext, out bufferRetained);
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
            // 零拷贝成功时像素缓冲已随纹理交给 MetalFrameResources，待本帧 GPU 执行完再释放。
            if (!bufferRetained)
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
    /// <param name="bufferRetained">
    /// 为 <c>true</c> 时像素缓冲已由 <see cref="MetalFrameResources"/> 接管，调用方不得再释放。
    /// </param>
    private SKImage? WrapBuffer(CVPixelBuffer buffer, GRContext? grContext, out bool bufferRetained)
    {
        bufferRetained = false;
        int width = (int)buffer.Width;
        int height = (int)buffer.Height;
        if (width <= 0 || height <= 0) return null;

        if (grContext?.Backend == GRBackend.Metal && buffer.PlaneCount >= 2)
        {
            SKImage? zeroCopy = TryWrapZeroCopy(buffer, grContext, width, height);
            if (zeroCopy != null)
            {
                LastFrameWasZeroCopy = true;
                bufferRetained = true;
                return zeroCopy;
            }
        }

        LastFrameWasZeroCopy = false;
        return WrapViaCpu(buffer, grContext, width, height);
    }

    /// <summary>
    /// 用 <c>CVMetalTextureCache</c> 把两个平面映射为 Metal 纹理并 shader 合成。
    /// 成功时像素缓冲与两张纹理一并交给 <see cref="MetalFrameResources"/>。
    /// </summary>
    private SKImage? TryWrapZeroCopy(CVPixelBuffer buffer, GRContext grContext, int width, int height)
    {
        var cache = EnsureTextureCache();
        if (cache == null) return null;

        // 让缓存回收已不再被引用的纹理（CoreVideo 要求定期调用）；仍在 GPU 上使用的纹理有人持有，不受影响。
        cache.Flush(CVOptionFlags.None);

        CVMetalTexture? yTexture = null;
        CVMetalTexture? uvTexture = null;
        SKImage? yImage = null;
        SKImage? uvImage = null;
        SKImage? composed = null;

        try
        {
            // 平面 0 = 亮度（单通道），平面 1 = 交错色度（双通道，半分辨率）。
            yTexture = cache.TextureFromImage(
                buffer, MTLPixelFormat.R8Unorm, width, height, planeIndex: 0, out CVReturn yStatus);
            if (yStatus != CVReturn.Success || yTexture?.Texture is not { } yMetal) return null;

            int uvWidth = (int)buffer.GetWidthOfPlane(1);
            int uvHeight = (int)buffer.GetHeightOfPlane(1);
            uvTexture = cache.TextureFromImage(
                buffer, MTLPixelFormat.RG8Unorm, uvWidth, uvHeight, planeIndex: 1, out CVReturn uvStatus);
            if (uvStatus != CVReturn.Success || uvTexture?.Texture is not { } uvMetal) return null;

            yImage = WrapTexture(grContext, yMetal, width, height, SKColorType.R8Unorm);
            uvImage = WrapTexture(grContext, uvMetal, uvWidth, uvHeight, SKColorType.Rg88);
            if (yImage == null || uvImage == null) return null;

            composed = Nv12FrameComposer.Compose(
                yImage, uvImage, width, height, grContext,
                Nv12FrameComposer.SelectColorSpace(height));
            if (composed != null)
                MetalFrameResources.RetainUntilFrameCompletes(new MappedFrame(buffer, yTexture, uvTexture));

            return composed;
        }
        finally
        {
            // 平面图像只是包装，Skia 自己持有底层纹理直到命令执行完；CoreVideo 对象的去留见上。
            yImage?.Dispose();
            uvImage?.Dispose();
            if (composed == null)
            {
                yTexture?.Dispose();
                uvTexture?.Dispose();
            }
        }
    }

    private static SKImage? WrapTexture(
        GRContext grContext, IMTLTexture texture, int width, int height, SKColorType colorType)
    {
        using var backendTexture = new GRBackendTexture(
            width, height, mipmapped: false, new GRMtlTextureInfo(texture));

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
    /// 惰性创建纹理缓存。只在渲染线程调用。
    /// 失败后记录标志不再重试（避免每帧都做一次失败的系统调用）。
    /// </summary>
    private CVMetalTextureCache? EnsureTextureCache()
    {
        if (_textureCache != null) return _textureCache;
        if (_textureCacheUnavailable) return null;

        // 与 MikoMetalView 相同的系统默认设备（iOS 只有一块 GPU），映射出的纹理才能被宿主的 GRContext 采样。
        var device = MTLDevice.SystemDefault;
        _textureCache = device == null ? null : CVMetalTextureCache.FromDevice(device);
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

    /// <summary>一帧零拷贝映射涉及的 CoreVideo 对象：像素缓冲及其两个平面的纹理，一并释放。</summary>
    private sealed class MappedFrame(CVPixelBuffer buffer, CVMetalTexture yTexture, CVMetalTexture uvTexture)
        : IDisposable
    {
        public void Dispose()
        {
            yTexture.Dispose();
            uvTexture.Dispose();
            buffer.Dispose();
        }
    }
}
