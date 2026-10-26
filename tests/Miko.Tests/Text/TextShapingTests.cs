using HarfBuzzSharp;
using Miko.Common;
using Miko.Fonts;
using Miko.Text;
using Miko.Utils;
using Shouldly;
using SkiaSharp;
using Direction = Miko.Common.Direction;

namespace Miko.Tests.Text;

/// <summary>
/// HarfBuzz shaping and the shaped-line pipeline (ISSUE-157), against subsets of Noto Sans Arabic /
/// Hebrew bundled under TestAssets/Fonts/Noto so the glyph-level results do not depend on the
/// machine's fonts. The two families are registered for the duration of the class.
/// </summary>
[Collection(Miko.Tests.GlobalFontStateCollection.Name)] // registers fonts (process-wide state)
public class TextShapingTests : IDisposable
{
    private const string Arabic = "MikoTestArabic";
    private const string Hebrew = "MikoTestHebrew";

    private static readonly string FontDir = Path.Combine(AppContext.BaseDirectory, "TestAssets", "Fonts", "Noto");
    private static readonly SKTypeface ArabicTypeface = SKTypeface.FromFile(Path.Combine(FontDir, "NotoSansArabic-Subset.ttf"));
    private static readonly SKTypeface HebrewTypeface = SKTypeface.FromFile(Path.Combine(FontDir, "NotoSansHebrew-Subset.ttf"));

    public TextShapingTests()
    {
        FontManager.Instance.RegisterFont(Arabic, Path.Combine(FontDir, "NotoSansArabic-Subset.ttf"));
        FontManager.Instance.RegisterFont(Hebrew, Path.Combine(FontDir, "NotoSansHebrew-Subset.ttf"));
    }

    public void Dispose()
    {
        FontManager.Instance.UnregisterFont(Arabic);
        FontManager.Instance.UnregisterFont(Hebrew);
    }

    [Fact]
    public void Should_Apply_Arabic_Contextual_Forms()
    {
        // A BEH between two others takes its medial form; alone it is isolated. (Noto draws BEH as a
        // dotless base plus a dot mark, so each character may produce more than one glyph.)
        var isolated = HarfBuzzShaper.Shape(ArabicTypeface, "\u0628", 0, 1, rightToLeft: true, Script.Arabic);
        var joined = HarfBuzzShaper.Shape(ArabicTypeface, "\u0628\u0628\u0628", 0, 3, rightToLeft: true, Script.Arabic);

        var medial = joined.Glyphs.Where((_, i) => joined.Clusters[i] == 1).ToArray();
        medial.ShouldNotBeEmpty();
        medial.ShouldNotBe(isolated.Glyphs, "the medial form is a different glyph from the isolated one");
        joined.Glyphs.ShouldNotContain(ArabicTypeface.GetGlyph(0x0628), "no character keeps its nominal cmap glyph");
    }

    [Fact]
    public void Should_Output_Rtl_Glyphs_In_Visual_Order()
    {
        // Right-to-left buffers come out left to right on screen: the last character first.
        const string text = "שלום";
        var shaped = HarfBuzzShaper.Shape(HebrewTypeface, text, 0, text.Length, rightToLeft: true, Script.Hebrew);

        shaped.Clusters.ShouldBe(new[] { 3, 2, 1, 0 });
    }

    [Fact]
    public void Should_Attach_Marks_To_Their_Base()
    {
        // BET + QAMATS: one cluster; the mark adds no advance.
        const string text = "בָ";
        var shaped = HarfBuzzShaper.Shape(HebrewTypeface, text, 0, text.Length, rightToLeft: true, Script.Hebrew);

        shaped.Clusters.ShouldAllBe(c => c == 0);
        shaped.Advances.Count(a => a != 0).ShouldBe(1);
    }

    [Fact]
    public void Should_Shape_Arabic_Narrower_Than_Its_Nominal_Glyphs()
    {
        const string text = "بببب";
        using var font = new SKFont(ArabicTypeface, 20);
        float nominal = font.MeasureText(text);

        float measured = TextMeasurer.MeasureTextWidth(text, Arabic, 20, FontWeight.Normal);
        var shaped = TextShaping.Shape(text, Arabic, 20, FontWeight.Normal, Direction.Rtl);

        measured.ShouldBe(shaped.Width, 0.01f);
        measured.ShouldBeLessThan(nominal, "joined forms are narrower than four isolated BEHs");
    }

    [Fact]
    public void Should_Reorder_An_Rtl_Run_Inside_Ltr_Text()
    {
        // The Hebrew subset covers the spaces but not Latin, so "ab" and "cd" fall back to Arial.
        const string text = "ab \u05E9\u05DC\u05D5\u05DD cd";
        var shaped = TextShaping.Shape(text, $"{Hebrew}, Arial", 16, FontWeight.Normal, Direction.Ltr);

        // Visual order: "ab", " ", the Hebrew word read right to left, " ", "cd".
        shaped.Runs.Select(r => r.Start).ShouldBe(new[] { 0, 2, 3, 7, 8 });
        var hebrewRun = shaped.Runs[2];
        hebrewRun.IsRightToLeft.ShouldBeTrue();
        hebrewRun.Clusters.ShouldBe(new[] { 6, 5, 4, 3 });
        shaped.Runs.Select(r => r.X).ShouldBe(shaped.Runs.Select(r => r.X).OrderBy(x => x));
    }

