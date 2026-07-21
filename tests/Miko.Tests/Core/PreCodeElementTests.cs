using Miko.Common;
using Miko.Components;
using Miko.Core.DomElements;
using Miko.Highlight;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Miko.Utils;
using Shouldly;

namespace Miko.Tests.Core;

/// <summary>
/// ISSUE-098：pre / code 标签与语法高亮的测试。
/// 覆盖：工厂注册、UA 默认样式、language/highlight 属性解析、
/// 预格式化文本（white-space: pre）的空白/换行/Tab 处理、以及高亮 token 的生成与缓存。
/// </summary>
public class PreCodeElementTests
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly StyleResolver _styleResolver = new();
    private readonly SyntaxHighlighter _highlighter = new();

    // ---------------------------------------------------------------------
    // 工厂注册（RenderTreeBuilder._tagMap）
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("pre", typeof(PreElement))]
    [InlineData("code", typeof(CodeElement))]
    public void OpenElement_ForNewTags_CreatesCorrectElement(string tag, Type expected)
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, tag);
        builder.CloseElement();

        builder.Build().ShouldBeOfType(expected);
    }

    // ---------------------------------------------------------------------
    // UA 默认样式
    // ---------------------------------------------------------------------

    [Fact]
    public void Pre_DefaultStyles_MatchBrowser()
    {
        var computed = _styleResolver.Resolve(new PreElement(), []);

        computed.Display.ShouldBe(Display.Block);
        computed.WhiteSpace.ShouldBe(WhiteSpace.Pre);
        computed.FontFamily.ShouldContain("Consolas");
        computed.MarginTop.Value.ShouldBe(16);
        computed.MarginBottom.Value.ShouldBe(16);
    }

    [Fact]
    public void Code_DefaultStyles_AreInlineMonospace()
    {
        var computed = _styleResolver.Resolve(new CodeElement(), []);

        computed.Display.ShouldBe(Display.Inline);
        computed.FontFamily.ShouldContain("Consolas");
    }

    [Fact]
    public void Pre_AuthorWhiteSpace_OverridesUaDefault()
    {
        // 作者样式优先于 UA 默认：显式 white-space: normal 覆盖 pre 的 UA 值。
        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("plain"), new Style { WhiteSpace = WhiteSpace.Normal });

        var computed = _styleResolver.Resolve(new PreElement { Class = "plain" }, [sheet]);

        computed.WhiteSpace.ShouldBe(WhiteSpace.Normal);
    }

    [Fact]
    public void Code_InPre_InheritsPreformattedWhiteSpace()
    {
        // pre > code > text：code 与其文本节点应继承 pre 的 white-space: pre。
        var pre = new PreElement();
        var code = new CodeElement();
        var text = new TextNode("a\nb");
        code.AddChild(text);
        pre.AddChild(code);

        var root = _layoutEngine.Layout(pre, [], 800, 600);

        var codeBox = root.Children[0];
        codeBox.ComputedStyle.WhiteSpace.ShouldBe(WhiteSpace.Pre);
        var textBox = codeBox.Children[0];
        textBox.ComputedStyle.WhiteSpace.ShouldBe(WhiteSpace.Pre);
    }

    // ---------------------------------------------------------------------
    // language / highlight 属性
    // ---------------------------------------------------------------------

    [Fact]
    public void Code_LanguageAndHighlightAttributes_AreParsed()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "code");
        builder.AddAttribute(1, "language", "csharp");
        builder.AddAttribute(2, "highlight", "false");
        builder.CloseElement();

        var code = builder.Build().ShouldBeOfType<CodeElement>();
        code.Language.ShouldBe("csharp");
        code.Highlight.ShouldBeFalse();
        code.IsHighlightActive.ShouldBeFalse();
    }

    [Fact]
    public void Code_SettingLanguage_EnablesHighlightByDefault()
    {
        var code = new CodeElement { Language = "csharp" };

        code.Highlight.ShouldBeTrue();
        code.IsHighlightActive.ShouldBeTrue();
    }

    [Fact]
    public void Code_WithoutLanguage_DoesNotHighlight()
    {
        new CodeElement().IsHighlightActive.ShouldBeFalse();
    }

    [Fact]
    public void Code_HighlightBooleanAttribute_NoValue_MeansTrue()
    {
        // HTML 布尔属性语义：无值属性出现即为真。
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "code");
        builder.AddAttribute(1, "language", "json");
        builder.AddAttribute(2, "highlight");
        builder.CloseElement();

        builder.Build().ShouldBeOfType<CodeElement>().IsHighlightActive.ShouldBeTrue();
    }

    // ---------------------------------------------------------------------
    // 预格式化文本处理（white-space: pre）
    // ---------------------------------------------------------------------

    [Fact]
    public void ProcessText_Pre_PreservesSpacesAndNewlines()
    {
        TextWrapper.ProcessText("  a   b\n  c  ", WhiteSpace.Pre)
            .ShouldBe("  a   b\n  c  ");
    }

    [Fact]
    public void ProcessText_Pre_NormalizesWindowsLineEndings()
    {
        // Windows 行尾的 Razor 源码会把 \r 带入文本，pre 下应归一为 \n，
        // 否则 \r 会作为多余字形参与测量与绘制。
        TextWrapper.ProcessText("a\r\nb\rc", WhiteSpace.Pre).ShouldBe("a\nb\nc");
    }

    [Fact]
    public void ProcessText_Pre_ExpandsTabsToTabStops()
    {
        // tab-size 8：制表符展开到下一个 8 的倍数列。
        TextWrapper.ProcessText("\tx", WhiteSpace.Pre).ShouldBe("        x");
        TextWrapper.ProcessText("ab\tcd", WhiteSpace.Pre).ShouldBe("ab      cd");
        // 换行后列号重置。
        TextWrapper.ProcessText("a\n\tx", WhiteSpace.Pre).ShouldBe("a\n        x");
    }

    [Fact]
    public void ProcessText_Pre_ReturnsSameInstance_WhenNothingToChange()
    {
        // 无 \r / \t 时返回原引用，避免逐帧分配（测量缓存依赖该行为保持零分配）。
        var text = "plain text\nwith newline";
        TextWrapper.ProcessText(text, WhiteSpace.Pre).ShouldBeSameAs(text);
    }

    [Fact]
    public void WrapText_Pre_SplitsOnExplicitNewlines()
    {
        var lines = TextWrapper.WrapText("line1\nline2", "Arial", 16, FontWeight.Normal, 1000, WhiteSpace.Pre);

        lines.Count.ShouldBe(2);
        lines[0].ShouldBe("line1");
        lines[1].ShouldBe("line2");
    }

    [Fact]
    public void WrapText_Pre_DoesNotSoftWrapLongLines()
    {
        // pre 不做软换行：即使行宽超过可用宽度，也保持单行。
        var lines = TextWrapper.WrapText(
            "a very long line that exceeds the available width by far",
            "Arial", 16, FontWeight.Normal, 50, WhiteSpace.Pre);

        lines.Count.ShouldBe(1);
    }

    [Fact]
    public void MeasureTextWithWrap_Pre_MeasuresMultipleLines()
    {
        float lineHeight = 20;
        var (width, height) = TextMeasurer.MeasureTextWithWrap(
            "short\na much longer line", "Arial", 16, FontWeight.Normal, 1000, lineHeight, WhiteSpace.Pre);

        height.ShouldBe(lineHeight * 2);
        width.ShouldBeGreaterThan(
            TextMeasurer.MeasureTextWidth("short", "Arial", 16, FontWeight.Normal));
    }

    // ---------------------------------------------------------------------
    // pre 布局
    // ---------------------------------------------------------------------

    [Fact]
    public void Pre_Layout_MultilineText_HasLineCountTimesLineHeight()
    {
        // <pre>line1\nline2\nline3</pre>：内容高度应为 3 倍行高，而非塌缩为 1 行。
        var pre = new PreElement();
        pre.AddChild(new TextNode("line1\nline2\nline3"));

        var root = _layoutEngine.Layout(pre, [], 800, 600);

        var textBox = root.Children[0];
        float lineHeight = Miko.Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(textBox.ComputedStyle);

        textBox.BoxModel.Content.Height.ShouldBe(lineHeight * 3, 0.5f);
    }

    [Fact]
    public void Pre_Layout_PreservesLeadingIndentation()
    {
        // 带缩进的行：内容宽度应包含前导空格的宽度（空格未被折叠）。
        var pre = new PreElement();
        pre.AddChild(new TextNode("x\n    x"));

        var root = _layoutEngine.Layout(pre, [], 800, 600);

        var textBox = root.Children[0];
        var style = textBox.ComputedStyle;
        float plainWidth = TextMeasurer.MeasureTextWidth(
            "x", style.FontFamily, style.FontSize.Value, style.FontWeight);

        // 第二行 "    x" 更宽，内容宽度取各行最大值。
        textBox.BoxModel.Content.Width.ShouldBeGreaterThan(plainWidth + 1);
    }

    // ---------------------------------------------------------------------
    // 语法高亮（SyntaxHighlighter + CodeElement 缓存）
    // ---------------------------------------------------------------------

    [Fact]
    public void Tokenize_CSharp_ClassifiesKeywordsTypesAndComments()
    {
        var text = "// demo\npublic class Demo { }";
        var tokens = _highlighter.Tokenize(text, "csharp").ShouldNotBeNull();

        FindToken(tokens, text, "// demo").ShouldBe(CodeTokenType.Comment);
        FindToken(tokens, text, "public").ShouldBe(CodeTokenType.Keyword);
        FindToken(tokens, text, "class").ShouldBe(CodeTokenType.Keyword);
        // 第二个 "Demo"（类名）应为 Type（第一个在注释里）。
        var demoTokens = tokens.Where(t => text.Substring(t.Start, t.Length) == "Demo").ToList();
        demoTokens.Count.ShouldBe(1);
        demoTokens[0].Type.ShouldBe(CodeTokenType.Type);
    }

    [Fact]
    public void Tokenize_CSharp_ClassifiesStringsNumbersAndFunctions()
    {
        var text = "var s = \"hi\";\nvar n = 42;\nConsole.WriteLine(s);";
        var tokens = _highlighter.Tokenize(text, "csharp").ShouldNotBeNull();

        FindToken(tokens, text, "\"hi\"").ShouldBe(CodeTokenType.String);
        FindToken(tokens, text, "42").ShouldBe(CodeTokenType.Number);
        FindToken(tokens, text, "WriteLine").ShouldBe(CodeTokenType.Function);
        FindToken(tokens, text, "var").ShouldBe(CodeTokenType.Keyword);
    }

    [Fact]
    public void Tokenize_CSharp_BlockCommentSpansLines()
    {
        var text = "int a; /* x\ny */ int b;";
        var tokens = _highlighter.Tokenize(text, "csharp").ShouldNotBeNull();

        var comment = tokens.Single(t => t.Type == CodeTokenType.Comment);
        text.Substring(comment.Start, comment.Length).ShouldBe("/* x\ny */");
    }

    [Fact]
    public void Tokenize_Json_ClassifiesStringsNumbersAndKeywords()
    {
        var text = "{\"ok\": true, \"count\": 12}";
        var tokens = _highlighter.Tokenize(text, "json").ShouldNotBeNull();

        FindToken(tokens, text, "\"ok\"").ShouldBe(CodeTokenType.String);
        FindToken(tokens, text, "true").ShouldBe(CodeTokenType.Keyword);
        FindToken(tokens, text, "12").ShouldBe(CodeTokenType.Number);
    }

    [Fact]
    public void Tokenize_Xml_ClassifiesTagsAttributesAndStrings()
    {
        var text = "<div class=\"box\">hi</div>";
        var tokens = _highlighter.Tokenize(text, "xml").ShouldNotBeNull();

        tokens.Where(t => t.Type == CodeTokenType.Keyword)
            .Select(t => text.Substring(t.Start, t.Length))
            .ShouldBe(["div", "div"]);
        FindToken(tokens, text, "class").ShouldBe(CodeTokenType.Type);
        FindToken(tokens, text, "\"box\"").ShouldBe(CodeTokenType.String);
    }

    [Fact]
    public void Tokenize_Sql_IsCaseInsensitive()
    {
        var tokens = _highlighter.Tokenize("select Id from Users", "sql").ShouldNotBeNull();

        FindToken(tokens, "select Id from Users", "select").ShouldBe(CodeTokenType.Keyword);
        FindToken(tokens, "select Id from Users", "from").ShouldBe(CodeTokenType.Keyword);
    }

    [Fact]
    public void Tokenize_LanguageAliases_Resolve()
    {
        _highlighter.Tokenize("var x = 1;", "c#").ShouldNotBeNull();
        _highlighter.Tokenize("var x = 1;", "cs").ShouldNotBeNull();
        _highlighter.Tokenize("let x = 1;", "js").ShouldNotBeNull();
        _highlighter.Tokenize("print(1)", "py").ShouldNotBeNull();
        _highlighter.Tokenize("echo hi", "sh").ShouldNotBeNull();
    }

    [Fact]
    public void Tokenize_UnknownLanguage_ReturnsNull()
    {
        _highlighter.Tokenize("some text", "cobol").ShouldBeNull();
        _highlighter.Tokenize("some text", "").ShouldBeNull();
        _highlighter.Tokenize("some text", null).ShouldBeNull();
    }

    [Fact]
    public void GetHighlightTokens_CachesByText()
    {
        var code = new CodeElement { Language = "csharp" };

        var first = code.GetHighlightTokens("class A { }", _highlighter).ShouldNotBeNull();
        var second = code.GetHighlightTokens("class A { }", _highlighter).ShouldNotBeNull();

        // 文本相同（引用相同）时直接命中缓存。
        ReferenceEquals(first, second).ShouldBeTrue();
        // 文本变化后重新计算。
        var third = code.GetHighlightTokens("class B { }", _highlighter).ShouldNotBeNull();
        ReferenceEquals(first, third).ShouldBeFalse();
    }

    [Fact]
    public void GetHighlightTokens_Recomputes_WhenHighlighterChanges()
    {
        // 高亮器实例变化（如应用注入了自定义实现）时，缓存的 token 应失效重算。
        var code = new CodeElement { Language = "csharp" };
        var other = new SyntaxHighlighter();

        var first = code.GetHighlightTokens("class A { }", _highlighter).ShouldNotBeNull();
        var second = code.GetHighlightTokens("class A { }", other).ShouldNotBeNull();

        ReferenceEquals(first, second).ShouldBeFalse();
        second.ShouldBe(first.ToList());
    }

    [Fact]
    public void GetHighlightTokens_RespectsHighlightSwitch()
    {
        var code = new CodeElement { Language = "csharp", Highlight = false };
        code.GetHighlightTokens("class A { }", _highlighter).ShouldBeNull();

        var unknown = new CodeElement { Language = "cobol" };
        unknown.GetHighlightTokens("whatever", _highlighter).ShouldBeNull();
    }

    private static CodeTokenType FindToken(IReadOnlyList<CodeToken> tokens, string text, string fragment)
    {
        var token = tokens.FirstOrDefault(t => text.Substring(t.Start, t.Length) == fragment);
        token.ShouldNotBe(default, $"应找到 token \"{fragment}\"");
        return token.Type;
    }
}
