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

public class PointerEventDispatchTests
{
    [Fact]
    public void PointerEvents_ExposeTargetCoordinates_AndCaptureMovesOutsideTarget()
    {
        var element = new DivElement
        {
            Style = new Style { Width = Length.Px(200), Height = Length.Px(20) },
        };
        var events = new List<(string type, MouseEventArgs args)>();
        element.OnMouseDown = args => events.Add((EventTypes.MouseDown, args));
        element.OnMouseMove = args => events.Add((EventTypes.MouseMove, args));
        element.OnMouseUp = args => events.Add((EventTypes.MouseUp, args));

        var options = new MikoAppOptions { RootComponentFactory = () => element };
        var engine = new MikoEngineBuilder().Build();
        var controller = new MikoInteractionController(
            Options.Create(options),
            new EmptyServiceProvider(),
            engine,
            new EventDispatcher(),
            new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance),
            NullLogger<MikoInteractionController>.Instance);

        using var surface = SKSurface.Create(new SKImageInfo(300, 100));
        controller.Initialize(surface.Canvas, 300, 100);
        engine.Render(surface.Canvas);

        controller.OnPointerDown(50, 10, MouseButton.Left);
        controller.OnPointerMove(250, 10);
        controller.OnPointerUp(250, 10, MouseButton.Left);

        events.Select(e => e.type).ShouldBe(new[]
        {
            EventTypes.MouseDown,
            EventTypes.MouseMove,
            EventTypes.MouseUp,
        });
        events[0].args.OffsetX.ShouldBe(50f);
        events[0].args.TargetWidth.ShouldBe(200f);
        events[0].args.IsButtonPressed.ShouldBeTrue();
        events[1].args.OffsetX.ShouldBe(250f);
        events[1].args.IsButtonPressed.ShouldBeTrue();
        events[2].args.IsButtonPressed.ShouldBeFalse();

        controller.OnPointerMove(50, 10);
        events.Count.ShouldBe(4);
        events[^1].args.IsButtonPressed.ShouldBeFalse();
    }

    [Fact]
    public void PointerEvents_ExposeTouchSource_AndKeepLegacyMouseHandlersWorking()
    {
        var element = new DivElement
        {
            Style = new Style { Width = Length.Px(200), Height = Length.Px(100) },
        };
        PointerEventArgs? pointerArgs = null;
        var legacyDown = 0;
        element.OnPointerDown = args => pointerArgs = args;
        element.OnMouseDown = _ => legacyDown++;

        var (controller, _, surface, _) = CreateController(element, 200, 100);
        using (surface)
        {
            controller.OnPointerDown(25, 30, MouseButton.Left, PointerType.Touch, pointerId: 7);
            controller.OnPointerUp(25, 30, MouseButton.Left, PointerType.Touch, pointerId: 7);
        }

        pointerArgs.ShouldNotBeNull();
        pointerArgs!.PointerType.ShouldBe(PointerType.Touch);
        pointerArgs.PointerId.ShouldBe(7);
        legacyDown.ShouldBe(1);
    }

    [Fact]
    public void TouchDrag_ScrollsContainer_AndSuppressesClick()
    {
        var scroller = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(200),
                Height = Length.Px(100),
                OverflowY = Overflow.Auto,
            },
        };
        scroller.AddChild(new DivElement
        {
            Style = new Style { Width = Length.Px(200), Height = Length.Px(400) },
        });
        var clicks = 0;
        scroller.OnClick = _ => clicks++;

        var (controller, _, surface, _) = CreateController(scroller, 200, 100);
        using (surface)
        {
            controller.OnPointerDown(50, 80, MouseButton.Left, PointerType.Touch);
            controller.OnPointerMove(50, 20);
            controller.OnPointerUp(50, 20, MouseButton.Left, PointerType.Touch);
        }

        scroller.LayoutBox!.ScrollTop.ShouldBe(60f);
        clicks.ShouldBe(0);
    }

    [Fact]
    public void TouchDrag_DoesNotScrollWhenPointerMovePreventsDefault()
    {
        var scroller = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(200),
                Height = Length.Px(100),
                OverflowY = Overflow.Auto,
            },
        };
        scroller.AddChild(new DivElement
        {
            Style = new Style { Width = Length.Px(200), Height = Length.Px(400) },
        });
        scroller.OnPointerMove = args => args.PreventDefault();

        var (controller, _, surface, _) = CreateController(scroller, 200, 100);
        using (surface)
        {
            controller.OnPointerDown(50, 80, MouseButton.Left, PointerType.Touch);
            controller.OnPointerMove(50, 20);
            controller.OnPointerUp(50, 20, MouseButton.Left, PointerType.Touch);
        }

        scroller.LayoutBox!.ScrollTop.ShouldBe(0f);
    }

    [Fact]
    public void PointerCancel_DispatchesCancel_AndSuppressesClick()
    {
        var element = new DivElement
        {
            Style = new Style { Width = Length.Px(200), Height = Length.Px(100) },
        };
        var cancels = 0;
        var clicks = 0;
        element.OnPointerCancel = _ => cancels++;
        element.OnClick = _ => clicks++;

        var (controller, _, surface, _) = CreateController(element, 200, 100);
        using (surface)
        {
            controller.OnPointerDown(20, 20, MouseButton.Left, PointerType.Touch);
            controller.OnPointerCancel(20, 20);
            controller.OnPointerUp(20, 20, MouseButton.Left, PointerType.Touch);
        }

        cancels.ShouldBe(1);
        clicks.ShouldBe(0);
    }

    [Fact]
    public async Task LongPress_FiresAfterHold_AndSuppressesClick()
    {
        var element = new DivElement
        {
            Style = new Style { Width = Length.Px(200), Height = Length.Px(100) },
        };
        var presses = 0;
        var clicks = 0;
        element.OnLongPress = _ => presses++;
        element.OnClick = _ => clicks++;

        var (controller, _, surface, dispatcher) = CreateController(element, 200, 100);
        using (surface)
        {
            controller.OnPointerDown(20, 20, MouseButton.Left, PointerType.Touch);
            await Task.Delay(650);
            controller.HasPendingWork.ShouldBeTrue();
            dispatcher.Drain();
            controller.OnPointerUp(20, 20, MouseButton.Left, PointerType.Touch);
        }

        presses.ShouldBe(1);
        clicks.ShouldBe(0);
    }

    private static (MikoInteractionController controller, MikoEngine engine, SKSurface surface,
        MikoDispatcher dispatcher) CreateController(Element root, int width, int height)
    {
        var options = new MikoAppOptions { RootComponentFactory = () => root };
        var engine = new MikoEngineBuilder().Build();
        var dispatcher = new MikoDispatcher();
        var controller = new MikoInteractionController(
            Options.Create(options),
            new EmptyServiceProvider(),
            engine,
            new EventDispatcher(),
            dispatcher,
            new HotReloadService(NullLogger<HotReloadService>.Instance),
            NullLogger<MikoInteractionController>.Instance);

        var surface = SKSurface.Create(new SKImageInfo(width, height));
        controller.Initialize(surface.Canvas, width, height);
        engine.Render(surface.Canvas);
        return (controller, engine, surface, dispatcher);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
