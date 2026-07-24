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
/// ISSUE-104 回归测试：交互引起的元素视觉变更（range 拖动/点轨、checkbox 切换、
/// 文本编辑、光标移动）必须让引擎产生待呈现工作（<see cref="MikoEngine.HasPendingVisualWork"/>），
/// 否则宿主渲染循环的稳态空闲检测（ISSUE-096）会跳过帧生产，画面只在其他事件
/// （如 :hover 状态变化）碰巧触发重绘时才更新。
///
/// 修复前这些路径只设置 <c>Element.IsDirty</c>——一个没有任何读取方的内部标志，
/// 既不进脏区域表也不递增 MutationVersion，因此不会调度任何重绘。
/// </summary>
public class InputInvalidationTests
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
    /// 与 issues/ISSUE-104.md 一致的场景：200×20 的 range 输入框位于 (0,0)。
    /// 轨道几何（与 UpdateRangeValue 一致）：thumbRadius = min(20/2-2, 8) = 8，
    /// 有效轨道 x ∈ [8, 192]，width = 184。
    /// </summary>
    private static (MikoAppOptions options, InputElement range) CreateRangeRepro()
    {
        var range = new InputElement
        {
            Type = InputType.Range,
            Min = 0,
            Max = 100,
            NumericValue = 0,
            Style = new Style { Width = Length.Px(200), Height = Length.Px(20) },
        };
        var root = new DivElement { Children = { range } };

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
        return (options, range);
    }

    private static SKSurface InitAndSettle(MikoInteractionController controller, MikoEngine engine)
    {
        var surface = SKSurface.Create(new SKImageInfo(500, 500));
        controller.Initialize(surface.Canvas, 500, 500);
        engine.Render(surface.Canvas);
        engine.HasPendingVisualWork.ShouldBeFalse(); // 稳态：无任何待呈现工作
        return surface;
    }

    [Fact]
    public void Range_TrackClick_MovesThumbImmediately_AndSchedulesRepaint()
    {
        var (options, range) = CreateRangeRepro();
        var engine = new MikoEngine();
        var controller = CreateController(options, engine);
        using var surface = InitAndSettle(controller, engine);

        // 点击轨道 75% 处：x=146 → (146-8)/184 = 0.75。
        controller.OnPointerDown(146, 10, MouseButton.Left);

        range.NumericValue.ShouldBe(75f, 0.5f);
        // 关键断言：值变更必须调度重绘，而不是等指针移出元素（:hover 变化）才刷新。
        engine.HasPendingVisualWork.ShouldBeTrue();

        controller.OnPointerUp(146, 10, MouseButton.Left);
    }

    [Fact]
    public void Range_Drag_ThumbFollowsPointer_EachMoveSchedulesRepaint()
    {
        var (options, range) = CreateRangeRepro();
        var engine = new MikoEngine();
        var controller = CreateController(options, engine);
        using var surface = InitAndSettle(controller, engine);

        // 按下轨道起点（value 0 → 不变），随后拖动。
        controller.OnPointerDown(8, 10, MouseButton.Left);
        range.NumericValue.ShouldBe(0f, 0.5f);

        controller.OnPointerMove(100, 10);   // → 50%
        range.NumericValue.ShouldBe(50f, 0.5f);
        engine.HasPendingVisualWork.ShouldBeTrue();

        engine.Render(surface.Canvas);
        engine.HasPendingVisualWork.ShouldBeFalse();

        controller.OnPointerMove(192, 10);   // → 100%
        range.NumericValue.ShouldBe(100f, 0.5f);
        engine.HasPendingVisualWork.ShouldBeTrue();

        // 拖出元素外：值应被钳制且仍跟随（拖拽捕获），并继续调度重绘。
        engine.Render(surface.Canvas);
        controller.OnPointerMove(400, 400);
        range.NumericValue.ShouldBe(100f, 0.5f);

        controller.OnPointerUp(400, 400, MouseButton.Left);
    }

    [Fact]
    public void Range_AfterPointerUp_MoveDoesNotChangeValue()
    {
        var (options, range) = CreateRangeRepro();
        var engine = new MikoEngine();
        var controller = CreateController(options, engine);
        using var surface = InitAndSettle(controller, engine);

        controller.OnPointerDown(100, 10, MouseButton.Left);
        controller.OnPointerUp(100, 10, MouseButton.Left);
        engine.Render(surface.Canvas);

        controller.OnPointerMove(192, 10);   // 未按下：仅悬停，不应改值
        range.NumericValue.ShouldBe(50f, 0.5f);
    }

    [Fact]
    public void Checkbox_Click_TogglesAndSchedulesRepaint()
    {
        var checkbox = new InputElement
        {
            Type = InputType.Checkbox,
            Style = new Style { Width = Length.Px(20), Height = Length.Px(20) },
        };
        var root = new DivElement { Children = { checkbox } };
        var options = new MikoAppOptions { RootComponentFactory = () => root };

        var engine = new MikoEngine();
        var controller = CreateController(options, engine);
        using var surface = InitAndSettle(controller, engine);

        controller.OnPointerDown(10, 10, MouseButton.Left);
        controller.OnPointerUp(10, 10, MouseButton.Left);

        checkbox.Checked.ShouldBeTrue();
        engine.HasPendingVisualWork.ShouldBeTrue();
    }

    [Fact]
    public void TextInput_TypingAndCursorMove_ScheduleRepaint()
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
        using var surface = InitAndSettle(controller, engine);

        // 点击聚焦（Focus 状态变化本身会调度一帧，先渲染掉以隔离后续断言）。
        controller.OnPointerDown(10, 10, MouseButton.Left);
        controller.OnPointerUp(10, 10, MouseButton.Left);
        engine.Render(surface.Canvas);
        engine.HasPendingVisualWork.ShouldBeFalse();

        controller.OnTextInput("ab");
        input.Value.ShouldBe("ab");
        engine.HasPendingVisualWork.ShouldBeTrue();

        engine.Render(surface.Canvas);
        controller.OnKeyDown(MikoKey.Left, MikoKeyModifiers.None);
        input.CursorPosition.ShouldBe(1);
        engine.HasPendingVisualWork.ShouldBeTrue();

        engine.Render(surface.Canvas);
        controller.OnKeyDown(MikoKey.Backspace, MikoKeyModifiers.None);
        input.Value.ShouldBe("b");   // 光标在 1，删除其前的 'a'
        engine.HasPendingVisualWork.ShouldBeTrue();
    }
}
