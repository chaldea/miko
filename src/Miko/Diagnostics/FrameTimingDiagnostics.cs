using System.Diagnostics;

namespace Miko.Diagnostics;

/// <summary>
/// Per-thread stage timings for the frame pipeline, with a dedicated first-frame snapshot.
///
/// <para>Counters are disabled by default: when disabled every probe site executes a single
/// boolean test and nothing else (no <see cref="Stopwatch"/> allocation, no timestamp read),
/// so normal rendering is unaffected. Enable with <see cref="Begin"/> and read with
/// <see cref="End"/> — same shape as <see cref="LayoutAllocationDiagnostics"/>.</para>
///
/// <para>Why the first frame is tracked separately: it is the only frame that pays the full
/// cost of building the element tree from scratch, and it cannot reuse the layout cache
/// (ISSUE-096) nor the intrinsic-size caches. Steady-state frames are dominated by painting,
/// which would otherwise mask regressions in the build stage (ISSUE-136).</para>
/// </summary>
internal static class FrameTimingDiagnostics
{
    [ThreadStatic] private static bool t_enabled;

    // 累计各阶段的 Stopwatch ticks（全部帧）。
    [ThreadStatic] private static long t_buildTicks;
    [ThreadStatic] private static long t_styleTicks;
    [ThreadStatic] private static long t_layoutTicks;
    [ThreadStatic] private static long t_paintTicks;

    // 首帧单独记账：它是唯一从零构建整棵树、且无法复用布局缓存的一帧。
    [ThreadStatic] private static long t_firstFrameBuildTicks;
    [ThreadStatic] private static long t_firstFrameStyleTicks;
    [ThreadStatic] private static long t_firstFrameLayoutTicks;
    [ThreadStatic] private static long t_firstFramePaintTicks;
    [ThreadStatic] private static bool t_firstLayoutSeen;

    [ThreadStatic] private static long t_buildCount;
    [ThreadStatic] private static long t_layoutCount;
    [ThreadStatic] private static long t_paintCount;

    // 构建嵌套深度。子组件的 Build() 由父组件的 CloseComponent 在父 Build() 内部调用，
    // 若逐层都记账，一棵 N 层的树会把同一段时间累计 N 次。只有最外层那次代表真实墙钟耗时。
    [ThreadStatic] private static int t_buildDepth;

    /// <summary>
    /// True while measuring. Probe sites read this before doing any work; it is also the
    /// public-ish switch the benchmarks and tests toggle.
    /// </summary>
    internal static bool IsEnabled => t_enabled;

    internal static void Begin()
    {
        t_buildTicks = 0;
        t_styleTicks = 0;
        t_layoutTicks = 0;
        t_paintTicks = 0;
        t_firstFrameBuildTicks = 0;
        t_firstFrameStyleTicks = 0;
        t_firstFrameLayoutTicks = 0;
        t_firstFramePaintTicks = 0;
        t_firstLayoutSeen = false;
        t_buildCount = 0;
        t_layoutCount = 0;
        t_paintCount = 0;
        t_buildDepth = 0;
        t_enabled = true;
    }

    internal static FrameTimingSnapshot End()
    {
        var snapshot = new FrameTimingSnapshot(
            ToMicroseconds(t_buildTicks),
            ToMicroseconds(t_styleTicks),
            ToMicroseconds(t_layoutTicks),
            ToMicroseconds(t_paintTicks),
            ToMicroseconds(t_firstFrameBuildTicks),
            ToMicroseconds(t_firstFrameStyleTicks),
            ToMicroseconds(t_firstFrameLayoutTicks),
            ToMicroseconds(t_firstFramePaintTicks),
            t_buildCount,
            t_layoutCount,
            t_paintCount);
        t_enabled = false;
        return snapshot;
    }

    /// <summary>
    /// Returns a timestamp to pass back to the matching <c>Record*</c> method, or 0 when
    /// measurement is off. Probe sites must treat 0 as "not measuring" and skip the record call.
    /// </summary>
    internal static long GetTimestamp() => t_enabled ? Stopwatch.GetTimestamp() : 0L;

