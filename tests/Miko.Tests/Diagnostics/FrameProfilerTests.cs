using Miko.Common;
using Miko.Core.DomElements;
using Miko.Diagnostics;
using Miko.Hosting;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Diagnostics;

/// <summary>
/// ISSUE-144：逐帧分段耗时探针的<b>公开</b>开关。
///
/// <para>issue 要求「为示例应用增加用于验证问题的探针并确定在 android 模拟器中确实存在性能
/// 问题，然后再进行优化」。<see cref="FrameTimingDiagnostics"/> 是 internal 的基准测试设施，
/// 拿不到真机数据；<see cref="FrameProfiler"/> 把它暴露给宿主与应用，让「哪一段吃掉了这一帧」
/// 可以在设备上被量出来。</para>
///
/// <para>除了分段耗时，这里还验证<b>绘制原语计数</b>（盒子数、文本/图片绘制次数与耗时）——
/// 正是它把「45ms 的一帧」定位到「45 次文本绘制占 28–41ms」，而总耗时本身分辨不出这一点。</para>
/// </summary>
[Collection(Miko.Tests.FrameTimingCollection.Name)] // 探针测量本线程的全部管线工作，需串行
public class FrameProfilerTests
{
    private static DivElement BuildPage(int rows)
    {
        var page = new DivElement { Class = "page" };
        for (int i = 0; i < rows; i++)
        {
            page.AddChild(new DivElement
            {
                Class = "row",
                Style = new Style { Height = Length.Px(20), BackgroundColor = Color.FromRgb(200, 200, 200) },
                TextContent = $"row {i}"
            });
        }
        return page;
    }

    [Fact]
    public void Disabled_ByDefault()
    {
        // 默认关闭：探针点只做一次布尔判断，不读时钟、不分配。
        FrameProfiler.IsEnabled.ShouldBeFalse();
        FrameProfiler.BeginFrame().ShouldBe(0L, "a disabled profiler must return the 'not measuring' sentinel");
    }

    [Fact]
    public void EndFrame_WithSentinel_DoesNotReport()
    {
        var frames = new List<FrameProfile>();
        try
        {
            FrameProfiler.Enable(frames.Add);
            // 0 是「没在测量」的哨兵；用它收尾必须什么都不投递。
            FrameProfiler.EndFrame(0L, rebuilt: false);
        }
        finally
        {
            FrameProfiler.Disable();
        }

        frames.ShouldBeEmpty();
    }

    [Fact]
    public void Disable_StopsReporting()
    {
        var frames = new List<FrameProfile>();
        FrameProfiler.Enable(frames.Add);
        FrameProfiler.IsEnabled.ShouldBeTrue();

        FrameProfiler.Disable();

        FrameProfiler.IsEnabled.ShouldBeFalse();
        // 关闭之后开一帧仍然是哨兵，投递不会发生。
        var scope = FrameProfiler.BeginFrame();
        scope.ShouldBe(0L);
        FrameProfiler.EndFrame(scope, rebuilt: false);
        frames.ShouldBeEmpty();
    }

    [Fact]
    public void ReportsStageTimingsAndPrimitiveCounts()
    {
        var frames = new List<FrameProfile>();
        var engine = new MikoEngineBuilder().Build();
        using var surface = SKSurface.Create(new SKImageInfo(400, 300));

        try
        {
            FrameProfiler.Enable(frames.Add);

            var scope = FrameProfiler.BeginFrame();
            engine.Initialize(BuildPage(10), new List<StyleSheet>(), surface.Canvas, 400, 300);
            FrameProfiler.EndFrame(scope, rebuilt: true);
        }
        finally
        {
            FrameProfiler.Disable();
        }

        var frame = frames.ShouldHaveSingleItem();
        frame.FrameIndex.ShouldBe(0);
        frame.Rebuilt.ShouldBeTrue();
        frame.TotalMicroseconds.ShouldBeGreaterThan(0);

        // 样式/布局/绘制三段都跑过了。
        frame.StyleMicroseconds.ShouldBeGreaterThan(0);
        frame.LayoutMicroseconds.ShouldBeGreaterThan(0);
        frame.LayoutPasses.ShouldBe(1);
        frame.PaintPasses.ShouldBe(1);

        // 绘制原语：10 行各自一个盒子 + 一个文本节点，加上页面根。
        frame.BoxDraws.ShouldBeGreaterThanOrEqualTo(11);
        frame.TextDraws.ShouldBe(10);

        // 分段之和不应超过整帧墙钟（探针不重复计时）。
        frame.MeasuredMilliseconds.ShouldBeLessThanOrEqualTo(frame.TotalMilliseconds);
    }

    [Fact]
    public void FrameIndex_AdvancesPerFrame()
    {
        var frames = new List<FrameProfile>();
        var engine = new MikoEngineBuilder().Build();
        using var surface = SKSurface.Create(new SKImageInfo(400, 300));

        try
        {
            FrameProfiler.Enable(frames.Add);

            var first = FrameProfiler.BeginFrame();
            engine.Initialize(BuildPage(5), new List<StyleSheet>(), surface.Canvas, 400, 300);
            FrameProfiler.EndFrame(first, rebuilt: true);

            var second = FrameProfiler.BeginFrame();
            engine.Render(surface.Canvas);
            FrameProfiler.EndFrame(second, rebuilt: false);
        }
        finally
        {
            FrameProfiler.Disable();
        }

        frames.Count.ShouldBe(2);
        frames[0].FrameIndex.ShouldBe(0);
        frames[1].FrameIndex.ShouldBe(1);
        frames[1].Rebuilt.ShouldBeFalse();
        // 第二帧命中布局缓存（ISSUE-096）：无重排，只重绘。
        frames[1].LayoutPasses.ShouldBe(0);
    }
}
