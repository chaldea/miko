using SkiaSharp;

namespace Miko.Rendering;

/// <summary>
/// 把宿主窗口的 GL 默认帧缓冲包装成一张可绘制的 <see cref="SKSurface"/>，并在尺寸与 FBO
/// 不变时<b>复用</b>它。
///
/// <para><b>为什么要复用</b>：包装对象本身不持有像素（像素在 GL 帧缓冲里），但
/// <see cref="GRBackendRenderTarget"/> 与 <see cref="SKSurface"/> 都是非托管 Skia 对象，
/// 每帧新建再释放会让 Skia 每帧重建该表面的内部渲染目标状态。这部分开销既不计入托管堆，
/// 也<b>不计入</b> <c>GRContext.GetResourceCacheUsage</c>——包装出的渲染目标不是 budgeted
/// 资源，Skia 的缓存账本看不到它，所以它曾长期藏在"原生内存"这个汇总数字里（ISSUE-143）。</para>
///
/// <para>实测（<c>examples/Diagnostics/ResizeMemoryProbe</c>，固定尺寸 1500 帧，仅清屏无绘制）：
/// 每帧新建 73.6 MB 私有内存 / 4.56 MB 托管分配，复用后 66.9 MB / 1.81 MB。稳态下
/// 复用把这条路径的托管分配降到零。</para>
///
/// <para><b>为什么键里要有 FBO</b>：宿主每帧读取当前 <c>GL_FRAMEBUFFER_BINDING</c>。
/// 模拟器等宿主会先渲染到离屏表面再合成，窗口 FBO 句柄在一次运行中并非恒定；
/// 若只按尺寸缓存，绑定变化后就会把内容画进上一个 FBO，窗口保持黑屏
/// （与 ISSUE-067 同一类现象）。</para>
///
/// <para>不是线程安全的：与 <see cref="GRContext"/> 一样，只能由持有 GL 上下文的渲染线程使用。</para>
/// </summary>
public sealed class WindowRenderTarget : IDisposable
{
    private readonly GRContext _context;
    private GRBackendRenderTarget? _target;
    private SKSurface? _surface;
    private int _width;
    private int _height;
    private uint _framebuffer;
    private bool _hasTarget;

    /// <summary>GL_RGBA8。宿主统一以 8 位 RGBA 呈现。</summary>
    private const uint GlRgba8 = 0x8058;

    /// <summary>抗锯齿采样数（0 表示不多重采样）与模板位数，与各宿主此前的取值一致。</summary>
    private const int SampleCount = 0;
    private const int StencilBits = 8;

    public WindowRenderTarget(GRContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <summary>
    /// 取当前帧可绘制的表面。尺寸与 FBO 与上一帧相同时返回同一张表面，否则重建。
    /// </summary>
    /// <param name="width">帧缓冲宽度（像素）。</param>
    /// <param name="height">帧缓冲高度（像素）。</param>
    /// <param name="framebuffer">当前绑定的 GL 帧缓冲句柄。</param>
    /// <returns>
    /// 可绘制的表面；尺寸非正或 Skia 拒绝创建时返回 <c>null</c>。
    /// <para>返回的表面由本对象拥有，调用方<b>不得</b> Dispose 它。</para>
    /// </returns>
    public SKSurface? Acquire(int width, int height, uint framebuffer)
    {
        // 最小化时窗口为 0×0：用 0 尺寸创建渲染目标会返回 null，解引用其 Canvas 会抛异常
        // 并污染 GRContext，导致恢复后再也无法渲染（ISSUE-067 现象）。直接跳过本帧。
        if (width <= 0 || height <= 0) return null;

        if (_hasTarget && !NeedsRebuild(_width, _height, _framebuffer, width, height, framebuffer))
            return _surface;

        ReleaseTarget();

        var fbInfo = new GRGlFramebufferInfo(framebuffer, GlRgba8);
        var target = new GRBackendRenderTarget(width, height, SampleCount, StencilBits, fbInfo);
        var surface = SKSurface.Create(_context, target, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        if (surface == null)
        {
            // 创建失败不缓存失败状态：下一帧尺寸可能已恢复正常，应当再试一次。
            target.Dispose();
            return null;
        }

        _target = target;
        _surface = surface;
        _width = width;
        _height = height;
        _framebuffer = framebuffer;
        _hasTarget = true;
        return _surface;
    }

    /// <summary>
    /// 缓存的表面是否必须为新的一帧重建。三个分量任一变化都必须重建：尺寸变了表面就画错区域，
    /// FBO 变了内容会画进用户看不到的帧缓冲（ISSUE-067 那类黑屏）。
    ///
    /// <para>抽成静态纯函数是为了可测：构造真实的 <see cref="GRContext"/> 需要活的 GL 上下文，
    /// 单元测试里没有；而"何时复用、何时重建"正是本类唯一的决策，必须被测住。</para>
    /// </summary>
    internal static bool NeedsRebuild(
        int cachedWidth, int cachedHeight, uint cachedFramebuffer,
        int width, int height, uint framebuffer) =>
        cachedWidth != width || cachedHeight != height || cachedFramebuffer != framebuffer;

    private void ReleaseTarget()
    {
        // 先表面后目标：表面引用目标，反序释放会让 Skia 访问已销毁的渲染目标。
        _surface?.Dispose();
        _surface = null;
        _target?.Dispose();
        _target = null;
        _hasTarget = false;
    }

    public void Dispose() => ReleaseTarget();
}
