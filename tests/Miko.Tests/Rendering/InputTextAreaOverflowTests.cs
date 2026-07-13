using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Fonts;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Miko.Styling.Selectors;
using Miko.Utils;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// 问题「Input 和 TextArea 内容超出后渲染错误」的回归测试。
///
/// - Input：内容超出宽度后应按光标水平滚动保持光标可见，且超出内容盒的文本被裁剪不显示。
/// - TextArea：内容超出行宽应自动换行（含无空格长文本的逐字符断行），且超出内容盒的文本被裁剪。
/// </summary>
public class InputTextAreaOverflowTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine;
    private readonly LayoutEngine _layoutEngine;

    public InputTextAreaOverflowTests()
    {
        _bitmap = new SKBitmap(400, 300);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
        _renderEngine = new RenderEngine();
        _renderEngine.SetCanvas(_canvas);
        _layoutEngine = new LayoutEngine();
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    // 是否存在“非白色”像素（即被绘制的黑色文本）位于给定列范围内。
    private bool HasDarkPixelInColumns(int xStart, int xEnd, int yStart, int yEnd)
    {
        xStart = Math.Max(0, xStart);
        yStart = Math.Max(0, yStart);
        xEnd = Math.Min(_bitmap.Width, xEnd);
        yEnd = Math.Min(_bitmap.Height, yEnd);
        for (int y = yStart; y < yEnd; y++)
        {
            for (int x = xStart; x < xEnd; x++)
            {
                var c = _bitmap.GetPixel(x, y);
                // 文本为黑色；背景为白色。取一个明显的暗度阈值。
                if (c.Red < 128 && c.Green < 128 && c.Blue < 128)
                    return true;
            }
        }
        return false;
    }

    // ---------------------------------------------------------------------
    // TextWrapper：无空格长文本应逐字符断行（overflow-wrap: anywhere）
    // ---------------------------------------------------------------------

    [Fact]
    public void WrapText_LongUnbrokenWord_WithBreakLongWords_WrapsIntoMultipleLines()
    {
        float avail = 80;
        var text = "abcdefghijklmnopqrstuvwxyz0123456789"; // 无空格，远超 avail

        var withoutBreak = TextWrapper.WrapText(text, "Arial", 16, FontWeight.Normal, avail, WhiteSpace.PreWrap, breakLongWords: false);
        var withBreak = TextWrapper.WrapText(text, "Arial", 16, FontWeight.Normal, avail, WhiteSpace.PreWrap, breakLongWords: true);

        // 旧行为：长单词整体成一行（溢出）
        withoutBreak.Count.ShouldBe(1);
        // 新行为：按字符断成多行
        withBreak.Count.ShouldBeGreaterThan(1);

        // 每一行的宽度都不超过可用宽度
        foreach (var line in withBreak)
        {
            TextMeasurer.MeasureTextWidth(line, "Arial", 16, FontWeight.Normal)
                .ShouldBeLessThanOrEqualTo(avail + 0.5f);
        }

        // 断行不丢字符
        string.Concat(withBreak).ShouldBe(text);
    }

    [Fact]
    public void WrapText_MixedSpacedAndLongWord_BreaksOnlyTheLongWord()
    {
        float avail = 120;
        var text = "hi supercalifragilisticexpialidocioussupercalifragilistic ok";

        var lines = TextWrapper.WrapText(text, "Arial", 16, FontWeight.Normal, avail, WhiteSpace.PreWrap, breakLongWords: true);

        lines.Count.ShouldBeGreaterThan(1);
        foreach (var line in lines)
        {
            TextMeasurer.MeasureTextWidth(line, "Arial", 16, FontWeight.Normal)
                .ShouldBeLessThanOrEqualTo(avail + 0.5f);
        }
    }

    // ---------------------------------------------------------------------
    // TextArea：自动换行 + 裁剪
    // ---------------------------------------------------------------------

    private TextAreaElement CreateSizedTextArea(string value, out LayoutBox box, bool focused = false)
    {
        var ta = new TextAreaElement { Value = value };
        ta.Class = "ta";
        if (focused) ta.SetState(ElementState.Focus);

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("ta"), new Style
        {
            Width = Length.Px(100),
            Height = Length.Px(60),
            BoxSizing = BoxSizing.BorderBox,
            FontSize = Length.Px(16),
            PaddingTop = Length.Px(2),
            PaddingBottom = Length.Px(2),
            PaddingLeft = Length.Px(2),
            PaddingRight = Length.Px(2),
            BorderWidth = Length.Px(1),
            BorderStyle = BorderStyle.Solid,
            BorderColor = Color.Gray,
        });

        box = _layoutEngine.Layout(ta, new List<StyleSheet> { sheet }, 400, 300);
        return ta;
    }

    [Fact]
    public void TextArea_LongUnbrokenText_DoesNotRenderBeyondContentBox()
    {
        var box = default(LayoutBox)!;
        CreateSizedTextArea(new string('W', 200), out box); // 无空格长文本

        _renderEngine.Render(box);

        var padding = box.BoxModel.PaddingBox;
        // 内容盒右侧之外（含边框区右侧）不应有暗色文本像素。
        int rightEdge = (int)Math.Ceiling(padding.Right);
        HasDarkPixelInColumns(rightEdge + 2, rightEdge + 60, 0, 300)
            .ShouldBeFalse("textarea overflow text must be clipped to the content/padding box");
    }

    [Fact]
    public void TextArea_LongText_WrapsAndRendersMultipleLines()
    {
        var box = default(LayoutBox)!;
        CreateSizedTextArea(new string('W', 200), out box);

        _renderEngine.Render(box);

        var content = box.BoxModel.Content;
        // 第一行区域应有文本
        int firstLineTop = (int)content.Top;
        HasDarkPixelInColumns((int)content.X, (int)content.Right, firstLineTop, firstLineTop + 18)
            .ShouldBeTrue("first wrapped line should render text");

        // 第二行区域（下移一行高度）也应有文本 —— 证明发生了换行
        int secondLineTop = (int)(content.Top + 18);
        HasDarkPixelInColumns((int)content.X, (int)content.Right, secondLineTop, secondLineTop + 18)
            .ShouldBeTrue("second wrapped line should render text (proves wrapping happened)");
    }

    [Fact]
    public void TextArea_OverflowingVerticalContent_IsClippedBelowBox()
    {
        var box = default(LayoutBox)!;
        // 很多行，超过 60px 高度
        CreateSizedTextArea(string.Join("\n", Enumerable.Range(0, 30).Select(i => "line" + i)), out box);

        _renderEngine.Render(box);

        var padding = box.BoxModel.PaddingBox;
        int bottomEdge = (int)Math.Ceiling(padding.Bottom);
        HasDarkPixelInColumns((int)padding.X, (int)padding.Right, bottomEdge + 3, bottomEdge + 60)
            .ShouldBeFalse("textarea content below the box must be clipped");
    }

    // ---------------------------------------------------------------------
    // Input：水平滚动 + 裁剪
    // ---------------------------------------------------------------------

    private InputElement CreateSizedInput(string value, int cursorPos, bool focused, out LayoutBox box)
    {
        var input = new InputElement { Type = InputType.Text, Value = value, CursorPosition = cursorPos };
        input.Class = "inp";
        if (focused) input.SetState(ElementState.Focus);

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("inp"), new Style
        {
            Width = Length.Px(80),
            Height = Length.Px(24),
            BoxSizing = BoxSizing.BorderBox,
            FontSize = Length.Px(16),
            PaddingTop = Length.Px(1),
            PaddingBottom = Length.Px(1),
            PaddingLeft = Length.Px(2),
            PaddingRight = Length.Px(2),
            BorderWidth = Length.Px(1),
            BorderStyle = BorderStyle.Solid,
            BorderColor = Color.Gray,
        });

        box = _layoutEngine.Layout(input, new List<StyleSheet> { sheet }, 400, 300);
        return input;
    }

    [Fact]
    public void Input_LongValue_DoesNotRenderBeyondContentBox()
    {
        var box = default(LayoutBox)!;
        // 光标在末尾，聚焦 —— 应向左滚动，右侧不应溢出
        var value = "abcdefghijklmnopqrstuvwxyz";
        CreateSizedInput(value, value.Length, focused: true, out box);

        _renderEngine.Render(box);

        var padding = box.BoxModel.PaddingBox;
        int rightEdge = (int)Math.Ceiling(padding.Right);
        HasDarkPixelInColumns(rightEdge + 2, rightEdge + 80, 0, 300)
            .ShouldBeFalse("input overflow text must be clipped to the content/padding box");
    }

    [Fact]
    public void Input_FocusedWithCursorAtEnd_ScrollsSoCursorIsVisible()
    {
        var box = default(LayoutBox)!;
        var value = "abcdefghijklmnopqrstuvwxyz";
        var input = CreateSizedInput(value, value.Length, focused: true, out box);

        _renderEngine.Render(box);

        var content = box.BoxModel.Content;
        // 光标在末尾且滚动后应位于内容盒右边缘附近 —— 该列范围内应有暗色像素（光标或文本尾字符）。
        int nearRight = (int)(content.Right - 8);
        HasDarkPixelInColumns(nearRight, (int)Math.Ceiling(content.Right), (int)content.Top, (int)content.Bottom)
            .ShouldBeTrue("caret/tail char should be visible near the right edge when scrolled to end");
    }

    [Fact]
    public void Input_ScrollOffset_IsZero_WhenTextFits()
    {
        // 短文本完全放得下：不滚动。通过“最左列有文本”来验证起始处从左边缘绘制。
        var box = default(LayoutBox)!;
        CreateSizedInput("ab", 2, focused: true, out box);

        _renderEngine.Render(box);

        var content = box.BoxModel.Content;
        HasDarkPixelInColumns((int)content.X, (int)content.X + 20, (int)content.Top, (int)content.Bottom)
            .ShouldBeTrue("short text should render from the left edge (no scroll)");
    }

    [Fact]
    public void Input_CursorAtStart_ShowsTextStart_ClipsRightOverflow()
    {
        // 光标在最左：不滚动，从起始处显示；右侧超出部分被裁剪不显示（问题描述的行为）。
        var box = default(LayoutBox)!;
        var value = "abcdefghijklmnopqrstuvwxyz";
        CreateSizedInput(value, 0, focused: true, out box);

        _renderEngine.Render(box);

        var content = box.BoxModel.Content;
        var padding = box.BoxModel.PaddingBox;

        // 起始处（左边缘）有文本
        HasDarkPixelInColumns((int)content.X, (int)content.X + 12, (int)content.Top, (int)content.Bottom)
            .ShouldBeTrue("with cursor at start, text should render from the beginning");

        // 内容盒右侧之外无文本（被裁剪）
        int rightEdge = (int)Math.Ceiling(padding.Right);
        HasDarkPixelInColumns(rightEdge + 2, rightEdge + 80, 0, 300)
            .ShouldBeFalse("right overflow must be clipped when cursor is at start");
    }

    [Fact]
    public void Input_Unfocused_DoesNotScroll_ClipsOverflow()
    {
        // 未聚焦：从起始处显示（不跟随光标滚动），右侧仍被裁剪。
        var box = default(LayoutBox)!;
        var value = "abcdefghijklmnopqrstuvwxyz";
        CreateSizedInput(value, value.Length, focused: false, out box);

        _renderEngine.Render(box);

        var content = box.BoxModel.Content;
        var padding = box.BoxModel.PaddingBox;

        HasDarkPixelInColumns((int)content.X, (int)content.X + 12, (int)content.Top, (int)content.Bottom)
            .ShouldBeTrue("unfocused input renders from start");
        int rightEdge = (int)Math.Ceiling(padding.Right);
        HasDarkPixelInColumns(rightEdge + 2, rightEdge + 80, 0, 300)
            .ShouldBeFalse("unfocused overflow must be clipped");
    }
}
