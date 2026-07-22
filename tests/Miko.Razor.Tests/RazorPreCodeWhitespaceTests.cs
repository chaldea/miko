using Miko.Core.DomElements;
using Miko.Testing;
using Shouldly;

namespace Miko.Razor.Tests;

/// <summary>
/// ISSUE-098 端到端验证：经 Miko.Razor.Compiler 编译的 <c>&lt;pre&gt;</c> 标记保留
/// 预格式化空白（换行与缩进），而普通元素之间的格式空白仍按既有行为裁剪。
///
/// 编译器历史上会裁剪标记间空白（ComponentWhitespacePass）并跳过纯空白内容
/// （ComponentRuntimeNodeWriter），这与 pre 语义冲突，因此对 pre 子树整体豁免。
/// </summary>
public class RazorPreCodeWhitespaceTests : IDisposable
{
    private readonly TestContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void Pre_PreservesNewlinesAndIndentation()
    {
        var cut = _ctx.Render<PreCodeWhitespaceFixture>();

        var pre = cut.Root.ShouldBeOfType<PreElement>();
        var code = pre.Children.ShouldHaveSingleItem().ShouldBeOfType<CodeElement>();

        // DOM 文本保留原始换行与缩进（Windows 源文件的 \r\n 在绘制前由引擎归一，
        // 这里先归一后比较）。编译器裁剪空白时该文本会丢失换行与缩进。
        var text = code.TextContent.ShouldNotBeNull().Replace("\r\n", "\n");
        text.ShouldBe("public class Demo\n{\n    public string Id { get; set; }\n}");
    }

    [Fact]
    public void Pre_LanguageAttribute_FlowsToCodeElement()
    {
        var cut = _ctx.Render<PreCodeWhitespaceFixture>();

        var code = cut.Root.Children[0].ShouldBeOfType<CodeElement>();
        code.Language.ShouldBe("csharp");
        code.IsHighlightActive.ShouldBeTrue();
    }

    [Fact]
    public void Pre_Layout_KeepsAllSourceLines()
    {
        var cut = _ctx.Render<PreCodeWhitespaceFixture>();

        // 代码样例共 4 行，文本盒高度应为 4 倍行高（换行未被吞掉）。
        var code = cut.Root.Children[0].ShouldBeOfType<CodeElement>();
        var textNode = code.Children.OfType<TextNode>().First();
        var textBox = cut.FindLayoutBox(textNode).ShouldNotBeNull();
        var style = cut.GetComputedStyle(textNode).ShouldNotBeNull();
        float lineHeight = Miko.Utils.TextMeasurer.MeasureTextHeight(
            style.FontFamily, style.FontSize.Value, style.FontWeight);
        textBox.BoxModel.Content.Height.ShouldBe(lineHeight * 4, 0.5f);
    }

    [Fact]
    public void PlainMarkup_StillTrimsInsignificantWhitespace()
    {
        // 对照组：普通元素之间的换行/缩进仍被裁剪，不产生产生布局的空白文本节点。
        var cut = _ctx.Render<PlainWhitespaceFixture>();

        var div = cut.Root.ShouldBeOfType<DivElement>();
        var span = div.Children.ShouldHaveSingleItem().ShouldBeOfType<SpanElement>();
        span.Children.ShouldHaveSingleItem().ShouldBeOfType<TextNode>().Text.ShouldBe("hello");
    }
}
