using Miko.Common;
using Miko.Fonts;
using Miko.Text;
using Miko.Utils;
using Shouldly;

namespace Miko.Tests.Text;

/// <summary>
/// A grapheme cluster is never split (ISSUE-157): not across fonts by fallback, not across lines by
/// breaking, and complex text is routed to the shaper.
/// </summary>
public class ClusterIntegrityTests
{
    [Theory]
    [InlineData("Hello, world", true)]
    [InlineData("Ελληνικά Кириллица", true)]
    [InlineData("中文かなカナ한국어", true)]
    [InlineData("—“quoted”… €5 → ★", true)]
    [InlineData("e\u0301", false)]   // e + COMBINING ACUTE: the mark must be positioned by the shaper
    [InlineData("مرحبا", false)]      // Arabic: contextual forms
    [InlineData("שלום", false)]       // Hebrew: right-to-left
    [InlineData("नमस्ते", false)]      // Devanagari: conjuncts and reordering
    [InlineData("สวัสดี", false)]     // Thai: stacking marks
    [InlineData("\U0001F600", false)] // outside the BMP
    [InlineData("a\u200Db", false)]   // ZWJ
    [InlineData("\u304B\u3099", false)]   // kana + COMBINING VOICED SOUND MARK
    public void Should_Classify_Simple_Text(string text, bool expected)
    {
        TextComplexity.IsSimple(text).ShouldBe(expected);
    }

    [Fact]
    public void Should_Keep_A_Surrogate_Pair_In_One_Run()
    {
        var resolver = new FontFallbackResolver(FontManager.Instance);
        var runs = resolver.ResolveTextRuns("a\U0001F600b", "Arial", FontWeight.Normal);

        string.Concat(runs.Select(r => r.Text)).ShouldBe("a\U0001F600b");
        foreach (var run in runs)
        {
            // No run starts or ends inside the pair.
            char.IsLowSurrogate(run.Text[0]).ShouldBeFalse();
            char.IsHighSurrogate(run.Text[^1]).ShouldBeFalse();
        }
    }

    [Fact]
    public void Should_Keep_A_Combining_Mark_In_Its_Base_Font()
    {
        var resolver = new FontFallbackResolver(FontManager.Instance);
        var runs = resolver.ResolveTextRuns("xe\u0301", "Arial", FontWeight.Normal);

        var markRun = runs.Single(r => r.StartIndex <= 2 && 2 < r.StartIndex + r.Length);
        (markRun.StartIndex <= 1).ShouldBeTrue("the mark shares a run with its base 'e'");
    }

    [Theory]
    [InlineData(0x0301, true)]   // combining acute
    [InlineData(0xFE0F, true)]   // VS16
    [InlineData(0x200D, true)]   // ZWJ
    [InlineData(0x1F3FB, true)]  // skin tone modifier
    [InlineData(0x0041, false)]
    [InlineData(0x1F600, false)]
    public void Should_Recognise_Cluster_Continuations(int codePoint, bool expected)
    {
        FontFallbackResolver.IsClusterContinuation(codePoint).ShouldBe(expected);
    }

    [Fact]
    public void Should_Not_Break_Cjk_Text_Before_A_Combining_Mark()
    {
        // か + U+3099: two UTF-16 units, one cluster, so one break unit.
        var units = TextWrapper.SplitBreakUnits("\u3042\u304B\u3099\u3055");

        units.Select(u => u.Text).ShouldBe(new[] { "\u3042", "\u304B\u3099", "\u3055" });
    }

    [Fact]
    public void Should_Not_Break_Inside_A_Surrogate_Pair()
    {
        // 𠀋 (CJK Extension B) is a surrogate pair.
        var units = TextWrapper.SplitBreakUnits("中\U0002000B文");

        foreach (var unit in units)
        {
            char.IsHighSurrogate(unit.Text[^1]).ShouldBeFalse();
            char.IsLowSurrogate(unit.Text[0]).ShouldBeFalse();
        }
        string.Concat(units.Select(u => u.Text)).ShouldBe("中\U0002000B文");
    }

    [Fact]
    public void Should_Break_A_Long_Word_Only_Between_Clusters()
    {
        // Forced character breaking of a word of emoji: every line holds whole emoji.
        var word = string.Concat(Enumerable.Repeat("\U0001F600", 12));
        var lines = TextWrapper.WrapText(word, "Arial", 16, FontWeight.Normal, 40, WhiteSpace.Normal, breakLongWords: true);

        lines.Count.ShouldBeGreaterThan(1);
        string.Concat(lines).ShouldBe(word);
        foreach (var line in lines)
        {
            (line.Length % 2).ShouldBe(0);
            char.IsHighSurrogate(line[^1]).ShouldBeFalse();
        }
    }
}
