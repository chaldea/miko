using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// ISSUE-117 回归测试：次级引擎（DevTools 的独立窗口）必须在**同进程内另一个引擎持续变更
/// 其 DOM** 的情况下依然能回到空闲。
///
/// <para>背景：变更版本号曾是进程级全局静态计数，任何引擎的 DOM 变更都会递增它。因此次级
/// 引擎的 <c>IsLayoutCurrent</c> 会被主窗口的活动持续击穿，
/// <see cref="MikoEngine.HasPendingVisualWork"/> 恒为 true —— DevTools 的空闲跳帧因此完全
/// 失效，每帧都跑全量样式解析+布局，内存呈锯齿、连带主程序卡顿。当时的处置是新增不含
/// 「布局时效性」这一项的 <see cref="MikoEngine.HasPendingRenderWork"/> 作为变通判据。</para>
///
/// <para>ISSUE-129 起变更版本号按引擎实例隔离（<see cref="MikoEngine.Mutations"/>），根因已
/// 消除：<b>两个判据现在对次级引擎都成立</b>。本类因此对二者都断言——
/// <see cref="MikoEngine.HasPendingRenderWork"/> 仍是公开 API，其语义不得回退。
/// 隔离性本身的完整覆盖见 <c>EngineIsolationTests</c>。</para>
/// </summary>
public class SecondaryEngineIdleTests
{
    private static (MikoEngine engine, Element root, List<StyleSheet> sheets) CreateEngine(string marker)
    {
        var root = new DivElement { Class = marker, Children = { new SpanElement { TextContent = "hello" } } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { Width = Length.Px(200), Height = Length.Px(100) },
        });

        return (new MikoEngineBuilder().Build(), root, new List<StyleSheet> { sheet });
    }

    [Fact]
    public void HasPendingRenderWork_Should_Settle_False_After_Render()
    {
        var (engine, root, sheets) = CreateEngine("main");
        using var surface = SKSurface.Create(new SKImageInfo(300, 300));

        engine.Initialize(root, sheets, surface.Canvas, 300, 300);
        engine.Render(surface.Canvas);

        engine.HasPendingRenderWork.ShouldBeFalse();
    }

    [Fact]
    public void HasPendingRenderWork_Should_Stay_False_When_Another_Engine_Mutates_Its_Dom()
    {
        // 次级引擎（模拟 DevTools 窗口）：渲染到稳态。
        var (devTools, devToolsRoot, devToolsSheets) = CreateEngine("devtools");
        using var devToolsSurface = SKSurface.Create(new SKImageInfo(300, 300));
        devTools.Initialize(devToolsRoot, devToolsSheets, devToolsSurface.Canvas, 300, 300);
        devTools.Render(devToolsSurface.Canvas);

        devTools.HasPendingRenderWork.ShouldBeFalse();

        // 主引擎（模拟主窗口）在另一棵树上持续变更 DOM。
        var (main, mainRoot, mainSheets) = CreateEngine("main");
        using var mainSurface = SKSurface.Create(new SKImageInfo(300, 300));
        main.Initialize(mainRoot, mainSheets, mainSurface.Canvas, 300, 300);

        long before = main.Mutations.Version;
        for (int i = 0; i < 5; i++)
        {
            mainRoot.AddChild(new SpanElement { TextContent = $"child {i}" });
        }
        // 前置条件：主引擎确实记录到了这些变更（否则本测试什么都没验证）。
        main.Mutations.Version.ShouldBeGreaterThan(before);

        // 关键断言：次级引擎自己的 DOM 一个字节都没变，因此它不该有任何待呈现工作。
        devTools.HasPendingRenderWork.ShouldBeFalse();

        // ISSUE-129：变更版本号按引擎隔离后，含布局时效性检查的判据同样不再被污染。
        // 这条断言在全局静态计数的年代是 true（DevTools 无法空闲的原因），现在必须是 false。
        devTools.HasPendingVisualWork.ShouldBeFalse();
    }

    [Fact]
    public void HasPendingRenderWork_Should_Become_True_When_Own_Element_Is_Invalidated()
    {
        var (engine, root, sheets) = CreateEngine("devtools");
        using var surface = SKSurface.Create(new SKImageInfo(300, 300));
        engine.Initialize(root, sheets, surface.Canvas, 300, 300);
        engine.Render(surface.Canvas);

        engine.HasPendingRenderWork.ShouldBeFalse();

        // 滚动/点击等交互走 InvalidateElement，必须被本判据捕获——否则跳帧会吞掉真实更新。
        engine.InvalidateElement(root);

        engine.HasPendingRenderWork.ShouldBeTrue();
    }

    [Fact]
    public void HasPendingRenderWork_Should_Be_True_Before_First_Render()
    {
        var engine = new MikoEngineBuilder().Build();

        // 首帧尚未渲染：必须出帧（与 HasPendingVisualWork 一致的兜底）。
        engine.HasPendingRenderWork.ShouldBeTrue();
    }
}
