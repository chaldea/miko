using Miko.Platform.Video;
using SkiaSharp;

namespace Miko.Windowing.Video;

/// <summary>
/// 软解基线的帧源：持有最近一帧的 RGBA 像素缓冲，在渲染线程把它包装成 <see cref="SKImage"/>。
/// <para>
/// 这是 Phase 1 的 CPU 上传实现：解码线程产出 RGBA 字节缓冲，渲染线程一次性上传为纹理图像。
/// Phase 2 的零拷贝硬解会在同一 <see cref="IVideoFrameSource"/> 契约下替换为
/// 「解码线程只入队 GPU 句柄、渲染线程零拷贝包装」的实现，调用方无需改动。
/// </para>
/// <para>
/// 线程模型：<see cref="PushFrame"/> 在解码线程调用，<see cref="AcquireCurrentFrame"/>/
/// <see cref="ReleaseCurrentFrame"/> 在渲染线程调用，两者以 <see cref="_lock"/> 互斥。
/// 仅保留「最近一帧」，渲染慢于解码时自动丢弃旧帧（基线策略，避免无界堆积）。
/// </para>
/// </summary>
internal sealed class FFmpegVideoFrameSource : IVideoFrameSource, IDisposable
{
    private readonly object _lock = new();

    // 最近一帧的 RGBA 缓冲与尺寸（CPU 端）。
    private byte[]? _pendingPixels;
    private int _pendingWidth;
    private int _pendingHeight;
    private bool _hasNewFrame;

    // 渲染线程持有的当前帧图像，新帧到达时替换并释放旧图像。
    private SKImage? _currentImage;
    private bool _disposed;

    /// <summary>
    /// 解码线程推入一帧 RGBA8888 像素（紧凑排列，stride = width*4）。
    /// 缓冲由调用方拥有，这里浅引用——调用方应在每帧使用独立缓冲或在下次推帧前不复用。
    /// </summary>
    public void PushFrame(byte[] rgba, int width, int height)
    {
        lock (_lock)
        {
            if (_disposed) return;
            _pendingPixels = rgba;
            _pendingWidth = width;
            _pendingHeight = height;
            _hasNewFrame = true;
        }
    }

    public SKImage? AcquireCurrentFrame(GRContext? grContext)
    {
        lock (_lock)
        {
            if (_disposed) return _currentImage;

            // 有新帧则上传为图像，替换旧图像。
            if (_hasNewFrame && _pendingPixels != null)
            {
                _hasNewFrame = false;

                var info = new SKImageInfo(_pendingWidth, _pendingHeight, SKColorType.Rgba8888, SKAlphaType.Opaque);
                // FromPixelCopy 一次性把 CPU 像素拷入图像；绘制到 GPU 画布时由 Skia 上传为纹理。
                var newImage = SKImage.FromPixelCopy(info, _pendingPixels);
                if (newImage != null)
                {
                    _currentImage?.Dispose();
                    _currentImage = newImage;
                }
            }

            return _currentImage;
        }
    }

    public void ReleaseCurrentFrame()
    {
        // 基线实现持有图像直到下一帧替换，这里无需逐帧释放。
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            _currentImage?.Dispose();
            _currentImage = null;
            _pendingPixels = null;
        }
    }
}
