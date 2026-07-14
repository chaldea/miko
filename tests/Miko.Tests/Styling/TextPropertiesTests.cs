using Miko.Common;
using Miko.Styling;
using Miko.Utils;
using Shouldly;

namespace Miko.Tests.Styling;

/// <summary>
/// ISSUE-088：文本排版补充属性测试 —— text-transform、letter-spacing、
/// overflow-wrap、word-break、text-overflow 的解析、继承与工具函数。
/// </summary>
public class TextPropertiesTests
{
    // ---- text-transform ----

    [Theory]
    [InlineData(TextTransform.Uppercase, "hello world", "HELLO WORLD")]
    [InlineData(TextTransform.Lowercase, "Hello WORLD", "hello world")]
    [InlineData(TextTransform.Capitalize, "hello world", "Hello World")]
    [InlineData(TextTransform.None, "hello world", "hello world")]
    public void TextTransformer_Apply_TransformsCase(TextTransform transform, string input, string expected)
    {
        TextTransformer.Apply(input, transform).ShouldBe(expected);
    }

    [Fact]
    public void TextTransformer_Capitalize_KeepsInnerCase()
    {
        // capitalize 仅将单词首字母大写，其余字符保持原样（与 CSS 一致）。
        TextTransformer.Apply("iOS mcDONALD", TextTransform.Capitalize).ShouldBe("IOS McDONALD");
    }

    [Fact]
    public void TextTransformer_Apply_NullOrEmpty_ReturnsEmpty()
    {
        TextTransformer.Apply(null, TextTransform.Uppercase).ShouldBe(string.Empty);
        TextTransformer.Apply("", TextTransform.Uppercase).ShouldBe(string.Empty);
    }

    [Fact]
    public void FromStyle_TextTransform_ResolvesAndDefaultsToNone()
    {
        ComputedStyle.FromStyle(new Style { TextTransform = TextTransform.Uppercase })
            .TextTransform.ShouldBe(TextTransform.Uppercase);
        ComputedStyle.FromStyle(new Style()).TextTransform.ShouldBe(TextTransform.None);
    }

    // ---- letter-spacing ----

    [Fact]
    public void FromStyle_LetterSpacing_ResolvesAndDefaultsToZero()
    {
        ComputedStyle.FromStyle(new Style { LetterSpacing = Length.Px(2) })
            .LetterSpacing.Value.ShouldBe(2f);
        ComputedStyle.FromStyle(new Style()).LetterSpacing.Value.ShouldBe(0f);
    }

    [Fact]
    public void LetterSpacingExtent_AddsPerCharacter()
    {
        // 每个字符追加 letterSpacing：5 字符 × 3px = 15px。
        TextMeasurer.LetterSpacingExtent("hello", 3f).ShouldBe(15f);
        // 0 spacing 不影响。
        TextMeasurer.LetterSpacingExtent("hello", 0f).ShouldBe(0f);
        TextMeasurer.LetterSpacingExtent("", 3f).ShouldBe(0f);
    }

    // ---- overflow-wrap / word-break ----

    [Theory]
    // word-break: break-all → 总是允许逐字符断行。
    [InlineData(WordBreak.BreakAll, OverflowWrap.Normal, true)]
    // overflow-wrap: break-word / anywhere → 允许长单词内部断行。
    [InlineData(WordBreak.Normal, OverflowWrap.BreakWord, true)]
    [InlineData(WordBreak.Normal, OverflowWrap.Anywhere, true)]
    // 默认：不断长单词。
    [InlineData(WordBreak.Normal, OverflowWrap.Normal, false)]
    [InlineData(WordBreak.KeepAll, OverflowWrap.Normal, false)]
    public void ShouldBreakLongWords_ReflectsWordBreakAndOverflowWrap(
        WordBreak wordBreak, OverflowWrap overflowWrap, bool expected)
    {
        TextWrapper.ShouldBreakLongWords(wordBreak, overflowWrap).ShouldBe(expected);
    }

    [Fact]
    public void FromStyle_WordBreakAndOverflowWrap_ResolveAndDefault()
    {
        var computed = ComputedStyle.FromStyle(new Style
        {
            WordBreak = WordBreak.BreakAll,
            OverflowWrap = OverflowWrap.BreakWord
        });
        computed.WordBreak.ShouldBe(WordBreak.BreakAll);
        computed.OverflowWrap.ShouldBe(OverflowWrap.BreakWord);

        var defaults = ComputedStyle.FromStyle(new Style());
        defaults.WordBreak.ShouldBe(WordBreak.Normal);
        defaults.OverflowWrap.ShouldBe(OverflowWrap.Normal);
    }

    [Fact]
    public void WrapText_BreakLongWords_SplitsOverlongWord()
    {
        // 一个远超行宽的连续长单词：breakLongWords 时应被拆成多行。
        var word = new string('W', 200);
        var broken = TextWrapper.WrapText(word, "Arial", 16, FontWeight.Normal, 100, WhiteSpace.Normal, breakLongWords: true);
        var notBroken = TextWrapper.WrapText(word, "Arial", 16, FontWeight.Normal, 100, WhiteSpace.Normal, breakLongWords: false);

        broken.Count.ShouldBeGreaterThan(1);   // 逐字符断行 → 多行
        notBroken.Count.ShouldBe(1);            // 默认 → 长单词整体溢出为单行
    }

    // ---- text-overflow ----

    [Fact]
    public void FromStyle_TextOverflow_ResolvesAndDefaultsToClip()
    {
        ComputedStyle.FromStyle(new Style { TextOverflow = TextOverflow.Ellipsis })
            .TextOverflow.ShouldBe(TextOverflow.Ellipsis);
        ComputedStyle.FromStyle(new Style()).TextOverflow.ShouldBe(TextOverflow.Clip);
    }
}
