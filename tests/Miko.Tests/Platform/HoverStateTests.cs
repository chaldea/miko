using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Platform;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Platform;

/// <summary>
/// ISSUE-099 问题5 回归测试：指针移动应维护 <see cref="ElementState.Hover"/>，
/// 使 :hover 伪类选择器生效。
///
/// 修复前 OnPointerMove 只解析光标，从不设置 Hover 状态——:hover 选择器
/// （PseudoClassSelectors 检查 ElementState.Hover）永不命中，.btn:hover 等样式无效。
/// </summary>
public class HoverStateTests
{
    private static MikoInteractionController CreateController(MikoAppOptions options, MikoEngine engine)
    {
        return new MikoInteractionController(
            Options.Create(options),
            new EmptyServiceProvider(),
            engine,
            new EventDispatcher(),
            new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance),
            NullLogger<MikoInteractionController>.Instance);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    /// <summary>与 issues/ISSUE-099.md 问题5 一致的 DOM 与样式（100×30 按钮 + .btn:hover）。</summary>
    private static (MikoAppOptions options, DivElement root, DivElement btn) CreateRepro()
    {
        var btn = new DivElement { Class = "btn", TextContent = "Click" };
        var sibling = new DivElement { Class = "sibling", Style = new Style { Width = Length.Px(50), Height = Length.Px(30) } };
        var root = new DivElement { Class = "root", Children = { btn, sibling } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["*"] = new() { BoxSizing = BoxSizing.BorderBox },
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".btn"] = new()
            {
                Width = Length.Px(100),
                Height = Length.Px(30),
                BackgroundColor = Color.Blue,
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Color = Color.White,
                Cursor = Cursor.Pointer,
            },
            [".btn:hover"] = new() { BackgroundColor = Color.Red },
        });

        var options = new MikoAppOptions
        {
            RootComponentFactory = () => root,
            StyleSheets = { sheet },
        };
        return (options, root, btn);
    }

    [Fact]
    public void PointerMove_OverElement_SetsHoverOnElementAndAncestors()
    {
        var (options, root, btn) = CreateRepro();
        var engine = new MikoEngine();
        var controller = CreateController(options, engine);
        using var surface = SKSurface.Create(new SKImageInfo(500, 500));
        controller.Initialize(surface.Canvas, 500, 500);

        btn.HasState(ElementState.Hover).ShouldBeFalse();

        // 移入按钮区域（btn 占 0..100 × 0..30）。
        controller.OnPointerMove(50, 15);

        btn.HasState(ElementState.Hover).ShouldBeTrue();
        root.HasState(ElementState.Hover).ShouldBeTrue();   // CSS：祖先链同样匹配 :hover
        root.Children[1].HasState(ElementState.Hover).ShouldBeFalse(); // 兄弟不悬停

        // 移出按钮（仍在 root 内）。
        controller.OnPointerMove(300, 300);

        btn.HasState(ElementState.Hover).ShouldBeFalse();
        root.HasState(ElementState.Hover).ShouldBeTrue();
    }

    [Fact]
    public void PointerMove_HoverReflectedInComputedStyle_AfterNextFrame()
    {
        var (options, root, btn) = CreateRepro();
        var engine = new MikoEngine();
        var controller = CreateController(options, engine);
        using var surface = SKSurface.Create(new SKImageInfo(500, 500));
        controller.Initialize(surface.Canvas, 500, 500);

        btn.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.Blue);

        // 悬停后：状态标脏使布局失效，下一帧渲染即按 :hover 重新级联。
        controller.OnPointerMove(50, 15);
        engine.HasPendingVisualWork.ShouldBeTrue();
        engine.Render(surface.Canvas);

        btn.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.Red);

        // 移开后恢复。
        controller.OnPointerMove(300, 300);
        engine.Render(surface.Canvas);

        btn.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.Blue);
    }

    [Fact]
    public void PointerMove_BetweenSiblings_MovesHoverChain()
    {
        var (options, root, btn) = CreateRepro();
        var engine = new MikoEngine();
        var controller = CreateController(options, engine);
        using var surface = SKSurface.Create(new SKImageInfo(500, 500));
        controller.Initialize(surface.Canvas, 500, 500);

        // btn 占 0..100 × 0..30；sibling 为块布局，占 0..50 × 30..60。
        var sibling = (DivElement)root.Children[1];

        controller.OnPointerMove(50, 15);   // 命中 btn
        btn.HasState(ElementState.Hover).ShouldBeTrue();

        controller.OnPointerMove(25, 45);   // 命中 sibling
        sibling.HasState(ElementState.Hover).ShouldBeTrue();
        btn.HasState(ElementState.Hover).ShouldBeFalse();
        root.HasState(ElementState.Hover).ShouldBeTrue();
    }
}
