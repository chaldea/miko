using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Platform;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace Miko.Tests.Platform;

/// <summary>
/// 回归测试：ISSUE-051 —— 在多线程宿主（Android GLThread / iOS CADisplayLink）上，
/// 渲染线程遍历 DOM（LayoutEngine.ComputeStyles 枚举 Element.Children）的同时，
/// UI 线程的输入处理同步修改 DOM（点击触发 StateHasChanged 重写 Children），
/// 二者并发会抛出 "Collection was modified; enumeration operation may not execute"。
/// MikoInteractionController 通过输入/渲染锁将二者串行化来修复该问题。
/// </summary>
public class ConcurrentInputRenderTests
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

    // A root element whose child list is rewritten on demand, mirroring what a Razor
    // component's StateHasChanged does to parent.Children during an input handler.
    private static Element BuildMutableRoot(out Action mutate)
    {
        var root = new DivElement();
        for (int i = 0; i < 8; i++)
            root.AddChild(new DivElement { TextContent = $"child {i}" });

        int counter = 0;
        mutate = () =>
        {
            // Replace the whole child collection, like StateHasChanged rebuilding a subtree.
            root.Children.Clear();
            int n = 4 + (++counter % 8);
            for (int i = 0; i < n; i++)
                root.AddChild(new DivElement { TextContent = $"child {i} v{counter}" });
        };
        return root;
    }

    [Fact]
    public void RenderFrame_ConcurrentWithDomMutatingInput_DoesNotThrow()
    {
        var root = BuildMutableRoot(out var mutate);

        // A global key handler stands in for any input that synchronously mutates the DOM
        // (e.g. a click -> component handler -> StateHasChanged). OnKeyDown runs it under
        // the same lock RenderFrame uses, so it must serialize against layout.
        var options = new MikoAppOptions
        {
            RootComponentFactory = () => root,
            GlobalKeyDownHandlers = { _ => { mutate(); return true; } }
        };

        var engine = new MikoEngine();
        var controller = CreateController(options, engine);

        using var surface = SKSurface.Create(new SKImageInfo(400, 400));
        var canvas = surface.Canvas;
        controller.Initialize(canvas, 400, 400);

        Exception? failure = null;
        using var cts = new CancellationTokenSource();

        // Render thread: continuously lay out + render the tree.
        var renderThread = new Thread(() =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                    controller.RenderFrame(canvas, 400, 400, 0.016f, c => engine.Render(c));
            }
            catch (Exception ex) { failure = ex; cts.Cancel(); }
        });

        // Input thread: hammer the DOM-mutating handler, like fast repeated taps.
        var inputThread = new Thread(() =>
        {
            try
            {
                for (int i = 0; i < 2000 && !cts.IsCancellationRequested; i++)
                    controller.OnKeyDown(MikoKey.Enter, MikoKeyModifiers.None);
            }
            catch (Exception ex) { failure = ex; cts.Cancel(); }
        });

        renderThread.Start();
        inputThread.Start();

        inputThread.Join(TimeSpan.FromSeconds(30)).ShouldBeTrue("input thread should finish");
        cts.Cancel();
        renderThread.Join(TimeSpan.FromSeconds(30)).ShouldBeTrue("render thread should stop");

        failure.ShouldBeNull();
    }
}
