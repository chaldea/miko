using Miko.Rendering;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;

namespace Miko.Windowing.Common;

/// <summary>
/// OpenGL 呈现后端（Windows/Linux）。窗口的 GL 上下文由 <see cref="SilkWindowThreadRunner"/>
/// 转交给渲染线程；窗口帧缓冲的包装表面按尺寸/FBO 复用（见 <see cref="WindowRenderTarget"/>）。
/// </summary>
internal sealed class GlWindowGraphics(IWindow window) : SilkWindowGraphics
{
    private GL? _gl;
    private WindowRenderTarget? _renderTarget;

    public override string Description => $"OpenGL ({_gl?.GetStringS(StringName.Renderer)})";

    public override void Initialize()
    {
        _gl = window.CreateOpenGL();

        var grInterface = GRGlInterface.Create(name =>
            window.GLContext!.TryGetProcAddress(name, out var addr) ? addr : IntPtr.Zero);

        Context = GRContext.CreateGl(grInterface);
        GpuResourceCache.Configure(Context);
        _renderTarget = new WindowRenderTarget(Context);
    }

    public override SKSurface? BeginFrame(int width, int height)
    {
        if (_gl == null || _renderTarget == null) return null;

        // 窗口默认帧缓冲的绑定必须在任何离屏渲染之前读取：Skia 渲染离屏 SKSurface 时会绑定离屏 FBO
        // 并保持绑定，之后再读就会把窗口内容画进用户看不到的离屏画布，窗口保持黑屏（ISSUE-067）。
        int fboId = _gl.GetInteger(GLEnum.FramebufferBinding);
        return _renderTarget.Acquire(width, height, (uint)fboId);
    }

    public override void Present()
    {
        Context.Flush();
        window.GLContext?.SwapBuffers();
    }

    public override void Resize(int width, int height)
        => _gl?.Viewport(new Vector2D<int>(width, height));

    public override void Dispose()
    {
        // 表面引用 GRContext，必须先于它释放。
        _renderTarget?.Dispose();
        _renderTarget = null;
        DisposeContext();
        _gl?.Dispose();
        _gl = null;
    }
}
