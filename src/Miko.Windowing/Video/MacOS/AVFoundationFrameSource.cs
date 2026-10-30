using System.Runtime.Versioning;
using Miko.Graphics;
using Miko.Platform.Video;
using Miko.Windowing.MacOS;
using static Miko.Windowing.MacOS.ObjC;
using static Miko.Windowing.Video.MacOS.AVFoundationInterop;

namespace Miko.Windowing.Video.MacOS;

/// <summary>
/// AVFoundation 的帧源：把 <c>CVPixelBuffer</c>（IOSurface 支撑）零拷贝映射为 Metal 纹理，
/// 再由 SkSL shader 在 GPU 上完成 NV12→RGB。与 iOS 帧源同一做法（ISSUE-147）。
///
/// <para>
/// **零拷贝路径**：<c>CVMetalTextureCache</c> 把像素缓冲的 IOSurface 直接包装为 Metal 纹理，
/// 不产生像素拷贝。NV12 的两个平面各映射一张纹理（平面 0 → <c>R8Unorm</c> 亮度，
/// 平面 1 → <c>RG8Unorm</c> 色度），交给 <see cref="Nv12FrameComposer"/> 合成。
/// 这要求窗口走 Metal 后端（见 <c>SilkWindowGraphics</c>）；OpenGL 时代这条路要经已弃用的
/// <c>CVOpenGLTextureCache</c> 与 <c>GL_TEXTURE_RECTANGLE</c>，因此此前一直是逐帧 CPU 拷贝。
/// </para>
/// <para>
/// **纹理寿命**：合成命令要到帧末才提交、之后才在 GPU 上执行，映射出的纹理与像素缓冲必须活到那时。
/// 因此映射成功后它们交给 <see cref="MetalFrameResources"/>，由窗口后端在本帧命令缓冲完成后释放。
/// </para>
/// <para>
/// 映射失败（纹理缓存不可用、GPU 上下文不是 Metal——例如 <c>MIKO_GRAPHICS_BACKEND=opengl</c>）时
/// 回退到 CPU 读取像素再上传，因此任何配置都能出画面。
/// </para>
///
/// <para>
/// 线程约束：<see cref="PushPixelBuffer"/> 在轮询线程调用，<see cref="AcquireCurrentFrame"/>
/// 在渲染线程调用；纹理缓存与映射只在后者进行。
/// </para>
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class AVFoundationFrameSource : IVideoFrameSource, IDisposable
{
    private readonly object _lock = new();

    // 尚未被渲染线程取走的最新一帧（CVPixelBufferRef，本类持有 +1 引用）。
    private IntPtr _pendingBuffer;

    private IntPtr _textureCache;
    private bool _textureCacheUnavailable;

    private Image? _currentImage;
    private bool _disposed;

    /// <summary>上一帧是否走了零拷贝纹理路径（供诊断）。</summary>
    internal bool LastFrameWasZeroCopy { get; private set; }

    /// <summary>
    /// 轮询线程推入一帧，接管调用方持有的 +1 引用。只保留最近一帧，旧帧立即释放
    /// （渲染慢于解码时丢帧而非堆积）。
    /// </summary>
    internal void PushPixelBuffer(IntPtr buffer)
    {
        IntPtr dropped;
        lock (_lock)
        {
            if (_disposed)
            {
                CFRelease(buffer);
                return;
            }

            dropped = _pendingBuffer;
            _pendingBuffer = buffer;
        }

        if (dropped != IntPtr.Zero)
            CFRelease(dropped);
    }

    /// <inheritdoc />
    public Image? AcquireCurrentFrame(GraphicsDevice device)
    {
        IntPtr buffer;
        lock (_lock)
        {
            if (_disposed) return null;
            buffer = _pendingBuffer;
            _pendingBuffer = IntPtr.Zero;
        }

        if (buffer == IntPtr.Zero)
            return _currentImage;

        bool bufferRetained = false;
        try
        {
            Image? image = WrapBuffer(buffer, device, out bufferRetained);
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
                CFRelease(buffer);
        }

        return _currentImage;
    }

    /// <inheritdoc />
    public void ReleaseCurrentFrame()
    {
        // 当前帧图像持有到下一帧替换为止。
    }

    /// <summary>优先零拷贝映射为纹理；不可用时回退到 CPU 读像素。</summary>
    /// <param name="bufferRetained">
    /// 为 <c>true</c> 时像素缓冲已由 <see cref="MetalFrameResources"/> 接管，调用方不得再释放。
    /// </param>
    private Image? WrapBuffer(IntPtr buffer, GraphicsDevice device, out bool bufferRetained)
    {
        bufferRetained = false;
        int width = (int)CVPixelBufferGetWidth(buffer);
        int height = (int)CVPixelBufferGetHeight(buffer);
        if (width <= 0 || height <= 0) return null;

        if (device.Backend == GraphicsBackend.Metal && CVPixelBufferGetPlaneCount(buffer) >= 2)
        {
            Image? zeroCopy = TryWrapZeroCopy(buffer, device, width, height);
            if (zeroCopy != null)
            {
                LastFrameWasZeroCopy = true;
                bufferRetained = true;
                return zeroCopy;
            }
        }

        LastFrameWasZeroCopy = false;
        return WrapViaCpu(buffer, device, width, height);
    }

    /// <summary>
    /// 用 <c>CVMetalTextureCache</c> 把两个平面映射为 Metal 纹理并 shader 合成。
    /// 成功时像素缓冲与两张纹理一并交给 <see cref="MetalFrameResources"/>。
    /// </summary>
    private Image? TryWrapZeroCopy(IntPtr buffer, GraphicsDevice device, int width, int height)
    {
        IntPtr cache = EnsureTextureCache();
        if (cache == IntPtr.Zero) return null;

        // 让缓存回收已不再被引用的纹理（CoreVideo 要求定期调用）；仍在 GPU 上使用的纹理有人持有，不受影响。
        CVMetalTextureCacheFlush(cache, 0);

        IntPtr yTexture = IntPtr.Zero;
        IntPtr uvTexture = IntPtr.Zero;
        Image? yImage = null;
        Image? uvImage = null;
        Image? composed = null;

        try
        {
            // 平面 0 = 亮度（单通道），平面 1 = 交错色度（双通道，半分辨率）。
            if (CVMetalTextureCacheCreateTextureFromImage(
                    IntPtr.Zero, cache, buffer, IntPtr.Zero, MetalInterop.PixelFormatR8Unorm,
                    (nuint)width, (nuint)height, 0, out yTexture) != CVReturnSuccess)
            {
                return null;
            }
            IntPtr yMetal = yTexture == IntPtr.Zero ? IntPtr.Zero : CVMetalTextureGetTexture(yTexture);
            if (yMetal == IntPtr.Zero) return null;

            int uvWidth = (int)CVPixelBufferGetWidthOfPlane(buffer, 1);
            int uvHeight = (int)CVPixelBufferGetHeightOfPlane(buffer, 1);
            if (CVMetalTextureCacheCreateTextureFromImage(
                    IntPtr.Zero, cache, buffer, IntPtr.Zero, MetalInterop.PixelFormatRG8Unorm,
                    (nuint)uvWidth, (nuint)uvHeight, 1, out uvTexture) != CVReturnSuccess)
            {
                return null;
            }
            IntPtr uvMetal = uvTexture == IntPtr.Zero ? IntPtr.Zero : CVMetalTextureGetTexture(uvTexture);
            if (uvMetal == IntPtr.Zero) return null;

            yImage = device.WrapMetalTexture(yMetal, width, height, ImagePixelFormat.R8);
            uvImage = device.WrapMetalTexture(uvMetal, uvWidth, uvHeight, ImagePixelFormat.Rg88);
            if (yImage == null || uvImage == null) return null;

            composed = Nv12FrameComposer.Compose(
                yImage, uvImage, width, height, device,
                Nv12FrameComposer.SelectColorSpace(height));
            if (composed != null)
                MetalFrameResources.RetainUntilFrameCompletes(new MappedFrame(buffer, yTexture, uvTexture));

            return composed;
        }
        finally
        {
            // 平面图像只是包装，渲染后端自己持有底层纹理直到命令执行完；CoreVideo 对象的去留见上。
            yImage?.Dispose();
            uvImage?.Dispose();
            if (composed == null)
            {
                if (yTexture != IntPtr.Zero) CFRelease(yTexture);
                if (uvTexture != IntPtr.Zero) CFRelease(uvTexture);
            }
        }
    }

    /// <summary>
    /// CPU 回退：只读锁定像素缓冲并按平面读出，交由通用 NV12/BGRA 路径成像。
    /// 各平面的 stride 由 <c>GetBytesPerRowOfPlane</c> 给出（通常有对齐填充）。
    /// </summary>
    private static Image? WrapViaCpu(IntPtr buffer, GraphicsDevice device, int width, int height)
    {
        // flags = kCVPixelBufferLock_ReadOnly (1)。
        const ulong readOnly = 1;
        if (CVPixelBufferLockBaseAddress(buffer, readOnly) != CVReturnSuccess)
            return null;

        try
        {
            if (CVPixelBufferGetPlaneCount(buffer) >= 2)
            {
                var yImage = UploadPlane(buffer, 0, ImagePixelFormat.R8, device);
                var uvImage = UploadPlane(buffer, 1, ImagePixelFormat.Rg88, device);

                try
                {
                    if (yImage == null || uvImage == null) return null;

                    return Nv12FrameComposer.Compose(
                        yImage, uvImage, width, height, device,
                        Nv12FrameComposer.SelectColorSpace(height));
                }
                finally
                {
                    yImage?.Dispose();
                    uvImage?.Dispose();
                }
            }

            // 非平面格式：按 BGRA 读取（不透明）。
            IntPtr baseAddress = CVPixelBufferGetBaseAddress(buffer);
            if (baseAddress == IntPtr.Zero) return null;

            return device.CreateImage(width, height, ImagePixelFormat.Bgrx8888, baseAddress, (int)CVPixelBufferGetBytesPerRow(buffer));
        }
        finally
        {
            CVPixelBufferUnlockBaseAddress(buffer, readOnly);
        }
    }

    private static Image? UploadPlane(IntPtr buffer, nuint plane, ImagePixelFormat format, GraphicsDevice device)
    {
        IntPtr baseAddress = CVPixelBufferGetBaseAddressOfPlane(buffer, plane);
        if (baseAddress == IntPtr.Zero) return null;

        int planeWidth = (int)CVPixelBufferGetWidthOfPlane(buffer, plane);
        int planeHeight = (int)CVPixelBufferGetHeightOfPlane(buffer, plane);
        int stride = (int)CVPixelBufferGetBytesPerRowOfPlane(buffer, plane);
        if (planeWidth <= 0 || planeHeight <= 0 || stride <= 0) return null;

        return device.CreateImage(planeWidth, planeHeight, format, baseAddress, stride);
    }

    /// <summary>
    /// 惰性创建纹理缓存。只在渲染线程调用。
    /// 失败后记录标志不再重试（避免每帧都做一次失败的系统调用）。
    /// </summary>
    private IntPtr EnsureTextureCache()
    {
        if (_textureCache != IntPtr.Zero) return _textureCache;
        if (_textureCacheUnavailable) return IntPtr.Zero;

        // 与 Metal 窗口后端相同的系统默认设备，映射出的纹理才能被宿主的 GPU 设备采样。
        // 缓存自己持有设备，这里的 +1 引用用完即还。
        IntPtr device = MetalInterop.CreateSystemDefaultDevice();
        if (device != IntPtr.Zero)
        {
            if (CVMetalTextureCacheCreate(IntPtr.Zero, IntPtr.Zero, device, IntPtr.Zero, out IntPtr cache) == CVReturnSuccess)
                _textureCache = cache;
            CFRelease(device);
        }

        if (_textureCache == IntPtr.Zero)
            _textureCacheUnavailable = true;

        return _textureCache;
    }

    public void Dispose()
    {
        IntPtr pending;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            pending = _pendingBuffer;
            _pendingBuffer = IntPtr.Zero;
            _currentImage?.Dispose();
            _currentImage = null;
        }

        if (pending != IntPtr.Zero)
            CFRelease(pending);
        if (_textureCache != IntPtr.Zero)
        {
            CFRelease(_textureCache);
            _textureCache = IntPtr.Zero;
        }
    }

    /// <summary>一帧零拷贝映射涉及的 CoreVideo 对象：像素缓冲及其两个平面的纹理，一并释放。</summary>
    private sealed class MappedFrame(IntPtr buffer, IntPtr yTexture, IntPtr uvTexture) : IDisposable
    {
        public void Dispose()
        {
            CFRelease(yTexture);
            CFRelease(uvTexture);
            CFRelease(buffer);
        }
    }
}
