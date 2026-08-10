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
/// ISSUE-123：长按退格连续删除。Silk 的 KeyDown 只在物理按下时触发一次（不带 OS 自动重复），
/// 所以宿主必须自行计时并调用 <see cref="MikoInteractionController.RepeatKey"/>。
/// Miko.Windowing 一直这么做，Miko.Simulator 此前漏了这条链路（只订阅 KeyDown/KeyChar，
/// 既无 KeyUp 也无重复节拍），表现为长按退格只删一个字符。
///
/// 宿主层是 Silk 窗口代码，无法单测；这里锁定它所依赖的控制器契约：
/// 哪些键可重复、重复是否真的执行编辑动作、以及每次重复必须调度重绘
/// （否则 ISSUE-096 的稳态空闲跳过会让画面停在第一次删除的结果上）。
/// </summary>
public class KeyRepeatTests
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

    // 建好一个已聚焦、已输入文本、且已渲染到稳态（无待办工作）的文本框。
    private static (MikoInteractionController controller, MikoEngine engine, InputElement input, SKSurface surface)
        CreateFocusedInput(string text)
    {
        var input = new InputElement
        {
            Type = InputType.Text,
            Style = new Style { Width = Length.Px(200), Height = Length.Px(20) },
        };
        var root = new DivElement { Children = { input } };
        var options = new MikoAppOptions { RootComponentFactory = () => root };

        var engine = new MikoEngine();
        var controller = CreateController(options, engine);

        var surface = SKSurface.Create(new SKImageInfo(500, 500));
        controller.Initialize(surface.Canvas, 500, 500);
        engine.Render(surface.Canvas);

        controller.OnPointerDown(10, 10, MouseButton.Left);
        controller.OnPointerUp(10, 10, MouseButton.Left);
        controller.OnTextInput(text);
        engine.Render(surface.Canvas);
        engine.HasPendingVisualWork.ShouldBeFalse();   // 稳态

        return (controller, engine, input, surface);
    }

    [Theory]
    [InlineData(MikoKey.Backspace)]
    [InlineData(MikoKey.Delete)]
    [InlineData(MikoKey.Left)]
    [InlineData(MikoKey.Right)]
    [InlineData(MikoKey.Home)]
    [InlineData(MikoKey.End)]
    public void EditingKeys_AreRepeatable(MikoKey key)
    {
        MikoInteractionController.IsRepeatableKey(key).ShouldBeTrue();
    }

    [Theory]
    [InlineData(MikoKey.Enter)]
    [InlineData(MikoKey.Tab)]
    [InlineData(MikoKey.Escape)]
    [InlineData(MikoKey.F5)]
    public void NonEditingKeys_AreNotRepeatable(MikoKey key)
    {
        MikoInteractionController.IsRepeatableKey(key).ShouldBeFalse();
    }

    /// <summary>
    /// ISSUE-123 主场景：一次 KeyDown 之后，宿主按节拍泵出的每次 RepeatKey 都要再删一个字符。
    /// 修复前模拟器从不调用 RepeatKey，文本停在 "abcd"。
    /// </summary>
    [Fact]
    public void RepeatBackspace_DeletesOneCharacterPerRepeat()
    {
        var (controller, engine, input, surface) = CreateFocusedInput("abcde");
        using var _ = surface;

        controller.OnKeyDown(MikoKey.Backspace, MikoKeyModifiers.None);
        input.Value.ShouldBe("abcd");

        controller.RepeatKey(MikoKey.Backspace);
        input.Value.ShouldBe("abc");

        controller.RepeatKey(MikoKey.Backspace);
        input.Value.ShouldBe("ab");

        // 删空后继续重复不应抛异常，也不应把值变成 null。
        controller.RepeatKey(MikoKey.Backspace);
        controller.RepeatKey(MikoKey.Backspace);
        controller.RepeatKey(MikoKey.Backspace);
        input.Value.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 每次重复都必须调度重绘：否则宿主的稳态空闲检测（ISSUE-096）会跳过帧生产，
    /// 文本已被删除但画面停在旧内容上——用户看到的仍是"长按无效"。
    /// </summary>
    [Fact]
    public void RepeatBackspace_SchedulesRepaintEachTime()
    {
        var (controller, engine, input, surface) = CreateFocusedInput("abcde");
        using var _ = surface;

        for (int i = 0; i < 3; i++)
        {
            engine.Render(surface.Canvas);
            engine.HasPendingVisualWork.ShouldBeFalse();

            controller.RepeatKey(MikoKey.Backspace);
            engine.HasPendingVisualWork.ShouldBeTrue($"repeat #{i} did not schedule a repaint");
        }

        input.Value.ShouldBe("ab");
    }

    /// <summary>光标移动类按键的重复同样要生效（长按左右方向键连续移动光标）。</summary>
    [Fact]
    public void RepeatArrowKey_MovesCursorPerRepeat()
    {
        var (controller, engine, input, surface) = CreateFocusedInput("abcde");
        using var _ = surface;

        input.CursorPosition.ShouldBe(5);

        controller.OnKeyDown(MikoKey.Left, MikoKeyModifiers.None);
        input.CursorPosition.ShouldBe(4);

        controller.RepeatKey(MikoKey.Left);
        controller.RepeatKey(MikoKey.Left);
        input.CursorPosition.ShouldBe(2);
    }

    /// <summary>
    /// 无焦点元素时重复是 no-op：宿主对被全局处理器消费的按键也会启动重复计时
    /// （KeyDown 已异步入队，宿主无法得知是否被消费），控制器必须容忍。
    /// </summary>
    [Fact]
    public void RepeatKey_WithoutFocusedEditable_IsNoOp()
    {
        var (controller, engine, input, surface) = CreateFocusedInput("abc");
        using var _ = surface;

        controller.OnPointerDown(400, 400, MouseButton.Left);   // 点到空白处：失焦
        controller.OnPointerUp(400, 400, MouseButton.Left);
        engine.Render(surface.Canvas);

        Should.NotThrow(() => controller.RepeatKey(MikoKey.Backspace));
        input.Value.ShouldBe("abc");
    }
}