    /// <summary>
    /// Enters an element-tree construction scope (<c>ComponentBase.Build</c> / <c>BuildNew</c>).
    /// Returns a timestamp for <see cref="ExitBuild"/>, or 0 when not measuring <em>or</em> when
    /// already inside an outer build — nested component builds run inside their parent's build,
    /// so only the outermost scope is timed (otherwise one span would be counted once per level).
    /// </summary>
    internal static long EnterBuild()
    {
        if (!t_enabled) return 0L;
        if (t_buildDepth++ > 0) return 0L;
        return Stopwatch.GetTimestamp();
    }

    /// <summary>Closes the scope opened by <see cref="EnterBuild"/>.</summary>
    internal static void ExitBuild(long startTimestamp)
    {
        if (!t_enabled) return;
        // 深度必须无条件回退，否则一次异常就会让后续所有构建都被当成嵌套而漏记。
        if (t_buildDepth > 0) t_buildDepth--;
        if (startTimestamp == 0L) return;

        var elapsed = Stopwatch.GetTimestamp() - startTimestamp;
        t_buildTicks += elapsed;
        t_buildCount++;
        // 构建先于布局发生，故「首帧构建」= 首次布局之前的全部构建。
        if (!t_firstLayoutSeen) t_firstFrameBuildTicks += elapsed;
    }

    /// <summary>Style cascade for the whole tree (<c>LayoutEngine.ComputeStyles</c>).</summary>
    internal static void RecordStyle(long startTimestamp)
    {
        if (!t_enabled || startTimestamp == 0L) return;
        var elapsed = Stopwatch.GetTimestamp() - startTimestamp;
        t_styleTicks += elapsed;
        if (!t_firstLayoutSeen) t_firstFrameStyleTicks += elapsed;
    }

    /// <summary>
    /// A full layout pass (<c>LayoutEngine.Layout</c>), excluding the style stage recorded above.
    /// Cache hits do not reach this probe, so the count reflects real relayouts only.
    /// </summary>
    internal static void RecordLayout(long startTimestamp)
    {
        if (!t_enabled || startTimestamp == 0L) return;
        var elapsed = Stopwatch.GetTimestamp() - startTimestamp;
        t_layoutTicks += elapsed;
        t_layoutCount++;
        if (!t_firstLayoutSeen)
        {
            t_firstFrameLayoutTicks += elapsed;
            // 本次布局即首帧布局；此后的构建/样式/布局都归入后续帧。
            t_firstLayoutSeen = true;
        }
    }

    /// <summary>Painting (<c>RenderEngine.Render</c> / <c>Update</c>).</summary>
    internal static void RecordPaint(long startTimestamp)
    {
        if (!t_enabled || startTimestamp == 0L) return;
        var elapsed = Stopwatch.GetTimestamp() - startTimestamp;
        t_paintTicks += elapsed;
        t_paintCount++;
        // 绘制发生在布局之后，首帧绘制即首次布局后的第一次绘制。
        if (t_paintCount == 1) t_firstFramePaintTicks += elapsed;
    }

    private static double ToMicroseconds(long ticks)
        => ticks * 1_000_000.0 / Stopwatch.Frequency;
}

/// <summary>
/// Stage timings in microseconds. <c>FirstFrame*</c> covers the work up to and including the
/// first layout pass; the unprefixed fields cover every measured frame.
/// </summary>
internal readonly record struct FrameTimingSnapshot(
    double BuildMicroseconds,
    double StyleMicroseconds,
    double LayoutMicroseconds,
    double PaintMicroseconds,
    double FirstFrameBuildMicroseconds,
    double FirstFrameStyleMicroseconds,
    double FirstFrameLayoutMicroseconds,
    double FirstFramePaintMicroseconds,
    long BuildCount,
    long LayoutCount,
    long PaintCount)
{
    /// <summary>Total measured pipeline time across every frame.</summary>
    public double TotalMicroseconds
        => BuildMicroseconds + StyleMicroseconds + LayoutMicroseconds + PaintMicroseconds;

    /// <summary>Total time attributed to the first frame.</summary>
    public double FirstFrameTotalMicroseconds
        => FirstFrameBuildMicroseconds + FirstFrameStyleMicroseconds
         + FirstFrameLayoutMicroseconds + FirstFramePaintMicroseconds;
}
