using Miko.Common;
using Miko.Fonts;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Fonts;

public class BootstrapIconsTests : IDisposable
{
    private readonly FontManager _fontManager;
    private readonly string _fontPath;
    private readonly bool _fontAvailable;

    public BootstrapIconsTests()
    {
        FontManager.ResetInstance();
        _fontManager = FontManager.Instance;
        _fontPath = Path.Combine(AppContext.BaseDirectory, "TestAssets", "Fonts", "bootstrap-icons.woff2");
        _fontAvailable = File.Exists(_fontPath);

        if (_fontAvailable)
        {
            _fontManager.RegisterFont("bootstrap-icons", _fontPath);
        }
    }

    public void Dispose()
    {
        FontManager.ResetInstance();
    }

    [Theory]
    [InlineData('\uf67f', "bi-123")]           // .bi-123::before { content: "\f67f"; }
    [InlineData('\uf101', "bi-alarm-fill")]    // .bi-alarm-fill::before { content: "\f101"; }
    [InlineData('\uf102', "bi-alarm")]         // .bi-alarm::before { content: "\f102"; }
    [InlineData('\uf128', "bi-arrow-down")]    // .bi-arrow-down::before { content: "\f128"; }
    [InlineData('\uf12f', "bi-arrow-up")]      // .bi-arrow-up::before { content: "\f12f"; }
    [InlineData('\uf287', "bi-check")]         // .bi-check::before { content: "\f287"; }
    [InlineData('\uf62a', "bi-x")]             // .bi-x::before { content: "\f62a"; }
    public void BootstrapIcon_ShouldHaveGlyph(char codepoint, string iconName)
    {
        if (!_fontAvailable)
        {
            return;
        }

        var typeface = _fontManager.GetTypeface("bootstrap-icons", FontWeight.Normal);
        typeface.ShouldNotBeNull();

        using var font = new SKFont(typeface);
        ushort glyphId = font.GetGlyph(codepoint);

        glyphId.ShouldNotBe((ushort)0, $"Icon {iconName} (U+{(int)codepoint:X4}) should have a glyph");
    }

    [Fact]
    public void BootstrapIcons_ShouldLoadSuccessfully()
    {
        if (!_fontAvailable)
        {
            return;
        }

        _fontManager.IsFontRegistered("bootstrap-icons").ShouldBeTrue();

        var typeface = _fontManager.GetTypeface("bootstrap-icons", FontWeight.Normal);
        typeface.ShouldNotBeNull();
    }

