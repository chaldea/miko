using Android.Graphics;
using Android.Opengl;
using Android.Views;
using Miko.Platform.Video;
using SkiaSharp;

namespace Miko.Android.Video;

/// <summary>
/// Android 帧源：把 <c>MediaCodec</c> 直出到 <c>SurfaceTexture</c> 的 OES 外部纹理
/// 包装为 <see cref="SKImage"/> —— **真零拷贝**，像素从解码器到屏幕全程驻留 GPU。
///
/// <para>
/// 与其它平台不同，这里不继承 <see cref="VideoFrameSourceBase"/>：那套骨架面向
/// 「解码线程入队帧数据」的模型，而 Android 的帧根本不经过 CPU —— 解码器直接写进
/// 纹理，本类只需在渲染线程把纹理内容刷新（<c>UpdateTexImage</c>）并复用同一个纹理 id。
/// </para>
///
/// <para>
/// 线程约束：<see cref="AcquireCurrentFrame"/> 在渲染线程调用，GL 纹理与
/// <c>SurfaceTexture</c> 都在此惰性创建 —— 它们必须属于渲染线程的 GL 上下文。
/// 解码线程只调用 <see cref="WaitForSurface"/> 与 <see cref="NotifyFrameAvailable"/>。
/// </para>
///
/// <para><b>未经真机运行验证</b>：本机无 Android 设备/模拟器，此实现仅通过编译验证。</para>
/// </summary>
internal sealed class AndroidVideoFrameSource : IVideoFrameSource, IDisposable
{
    /// <summary>
    /// <c>GL_TEXTURE_EXTERNAL_OES</c>。Skia 的 <c>GRGlTextureInfo.Target</c> 接受任意 GL 枚举，
    /// 因此可以直接用外部纹理目标 —— 这是本路径可行的关键。
    /// </summary>
    private const uint GlTextureExternalOes = 0x8D65;

    /// <summary><c>GL_RGBA8</c>：外部纹理采样后即为 RGB，按 RGBA8 声明格式。</summary>
    private const uint GlRgba8 = 0x8058;

    private readonly object _lock = new();
    private readonly ManualResetEventSlim _surfaceReady = new(false);

    private SurfaceTexture? _surfaceTexture;
    private Surface? _surface;
    private int _textureId;

    private SKImage? _currentImage;
    private bool _hasPendingFrame;
    private int _frameWidth;
    private int _frameHeight;
    private bool _disposed;

    /// <summary>
    /// 解码线程等待渲染线程创建好输出 Surface。<c>MediaCodec.Configure</c> 需要它，
    /// 而它只能在持有 GL 上下文的渲染线程创建，故用事件同步。
    /// </summary>
    internal Surface? WaitForSurface(TimeSpan timeout)
    {
        // 渲染线程尚未走到 AcquireCurrentFrame 时，这里会阻塞等待其创建纹理。
        if (!_surfaceReady.Wait(timeout))
            return null;

        lock (_lock) { return _surface; }
    }

    /// <summary>解码线程通知有新帧已写入 Surface，渲染线程下次取帧时刷新纹理。</summary>
    internal void NotifyFrameAvailable(TimeSpan pts, int width, int height)
    {
        lock (_lock)
        {
            if (_disposed) return;
            _hasPendingFrame = true;
            _frameWidth = width;
            _frameHeight = height;
        }
    }

    /// <inheritdoc />
    public SKImage? AcquireCurrentFrame(GRContext? grContext)
    {
        // 无 GPU 上下文时外部纹理无从采样（Android 宿主始终有 GL 上下文，此为防御）。
        if (grContext == null) return null;

        lock (_lock)
        {
            if (_disposed) return null;

            EnsureSurfaceCreated();

            if (!_hasPendingFrame)
                return _currentImage;

            _hasPendingFrame = false;

            // 把最新一帧刷进 OES 纹理。必须在渲染线程、GL 上下文内调用。
            _surfaceTexture?.UpdateTexImage();

            if (_frameWidth <= 0 || _frameHeight <= 0)
                return _currentImage;

            // 纹理内容已更新，但纹理 id 不变。仍需重建 SKImage：Skia 会缓存图像内容，
            // 复用同一个 SKImage 会导致画面停在首帧。
            var glInfo = new GRGlTextureInfo(GlTextureExternalOes, (uint)_textureId, GlRgba8);
            var backendTexture = new GRBackendTexture(_frameWidth, _frameHeight, mipmapped: false, glInfo);

            var image = SKImage.FromTexture(
                grContext, backendTexture, GRSurfaceOrigin.TopLeft,
                SKColorType.Rgba8888, SKAlphaType.Opaque);

            if (image != null)
            {
                _currentImage?.Dispose();
                _currentImage = image;
            }

            return _currentImage;
        }
    }

    /// <inheritdoc />
    public void ReleaseCurrentFrame()
    {
        // 纹理由 SurfaceTexture 拥有，帧内容持有到下次 UpdateTexImage 为止，无需逐帧释放。
    }

    /// <summary>
    /// 惰性创建 OES 纹理与 <c>SurfaceTexture</c>。只在渲染线程调用（GL 上下文所属线程）。
    /// </summary>
    private void EnsureSurfaceCreated()
    {
        if (_surfaceTexture != null) return;

        var textureIds = new int[1];
        GLES20.GlGenTextures(1, textureIds, 0);
        _textureId = textureIds[0];
        if (_textureId == 0) return;

        GLES20.GlBindTexture(GLES11Ext.GlTextureExternalOes, _textureId);
        // 外部纹理不支持 mipmap，且必须 CLAMP_TO_EDGE，否则采样出错。
        GLES20.GlTexParameteri(GLES11Ext.GlTextureExternalOes, GLES20.GlTextureMinFilter, GLES20.GlLinear);
        GLES20.GlTexParameteri(GLES11Ext.GlTextureExternalOes, GLES20.GlTextureMagFilter, GLES20.GlLinear);
        GLES20.GlTexParameteri(GLES11Ext.GlTextureExternalOes, GLES20.GlTextureWrapS, GLES20.GlClampToEdge);
        GLES20.GlTexParameteri(GLES11Ext.GlTextureExternalOes, GLES20.GlTextureWrapT, GLES20.GlClampToEdge);

        _surfaceTexture = new SurfaceTexture(_textureId);
        _surface = new Surface(_surfaceTexture);

        // 通知等待中的解码线程：Surface 已可用于 MediaCodec.Configure。
        _surfaceReady.Set();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;

            _currentImage?.Dispose();
            _currentImage = null;

            _surface?.Release();
            _surface = null;

            _surfaceTexture?.Release();
            _surfaceTexture = null;

            if (_textureId != 0)
            {
                // 删除纹理必须在拥有 GL 上下文的线程。正常回收路径（引擎 SyncVideoSessions）
                // 发生在 OnPaintSurface 驱动的渲染线程上，故此处通常成立；但若从其它线程
                // 释放（宿主关闭顺序等），GL 调用会静默失败或抛出 —— 捕获后交由上下文销毁
                // 时统一回收，避免 Dispose 抛异常掩盖真正的关闭错误。
                try
                {
                    GLES20.GlDeleteTextures(1, new[] { _textureId }, 0);
                }
                catch (Exception)
                {
                    // 上下文已失效或不在 GL 线程：纹理随上下文销毁一并释放。
                }

                _textureId = 0;
            }
        }

        // 释放可能仍在等待 Surface 的解码线程，使其以超时收尾而不是永久阻塞。
        _surfaceReady.Set();
        _surfaceReady.Dispose();
    }
}
