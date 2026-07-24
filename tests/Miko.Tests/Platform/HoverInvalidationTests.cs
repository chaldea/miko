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
/// ISSUE-104 问题1 回归测试：鼠标在与任何 :hover 规则无关的元素间来回移动时，
/// 悬停状态只作标志位静默跟踪——不得递增 MutationVersion、不得产生待呈现工作，
/// 否则每帧全量重排会造成 dotMemory 中可见的内存锯齿。
/// 与 :hover 规则相关的元素仍须正常置状态并触发样式重算（ISSUE-099 行为保持）。
/// </summary>
public class HoverInvalidationTests
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

    /// <summary>
    /// 与 issues/ISSUE-104.md 问题1 一致的场景：h1 + p 页面，样式表无任何 :hover 规则。
    /// </summary>
    private static (MikoAppOptions options, H1Element h1, ParagraphElement p) CreateIssueRepro()
    {
        var h1 = new H1Element { TextContent = "Miko UI Components" };
        var p = new ParagraphElement { TextContent = "Ionic apps are made of high-level building blocks..." };
        var root = new DivElement { Children = { h1, p } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
        });

        var options = new MikoAppOptions
        {
            RootComponentFactory = () => root,
            StyleSheets = { sheet },
        };
        return (options, h1, p);
    }

    [Fact]
    public void PointerMove_WithoutHoverRules_DoesNotInvalidateLayout()
    {
        var (options, h1, p) = CreateIssueRepro();
        var engine = new MikoEngine();
        var controller = CreateController(options, engine);
        using var surface = SKSurface.Create(new SKImageInfo(500, 500));
        controller.Initialize(surface.Canvas, 500, 500);
        engine.Render(surface.Canvas);
        engine.HasPendingVisualWork.ShouldBeFalse();   // 稳态

        long version = Element.MutationVersion;

        // 鼠标在 p 上进/出循环（坐标取自实际布局盒，避免硬编码）。
        var h1Rect = h1.LayoutBox!.BoxModel.BorderBox;
        var pRect = p.LayoutBox!.BoxModel.BorderBox;
        float h1X = h1Rect.Left + 1, h1Y = h1Rect.Top + 1;
        float pX = pRect.Left + 1, pY = pRect.Top + 1;

        for (int i = 0; i < 10; i++)
        {
            controller.OnPointerMove(pX, pY);
            controller.OnPointerMove(h1X, h1Y);
        }

        // 悬停状态仍按 CSS 语义跟踪（祖先链同样持有状态）。
        h1.HasState(ElementState.Hover).ShouldBeTrue();

        // 但没有 :hover 规则 → 悬停不可能影响样式 → 全程零失效、零待呈现工作。
        Element.MutationVersion.ShouldBe(version);
        engine.HasPendingVisualWork.ShouldBeFalse();
    }

    [Fact]
    public void PointerMove_WithHoverRules_OnlyRelevantElementsInvalidate()
    {
        var btn = new DivElement { Class = "btn", TextContent = "Click" };
        var text = new ParagraphElement { TextContent = "plain" };
        var root = new DivElement { Children = { btn, text } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".btn"] = new() { Width = Length.Px(100), Height = Length.Px(30), BackgroundColor = Color.Blue },
            [".btn:hover"] = new() { BackgroundColor = Color.Red },
        });

        var options = new MikoAppOptions
        {
            RootComponentFactory = () => root,
            StyleSheets = { sheet },
        };

        var engine = new MikoEngine();
        var controller = CreateController(options, engine);
        using var surface = SKSurface.Create(new SKImageInfo(500, 500));
        controller.Initialize(surface.Canvas, 500, 500);
        engine.Render(surface.Canvas);

        // 在无关元素 p 上移动：状态照置，但不失效。
        var pRect = text.LayoutBox!.BoxModel.BorderBox;
        float pX = pRect.Left + 1, pY = pRect.Top + 1;
        var btnRect = btn.LayoutBox!.BoxModel.BorderBox;

        long version = Element.MutationVersion;
        controller.OnPointerMove(pX, pY);
        text.HasState(ElementState.Hover).ShouldBeTrue();
        Element.MutationVersion.ShouldBe(version);
        engine.HasPendingVisualWork.ShouldBeFalse();

        // 移到与 .btn:hover 相关的元素上：必须失效并在下一帧应用悬停样式。
        controller.OnPointerMove(btnRect.Left + 1, btnRect.Top + 1);
        btn.HasState(ElementState.Hover).ShouldBeTrue();
        Element.MutationVersion.ShouldBeGreaterThan(version);
        engine.HasPendingVisualWork.ShouldBeTrue();

        engine.Render(surface.Canvas);
        btn.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.Red);

        // 移回无关元素：.btn 失悬需失效（样式要恢复），p 进悬不失效。
        controller.OnPointerMove(pX, pY);
        engine.Render(surface.Canvas);
        btn.LayoutBox!.ComputedStyle.BackgroundColor.ShouldBe(Color.Blue);
    }

    [Fact]
    public void PointerMove_AncestorHoverCombinator_AppliesStyleOnDescendantRule()
    {
        // .card:hover .title：悬停 .card（含其后代）时 .title 变式。
        // 悬停状态必须设在 .card 上（即使指针命中的是 .title 文本）。
        var title = new DivElement { Class = "title", TextContent = "T" };
        var card = new DivElement { Class = "card", Children = { title } };
        var root = new DivElement { Children = { card } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".card"] = new() { Width = Length.Px(200), Height = Length.Px(100) },
            [".title"] = new() { Color = Color.Black },
            [".card:hover .title"] = new() { Color = Color.Green },
        });

        var options = new MikoAppOptions
        {
            RootComponentFactory = () => root,
            StyleSheets = { sheet },
        };

        var engine = new MikoEngine();
        var controller = CreateController(options, engine);
        using var surface = SKSurface.Create(new SKImageInfo(500, 500));
        controller.Initialize(surface.Canvas, 500, 500);
        engine.Render(surface.Canvas);
        title.LayoutBox!.ComputedStyle.Color.ShouldBe(Color.Black);

        controller.OnPointerMove(10, 10);   // 命中 .title（.card 后代）
        card.HasState(ElementState.Hover).ShouldBeTrue();
        engine.HasPendingVisualWork.ShouldBeTrue();

        engine.Render(surface.Canvas);
        title.LayoutBox!.ComputedStyle.Color.ShouldBe(Color.Green);
    }
}
