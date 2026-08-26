using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Rendering;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// 回归测试：<b>不改变 DOM 却改变画面</b>的变化必须能唤醒宿主出帧（ISSUE-129）。
///
/// <para>现场是 DevTools 的「在主窗口高亮选中节点」：点击 DevTools 的 DOM 树只会给主引擎的
/// <see cref="RenderEngine.OverlayCallback"/> 挂一个绘制回调，主窗口的 DOM 一个字节都没动，
/// 因此没有任何元素可以标脏。宿主在 <see cref="MikoEngine.HasPendingVisualWork"/> 为 false 时
/// 会整帧跳过（ISSUE-096 的空闲跳帧），高亮于是永远画不出来。</para>
///
/// <para>变更版本号还是进程级全局静态时，这类调用方能<b>侥幸</b>工作：DevTools 窗口自身的
/// DOM 重建会不断递增全局计数，连带击穿主引擎的布局缓存，主窗口因此永不空闲、每帧都重绘。
/// ISSUE-129 按引擎隔离后这个巧合消失，就必须显式 <see cref="MikoEngine.RequestRepaint"/>。</para>
/// </summary>
public class OverlayRepaintTests
{
    private static (MikoEngine engine, Element root, List<StyleSheet> sheets) NewEngine(string marker)
    {
        var root = new DivElement { Class = marker, Children = { new SpanElement { TextContent = "hello" } } };
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { Width = Length.Px(200), Height = Length.Px(100) },
        });
        return (new MikoEngineBuilder().Build(), root, new List<StyleSheet> { sheet });
    }

    private static SKSurface RenderToIdle(MikoEngine engine, Element root, List<StyleSheet> sheets)
    {
        var surface = SKSurface.Create(new SKImageInfo(300, 300));
        engine.Initialize(root, sheets, surface.Canvas, 300, 300);
        engine.Render(surface.Canvas);
        return surface;
    }

    [Fact]
    public void RequestRepaint_WakesAnIdleEngine()
    {
        var (engine, root, sheets) = NewEngine("main");
        using var surface = RenderToIdle(engine, root, sheets);
        engine.HasPendingVisualWork.ShouldBeFalse("前提：应已进入稳态");

        engine.RequestRepaint();

        engine.HasPendingVisualWork.ShouldBeTrue("宿主据此决定出帧，否则整帧被跳过");
    }

    [Fact]
    public void RequestRepaint_IsConsumedByRendering()
    {
        var (engine, root, sheets) = NewEngine("main");
        using var surface = RenderToIdle(engine, root, sheets);

        engine.RequestRepaint();
        engine.Render(surface.Canvas);

        // 只请求一帧：画完就该回到空闲，不能变成每帧重绘。
        engine.HasPendingVisualWork.ShouldBeFalse();
    }

    [Fact]
    public void RequestRepaint_DoesNotInvalidateLayout()
    {
        var (engine, root, sheets) = NewEngine("main");
        using var surface = RenderToIdle(engine, root, sheets);
        var layoutBefore = engine.GetCurrentLayout();

        engine.RequestRepaint();
        engine.Render(surface.Canvas);

        // 重绘不等于重排：布局输入没变，布局树应被原样复用（ISSUE-096 的快速路径）。
        engine.GetCurrentLayout().ShouldBeSameAs(layoutBefore);
    }

    /// <summary>
    /// 端到端复现 DevTools 的高亮场景：次级引擎在主引擎<b>已空闲</b>之后挂上覆盖层回调，
    /// 主引擎必须能出一帧并真的调用该回调。
    /// </summary>
    [Fact]
    public void OverlaySetAfterIdle_IsDrawnOnTheNextFrame()
    {
        var (main, mainRoot, mainSheets) = NewEngine("main");
        using var mainSurface = RenderToIdle(main, mainRoot, mainSheets);

        // 次级引擎（模拟 DevTools 窗口）在自己的树上忙碌——不应影响主引擎的空闲判定。
        var (devTools, devToolsRoot, devToolsSheets) = NewEngine("devtools");
        using var devToolsSurface = RenderToIdle(devTools, devToolsRoot, devToolsSheets);
        for (int i = 0; i < 3; i++)
            devToolsRoot.AddChild(new SpanElement { TextContent = $"row {i}" });
        devTools.Render(devToolsSurface.Canvas);

        main.HasPendingVisualWork.ShouldBeFalse("主引擎的 DOM 没变，应保持空闲");

        // DevTools 选中一个节点：只挂覆盖层回调 + 请求重绘，绝不碰主窗口的 DOM。
        var mainRenderEngine = main.RenderEngine;
        int overlayDraws = 0;
        mainRenderEngine.OverlayCallback = _ => overlayDraws++;
        main.RequestRepaint();

        // 宿主的空闲跳帧判据必须放行这一帧。
        main.HasPendingVisualWork.ShouldBeTrue();
        main.Render(mainSurface.Canvas);

        overlayDraws.ShouldBe(1, "高亮必须被真正画出来");
    }

    [Fact]
    public void OverlayIsRedrawnOnEveryFrame_SoItFollowsTheElement()
    {
        var (engine, root, sheets) = NewEngine("main");
        using var surface = RenderToIdle(engine, root, sheets);

        int overlayDraws = 0;
        engine.RenderEngine.OverlayCallback = _ => overlayDraws++;

        // 后续任何一帧（这里由 DOM 变化驱动）都应重画覆盖层，
        // 使高亮框跟随元素移动，而不是停在初次绘制的位置。
        root.AddChild(new SpanElement { TextContent = "more" });
        engine.Render(surface.Canvas);
        engine.Render(surface.Canvas);

        overlayDraws.ShouldBe(2);
    }
}
