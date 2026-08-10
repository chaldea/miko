using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Components;
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
/// ISSUE-121 回归测试：事件处理器触发的组件重渲染不得丢掉焦点与文本光标。
///
/// <para>重渲染产出的是<b>全新</b>的元素实例替换整棵子树，而焦点状态与光标位置活在实例上
/// （不像 <c>Value</c> 那样每次由参数重新写入），控制器也按引用缓存焦点目标。修复前：</para>
/// <list type="bullet">
/// <item>label 上挂 <c>@onclick</c> → 点击输入框时该处理器重建子树，焦点落在已脱离树的旧
/// <c>&lt;input&gt;</c> 上，渲染看不到 Focus 状态，光标不画。</item>
/// <item>input 上挂 <c>@oninput</c> → 每次输入都重建子树，光标位置回到 0，不跟随文本。</item>
/// </list>
/// <para>结构刻意照搬 <c>IonInput</c> 的真实嵌套（label.input-wrapper &gt; .native-wrapper &gt;
/// input.native-input，label 与 input 各自带处理器），否则复现不出该问题。</para>
/// </summary>
public class FocusAcrossRerenderTests
{
    /// <summary>IonInput 的可复现最小骨架：label 带 @onclick，input 带 @oninput。</summary>
    private sealed class InputHostComponent : ComponentBase
    {
        public string? Value { get; set; }
        public int LabelClicks { get; private set; }
        public int InputEvents { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "ion-input");

            builder.OpenElement(2, "label");
            builder.AddAttribute(3, "class", "input-wrapper");
            builder.AddAttribute(4, "onclick",
                EventCallback.Factory.Create<MouseEventArgs>(this, HandleLabelClick));

            builder.OpenElement(5, "div");
            builder.AddAttribute(6, "class", "native-wrapper");

            builder.OpenElement(7, "input");
            builder.AddAttribute(8, "class", "native-input");
            builder.AddAttribute(9, "value", Value);
            builder.AddAttribute(10, "oninput",
                EventCallback.Factory.Create<InputEventArgs>(this, HandleInput));
            builder.CloseElement();

            builder.CloseElement();
            builder.CloseElement();
            builder.CloseElement();
        }

        /// <summary>触发一次与输入无关的重渲染（模拟其他状态变化引起的重渲染）。</summary>
        public void ForceRender() => StateHasChanged();

        // Ionic 在 label 自身被点中时阻止冒泡；这里保留处理器的存在本身即可复现。
        private void HandleLabelClick(MouseEventArgs args) => LabelClicks++;

