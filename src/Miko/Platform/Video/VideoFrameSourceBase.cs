using Miko.Graphics;

namespace Miko.Platform.Video;

/// <summary>
/// 所有平台后端共用的帧源骨架：解码线程只入队 <see cref="VideoFrameBuffer"/>，
/// 渲染线程在 <see cref="AcquireCurrentFrame"/> 里把它包装成可绘制的 <see cref="Image"/>。
///
/// <para>
/// **分层策略**（本类的核心价值）：一帧可能是 GPU 纹理句柄（零拷贝）或 CPU 缓冲（回退）。
/// 两条路径的包装逻辑都在这里实现一次，五个平台后端只需按自身能力填充 <c>VideoFrameBuffer</c>，
/// 不必各自重复「零拷贝命中就包纹理、否则上传像素」的判断。
/// NV12 帧统一经 <see cref="Nv12FrameComposer"/> 做 GPU 侧 YUV→RGB。
/// </para>
///
/// <para>
/// **线程模型**：<see cref="PushFrame"/> 在解码线程调用；<see cref="AcquireCurrentFrame"/> 与
/// <see cref="ReleaseCurrentFrame"/> 在渲染线程调用；两者以 <c>_lock</c> 互斥。
/// 只保留「最近一帧」——渲染慢于解码时自动丢弃旧帧，避免无界堆积（沿用 FFmpeg 基线的策略）。
/// </para>
/// </summary>
public abstract class VideoFrameSourceBase : IVideoFrameSource, IDisposable
{
    private readonly object _lock = new();

    private VideoFrameBuffer? _pendingFrame;
    private Image? _currentImage;
    private bool _disposed;

    /// <summary>
    /// 上一次包装是否走了零拷贝路径。供宿主记录日志/诊断用（回退是静默的，
    /// 否则驱动不支持时用户只会看到"能播但费电"而不知原因）。
    /// </summary>
    public bool LastFrameWasZeroCopy { get; private set; }

    /// <summary>
    /// 解码线程推入一帧。旧的未消费帧会被丢弃并触发其 <see cref="VideoFrameBuffer.OnReleased"/>，
    /// 使 GPU 资源及时交还解码器。
    /// </summary>
    public void PushFrame(VideoFrameBuffer frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        VideoFrameBuffer? dropped;
        lock (_lock)
        {
            if (_disposed)
            {
                // 已释放：立刻归还资源，不入队。
                frame.OnReleased?.Invoke();
                return;
            }

            dropped = _pendingFrame;
            _pendingFrame = frame;
        }

        // 在锁外回收被丢弃的帧：OnReleased 可能回调进解码器，避免锁内重入。
        dropped?.OnReleased?.Invoke();
    }

    /// <inheritdoc />
    public Image? AcquireCurrentFrame(GraphicsDevice device)
    {
        VideoFrameBuffer? frame;
        lock (_lock)
        {
            if (_disposed) return null;
            frame = _pendingFrame;
            _pendingFrame = null;
        }

        if (frame == null)
            return Volatile.Read(ref _currentImage);   // 无新帧：继续显示上一帧

        Image? image = null;
        try
        {
            image = WrapFrame(frame, device ?? GraphicsDevice.Software);
        }
        catch (Exception)
        {
            // 包装失败（纹理已失效 / 上下文丢失）不应打断渲染：保留上一帧，下一帧再试。
            image = null;
        }
        finally
        {
            frame.OnReleased?.Invoke();
        }

        if (image != null)
        {
            LastFrameWasZeroCopy = frame.IsGpuFrame;

            lock (_lock)
            {
                if (_disposed)
                {
                    image.Dispose();
                    return null;
                }
                _currentImage?.Dispose();
                _currentImage = image;
            }
        }

        return Volatile.Read(ref _currentImage);
    }

    /// <inheritdoc />
    public void ReleaseCurrentFrame()
    {
        // 当前帧图像持有到下一帧替换为止，无需逐帧释放。
        // GPU 资源的归还已在 AcquireCurrentFrame 中经 OnReleased 完成。
    }

    /// <summary>
    /// 把一帧包装为图像：GPU 帧走纹理包装（零拷贝），CPU 帧走像素上传（回退）。
    /// NV12 两种形态都要额外经 shader 转 RGB。
    /// </summary>
    private static Image? WrapFrame(VideoFrameBuffer frame, GraphicsDevice device)
    {
        // 软件渲染（没有 GL 上下文）时纹理句柄无从采样，只能放弃该帧。
        if (frame.IsGpuFrame && device.Backend != GraphicsBackend.OpenGl)
            return null;

        return frame.Format switch
        {
            VideoPixelFormat.Nv12 => WrapNv12(frame, device),
            _ => WrapSinglePlane(frame, device),
        };
    }