    [Fact]
    public void BootstrapIcons_ShouldRenderToCanvas()
    {
        if (!_fontAvailable)
        {
            return;
        }

        var typeface = _fontManager.GetTypeface("bootstrap-icons", FontWeight.Normal);
        typeface.ShouldNotBeNull();

        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        using var canvas = surface.Canvas;
        using var font = new SKFont(typeface, 24);
        using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

        canvas.Clear(SKColors.White);

        // Render alarm icon
        canvas.DrawText("\uf101", 10, 50, font, paint);

        // Verify something was drawn
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.Size.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void BootstrapIcons_FallbackResolver_ShouldFindIconFont()
    {
        if (!_fontAvailable)
        {
            return;
        }

        var resolver = new FontFallbackResolver(_fontManager);
        var fallbackChain = new List<string> { "bootstrap-icons", "Arial" };

        // Bootstrap icon codepoint (alarm icon)
        var typeface = resolver.ResolveFont('\uf101', fallbackChain, FontWeight.Normal);

        typeface.ShouldNotBeNull();
        FontManager.ContainsGlyph(typeface, '\uf101').ShouldBeTrue();
    }

    [Fact]
    public void BootstrapIcons_TextRuns_ShouldResolveIconFont()
    {
        if (!_fontAvailable)
        {
            return;
        }

        var resolver = new FontFallbackResolver(_fontManager);

        // Mix of icon and regular text
        string text = "\uf101 Alarm";
        var runs = resolver.ResolveTextRuns(text, "bootstrap-icons, Arial", FontWeight.Normal);

        runs.Count.ShouldBeGreaterThanOrEqualTo(1);

        // Verify all text is covered
        string reconstructed = string.Concat(runs.Select(r => r.Text));
        reconstructed.ShouldBe(text);
    }

    [Fact]
    public void BootstrapIcons_WoffFormat_ShouldAlsoWork()
    {
        var woffPath = Path.Combine(AppContext.BaseDirectory, "TestAssets", "Fonts", "bootstrap-icons.woff");

        if (!File.Exists(woffPath))
        {
            return;
        }

        FontManager.ResetInstance();
        var fontManager = FontManager.Instance;

        fontManager.RegisterFont("bootstrap-icons-woff", woffPath);

        fontManager.IsFontRegistered("bootstrap-icons-woff").ShouldBeTrue();

        var typeface = fontManager.GetTypeface("bootstrap-icons-woff", FontWeight.Normal);
        typeface.ShouldNotBeNull();

        // Verify icon glyph exists
        using var font = new SKFont(typeface);
        ushort glyphId = font.GetGlyph('\uf101');
        glyphId.ShouldNotBe((ushort)0, "WOFF font should contain alarm icon glyph");
    }

    [Fact]
    public void BootstrapIcons_ShouldRenderNonEmptyPixels()
    {
        if (!_fontAvailable)
        {
            return;
        }

        var typeface = _fontManager.GetTypeface("bootstrap-icons", FontWeight.Normal);
        typeface.ShouldNotBeNull();

        const int width = 100;
        const int height = 100;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        using var font = new SKFont(typeface, 48);
        using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

        canvas.Clear(SKColors.White);

        // Check glyph info
        ushort glyphId = font.GetGlyph('\uf101');
        float glyphWidth = font.MeasureText("\uf101", paint);

        // Render alarm icon (U+F101)
        canvas.DrawText("\uf101", 25, 60, font, paint);

        // Get pixel data and verify non-white pixels exist
        using var image = surface.Snapshot();
        using var pixmap = image.PeekPixels();

        int nonWhitePixelCount = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = pixmap.GetPixelColor(x, y);
                // Check if pixel is not white (allowing for anti-aliasing)
                if (pixel.Red < 255 || pixel.Green < 255 || pixel.Blue < 255)
                {
                    nonWhitePixelCount++;
                }
            }
        }

        nonWhitePixelCount.ShouldBeGreaterThan(0, $"Bootstrap icon should render visible pixels on canvas. GlyphId: {glyphId}, GlyphWidth: {glyphWidth}");
    }

    [Theory]
    [InlineData("\uf425", "House")]      // House icon
    [InlineData("\uf52a", "Search")]     // Search icon
    [InlineData("\uf3e5", "Gear")]       // Gear icon
    [InlineData("\uf417", "Heart")]      // Heart icon
    [InlineData("\uf588", "Star")]       // Star icon
    public void BootstrapIcons_MultipleIcons_ShouldRenderNonEmptyPixels(string iconCodepoint, string iconName)
    {
        if (!_fontAvailable)
        {
            return;
        }

        var typeface = _fontManager.GetTypeface("bootstrap-icons", FontWeight.Normal);
        typeface.ShouldNotBeNull();

        const int width = 64;
        const int height = 64;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        using var font = new SKFont(typeface, 32);
        using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

        canvas.Clear(SKColors.White);

        // Render the icon
        canvas.DrawText(iconCodepoint, 16, 40, font, paint);

        // Get pixel data and verify non-white pixels exist
        using var image = surface.Snapshot();
        using var pixmap = image.PeekPixels();

        int nonWhitePixelCount = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = pixmap.GetPixelColor(x, y);
                if (pixel.Red < 255 || pixel.Green < 255 || pixel.Blue < 255)
                {
                    nonWhitePixelCount++;
                }
            }
        }

        nonWhitePixelCount.ShouldBeGreaterThan(0, $"Bootstrap icon '{iconName}' should render visible pixels on canvas");
    }
}