        private void HandleInput(InputEventArgs args)
        {
            InputEvents++;
            Value = args.Data;
        }
    }

    private static MikoInteractionController CreateController(MikoAppOptions options, MikoEngine engine)
        => new(
            Options.Create(options),
            new EmptyServiceProvider(),
            engine,
            new EventDispatcher(),
            new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance),
            NullLogger<MikoInteractionController>.Instance);

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    /// <summary>
    /// 布好 200×40 的输入框（内容盒充满宿主 div），初始化并渲染到稳态。
    /// </summary>
    private static (MikoInteractionController controller, MikoEngine engine, InputHostComponent component, SKSurface surface)
        CreateHost()
    {
        var component = new InputHostComponent();

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["input"] = new() { Width = Length.Px(200), Height = Length.Px(40) },
        });

        var options = new MikoAppOptions
        {
            RootComponentFactory = component.Build,
            StyleSheets = { sheet },
        };

        var engine = new MikoEngine();
        var controller = CreateController(options, engine);
        var surface = SKSurface.Create(new SKImageInfo(300, 200));
        controller.Initialize(surface.Canvas, 300, 200);
        engine.Render(surface.Canvas);

        return (controller, engine, component, surface);
    }

    /// <summary>当前在树中的原生 input（每次重渲染都是新实例，必须重新查找）。</summary>
    private static InputElement CurrentInput(MikoEngine engine)
        => engine.GetRoot()!.FindByClass("native-input").OfType<InputElement>().Single();

    [Fact]
    public void Click_WithLabelClickHandler_LeavesLiveInputFocused()
    {
        var (controller, engine, component, surface) = CreateHost();
        using (surface)
        {
            var before = CurrentInput(engine);

            controller.OnPointerDown(10, 10, MouseButton.Left);
            controller.OnPointerUp(10, 10, MouseButton.Left);

            // label 的处理器确实跑了，并因此重建了子树（新的 input 实例）。
            component.LabelClicks.ShouldBe(1);
            var live = CurrentInput(engine);
            live.ShouldNotBeSameAs(before);

            // 关键断言：在场的那个 input 持有焦点。修复前焦点留在 before 上，
            // 渲染遍历的是 live，于是光标不画（ISSUE-121）。
            live.HasState(ElementState.Focus).ShouldBeTrue();
        }
    }

    [Fact]
    public void Typing_WithInputHandler_CaretFollowsTextOnLiveInput()
    {
        var (controller, engine, component, surface) = CreateHost();
        using (surface)
        {
            controller.OnPointerDown(10, 10, MouseButton.Left);
            controller.OnPointerUp(10, 10, MouseButton.Left);
            engine.Render(surface.Canvas);

            controller.OnTextInput("ab");

            component.InputEvents.ShouldBe(1);

            // 每次输入都会重建子树；值与光标都必须落在在场实例上。
            var live = CurrentInput(engine);
            live.Value.ShouldBe("ab");
            live.CursorPosition.ShouldBe(2);   // 修复前为 0：光标不跟随输入内容
            live.HasState(ElementState.Focus).ShouldBeTrue();

            // 继续输入仍作用于同一个逻辑控件（控制器的焦点引用已转发到新实例）。
            engine.Render(surface.Canvas);
            controller.OnTextInput("c");

            var after = CurrentInput(engine);
            after.Value.ShouldBe("abc");
            after.CursorPosition.ShouldBe(3);
        }
    }

    [Fact]
    public void Backspace_AfterRerender_EditsLiveInput()
    {
        var (controller, engine, _, surface) = CreateHost();
        using (surface)
        {
            controller.OnPointerDown(10, 10, MouseButton.Left);
            controller.OnPointerUp(10, 10, MouseButton.Left);
            controller.OnTextInput("ab");
            engine.Render(surface.Canvas);

            controller.OnKeyDown(MikoKey.Backspace, MikoKeyModifiers.None);

            var live = CurrentInput(engine);
            live.Value.ShouldBe("a");
            live.CursorPosition.ShouldBe(1);
            // 编辑必须调度重绘，否则稳态空闲会跳过帧生产（ISSUE-104）。
            engine.HasPendingVisualWork.ShouldBeTrue();
        }
    }

    [Fact]
    public void Typing_AfterRerender_SchedulesRepaint()
    {
        // 光标要真的出现在画面上，重绘还必须被调度（ISSUE-104 的不变量在重渲染路径下依然成立）：
        // 焦点被搬到新实例后，标脏的对象也必须是那个在场实例。
        var (controller, engine, _, surface) = CreateHost();
        using (surface)
        {
            controller.OnPointerDown(10, 10, MouseButton.Left);
            controller.OnPointerUp(10, 10, MouseButton.Left);
            engine.Render(surface.Canvas);
            engine.HasPendingVisualWork.ShouldBeFalse();

            controller.OnTextInput("a");

            engine.HasPendingVisualWork.ShouldBeTrue();
        }
    }

    [Fact]
    public void Rerender_DoesNotCarryDisabledState()
    {
        // Disabled 由组件按参数每次重新标注（IonInput.Build 就是这样做的），不属于
        // 「交互产生」的状态：搬迁它会让 Disabled 参数转为 false 后元素仍卡在禁用态。
        var (_, engine, component, surface) = CreateHost();
        using (surface)
        {
            CurrentInput(engine).SetState(ElementState.Disabled);

            component.ForceRender();

            CurrentInput(engine).HasState(ElementState.Disabled).ShouldBeFalse();
        }
    }

    [Fact]
    public void Rerender_DoesNotResurrectFocusOnBlurredInput()
    {
        // 焦点迁移的方向性：状态只从被替换的旧实例搬到新实例，不会凭空产生。
        var (controller, engine, component, surface) = CreateHost();
        using (surface)
        {
            controller.OnPointerDown(10, 10, MouseButton.Left);
            controller.OnPointerUp(10, 10, MouseButton.Left);
            engine.Render(surface.Canvas);
            CurrentInput(engine).HasState(ElementState.Focus).ShouldBeTrue();

            // 点击输入框之外：焦点清除。
            controller.OnPointerDown(280, 190, MouseButton.Left);
            controller.OnPointerUp(280, 190, MouseButton.Left);
            engine.Render(surface.Canvas);
            CurrentInput(engine).HasState(ElementState.Focus).ShouldBeFalse();

            // 一次与输入无关的重渲染不应把焦点带回来。
            component.ForceRender();
            engine.Render(surface.Canvas);

            CurrentInput(engine).HasState(ElementState.Focus).ShouldBeFalse();
        }
    }
}
