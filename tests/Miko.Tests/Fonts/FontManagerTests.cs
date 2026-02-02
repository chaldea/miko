using Miko.Common;
using Miko.Fonts;
using Shouldly;

namespace Miko.Tests.Fonts;

public class FontManagerTests : IDisposable
{
    public FontManagerTests()
    {
        // Reset singleton for each test
        FontManager.ResetInstance();
    }

    public void Dispose()
    {
        FontManager.ResetInstance();
    }

    [Fact]
    public void ParseFontFamilyChain_SingleFont_ShouldReturnSingleItem()
    {
        var fontManager = FontManager.Instance;

        var chain = fontManager.ParseFontFamilyChain("Arial");

        chain.Count.ShouldBe(1);
        chain[0].ShouldBe("Arial");
    }

    [Fact]
    public void ParseFontFamilyChain_MultipleFonts_ShouldReturnList()
    {
        var fontManager = FontManager.Instance;

        var chain = fontManager.ParseFontFamilyChain("Font1, Font2, Arial");

        chain.Count.ShouldBe(3);
        chain[0].ShouldBe("Font1");
        chain[1].ShouldBe("Font2");
        chain[2].ShouldBe("Arial");
    }

    [Fact]
    public void ParseFontFamilyChain_QuotedFonts_ShouldTrimQuotes()
    {
        var fontManager = FontManager.Instance;

        var chain = fontManager.ParseFontFamilyChain("\"Font Name\", 'Another Font', Arial");

        chain.Count.ShouldBe(3);
        chain[0].ShouldBe("Font Name");
        chain[1].ShouldBe("Another Font");
        chain[2].ShouldBe("Arial");
    }

    [Fact]
    public void ParseFontFamilyChain_EmptyString_ShouldReturnDefaultFont()
    {
        var fontManager = FontManager.Instance;

        var chain = fontManager.ParseFontFamilyChain("");

        chain.Count.ShouldBe(1);
        chain[0].ShouldBe("Arial");
    }

    [Fact]
    public void ParseFontFamilyChain_NullString_ShouldReturnDefaultFont()
    {
        var fontManager = FontManager.Instance;

        var chain = fontManager.ParseFontFamilyChain(null!);

        chain.Count.ShouldBe(1);
        chain[0].ShouldBe("Arial");
    }

    [Fact]
    public void GetTypeface_SystemFont_ShouldReturnTypeface()
    {
        var fontManager = FontManager.Instance;

        var typeface = fontManager.GetTypeface("Arial", FontWeight.Normal);

        typeface.ShouldNotBeNull();
    }

    [Fact]
    public void GetTypeface_NonExistentFont_ShouldFallbackToSystem()
    {
        var fontManager = FontManager.Instance;

        var typeface = fontManager.GetTypeface("NonExistentFontXYZ123", FontWeight.Normal);

        typeface.ShouldNotBeNull();
    }

    [Fact]
    public void IsFontRegistered_UnregisteredFont_ShouldReturnFalse()
    {
        var fontManager = FontManager.Instance;

        fontManager.IsFontRegistered("UnregisteredFont").ShouldBeFalse();
    }

    [Fact]
    public void RegisterFont_FromByteArray_ShouldSucceed()
    {
        var fontManager = FontManager.Instance;
        var fontPath = Path.Combine(AppContext.BaseDirectory, "TestAssets", "Fonts", "bootstrap-icons.woff2");

        if (!File.Exists(fontPath))
        {
            // Skip test if font file not available
            return;
        }

        byte[] fontData = File.ReadAllBytes(fontPath);
        fontManager.RegisterFont("bootstrap-icons", fontData);

        fontManager.IsFontRegistered("bootstrap-icons").ShouldBeTrue();
    }

    [Fact]
    public void RegisterFont_FromFilePath_ShouldSucceed()
    {
        var fontManager = FontManager.Instance;
        var fontPath = Path.Combine(AppContext.BaseDirectory, "TestAssets", "Fonts", "bootstrap-icons.woff2");

        if (!File.Exists(fontPath))
        {
            // Skip test if font file not available
            return;
        }

        fontManager.RegisterFont("bootstrap-icons", fontPath);

        fontManager.IsFontRegistered("bootstrap-icons").ShouldBeTrue();
    }

    [Fact]
    public void GetTypeface_RegisteredFont_ShouldReturnRegisteredTypeface()
    {
        var fontManager = FontManager.Instance;
        var fontPath = Path.Combine(AppContext.BaseDirectory, "TestAssets", "Fonts", "bootstrap-icons.woff2");

        if (!File.Exists(fontPath))
        {
            return;
        }

        fontManager.RegisterFont("bootstrap-icons", fontPath);

        var typeface = fontManager.GetTypeface("bootstrap-icons", FontWeight.Normal);

        typeface.ShouldNotBeNull();
    }

    [Fact]
    public void UnregisterFont_RegisteredFont_ShouldRemoveFont()
    {
        var fontManager = FontManager.Instance;
        var fontPath = Path.Combine(AppContext.BaseDirectory, "TestAssets", "Fonts", "bootstrap-icons.woff2");

        if (!File.Exists(fontPath))
        {
            return;
        }

        fontManager.RegisterFont("bootstrap-icons", fontPath);
        fontManager.IsFontRegistered("bootstrap-icons").ShouldBeTrue();

        fontManager.UnregisterFont("bootstrap-icons");

        fontManager.IsFontRegistered("bootstrap-icons").ShouldBeFalse();
    }

    [Fact]
    public void ClearCache_ShouldNotThrow()
    {
        var fontManager = FontManager.Instance;

        // Get some typefaces to populate cache
        fontManager.GetTypeface("Arial", FontWeight.Normal);
        fontManager.GetTypeface("Arial", FontWeight.Bold);

        Should.NotThrow(() => fontManager.ClearCache());
    }
}
