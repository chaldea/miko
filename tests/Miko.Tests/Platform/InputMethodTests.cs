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

    /// <summary>
    /// 用户可以用输入法自带的隐藏键收起软键盘，系统不会回调通知引擎——焦点仍在原输入框上。
    /// 再次点击同一输入框时发布的状态与上一次完全相同，会被去重挡掉，因此"显示键盘"必须
    /// 是一个独立于状态的显式动作，否则键盘再也不会出现。
    /// </summary>
    [Fact]
    public void TappingAlreadyFocusedInput_RequestsKeyboardAgainDespiteUnchangedState()
    {
        var input = new InputElement
        {
            Value = "text",
            Style = new Style { Width = Length.Px(240), Height = Length.Px(32) },
        };
        var (controller, endpoint, surface) = CreateController(input);
        using (surface)
        {
            Focus(controller, 10, 10);
            endpoint.ShowKeyboardCount.ShouldBe(1);
            var stateCallsAfterFirstTap = endpoint.SetStateCount;

            // 第二次点击同一输入框：光标已在末尾，状态一字不变。
            Focus(controller, 10, 10);

            endpoint.SetStateCount.ShouldBe(stateCallsAfterFirstTap, "identical state must stay deduplicated");
            endpoint.ShowKeyboardCount.ShouldBe(2, "tapping an input always means 'show me the keyboard'");
        }
    }

    /// <summary>
    /// 状态每帧发布，但只有真正变化时才允许下发给宿主。Android 宿主会把 SetState 翻译成
    /// 输入法调用，若每帧都下发，正在进行的拼音组合会被反复打断（候选框一闪即逝）。
    /// </summary>
    [Fact]
    public void IdleFrames_DoNotRepublishUnchangedInputMethodState()
    {
        var input = new InputElement
        {
            Value = "text",
            Style = new Style { Width = Length.Px(240), Height = Length.Px(32) },
        };
        var (controller, endpoint, surface) = CreateController(input);
        using (surface)
        {
            Focus(controller, 10, 10);
            var baseline = endpoint.SetStateCount;

            for (var i = 0; i < 5; i++)
                controller.RenderFrame(surface.Canvas, 400, 200, 1f / 60f, _ => { });

            endpoint.SetStateCount.ShouldBe(baseline);
        }
    }

    /// <summary>
    /// number 类型必须走完整的文本编辑路径（可编辑 + 点击聚焦），并向平台请求数字键盘。
    /// 此前 InputMethodType.Number 只映射给 InputType.Range，而 Range 不可编辑，是死分支。
    /// </summary>
    [Fact]
    public void NumberInput_IsEditableAndRequestsNumericKeyboard()
    {
        var input = new InputElement
        {
            Type = InputType.Number,
            Value = "42",
            Style = new Style { Width = Length.Px(240), Height = Length.Px(32) },
        };
        input.IsEditable.ShouldBeTrue();

        var (controller, endpoint, surface) = CreateController(input);
        using (surface)
        {
            controller.OnPointerDown(10, 10, MouseButton.Left);
            controller.OnPointerUp(10, 10, MouseButton.Left);

            input.HasState(ElementState.Focus).ShouldBeTrue();
            var state = endpoint.State.ShouldNotBeNull();
            state.InputType.ShouldBe(InputMethodType.Number);

            controller.OnTextInput("7");
            input.Value.ShouldBe("427");
        }
    }

    /// <summary>
    /// Android 的 InputConnection 依赖发布状态里的文本与光标回读文档内容——输入法据此判断
    /// "光标前有没有字符"，进而决定是否发出退格。文本一旦与编辑器实际内容脱节，退格就会失效。
    /// </summary>
    [Fact]
    public void PublishedState_TracksTextAndCursorForConnectionReadback()
    {
        var input = new InputElement
        {
            Value = "ab",
            Style = new Style { Width = Length.Px(240), Height = Length.Px(32) },
        };
        var (controller, endpoint, surface) = CreateController(input);
        using (surface)
        {
            Focus(controller, 10, 10);
            endpoint.State.ShouldNotBeNull().Text.ShouldBe("ab");
            endpoint.State!.CursorPosition.ShouldBe(2);

            controller.OnTextInput("c");
            endpoint.State.ShouldNotBeNull().Text.ShouldBe("abc");
            endpoint.State!.CursorPosition.ShouldBe(3);

            controller.OnKeyDown(MikoKey.Backspace, MikoKeyModifiers.None);
            controller.RenderFrame(surface.Canvas, 400, 200, 1f / 60f, _ => { });
            endpoint.State.ShouldNotBeNull().Text.ShouldBe("ab");
            endpoint.State!.CursorPosition.ShouldBe(2);
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
        public int SetStateCount { get; private set; }
        public int ShowKeyboardCount { get; private set; }

        public override void SetState(InputMethodState? state)
        {
            State = state;
            SetStateCount++;
        }

        public override void ShowKeyboard() => ShowKeyboardCount++;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
