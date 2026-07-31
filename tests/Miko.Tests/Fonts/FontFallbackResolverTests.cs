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

    [Fact]
    public void ResolveFont_CjkCharacter_MustReturnGlyphCapableTypeface()
    {
        // 回归（ISSUE-110 后续 / macOS CI）：SKTypeface.FromFamilyName 在字体名不存在时
        // 静默返回系统默认字体；此前 GetSystemFallbackForScript 不校验字形，会把不含
        // CJK 字形的默认字体当作"系统回退"返回，中文全部按 .notdef 宽度测量。
        // 修复后：回退链必须返回真正覆盖该码点的字体（Windows 微软雅黑 /
        // macOS PingFang SC / Linux Noto Sans CJK 或 MatchCharacter 匹配结果）。
        // 无 CJK 字体的极简环境跳过（xUnit 2 无动态 Skip）。
        bool systemHasCjkFont = SkiaSharp.SKFontManager.Default.MatchCharacter(
            null,
            SkiaSharp.SKFontStyleWeight.Normal,
            SkiaSharp.SKFontStyleWidth.Normal,
            SkiaSharp.SKFontStyleSlant.Upright,
            null,
            '桌') != null;
        if (!systemHasCjkFont)
        {
            return;
        }

        var fontManager = FontManager.Instance;
        var resolver = new FontFallbackResolver(fontManager);

        // 主链给一个必然不存在的字体名，强制走系统回退路径。
        var typeface = resolver.ResolveFont('桌', new List<string> { "DefinitelyMissingFont12345" }, FontWeight.Normal);

        typeface.ShouldNotBeNull();
        FontManager.ContainsGlyph(typeface, '桌').ShouldBeTrue(
            "系统装有 CJK 字体时，回退结果必须真正包含该汉字字形，而不是缺字形的默认字体");
    }
}
