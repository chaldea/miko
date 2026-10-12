using Miko.Rendering;
using Miko.Windowing.MacOS;
using Silk.NET.Windowing;
using SkiaSharp;

namespace Miko.Windowing.Common;

/// <summary>
/// 把 Skia 的 GPU 上下文接到 Silk.NET 窗口上的呈现后端：Windows/Linux 用 OpenGL，macOS 用 Metal
/// （与 iOS 宿主同一条 Skia Metal 渲染路径，见 ISSUE-147）。桌面宿主与模拟器共用它，
/// 自身不再直接接触 GL 或 Metal。
///
/// <para><b>为什么 macOS 不再用 OpenGL</b>：macOS 的 OpenGL 自 10.14 起已弃用、停在 4.1，
/// 在 Apple Silicon 上只是 Metal 之上的一层转换（渲染器版本串即 <c>4.1 Metal</c>）。改用 Metal 后，
/// 视频帧可以像 iOS 一样经 <c>CVMetalTextureCache</c> 零拷贝采样，Retina 屏按 backing scale 出图，
/// 着色器/混合行为也与 iOS 真机一致。</para>
///
/// <para><b>线程约定</b>：<see cref="Create"/> 在窗口线程（macOS 上即主线程）的 <c>Load</c> 回调里调用——
/// Metal 后端要在这里把 <c>CAMetalLayer</c> 挂到窗口视图上，AppKit 只允许主线程这样做。
/// 其余成员只由渲染线程调用：<see cref="Initialize"/> 创建 <see cref="GRContext"/>，
/// 每帧 <see cref="BeginFrame"/> 取一张窗口表面、画完后 <see cref="Present"/>。</para>
///
/// <para>诊断用：环境变量 <c>MIKO_GRAPHICS_BACKEND=opengl</c> 让 macOS 退回 OpenGL，
/// 便于用同一探针对比两种后端。</para>
/// </summary>
public abstract class SilkWindowGraphics : IDisposable
{
    private static readonly Lazy<bool> s_useMetal = new(ResolveUseMetal);
    private GRContext? _context;

    /// <summary>
    /// 创建窗口时应请求的图形 API：Metal 后端需要一个不带 GL 上下文的窗口（<see cref="GraphicsAPI.None"/>），
    /// 必须在 <c>Window.Create</c> 之前就确定。
    /// </summary>
    public static GraphicsAPI WindowApi => s_useMetal.Value ? GraphicsAPI.None : GraphicsAPI.Default;

    /// <summary>为已初始化的窗口创建呈现后端。在窗口线程的 <c>Load</c> 回调中调用。</summary>
    public static SilkWindowGraphics Create(IWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (s_useMetal.Value && OperatingSystem.IsMacOS())
            return new MetalWindowGraphics(window);
        return new GlWindowGraphics(window);
    }

    /// <summary>当前后端的 GPU 上下文。<see cref="Initialize"/> 之后可用。</summary>
    public GRContext Context
    {
        get => _context ?? throw new InvalidOperationException("Window graphics have not been initialized.");
        protected set => _context = value;
    }

    /// <summary>后端与设备的简短描述，用于日志。</summary>
    public abstract string Description { get; }

    /// <summary>
    /// 窗口坐标到表面像素的缩放系数。宿主以窗口坐标（逻辑尺寸）布局，绘制前按它缩放画布。
    /// Metal 后端为窗口的 backing scale（Retina 屏为 2）；OpenGL 后端恒为 1，保持原有行为。
    /// </summary>
    public virtual float Scale => 1f;

    /// <summary>渲染线程：创建 GPU 上下文。</summary>
    public abstract void Initialize();

    /// <summary>
    /// 渲染线程：取本帧的窗口表面。须在任何离屏 GPU 渲染之前调用（OpenGL 后端在这里读取窗口帧缓冲绑定）。
    /// 返回非 <c>null</c> 时必须以 <see cref="Present"/> 结束本帧；表面归后端所有，调用方不得 Dispose。
    /// </summary>
    /// <param name="width">窗口宽度（窗口坐标）。</param>
    /// <param name="height">窗口高度（窗口坐标）。</param>
    /// <returns>本帧表面；窗口尺寸为 0（最小化）或暂时拿不到可绘制对象时为 <c>null</c>，跳过本帧即可。</returns>
    public abstract SKSurface? BeginFrame(int width, int height);

    /// <summary>渲染线程：提交本帧的 GPU 命令并呈现到窗口。</summary>
    public abstract void Present();

    /// <summary>渲染线程：窗口尺寸变化（窗口坐标）。</summary>
    public virtual void Resize(int width, int height)
    {
    }

    /// <summary>渲染线程：释放 GPU 上下文与后端资源。之前应先释放引用该上下文的表面与图像。</summary>
    public abstract void Dispose();

    /// <summary>清空资源缓存后释放 GPU 上下文（未创建时什么都不做）。</summary>
    protected void DisposeContext()
    {
        if (_context == null) return;
        GpuResourceCache.PurgeAllResources(_context);
        _context.Dispose();
        _context = null;
    }

    private static bool ResolveUseMetal()
    {
        if (!OperatingSystem.IsMacOS()) return false;
        if (string.Equals(Environment.GetEnvironmentVariable("MIKO_GRAPHICS_BACKEND"), "opengl",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 没有 Metal 设备的环境（部分虚拟机）退回 OpenGL，而不是开出一个画不出东西的窗口。
        return MetalWindowGraphics.IsSupported;
    }
}
