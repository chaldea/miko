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
/// The input a remote desktop (or a game, a canvas editor) needs beyond text editing: the middle and right
/// buttons, the wheel as an event, key releases and physical key codes, and focus kept by a prevented press.
/// </summary>
public class RawInputEventTests
{
    [Fact]
    public void Right_And_Middle_Buttons_Reach_The_Element_Without_A_Click_Or_A_Focus_Change()
    {
        var input = new InputElement { Type = InputType.Text, Style = new Style { Width = Length.Px(100), Height = Length.Px(20) } };
        var target = new DivElement { Style = new Style { Width = Length.Px(200), Height = Length.Px(100) } };
        var events = new List<string>();
        target.OnPointerDown = e => events.Add($"pointerdown {e.Button}");
        target.OnMouseDown = e => events.Add($"mousedown {e.Button}");
        target.OnPointerUp = e => events.Add($"pointerup {e.Button}");
        target.OnMouseUp = e => events.Add($"mouseup {e.Button}");
        target.OnClick = e => events.Add("click");
        var (controller, _, surface) = Create(new DivElement { Children = { input, target } }, 200, 200);
        using (surface)
        {
            controller.OnPointerDown(10, 10, MouseButton.Left);
            controller.OnPointerUp(10, 10, MouseButton.Left);
            input.HasState(ElementState.Focus).ShouldBeTrue();

            controller.OnPointerDown(50, 60, MouseButton.Right);
            controller.OnPointerUp(50, 60, MouseButton.Right);
            controller.OnPointerDown(50, 60, MouseButton.Middle);
            controller.OnPointerUp(150, 90, MouseButton.Middle);   // released elsewhere: the press target still gets it
        }

        events.ShouldBe([
            "pointerdown Right", "mousedown Right", "pointerup Right", "mouseup Right",
            "pointerdown Middle", "mousedown Middle", "pointerup Middle", "mouseup Middle",
        ]);
        input.HasState(ElementState.Focus).ShouldBeTrue("a secondary button does not move focus");
    }

    [Fact]
    public void Wheel_Goes_To_The_Element_Under_The_Pointer_And_Prevented_It_Scrolls_Nothing()
    {
        var scroller = new DivElement { Style = new Style { Width = Length.Px(200), Height = Length.Px(100), OverflowY = Overflow.Auto } };
        var content = new DivElement { Style = new Style { Width = Length.Px(200), Height = Length.Px(400) } };
        scroller.AddChild(content);
        WheelEventArgs? seen = null;
        bool prevent = false;
        content.OnWheel = e => { seen = e; if (prevent) e.PreventDefault(); };
        var (controller, _, surface) = Create(scroller, 200, 100);
        using (surface)
        {
            controller.OnScroll(50, 50, 0, 40);
            seen.ShouldNotBeNull();
            seen!.DeltaY.ShouldBe(40);
            seen.OffsetX.ShouldBe(50);
            scroller.LayoutBox!.ScrollTop.ShouldBe(40, "not prevented: the container scrolls as before");

            prevent = true;
            controller.OnScroll(50, 50, 0, 40);
            scroller.LayoutBox!.ScrollTop.ShouldBe(40, "prevented: nothing scrolls");
        }
    }

    [Fact]
    public void Keys_Reach_The_Focused_Editor_With_Code_Modifiers_And_Release()
    {
        var input = new InputElement { Type = InputType.Text, Style = new Style { Width = Length.Px(100), Height = Length.Px(20) } };
        var events = new List<KeyboardEventArgs>();
        input.OnKeyDown = e => events.Add(e);
        input.OnKeyUp = e => events.Add(e);
        var (controller, _, surface) = Create(new DivElement { Children = { input } }, 200, 100);
        using (surface)
        {
            controller.OnPointerDown(10, 10, MouseButton.Left);
            controller.OnPointerUp(10, 10, MouseButton.Left);
            controller.OnKeyDown(MikoKey.C, MikoKeyModifiers.Control | MikoKeyModifiers.Meta);
            controller.OnKeyUp(MikoKey.C, MikoKeyModifiers.None);
            controller.OnKeyDown(MikoKey.Up, MikoKeyModifiers.None);
            controller.RepeatKey(MikoKey.Up);
        }

        events.Count.ShouldBe(4);
        (events[0].Code, events[0].Key, events[0].CtrlKey, events[0].MetaKey, events[0].Repeat).ShouldBe((MikoKey.C, "C", true, true, false));
        (events[1].Code, events[1].CtrlKey).ShouldBe((MikoKey.C, false));
        events[2].Code.ShouldBe(MikoKey.Up);
        events[3].Repeat.ShouldBeTrue("holding a key repeats its keydown");
        MikoInteractionController.IsRepeatableKey(MikoKey.Down).ShouldBeTrue();
    }

