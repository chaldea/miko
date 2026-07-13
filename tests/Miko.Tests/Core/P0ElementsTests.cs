using Miko.Common;
using Miko.Components;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Core;

/// <summary>
/// P0 新增标签（textarea、br、hr）及表格 caption/colgroup/col 工厂注册的测试。
/// 覆盖：工厂注册、UA 默认样式、以及 br 的强制换行与 textarea 的多行高度布局行为。
/// </summary>
public class P0ElementsTests
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly StyleResolver _styleResolver = new();

    // ---------------------------------------------------------------------
    // 工厂注册（RenderTreeBuilder._tagMap）
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("textarea", typeof(TextAreaElement))]
    [InlineData("br", typeof(BrElement))]
    [InlineData("hr", typeof(HrElement))]
    [InlineData("caption", typeof(CaptionElement))]
    [InlineData("colgroup", typeof(ColgroupElement))]
    [InlineData("col", typeof(ColElement))]
    public void OpenElement_ForNewTags_CreatesCorrectElement(string tag, Type expected)
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, tag);
        builder.CloseElement();

        builder.Build().ShouldBeOfType(expected);
    }

    // ---------------------------------------------------------------------
    // textarea 默认样式与布局
    // ---------------------------------------------------------------------

    [Fact]
    public void TextArea_DefaultStyles_MatchBrowser()
    {
        var computed = _styleResolver.Resolve(new TextAreaElement(), []);

        computed.Display.ShouldBe(Display.InlineBlock);
        computed.BoxSizing.ShouldBe(BoxSizing.BorderBox);
        computed.BorderTopWidth.Value.ShouldBe(1);
        computed.WhiteSpace.ShouldBe(WhiteSpace.PreWrap);
    }

    [Fact]
    public void TextArea_AutoHeight_IsDerivedFromRows()
    {
        // rows=3 时内容高度应约为 3 行文本高度（>1 行，随 rows 增长）。
        var oneRow = new TextAreaElement { Rows = 1 };
        var threeRows = new TextAreaElement { Rows = 3 };

        var oneRowBox = _layoutEngine.Layout(oneRow, [], 800, 600);
        var threeRowBox = _layoutEngine.Layout(threeRows, [], 800, 600);

        float oneRowHeight = oneRowBox.BoxModel.Content.Height;
        oneRowHeight.ShouldBeGreaterThan(0);
        threeRowBox.BoxModel.Content.Height.ShouldBe(oneRowHeight * 3, 0.5);
    }

    [Fact]
    public void TextArea_AutoWidth_IsDerivedFromCols()
    {
        // 问题1修复：默认 cols=20，内容宽度应约为 20 × 平均字符宽度（远大于 0），
        // 而非塌缩为 0（仅剩 padding+border）。
        var textArea = new TextAreaElement(); // Cols = 20 (默认)
        var root = _layoutEngine.Layout(textArea, [], 800, 600);

        float avgCharWidth = Miko.Utils.TextMeasurer.MeasureTextWidth(
            "0", root.ComputedStyle.FontFamily, root.ComputedStyle.FontSize.Value, root.ComputedStyle.FontWeight);
        avgCharWidth.ShouldBeGreaterThan(0);

        // 内容宽度应等于 cols × 平均字符宽度
        root.BoxModel.Content.Width.ShouldBe(avgCharWidth * 20, 0.5);
        // 且明显大于 0（回归保护：修复前为 0）
        root.BoxModel.Content.Width.ShouldBeGreaterThan(avgCharWidth * 10);
    }

    [Fact]
    public void TextArea_AutoWidth_ScalesWithCols()
    {
        // cols 越大，内容宽度越宽（成比例）。
        var narrow = new TextAreaElement { Cols = 10 };
        var wide = new TextAreaElement { Cols = 40 };

        var narrowBox = _layoutEngine.Layout(narrow, [], 800, 600);
        var wideBox = _layoutEngine.Layout(wide, [], 800, 600);

        float narrowWidth = narrowBox.BoxModel.Content.Width;
        narrowWidth.ShouldBeGreaterThan(0);
        wideBox.BoxModel.Content.Width.ShouldBe(narrowWidth * 4, 0.5);
    }

    [Fact]
    public void TextArea_ExplicitWidth_OverridesColsDefault()
    {
        // 显式 width 应覆盖由 cols 推导的默认宽度。
        var textArea = new TextAreaElement { Cols = 20, Style = new Style { Width = Length.Px(300) } };
        var root = _layoutEngine.Layout(textArea, [], 800, 600);

        // border-box：内容宽度 = 300 - padding(2+2) - border(1+1) = 294
        root.BoxModel.Content.Width.ShouldBe(294, 0.5);
    }

    [Fact]
    public void TextArea_InitialTextContent_IsCollapsedIntoValue()
    {
        // HTML 中 textarea 的初始文本写在标签内容里，应回收进 Value 而非作为子节点参与布局。
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "textarea");
        builder.AddContent(1, "hello");
        builder.CloseElement();

        var textArea = builder.Build().ShouldBeOfType<TextAreaElement>();
        textArea.Value.ShouldBe("hello");
        textArea.Children.ShouldNotContain(c => c is TextNode);
    }

    [Fact]
    public void TextArea_ValueAttribute_TakesPrecedenceOverContent()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "textarea");
        builder.AddAttribute(1, "value", "from-attr");
        builder.AddContent(2, "from-content");
        builder.CloseElement();

        var textArea = builder.Build().ShouldBeOfType<TextAreaElement>();
        textArea.Value.ShouldBe("from-attr");
    }

    [Fact]
    public void TextArea_RowsAndColsAttributes_AreParsed()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "textarea");
        builder.AddAttribute(1, "rows", "5");
        builder.AddAttribute(2, "cols", "40");
        builder.CloseElement();

        var textArea = builder.Build().ShouldBeOfType<TextAreaElement>();
        textArea.Rows.ShouldBe(5);
        textArea.Cols.ShouldBe(40);
    }

    // ---------------------------------------------------------------------
    // textarea 文本编辑（ITextEditable）
    // ---------------------------------------------------------------------

    [Fact]
    public void TextArea_InsertAndBackspace_EditsValueAtCursor()
    {
        var textArea = new TextAreaElement();
        textArea.InsertText("ab");
        textArea.InsertText("c");

        textArea.Value.ShouldBe("abc");
        textArea.CursorPosition.ShouldBe(3);

        textArea.Backspace().ShouldBeTrue();
        textArea.Value.ShouldBe("ab");
        textArea.CursorPosition.ShouldBe(2);
    }

    [Fact]
    public void TextArea_IsMultilineAndEditable()
    {
        var textArea = new TextAreaElement();
        ((ITextEditable)textArea).IsEditable.ShouldBeTrue();
        ((ITextEditable)textArea).IsMultiline.ShouldBeTrue();
    }

    // ---------------------------------------------------------------------
    // br 强制换行
    // ---------------------------------------------------------------------

    [Fact]
    public void Br_DefaultDisplay_IsInline()
    {
        _styleResolver.Resolve(new BrElement(), []).Display.ShouldBe(Display.Inline);
    }

    [Fact]
    public void Br_ForcesLineBreak_PushesFollowingTextToNewLine()
    {
        // <div>a<br/>b</div>：a 与 b 应分处两行，b 的顶部不小于 a 的底部。
        var div = new DivElement();
        div.AddChild(new TextNode("a"));
        div.AddChild(new BrElement());
        div.AddChild(new TextNode("b"));

        var root = _layoutEngine.Layout(div, [], 800, 600);

        // 布局树：a、br、b 三个行内子盒
        var boxes = root.Children;
        boxes.Count.ShouldBe(3);
        var aBox = boxes[0];
        var bBox = boxes[2];

        bBox.BoxModel.MarginBox.Top.ShouldBeGreaterThanOrEqualTo(aBox.BoxModel.MarginBox.Bottom - 0.5f);
    }

    [Fact]
    public void Br_WithoutBreak_KeepsTextOnSameLine()
    {
        // 对照组：无 br 时 a、b 同处一行（顶部一致）。
        var div = new DivElement();
        div.AddChild(new TextNode("a"));
        div.AddChild(new TextNode("b"));

        var root = _layoutEngine.Layout(div, [], 800, 600);
        var aBox = root.Children[0];
        var bBox = root.Children[1];

        bBox.BoxModel.MarginBox.Top.ShouldBe(aBox.BoxModel.MarginBox.Top, 0.5);
    }

    // ---------------------------------------------------------------------
    // hr 主题分隔线
    // ---------------------------------------------------------------------

    [Fact]
    public void Hr_DefaultStyles_HaveTopBorderLine()
    {
        var computed = _styleResolver.Resolve(new HrElement(), []);

        computed.Display.ShouldBe(Display.Block);
        computed.BorderTopWidth.Value.ShouldBe(1);
        computed.BorderTopStyle.ShouldBe(BorderStyle.Solid);
        computed.ComputedBorderTop.IsVisible.ShouldBeTrue();
    }

    [Fact]
    public void Hr_Layout_FillsWidthWithZeroContentHeight()
    {
        // hr 无内容：内容高度为 0，但因 1px 上边框，border-box 高度为 1。
        var hr = new HrElement();
        var root = _layoutEngine.Layout(hr, [], 800, 600);

        root.BoxModel.Content.Height.ShouldBe(0);
        root.BoxModel.BorderBox.Height.ShouldBe(1);
        root.BoxModel.Content.Width.ShouldBeGreaterThan(0);
    }
}
