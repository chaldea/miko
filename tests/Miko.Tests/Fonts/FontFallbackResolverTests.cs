using Miko.Common;
using Miko.Fonts;
using Shouldly;

namespace Miko.Tests.Fonts;

public class FontFallbackResolverTests : IDisposable
{
    public FontFallbackResolverTests()
    {
        FontManager.ResetInstance();
    }

    public void Dispose()
    {
        FontManager.ResetInstance();
    }

    [Fact]
    public void GetCharacterScript_LatinCharacter_ShouldReturnLatin()
    {
        FontFallbackResolver.GetCharacterScript('A').ShouldBe(UnicodeScript.Latin);
        FontFallbackResolver.GetCharacterScript('z').ShouldBe(UnicodeScript.Latin);
        FontFallbackResolver.GetCharacterScript('0').ShouldBe(UnicodeScript.Latin);
    }

    [Fact]
    public void GetCharacterScript_ChineseCharacter_ShouldReturnCJK()
    {
        FontFallbackResolver.GetCharacterScript('你').ShouldBe(UnicodeScript.CJK);
        FontFallbackResolver.GetCharacterScript('好').ShouldBe(UnicodeScript.CJK);
        FontFallbackResolver.GetCharacterScript('中').ShouldBe(UnicodeScript.CJK);
        FontFallbackResolver.GetCharacterScript('国').ShouldBe(UnicodeScript.CJK);
    }

    [Fact]
    public void GetCharacterScript_JapaneseHiragana_ShouldReturnCJK()
    {
        FontFallbackResolver.GetCharacterScript('あ').ShouldBe(UnicodeScript.CJK);
        FontFallbackResolver.GetCharacterScript('い').ShouldBe(UnicodeScript.CJK);
    }

    [Fact]
    public void GetCharacterScript_JapaneseKatakana_ShouldReturnCJK()
    {
        FontFallbackResolver.GetCharacterScript('ア').ShouldBe(UnicodeScript.CJK);
        FontFallbackResolver.GetCharacterScript('イ').ShouldBe(UnicodeScript.CJK);
    }

    [Fact]
    public void GetCharacterScript_KoreanHangul_ShouldReturnCJK()
    {
        FontFallbackResolver.GetCharacterScript('한').ShouldBe(UnicodeScript.CJK);
        FontFallbackResolver.GetCharacterScript('글').ShouldBe(UnicodeScript.CJK);
    }

    [Fact]
    public void GetCharacterScript_CyrillicCharacter_ShouldReturnCyrillic()
    {
        FontFallbackResolver.GetCharacterScript('А').ShouldBe(UnicodeScript.Cyrillic);
        FontFallbackResolver.GetCharacterScript('Б').ShouldBe(UnicodeScript.Cyrillic);
    }

    [Fact]
    public void GetCharacterScript_ArabicCharacter_ShouldReturnArabic()
    {
        FontFallbackResolver.GetCharacterScript('ا').ShouldBe(UnicodeScript.Arabic);
        FontFallbackResolver.GetCharacterScript('ب').ShouldBe(UnicodeScript.Arabic);
    }

    [Fact]
    public void GetCharacterScript_PrivateUseArea_ShouldReturnSymbol()
    {
        // Bootstrap icons use Private Use Area
        FontFallbackResolver.GetCharacterScript('\uF101').ShouldBe(UnicodeScript.Symbol);
        FontFallbackResolver.GetCharacterScript('\uF67F').ShouldBe(UnicodeScript.Symbol);
    }

    [Fact]
    public void ResolveTextRuns_EmptyText_ShouldReturnEmptyList()
    {
        var fontManager = FontManager.Instance;
        var resolver = new FontFallbackResolver(fontManager);

        var runs = resolver.ResolveTextRuns("", "Arial", FontWeight.Normal);

        runs.ShouldBeEmpty();
    }

    [Fact]
    public void ResolveTextRuns_NullText_ShouldReturnEmptyList()
    {
        var fontManager = FontManager.Instance;
        var resolver = new FontFallbackResolver(fontManager);

        var runs = resolver.ResolveTextRuns(null!, "Arial", FontWeight.Normal);

        runs.ShouldBeEmpty();
    }

    [Fact]
    public void ResolveTextRuns_SimpleLatinText_ShouldReturnSingleRun()
    {
        var fontManager = FontManager.Instance;
        var resolver = new FontFallbackResolver(fontManager);

        var runs = resolver.ResolveTextRuns("Hello World", "Arial", FontWeight.Normal);

        runs.Count.ShouldBeGreaterThanOrEqualTo(1);

        // Verify all text is covered
        string reconstructed = string.Concat(runs.Select(r => r.Text));
        reconstructed.ShouldBe("Hello World");
    }

    [Fact]
    public void ResolveTextRuns_MixedChineseEnglish_ShouldCoverAllText()
    {
        var fontManager = FontManager.Instance;
        var resolver = new FontFallbackResolver(fontManager);

        string text = "Hello 你好 World 世界";
        var runs = resolver.ResolveTextRuns(text, "Arial, Microsoft YaHei", FontWeight.Normal);

        runs.Count.ShouldBeGreaterThanOrEqualTo(1);

        // Verify all text is covered
        string reconstructed = string.Concat(runs.Select(r => r.Text));
        reconstructed.ShouldBe(text);
    }

    [Fact]
    public void ResolveTextRuns_AllRunsHaveTypeface()
    {
        var fontManager = FontManager.Instance;
        var resolver = new FontFallbackResolver(fontManager);

        var runs = resolver.ResolveTextRuns("Test 测试", "Arial", FontWeight.Normal);

        foreach (var run in runs)
        {
            run.Typeface.ShouldNotBeNull();
            run.Text.ShouldNotBeNullOrEmpty();
            run.Length.ShouldBeGreaterThan(0);
        }
    }

    [Fact]
    public void ResolveFont_LatinCharacter_ShouldReturnTypeface()
    {
        var fontManager = FontManager.Instance;
        var resolver = new FontFallbackResolver(fontManager);
        var fallbackChain = new List<string> { "Arial" };

        var typeface = resolver.ResolveFont('A', fallbackChain, FontWeight.Normal);

        typeface.ShouldNotBeNull();
    }
}