    [Fact]
    public void A_Repeat_That_Is_Prevented_Does_Not_Edit()
    {
        var input = new InputElement { Type = InputType.Text, Value = "abc", Style = new Style { Width = Length.Px(100), Height = Length.Px(20) } };
        input.OnKeyDown = e => { if (e.Repeat) e.PreventDefault(); };
        var (controller, _, surface) = Create(new DivElement { Children = { input } }, 200, 100);
        using (surface)
        {
            controller.OnPointerDown(10, 10, MouseButton.Left);
            controller.OnPointerUp(10, 10, MouseButton.Left);
            controller.OnKeyDown(MikoKey.Backspace, MikoKeyModifiers.None);
            input.Value.ShouldBe("ab");
            controller.RepeatKey(MikoKey.Backspace);
            input.Value.ShouldBe("ab");
        }
    }

    [Fact]
    public void A_Press_With_Prevented_Default_Keeps_Focus_Where_It_Is()
    {
        var input = new InputElement { Type = InputType.Text, Style = new Style { Width = Length.Px(100), Height = Length.Px(20) } };
        var keeper = new DivElement { Style = new Style { Width = Length.Px(200), Height = Length.Px(40) } };
        keeper.OnPointerDown = e => e.PreventDefault();
        var plain = new DivElement { Style = new Style { Width = Length.Px(200), Height = Length.Px(40) } };
        var (controller, _, surface) = Create(new DivElement { Children = { input, keeper, plain } }, 200, 200);
        using (surface)
        {
            controller.OnPointerDown(10, 10, MouseButton.Left);
            controller.OnPointerUp(10, 10, MouseButton.Left);
            input.HasState(ElementState.Focus).ShouldBeTrue();

            controller.OnPointerDown(50, 40, MouseButton.Left);    // keeper (y 20–60)
            controller.OnPointerUp(50, 40, MouseButton.Left);
            input.HasState(ElementState.Focus).ShouldBeTrue("as in the browser: mousedown.preventDefault() keeps focus");

            controller.OnPointerDown(50, 80, MouseButton.Left);    // plain (y 60–100)
            controller.OnPointerUp(50, 80, MouseButton.Left);
            input.HasState(ElementState.Focus).ShouldBeFalse("a plain press elsewhere still blurs");
        }
    }

    [Fact]
    public void Cursor_None_Is_Reported_To_The_Host()
    {
        var element = new DivElement { Style = new Style { Width = Length.Px(200), Height = Length.Px(100), Cursor = Cursor.None } };
        var (controller, _, surface) = Create(element, 200, 100);
        Cursor? reported = null;
        controller.CursorChanged += c => reported = c;
        using (surface) controller.OnPointerMove(20, 20);
        reported.ShouldBe(Cursor.None);
    }

    private static (MikoInteractionController, MikoEngine, SKSurface) Create(Element root, int width, int height)
    {
        var options = new MikoAppOptions { RootComponentFactory = () => root };
        var engine = new MikoEngineBuilder().Build();
        var controller = new MikoInteractionController(
            Options.Create(options), new EmptyServiceProvider(), engine, new EventDispatcher(), new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance), NullLogger<MikoInteractionController>.Instance);
        var surface = SKSurface.Create(new SKImageInfo(width, height));
        controller.Initialize(surface.Canvas, width, height);
        engine.Render(surface.Canvas);
        return (controller, engine, surface);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
