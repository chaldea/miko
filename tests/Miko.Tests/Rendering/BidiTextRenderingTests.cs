using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// Bidi and shaped text reach the canvas (ISSUE-157): right-to-left paragraphs paint on the right,
/// shaped scripts paint at all, and an overflowing RTL line is truncated with an ellipsis.
/// </summary>
public class BidiTextRenderingTests : IDisposable
{
    private const int Width = 300;
    private const int Height = 40;

    private readonly SKBitmap _bitmap = new(Width, Height);
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine = new();
    private readonly LayoutEngine _layoutEngine = new();

    public BidiTextRenderingTests()
    {
        _canvas = new SKCanvas(_bitmap);
        _renderEngine.SetCanvas(_canvas);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private void Render(string text, Direction direction)
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".box"] = new()
            {
                Display = Display.Block,
                Width = Length.Px(Width),
                Height = Length.Px(Height),
                FontSize = Length.Px(20),
                Color = Color.White,
                Direction = direction,
            },
        });
        var box = new DivElement { Class = "box" };
        box.AddChild(new TextNode(text));

        _canvas.Clear(SKColors.Black);
        _renderEngine.Render(_layoutEngine.Layout(box, [sheet], Width, Height));
    }

    private (int MinX, int MaxX) InkExtent()
    {
        int minX = int.MaxValue, maxX = int.MinValue;
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            if (_bitmap.GetPixel(x, y).Red <= 128) continue;
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
        }
        return (minX, maxX);
    }

    [Fact]
    public void Should_Paint_An_Rtl_Paragraph_Against_The_Right_Edge()
    {
        Render("שלום", Direction.Rtl);

        var (minX, maxX) = InkExtent();
        minX.ShouldBeLessThan(int.MaxValue, "the text is painted");
        maxX.ShouldBeGreaterThan(Width - 8);
        minX.ShouldBeGreaterThan(Width / 2);
    }

    [Fact]
    public void Should_Paint_The_Same_Text_On_The_Left_In_An_Ltr_Paragraph()
    {
        Render("שלום", Direction.Ltr);

        var (minX, maxX) = InkExtent();
        minX.ShouldBeLessThan(8);
        maxX.ShouldBeLessThan(Width / 2);
    }

    [Fact]
    public void Should_Paint_Shaped_Arabic_Text()
    {
        Render("مرحبا بالعالم", Direction.Rtl);

        var (minX, maxX) = InkExtent();
        minX.ShouldBeLessThan(int.MaxValue, "shaped glyphs are drawn through the text blob path");
        (maxX - minX).ShouldBeGreaterThan(20);
    }

    [Fact]
    public void Should_Truncate_Overflowing_Shaped_Text_To_The_Box_With_An_Ellipsis()
    {
        var text = string.Concat(Enumerable.Repeat("שלום עולם ", 20));
        var full = Miko.Text.TextShaping.Shape(text, "Arial", 20, FontWeight.Normal, Direction.Rtl);

        var truncated = Painter.TruncateWithEllipsis(text, 200, "Arial", 20, FontWeight.Normal, Direction.Rtl, 0);

        full.Width.ShouldBeGreaterThan(200);
        truncated.Width.ShouldBeLessThanOrEqualTo(200);
        truncated.Width.ShouldBeGreaterThan(150, "it keeps as much text as fits");
    }
}