    /// <summary>NV12：Y/UV 各包一张单通道图像，再由 shader 合成 RGB。</summary>
    private static Image? WrapNv12(VideoFrameBuffer frame, GraphicsDevice device)
    {
        Image? yImage = null;
        Image? uvImage = null;
        try
        {
            if (frame.IsGpuFrame)
            {
                var planes = frame.TexturePlanes!;
                if (planes.Count < 2) return null;

                yImage = WrapTexture(planes[0], device, ImagePixelFormat.R8);
                uvImage = WrapTexture(planes[1], device, ImagePixelFormat.Rg88);
            }
            else
            {
                var planes = frame.CpuPlanes!;
                var strides = frame.CpuStrides!;
                if (planes.Count < 2) return null;

                // UV 平面为半分辨率（宽高各半，向上取整以容纳奇数尺寸）。
                int uvWidth = (frame.Width + 1) / 2;
                int uvHeight = (frame.Height + 1) / 2;

                yImage = UploadPixels(planes[0], strides[0], frame.Width, frame.Height, ImagePixelFormat.R8, device);
                uvImage = UploadPixels(planes[1], strides[1], uvWidth, uvHeight, ImagePixelFormat.Rg88, device);
            }

            if (yImage == null || uvImage == null) return null;

            return Nv12FrameComposer.Compose(
                yImage, uvImage, frame.Width, frame.Height, device,
                Nv12FrameComposer.SelectColorSpace(frame.Height));
        }
        finally
        {
            // 合成结果是独立图像，两个平面图像用完即弃。
            yImage?.Dispose();
            uvImage?.Dispose();
        }
    }

    /// <summary>RGBA/BGRA/外部纹理：单平面直接成像，无需色彩转换。</summary>
    private static Image? WrapSinglePlane(VideoFrameBuffer frame, GraphicsDevice device)
    {
        // 视频帧不透明：第四个字节常是未定义的填充（BGRX），不能当 alpha。
        var format = frame.Format switch
        {
            VideoPixelFormat.Bgra8888 => ImagePixelFormat.Bgrx8888,
            // 外部纹理采样即得 RGB，按 RGBA 解释。
            _ => ImagePixelFormat.Rgbx8888,
        };

        if (frame.IsGpuFrame)
            return WrapTexture(frame.TexturePlanes![0], device, format);

        return UploadPixels(
            frame.CpuPlanes![0], frame.CpuStrides![0], frame.Width, frame.Height, format, device);
    }

    /// <summary>
    /// 把平台 GPU 纹理句柄包装为图像 —— 零拷贝路径的落点。纹理所有权仍属解码器，
    /// 图像释放时不销毁它。
    /// </summary>
    private static Image? WrapTexture(VideoTexturePlane plane, GraphicsDevice device, ImagePixelFormat format)
        => device.WrapGlTexture(plane.Target, plane.Id, plane.Format, plane.Width, plane.Height, format);

    /// <summary>
    /// CPU 回退：把（可能带行填充的）像素缓冲上传为图像。
    /// stride 与紧凑宽度不等时必须按 stride 逐行寻址，否则画面斜切；
    /// 缓冲比声明的尺寸短（截断的帧）时只上传完整的行，其余留黑。
    /// </summary>
    private static Image? UploadPixels(byte[] pixels, int stride, int width, int height, ImagePixelFormat format, GraphicsDevice device)
    {
        if (width <= 0 || height <= 0) return null;

        int rowBytes = width * BytesPerPixel(format);
        if (stride >= rowBytes && pixels.Length >= (long)stride * (height - 1) + rowBytes)
            return device.CreateImage(width, height, format, pixels, stride);

        // 截断的缓冲：逐行拷贝能拿到的部分。
        var compact = new byte[rowBytes * height];
        for (int y = 0; y < height; y++)
        {
            int srcOffset = y * stride;
            if (srcOffset + rowBytes > pixels.Length) break;
            Buffer.BlockCopy(pixels, srcOffset, compact, y * rowBytes, rowBytes);
        }

        return device.CreateImage(width, height, format, compact);
    }

    private static int BytesPerPixel(ImagePixelFormat format) => format switch
    {
        ImagePixelFormat.R8 => 1,
        ImagePixelFormat.Rg88 => 2,
        _ => 4,
    };

    /// <summary>释放当前帧图像与未消费的待显示帧。</summary>
    public virtual void Dispose()
    {
        VideoFrameBuffer? pending;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            pending = _pendingFrame;
            _pendingFrame = null;
            _currentImage?.Dispose();
            _currentImage = null;
        }

        pending?.OnReleased?.Invoke();
        GC.SuppressFinalize(this);
    }
}
