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
/// ISSUE-154 BUG-04：文本编辑不得拆开 Unicode 字符。
///
/// <para>位置与长度按 UTF-16 码元计（<c>CursorPosition</c>、输入法选区、<c>MaxLength</c>，与 DOM 一致），
/// 编辑按字素簇进行：整段输入一次提交，<c>maxlength</c> 截断不留孤立代理项，左右移动、退格、删除一次
/// 跨过一个完整字符（emoji、组合重音、ZWJ 序列、国旗）。</para>
/// </summary>
public class UnicodeTextEditingTests
{
    private const string Grin = "\U0001F600";                                   // 😀 代理对
    private const string Family = "\U0001F468\u200D\U0001F469\u200D\U0001F467"; // 👨‍👩‍👧 ZWJ 序列
    private const string Flag = "\U0001F1E8\U0001F1F3";                         // 🇨🇳 两个区域指示符
    private const string EAcute = "e\u0301";                                    // é 组合重音

    private static bool HasLoneSurrogate(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]))
            {
                if (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1])) return true;
                i++;
            }
            else if (char.IsLowSurrogate(text[i]))
            {
                return true;
            }
        }
        return false;
    }

    // ---- 元素层 -------------------------------------------------------------------------

    [Theory]
    [InlineData(Grin)]
    [InlineData(Family)]
    [InlineData(Flag)]
    [InlineData(EAcute)]
    public void Backspace_and_delete_should_remove_a_whole_grapheme(string grapheme)
    {
        var input = new InputElement { Value = "a" + grapheme + "b" };

        input.CursorPosition = 1 + grapheme.Length;
        input.Backspace().ShouldBeTrue();
        input.Value.ShouldBe("ab");
        input.CursorPosition.ShouldBe(1);

        var textArea = new TextAreaElement { Value = "a" + grapheme + "b", CursorPosition = 1 };
        textArea.Delete().ShouldBeTrue();
        textArea.Value.ShouldBe("ab");
        textArea.CursorPosition.ShouldBe(1);
    }

    [Fact]
    public void MaxLength_truncation_should_never_leave_a_lone_surrogate()
    {
        var input = new InputElement { Value = "abc", MaxLength = 4 };
        input.MoveCursorToEnd();

        input.InsertText(Grin);       // 只剩 1 个码元的空间：emoji 整个放不下

        input.Value.ShouldBe("abc");
        HasLoneSurrogate(input.Value!).ShouldBeFalse();
    }

    [Fact]
    public void MaxLength_should_count_utf16_code_units_and_keep_whole_graphemes()
    {
        var textArea = new TextAreaElement { MaxLength = 5 };

        textArea.InsertText(Grin + Grin + Grin);   // 每个 2 码元：放得下两个

        textArea.Value.ShouldBe(Grin + Grin);
        textArea.CursorPosition.ShouldBe(4);
    }

    [Fact]
    public void Editing_from_a_position_inside_a_grapheme_should_snap_to_its_start()
    {
        var input = new InputElement { Value = "a" + Grin, CursorPosition = 2 };   // 落在代理对中间

        input.InsertText("x");

        input.Value.ShouldBe("ax" + Grin);
        HasLoneSurrogate(input.Value!).ShouldBeFalse();
    }

    [Fact]
    public void Backspace_should_treat_a_crlf_pair_as_one_character()
    {
        var textArea = new TextAreaElement { Value = "a\r\nb", CursorPosition = 3 };

        textArea.Backspace().ShouldBeTrue();

        textArea.Value.ShouldBe("ab");
    }

    // ---- 控制器层 -----------------------------------------------------------------------

    [Fact]
    public void OnTextInput_should_insert_the_whole_string_once_and_report_it()
    {
        var input = Input();
        var (controller, surface) = CreateController(input);
        using (surface)
        {
            var events = new List<InputEventArgs>();
            input.OnInput = events.Add;
            Focus(controller);

            controller.OnTextInput("a" + Family + "b");

            input.Value.ShouldBe("a" + Family + "b");
            events.Count.ShouldBe(1);
            events[0].InsertedText.ShouldBe("a" + Family + "b");
        }
    }

    [Fact]
    public void OnTextInput_should_report_only_what_maxlength_let_in_and_skip_noops()
    {
        var input = Input();
        input.MaxLength = 3;
        var (controller, surface) = CreateController(input);
        using (surface)
        {
            var events = new List<InputEventArgs>();
            input.OnInput = events.Add;
            Focus(controller);

            controller.OnTextInput("a" + Grin + "b");
            input.Value.ShouldBe("a" + Grin);
            events.Single().InsertedText.ShouldBe("a" + Grin);

            controller.OnTextInput("c");   // 已满：值不变，不派发 input
            input.Value.ShouldBe("a" + Grin);
            events.Count.ShouldBe(1);
        }
    }

    [Fact]
    public void OnTextInput_should_join_a_surrogate_pair_split_across_two_calls()
    {
        var input = Input();
        input.MaxLength = 2;
        var (controller, surface) = CreateController(input);
        using (surface)
        {
            Focus(controller);

            controller.OnTextInput(Grin[0].ToString());   // WM_CHAR 式：先来高位代理
            input.Value.ShouldBeNullOrEmpty();
            controller.OnTextInput(Grin[1].ToString());

            input.Value.ShouldBe(Grin);
        }
    }

    [Fact]
    public void OnTextInput_should_drop_lone_surrogates()
    {
        var input = Input();
        var (controller, surface) = CreateController(input);
        using (surface)
        {
            Focus(controller);

            controller.OnTextInput("a\uDE00b");   // 孤立的低位代理
            controller.OnTextInput("\uD83Dc");    // 后面不是低位代理的高位代理

            input.Value.ShouldBe("abc");
        }
    }

    [Fact]
    public void Arrow_keys_and_backspace_should_step_over_whole_graphemes()
    {
        var input = Input();
        input.Value = "a" + Family + Flag;
        var (controller, surface) = CreateController(input);
        using (surface)
        {
            Focus(controller);
            input.CursorPosition.ShouldBe(input.Value!.Length);

            controller.OnKeyDown(MikoKey.Left, MikoKeyModifiers.None);
            input.CursorPosition.ShouldBe(1 + Family.Length);

            controller.OnKeyDown(MikoKey.Left, MikoKeyModifiers.None);
            input.CursorPosition.ShouldBe(1);

            controller.OnKeyDown(MikoKey.Right, MikoKeyModifiers.None);
            input.CursorPosition.ShouldBe(1 + Family.Length);

            controller.OnKeyDown(MikoKey.Backspace, MikoKeyModifiers.None);
            input.Value.ShouldBe("a" + Flag);
            input.CursorPosition.ShouldBe(1);
        }
    }

    [Fact]
    public void SetTextSelection_should_snap_to_a_grapheme_boundary()
    {
        var input = Input();
        input.Value = "a" + Grin;
        var (controller, surface) = CreateController(input);
        using (surface)
        {
            Focus(controller);

            controller.SetTextSelection(2, 2);   // 代理对中间

            input.CursorPosition.ShouldBe(1);
        }
    }

    [Fact]
    public void DeleteSurroundingText_should_not_split_a_surrogate_pair()
    {
        var input = Input();
        input.Value = "a" + Grin;
        var (controller, surface) = CreateController(input);
        using (surface)
        {
            var events = 0;
            input.OnInput = _ => events++;
            Focus(controller);

            controller.DeleteSurroundingText(1, 0);   // 输入法只要求删 1 个码元

            input.Value.ShouldBe("a");
            HasLoneSurrogate(input.Value!).ShouldBeFalse();
            events.ShouldBe(1);
        }
    }

    // ---- 基础设施 -----------------------------------------------------------------------

    private static InputElement Input() => new()
    {
        Style = new Style { Width = Length.Px(240), Height = Length.Px(32) },
    };

    private static (MikoInteractionController controller, SKSurface surface) CreateController(Element editable)
    {
        var root = new DivElement { Children = { editable } };
        var options = new MikoAppOptions { RootComponentFactory = () => root };
        var engine = new MikoEngineBuilder().Build();
        var controller = new MikoInteractionController(
            Options.Create(options),
            new EmptyServiceProvider(),
            engine,
            new EventDispatcher(),
            new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance),
            NullLogger<MikoInteractionController>.Instance);
        var surface = SKSurface.Create(new SKImageInfo(400, 200));
        controller.Initialize(surface.Canvas, 400, 200);
        engine.Render(surface.Canvas);
        return (controller, surface);
    }

    private static void Focus(MikoInteractionController controller)
    {
        controller.OnPointerDown(10, 10, MouseButton.Left);
        controller.OnPointerUp(10, 10, MouseButton.Left);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
