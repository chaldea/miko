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

public class InputMethodTests
{
    [Fact]
    public void FocusedInput_PublishesInsertionCursorInsteadOfControlOrigin()
    {
        var input = new InputElement
        {
            Value = "candidate",
            Style = new Style { Width = Length.Px(240), Height = Length.Px(32) },
        };
        var (controller, endpoint, surface) = CreateController(input);
        using (surface)
        {
            Focus(controller, 10, 10);

            var state = endpoint.State.ShouldNotBeNull();
            state.CursorPosition.ShouldBe(input.Value!.Length);
            state.CursorRect.Left.ShouldBeGreaterThan(20);
            state.CursorRect.Bottom.ShouldBeGreaterThan(state.CursorRect.Top);
        }
    }

    [Fact]
    public void FocusedTextArea_PublishesCursorOnCurrentVisualLine()
    {
        var textArea = new TextAreaElement
        {
            Value = "first\nsecond",
            Style = new Style { Width = Length.Px(240), Height = Length.Px(80) },
        };
        var (controller, endpoint, surface) = CreateController(textArea);
        using (surface)
        {
            Focus(controller, 10, 10);

            var state = endpoint.State.ShouldNotBeNull();
            state.IsMultiline.ShouldBeTrue();
            state.CursorPosition.ShouldBe(textArea.Value!.Length);
            state.CursorRect.Top.ShouldBeGreaterThan(state.CursorRect.Height);
            state.CursorRect.Left.ShouldBeGreaterThan(20);
        }
    }

    private static (MikoInteractionController controller, TestInputMethod endpoint, SKSurface surface)
        CreateController(Element editable)
    {
        var root = new DivElement { Children = { editable } };
        var options = new MikoAppOptions { RootComponentFactory = () => root };
        var engine = new MikoEngineBuilder().Build();
        var endpoint = new TestInputMethod();
        var controller = new MikoInteractionController(
            Options.Create(options),
            new EmptyServiceProvider(),
            engine,
            new EventDispatcher(),
            new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance),
            NullLogger<MikoInteractionController>.Instance,
            endpoint);
        var surface = SKSurface.Create(new SKImageInfo(400, 200));
        controller.Initialize(surface.Canvas, 400, 200);
        engine.Render(surface.Canvas);
        return (controller, endpoint, surface);
    }

    private static void Focus(MikoInteractionController controller, float x, float y)
    {
        controller.OnPointerDown(x, y, MouseButton.Left);
        controller.OnPointerUp(x, y, MouseButton.Left);
    }

    private sealed class TestInputMethod : InputMethodBase
    {
        public InputMethodState? State { get; private set; }
        public override void SetState(InputMethodState? state) => State = state;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
