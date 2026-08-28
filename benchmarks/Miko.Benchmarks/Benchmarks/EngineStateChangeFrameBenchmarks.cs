using BenchmarkDotNet.Attributes;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Styling;
using Miko.Styling.Selectors;
using SkiaSharp;

namespace Miko.Benchmarks.Benchmarks;

/// <summary>
/// 状态变更帧的成本（ISSUE-132 的防回归项）。
///
/// <para>拖动 <c>IonRange</c> 时每个 mousemove 分配约 2 MB，其中 89% 在 <c>LayoutEngine.Layout</c>
/// ——只改了 knob 的 <c>left</c>，却付整页 restyle + 整树 relayout 的代价。Gen0 被瞬间打满，
/// 对象被提升进 Gen2，于是 G2 单调上涨（27 MB → 328 MB）且很难回收。增量布局让未受影响的子树
/// 复用上一帧的 <see cref="Miko.Layout.LayoutBox"/>，把这一项压回空闲帧量级。</para>
///
/// <para><b>必须走引擎</b>（<see cref="MikoEngine.Render"/>）而不是直接调 <c>LayoutEngine</c>：
/// 布局缓存以「根元素 + 样式表列表」的<b>引用</b>为键，脱离引擎自行传参会因为传了另一个列表实例
/// 而恒不命中，测出来的永远是全量重排的数字。同样的教训见 memory: paint-only-animation-overlay
/// ——只有走 <c>MikoEngine.Tick</c> 的基准才测得出 overlay 的效果。</para>
/// </summary>
[MemoryDiagnoser]
public class EngineStateChangeFrameBenchmarks
{
    private MikoEngine _engine = null!;
    private SKSurface _surface = null!;
    private Element _page = null!;
    private Element _knob = null!;
    private Element _shallowKnob = null!;
    private List<StyleSheet> _styles = null!;
    private int _tick;

    /// <summary>页面行数。真实的 Ionic 页面在数百个元素量级。</summary>
    [Params(50, 200)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _surface = SKSurface.Create(new SKImageInfo(400, 800));
        _styles =
        [
            new StyleSheet
            {
                Rules =
                [
                    new StyleRule
                    {
                        Selector = new ClassSelector("page"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(400) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("row"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            Height = Length.Px(40),
                            Padding = Length.Px(8),
                            BackgroundColor = Color.FromRgb(240, 242, 245)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("cell"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(100) }
                    },
                ]
            }
        ];

        _page = new DivElement { Class = "page" };
        for (int i = 0; i < RowCount; i++)
        {
            var row = new DivElement { Class = "row" };
            for (int c = 0; c < 3; c++)
                row.AddChild(new DivElement { Class = "cell", TextContent = $"r{i}c{c}" });
            _page.AddChild(row);
        }

        // 深处的「滑块把手」：改它只影响它自己那一小棵子树，正是 IonRange 拖动的形态。
        _knob = _page.Children[RowCount / 2].Children[0];
        // 根的直接子元素：改它会让整个兄弟层（RowCount 个）都重新匹配一次选择器，
        // 是增量路径的<b>最坏</b>形态，用来守住上界。
        _shallowKnob = _page.Children[0];

        _engine = new MikoEngineBuilder().Build();
        _engine.Initialize(_page, _styles, _surface.Canvas, 400, 800);
        _engine.Render(_surface.Canvas);
    }

    [GlobalCleanup]
    public void Cleanup() => _surface.Dispose();

    /// <summary>空闲帧基线：无变更，布局整棵复用（ISSUE-096 的快速路径）。</summary>
    [Benchmark(Baseline = true, Description = "Idle frame (layout fully reused)")]
    public void IdleFrame() => _engine.Render(_surface.Canvas);

    /// <summary>
    /// ISSUE-132 的目标场景：页面深处一个元素的行内样式每帧都在变（拖动滑块）。
    /// 目标是与空闲帧同量级，而不是与全量重排同量级。
    /// </summary>
    [Benchmark(Description = "Deep inline-style change (drag a slider knob)")]
    public void DeepStateChangeFrame()
    {
        _knob.Style = new Style { Display = Display.Block, MarginLeft = Length.Px(_tick++ % 64) };
        _engine.InvalidateElement(_knob);
        _engine.Render(_surface.Canvas);
    }

    /// <summary>
    /// 最坏形态：变更元素是根的直接子元素，于是整个兄弟层都要重新匹配选择器。
    /// 兄弟们的<b>子树</b>仍然复用，因此仍显著优于全量重排。
    /// </summary>
    [Benchmark(Description = "Shallow change (whole sibling layer re-matches)")]
    public void ShallowStateChangeFrame()
    {
        _shallowKnob.Style = new Style { Display = Display.Flex, MarginLeft = Length.Px(_tick++ % 64) };
        _engine.InvalidateElement(_shallowKnob);
        _engine.Render(_surface.Canvas);
    }

    /// <summary>
    /// 对照组：强制每帧全量重排（改动前的行为）。上面两项相对它的差距就是本次优化的收益。
    /// </summary>
    [Benchmark(Description = "Forced full relayout (pre-ISSUE-132 behaviour)")]
    public void FullRelayoutFrame()
    {
        _knob.Style = new Style { Display = Display.Block, MarginLeft = Length.Px(_tick++ % 64) };
        _engine.InvalidateLayoutCache();
        _engine.InvalidateElement(_knob);
        _engine.Render(_surface.Canvas);
    }
}
