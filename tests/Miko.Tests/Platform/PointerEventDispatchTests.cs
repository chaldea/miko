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
        var engine = new MikoEngine();
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

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
