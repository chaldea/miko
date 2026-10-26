using Miko.Text.Bidi;
using Shouldly;

namespace Miko.Tests.Text;

/// <summary>The UTF-16 facade over the bidi algorithm (ISSUE-157).</summary>
public class UnicodeBidiTests
{
    [Theory]
    [InlineData("Hello, world 123", false)]
    [InlineData("Привет мир", false)]
    [InlineData("你好，世界", false)]
    [InlineData("שלום", true)]
    [InlineData("abc مرحبا", true)]
    [InlineData("٣", true)]            // ARABIC-INDIC DIGIT THREE (AN)
    [InlineData("a\u202Eb", true)]       // RLO
    [InlineData("a\u2067b\u2069", true)] // RLI ... PDI
    [InlineData("\U00010800", true)]   // CYPRIOT SYLLABLE A (R, outside the BMP)
    public void Should_Detect_Text_That_May_Need_Reordering(string text, bool expected)
    {
        UnicodeBidi.MayNeedReordering(text).ShouldBe(expected);
    }

    [Theory]
    [InlineData("abc שלום", false)]
    [InlineData("שלום abc", true)]
    [InlineData("123 مرحبا", true)]
    [InlineData("\u2067\u05E9\u05DC\u05D5\u05DD\u2069 abc", false)] // an isolate is skipped (P2)
    [InlineData("123 ...", null)]
    public void Should_Find_First_Strong_Direction(string text, bool? expected)
    {
        UnicodeBidi.IsFirstStrongRightToLeft(text).ShouldBe(expected);
    }

    [Fact]
    public void Should_Resolve_Numbers_In_Rtl_Text_To_An_Even_Level()
    {
        // "אבג 123": Hebrew at level 1, the European digits at level 2 (W7 does not apply after R).
        var levels = UnicodeBidi.GetLevels("אבג 123", -1, resetTrailingWhitespace: true, out var paragraphLevel);

        paragraphLevel.ShouldBe((byte)1);
        levels.ShouldBe(new byte[] { 1, 1, 1, 1, 2, 2, 2 });
    }

    [Fact]
    public void Should_Give_Both_Halves_Of_A_Surrogate_Pair_The_Same_Level()
    {
        var levels = UnicodeBidi.GetLevels("א\U00010800b", 0, resetTrailingWhitespace: true, out _);

        levels.Length.ShouldBe(4);
        levels[1].ShouldBe(levels[2]);
        levels.ShouldBe(new byte[] { 1, 1, 1, 0 });
    }

    [Fact]
    public void Should_Reverse_Rtl_Runs_In_Visual_Order()
    {
        // abc ABC def with ABC Hebrew: the Hebrew run reads right to left inside the LTR line.
        var levels = UnicodeBidi.GetLevels("ab אבג cd", 0, resetTrailingWhitespace: true, out _);
        var order = BidiParagraph.ComputeReordering(levels);

        order.ShouldBe(new[] { 0, 1, 2, 5, 4, 3, 6, 7, 8 });
    }

    [Fact]
    public void Should_Reset_Trailing_Whitespace_To_The_Paragraph_Level()
    {
        var withReset = UnicodeBidi.GetLevels("abc  ", 1, resetTrailingWhitespace: true, out _);
        var withoutReset = UnicodeBidi.GetLevels("abc  ", 1, resetTrailingWhitespace: false, out _);

        withReset.ShouldBe(new byte[] { 2, 2, 2, 1, 1 });
        // Without L1 the trailing spaces resolve by N1/N2: between L and eos (R) → embedding level.
        withoutReset[3].ShouldBe((byte)1);
    }
}