    [Fact]
    public void Should_Put_Trailing_Punctuation_On_The_Left_In_An_Rtl_Paragraph()
    {
        // "abc!" in an RTL paragraph: "abc" is level 2, "!" level 1 — the "!" is drawn first (left).
        var shaped = TextShaping.Shape("abc!", "Arial", 16, FontWeight.Normal, Direction.Rtl);

        shaped.IsRightToLeft.ShouldBeTrue();
        shaped.Runs.First().Start.ShouldBe(3);
        shaped.Runs.Last().Start.ShouldBe(0);
    }

    [Fact]
    public void Should_Not_Reorder_Plain_Ltr_Text()
    {
        var shaped = TextShaping.Shape("Hello world", "Arial", 16, FontWeight.Normal, Direction.Ltr);

        shaped.Runs.ShouldAllBe(r => !r.IsRightToLeft);
        shaped.Width.ShouldBe(TextMeasurer.MeasureTextWidth("Hello world", "Arial", 16, FontWeight.Normal), 0.01f);
    }

    [Fact]
    public void Should_Place_The_Caret_At_The_Right_Edge_At_The_Start_Of_Rtl_Text()
    {
        var shaped = TextShaping.Shape("שלום", Hebrew, 16, FontWeight.Normal, Direction.Rtl);

        shaped.GetCaretX(0).ShouldBe(shaped.Width, 0.01f);
        shaped.GetCaretX(4).ShouldBe(0, 0.01f);
        var middle = shaped.GetCaretX(2);
        middle.ShouldBeGreaterThan(0);
        middle.ShouldBeLessThan(shaped.Width);
        // Moving the caret forward moves it left.
        shaped.GetCaretX(1).ShouldBeGreaterThan(middle);
    }

    [Fact]
    public void Should_Measure_The_Caret_Like_The_Prefix_Width_In_Simple_Ltr_Text()
    {
        // The input scroll offset, caret and IME rectangle all use this; for plain LTR text it must
        // equal the width before the caret, which is what they used before.
        float caret = TextMeasurer.MeasureCaretOffset("Hello", 2, "Arial", 16, FontWeight.Normal);

        caret.ShouldBe(TextMeasurer.MeasureTextWidth("He", "Arial", 16, FontWeight.Normal), 0.01f);
    }

    [Fact]
    public void Should_Measure_The_Rtl_Caret_From_The_Left_Edge()
    {
        const string text = "שלום";
        float width = TextMeasurer.MeasureTextWidth(text, Hebrew, 16, FontWeight.Normal);

        TextMeasurer.MeasureCaretOffset(text, 0, Hebrew, 16, FontWeight.Normal, Direction.Rtl).ShouldBe(width, 0.01f);
        TextMeasurer.MeasureCaretOffset(text, text.Length, Hebrew, 16, FontWeight.Normal, Direction.Rtl).ShouldBe(0, 0.01f);
    }

    [Fact]
    public void Should_Apply_Letter_Spacing_Per_Utf16_Unit()
    {
        var plain = TextShaping.Shape("بببب", Arabic, 20, FontWeight.Normal, Direction.Rtl);
        var spaced = TextShaping.Shape("بببب", Arabic, 20, FontWeight.Normal, Direction.Rtl, letterSpacing: 2);

        spaced.Width.ShouldBe(plain.Width + 4 * 2, 0.01f);
    }

    [Fact]
    public void Should_Mirror_Brackets_In_Rtl_Runs()
    {
        // "(alef)" in an RTL paragraph: the brackets resolve to R (N0) and are drawn mirrored, so the
        // glyph on the left is the one for ')' ... which, mirrored, looks like '('.
        var shaped = TextShaping.Shape("(\u05D0)", $"{Hebrew}, Arial", 16, FontWeight.Normal, Direction.Rtl);
        var arial = FontManager.Instance.GetTypeface("Arial", FontWeight.Normal)!;
        ushort open = arial.GetGlyph('(');

        var leftmost = shaped.Runs.First();
        leftmost.Clusters[0].ShouldBe(2);           // logically the closing bracket
        leftmost.Glyphs[0].ShouldBe(open);          // drawn with the opening bracket's glyph
    }

    [Fact]
    public void Should_Shape_An_Emoji_Zwj_Sequence_As_One_Glyph_When_The_Font_Supports_It()
    {
        var emoji = SKFontManager.Default.MatchCharacter(0x1F468);
        // Machines without a colour emoji font have nothing to ligate.
        if (emoji == null || !emoji.FamilyName.Contains("Emoji", StringComparison.OrdinalIgnoreCase)) return;

        const string family = "\U0001F468‍\U0001F469‍\U0001F467";
        var shaped = TextShaping.Shape(family, "Arial", 16, FontWeight.Normal, Direction.Ltr);

        shaped.Runs.Sum(r => r.Glyphs.Length).ShouldBe(1);
    }
}
