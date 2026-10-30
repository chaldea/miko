using Android.Opengl;
using Javax.Microedition.Khronos.Opengles;
using Miko.Graphics;
using EGLConfig = Javax.Microedition.Khronos.Egl.EGLConfig;

namespace Miko.Android;

/// <summary>
/// <see cref="MikoSurfaceView"/> 的 GL 渲染器：在 GL 线程上持有 <see cref="GraphicsDevice"/>，
/// 把窗口帧缓冲包装成 <see cref="RenderSurface"/> 交给视图绘制（ISSUE-162）。
///
/// <para>做法对齐此前所用的 SkiaSharp.Views <c>SKGLSurfaceViewRenderer</c>：每帧先清屏，设备在首帧惰性创建
/// （用 EGL 加载 GL 函数），帧缓冲的包装表面按尺寸与配置复用，画完后提交 GPU 命令。
/// 与它不同的一点：EGL 上下文丢失后（<see cref="OnSurfaceCreated"/> 再次被调用）放弃旧设备、在新上下文上
/// 重建，而不是继续在已失效的上下文上发命令。</para>
/// </summary>
internal sealed class MikoGLRenderer : Java.Lang.Object, GLSurfaceView.IRenderer
{
    private readonly Action<RenderSurface, GraphicsDevice> _paint;
    private readonly Action<GraphicsDevice?> _deviceChanged;
    private readonly int[] _queryBuffer = new int[3];
    private GraphicsDevice? _device;
    private WindowRenderTarget? _target;
    private int _width;
    private int _height;
    private bool _surfaceCreatedOnce;

    /// <param name="paint">GL 线程：绘制一帧（表面归渲染器所有）。</param>
    /// <param name="deviceChanged">GL 线程：设备创建或因上下文丢失而作废时通知（作废时为 <c>null</c>）。</param>
    public MikoGLRenderer(Action<RenderSurface, GraphicsDevice> paint, Action<GraphicsDevice?> deviceChanged)
    {
        _paint = paint;
        _deviceChanged = deviceChanged;
    }

    public void OnSurfaceCreated(IGL10? gl, EGLConfig? config)
    {
        // 首次调用是 GL 线程启动；之后再被调用说明 EGL 上下文丢失后已重建，旧设备里的 GL 对象全部失效。
        if (_surfaceCreatedOnce && _device != null)
        {
            _target = null;
            _device.Abandon();
            _device = null;
            _deviceChanged(null);
        }
        _surfaceCreatedOnce = true;
    }

    public void OnSurfaceChanged(IGL10? gl, int width, int height)
    {
        GLES20.GlViewport(0, 0, width, height);
        _width = width;
        _height = height;
    }

    public void OnDrawFrame(IGL10? gl)
    {
        GLES20.GlClear(GLES20.GlColorBufferBit | GLES20.GlDepthBufferBit | GLES20.GlStencilBufferBit);

        if (_device == null)
        {
            _device = GraphicsDevice.CreateOpenGl();
            _target = _device.CreateWindowRenderTarget();
            _deviceChanged(_device);
        }

        GLES20.GlGetIntegerv(GLES20.GlFramebufferBinding, _queryBuffer, 0);
        GLES20.GlGetIntegerv(GLES20.GlStencilBits, _queryBuffer, 1);
        GLES20.GlGetIntegerv(GLES20.GlSamples, _queryBuffer, 2);
        var surface = _target!.Acquire(_width, _height, (uint)_queryBuffer[0], _queryBuffer[2], _queryBuffer[1]);
        if (surface == null) return;

        int save = surface.Context.Save();
        try
        {
            _paint(surface, _device);
        }
        finally
        {
            surface.Context.RestoreToCount(save);
        }

        _device.Flush();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _target?.Dispose();
            _target = null;
            _device?.Dispose();
            _device = null;
        }
        base.Dispose(disposing);
    }
}
