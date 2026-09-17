using Miko.Common;
using Miko.Components;
using Miko.Core.DomElements;
using Miko.Diagnostics;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Diagnostics;

/// <summary>
/// ISSUE-136：首帧布局耗时探针。
///
/// <para>issue 要求「增加首帧布局的探针，为当前布局引擎增加布局耗时的探针，用于验证优化效果」。
/// 探针默认关闭——正常渲染路径上每个探测点只执行一次布尔判断——因此这里既验证分段耗时确实
/// 被记录下来，也验证不开启时不会留下任何状态。</para>
/// </summary>
[Collection(Miko.Tests.FrameTimingCollection.Name)] // 探针测量本线程的全部管线工作，需串行（ISSUE-136）
public class FrameTimingDiagnosticsTests
{
    private static readonly List<StyleSheet> Styles =
    [
        new StyleSheet
        {
            Rules =
            [
                new StyleRule
                {
                    Selector = new Miko.Styling.Selectors.ClassSelector("probe-item"),
                    Style = new Style { Display = Display.Block, Height = Length.Px(20) },
                },
            ],
        },
    ];

    [Fact]
    public void Disabled_ByDefault()
    {
        // 未调用 Begin 时探针必须是关闭的，否则正常渲染会白白读时间戳。
        FrameTimingDiagnostics.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void GetTimestamp_WhenDisabled_ReturnsZeroSentinel()
    {
        // 0 是「没在测量」的哨兵，探测点据此跳过记录。
        FrameTimingDiagnostics.GetTimestamp().ShouldBe(0L);
    }

    [Fact]
    public void End_DisablesMeasurement()
    {
        FrameTimingDiagnostics.Begin();
        FrameTimingDiagnostics.IsEnabled.ShouldBeTrue();

        FrameTimingDiagnostics.End();

        FrameTimingDiagnostics.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void MeasuresBuildStyleAndLayout_ForAFirstFrame()
    {
        var layoutEngine = new LayoutEngine();

        FrameTimingDiagnostics.Begin();
        FrameTimingSnapshot snapshot;
        try
        {
            var page = new ProbePage { ItemCount = 30 };
            var root = page.Build();
            layoutEngine.Layout(root, Styles, 800, 600);
        }
        finally
        {
            snapshot = FrameTimingDiagnostics.End();
        }

        // 构建与布局各自被记账一次（构建按最外层去重，嵌套子组件不重复计入）。
        snapshot.BuildCount.ShouldBe(1);
        snapshot.LayoutCount.ShouldBe(1);

        // 三个阶段都应产生可测的耗时。
        snapshot.BuildMicroseconds.ShouldBeGreaterThan(0);
        snapshot.StyleMicroseconds.ShouldBeGreaterThan(0);
        snapshot.LayoutMicroseconds.ShouldBeGreaterThan(0);

        // 这一帧就是首帧，因此首帧分段等于总分段。
        snapshot.FirstFrameBuildMicroseconds.ShouldBe(snapshot.BuildMicroseconds);
        snapshot.FirstFrameStyleMicroseconds.ShouldBe(snapshot.StyleMicroseconds);
        snapshot.FirstFrameLayoutMicroseconds.ShouldBe(snapshot.LayoutMicroseconds);
    }

    [Fact]
    public void NestedComponentBuilds_AreCountedOnce()
    {
        // 子组件的 Build 发生在父组件 Build 内部；逐层记账会把同一段墙钟时间重复累加。
        var layoutEngine = new LayoutEngine();

        FrameTimingDiagnostics.Begin();
        FrameTimingSnapshot snapshot;
        try
        {
            var page = new ProbePage { ItemCount = 10 };
            layoutEngine.Layout(page.Build(), Styles, 800, 600);
        }
        finally
        {
            snapshot = FrameTimingDiagnostics.End();
        }

        // 10 个子组件 + 1 个页面，但只有最外层那次计入。
        snapshot.BuildCount.ShouldBe(1);
    }

    [Fact]
    public void SubsequentFrame_IsExcludedFromFirstFrameTotals()
    {
        var layoutEngine = new LayoutEngine();
        var page = new ProbePage { ItemCount = 10 };

        FrameTimingDiagnostics.Begin();
        FrameTimingSnapshot snapshot;
        try
        {
            layoutEngine.Layout(page.Build(), Styles, 800, 600);

            // 第二帧：重新构建并重排。
            layoutEngine.Layout(page.Build(), Styles, 800, 600);
        }
        finally
        {
            snapshot = FrameTimingDiagnostics.End();
        }

        snapshot.BuildCount.ShouldBe(2);
        snapshot.LayoutCount.ShouldBe(2);
        // 首帧只占其中一次，故首帧耗时必须严格小于总耗时。
        snapshot.FirstFrameLayoutMicroseconds.ShouldBeLessThan(snapshot.LayoutMicroseconds);
        snapshot.FirstFrameTotalMicroseconds.ShouldBeLessThan(snapshot.TotalMicroseconds);
    }

    private sealed class ProbePage : ComponentBase
    {
        public int ItemCount { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            for (int i = 0; i < ItemCount; i++)
            {
                var item = builder.OpenComponent<ProbeItem>();
                item.Index = i;
                builder.CloseComponent();
            }
            builder.CloseElement();
        }
    }

    private sealed class ProbeItem : ComponentBase
    {
        [Parameter] public int Index { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var div = builder.OpenElement<DivElement>();
            div.Class = "probe-item";
            builder.AddContent($"item {Index}");
            builder.CloseElement();
        }
    }
}
