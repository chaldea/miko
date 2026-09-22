using System.Diagnostics;

namespace Miko.Diagnostics;

/// <summary>
/// 一帧的分段耗时（微秒）与该帧做过的工作量。由 <see cref="FrameProfiler"/> 在启用后
/// 逐帧投递给宿主/应用设置的接收器。
/// </summary>
/// <param name="FrameIndex">自启用以来的帧序号（从 0 起）。</param>
/// <param name="TotalMicroseconds">
/// 整帧墙钟耗时：包含 DOM 重建、动画推进、样式、布局与绘制（宿主的清屏/合成回调也在内，
/// 它由 <c>MikoInteractionController.RenderFrame</c> 的 render 委托执行）。
/// </param>
/// <param name="BuildMicroseconds">组件树构建（<c>ComponentBase.Build</c> / <c>BuildNew</c>）。</param>
/// <param name="StyleMicroseconds">样式级联（<c>LayoutEngine.ComputeStyles</c>）。</param>
/// <param name="LayoutMicroseconds">布局计算（不含上面的样式阶段）。</param>
/// <param name="PaintMicroseconds">绘制（<c>RenderEngine.Render</c>，不含 GPU 侧的 flush）。</param>
/// <param name="BuildPasses">本帧的顶层组件构建次数。</param>
/// <param name="LayoutPasses">本帧真正跑完的完整布局遍历次数（命中布局缓存的帧为 0）。</param>
/// <param name="PaintPasses">本帧的整树绘制次数（页面转场期间为 2：leaving + entering 两个图层）。</param>
/// <param name="AllocatedBytes">本帧在渲染线程上的托管分配量。</param>
/// <param name="Rebuilt">本帧是否重建了整棵 DOM 树（路由导航或热重载）。</param>
/// <param name="BoxDraws">本帧真正走完绘制的盒子数。</param>
/// <param name="TextDraws">本帧的文本节点绘制次数。</param>
/// <param name="TextMicroseconds">文本绘制累计耗时。</param>
/// <param name="ImageDraws">本帧的位图绘制次数（含背景图平铺的每一块）。</param>
/// <param name="ImageMicroseconds">位图绘制累计耗时。</param>
public readonly record struct FrameProfile(
    long FrameIndex,
    double TotalMicroseconds,
    double BuildMicroseconds,
    double StyleMicroseconds,
    double LayoutMicroseconds,
    double PaintMicroseconds,
    long BuildPasses,
    long LayoutPasses,
    long PaintPasses,
    long AllocatedBytes,
    bool Rebuilt,
    long BoxDraws,
    long TextDraws,
    double TextMicroseconds,
    long ImageDraws,
    double ImageMicroseconds)
{
    /// <summary>整帧耗时（毫秒）。</summary>
    public double TotalMilliseconds => TotalMicroseconds / 1000.0;

    /// <summary>被分段探针归类的耗时之和（毫秒）；与 <see cref="TotalMilliseconds"/> 的差额即未被归类的部分。</summary>
    public double MeasuredMilliseconds
        => (BuildMicroseconds + StyleMicroseconds + LayoutMicroseconds + PaintMicroseconds) / 1000.0;
}

/// <summary>
/// 逐帧分段耗时探针的<b>公开</b>开关。把内部的 <see cref="FrameTimingDiagnostics"/>
/// （基准测试用，进程内 internal）暴露给宿主与示例应用，使性能问题可以在<b>真机/模拟器上</b>
/// 被测量，而不是只能在桌面基准里推断（ISSUE-144）。
///
/// <para>默认关闭。关闭时每帧只多一次布尔判断，所有探针点都不读时钟、不分配——与
/// <see cref="FrameTimingDiagnostics"/> 的既有约定一致。</para>
///
/// <para>线程模型：<see cref="Enable"/> 可从任意线程调用（宿主通常在创建视图时调用），
/// 而计时状态是渲染线程本地的——<see cref="BeginFrame"/> / <see cref="EndFrame"/> 由
/// <c>MikoInteractionController.RenderFrame</c> 在渲染线程上成对调用，因此 Android 的 GL 线程、
/// iOS 的 CADisplayLink 线程与桌面的主线程各自记账，互不干扰。</para>
/// </summary>
public static class FrameProfiler
{
    private static volatile Action<FrameProfile>? _sink;
    private static long _frameIndex;

    [ThreadStatic] private static long t_allocatedAtFrameStart;

    /// <summary>探针当前是否启用。</summary>
    public static bool IsEnabled => _sink != null;

    /// <summary>
    /// 启用探针，并把每帧的 <see cref="FrameProfile"/> 投递给 <paramref name="sink"/>。
    /// <paramref name="sink"/> 在渲染线程上、且在渲染锁之内被调用，因此它必须廉价
    /// （写日志/累加计数器）——在其中做重活会直接拖慢被测量的那一帧。
    /// </summary>
    public static void Enable(Action<FrameProfile> sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        _frameIndex = 0;
        _sink = sink;
    }

    /// <summary>关闭探针。</summary>
    public static void Disable() => _sink = null;

    /// <summary>
    /// 开启一帧的计时。返回传给 <see cref="EndFrame"/> 的时间戳；探针关闭时返回 0，
    /// 此时 <see cref="EndFrame"/> 是 no-op。
    /// </summary>
    internal static long BeginFrame()
    {
        if (_sink == null) return 0L;

        FrameTimingDiagnostics.Begin();
        t_allocatedAtFrameStart = GC.GetAllocatedBytesForCurrentThread();
        return Stopwatch.GetTimestamp();
    }

    /// <summary>结束 <see cref="BeginFrame"/> 开启的计时并投递结果。</summary>
    internal static void EndFrame(long startTimestamp, bool rebuilt)
    {
        if (startTimestamp == 0L) return;

        var elapsed = Stopwatch.GetTimestamp() - startTimestamp;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - t_allocatedAtFrameStart;
        var stages = FrameTimingDiagnostics.End();
        var sink = _sink;
        if (sink == null) return;

        sink(new FrameProfile(
            Interlocked.Increment(ref _frameIndex) - 1,
            elapsed * 1_000_000.0 / Stopwatch.Frequency,
            stages.BuildMicroseconds,
            stages.StyleMicroseconds,
            stages.LayoutMicroseconds,
            stages.PaintMicroseconds,
            stages.BuildCount,
            stages.LayoutCount,
            stages.PaintCount,
            allocated,
            rebuilt,
            stages.BoxCount,
            stages.TextDrawCount,
            stages.TextDrawMicroseconds,
            stages.ImageDrawCount,
            stages.ImageDrawMicroseconds));
    }
}
