using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

public class BackgroundRenderingTests : IDisposable
{
    private readonly SKBitmap _canvasBitmap;
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine;
    private readonly LayoutEngine _layoutEngine;

    public BackgroundRenderingTests()
    {
        _canvasBitmap = new SKBitmap(200, 200);
        _canvas = new SKCanvas(_canvasBitmap);
        _canvas.Clear(SKColors.White);
        _renderEngine = new RenderEngine();
        _renderEngine.SetCanvas(_canvas);
        _layoutEngine = new LayoutEngine();
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _canvasBitmap.Dispose();
    }

    [Fact]
    public void BackgroundImage_NoRepeat_ShouldDrawOnce()
    {
        var bg = BackgroundImage.FromBitmap(CreateTestBitmap(20, 20, SKColors.Red));
        bg.Repeat = BackgroundRepeat.NoRepeat;

        var root = CreateRootWithBackground(bg, 100, 100);
        RenderElement(root);

        GetPixelColor(10, 10).ShouldBe(SKColors.Red);
        GetPixelColor(50, 50).ShouldBe(SKColors.White);
    }

    [Fact]
    public void BackgroundImage_Repeat_ShouldTile()
    {
        var bg = BackgroundImage.FromBitmap(CreateTestBitmap(20, 20, SKColors.Blue));
        bg.Repeat = BackgroundRepeat.Repeat;

        var root = CreateRootWithBackground(bg, 100, 100);
        RenderElement(root);

        GetPixelColor(10, 10).ShouldBe(SKColors.Blue);
        GetPixelColor(30, 30).ShouldBe(SKColors.Blue);
        GetPixelColor(70, 70).ShouldBe(SKColors.Blue);
        GetPixelColor(90, 90).ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void BackgroundImage_RepeatX_ShouldTileHorizontallyOnly()
    {
        var bg = BackgroundImage.FromBitmap(CreateTestBitmap(20, 20, SKColors.Green));
        bg.Repeat = BackgroundRepeat.RepeatX;

        var root = CreateRootWithBackground(bg, 100, 100);
        RenderElement(root);

        GetPixelColor(10, 10).ShouldBe(SKColors.Green);
        GetPixelColor(50, 10).ShouldBe(SKColors.Green);
        GetPixelColor(10, 50).ShouldBe(SKColors.White);
    }

    [Fact]
    public void BackgroundImage_RepeatY_ShouldTileVerticallyOnly()
    {
        var bg = BackgroundImage.FromBitmap(CreateTestBitmap(20, 20, SKColors.Green));
        bg.Repeat = BackgroundRepeat.RepeatY;

        var root = CreateRootWithBackground(bg, 100, 100);
        RenderElement(root);

        GetPixelColor(10, 10).ShouldBe(SKColors.Green);
        GetPixelColor(10, 50).ShouldBe(SKColors.Green);
        GetPixelColor(50, 10).ShouldBe(SKColors.White);
    }

    [Fact]
    public void BackgroundImage_NoRepeat_Center_ShouldPositionInCenter()
    {
        var bg = BackgroundImage.FromBitmap(CreateTestBitmap(20, 20, SKColors.Red));
        bg.Repeat = BackgroundRepeat.NoRepeat;
        bg.Position = BackgroundPosition.Center;

        var root = CreateRootWithBackground(bg, 100, 100);
        RenderElement(root);

        GetPixelColor(50, 50).ShouldBe(SKColors.Red);
        GetPixelColor(5, 5).ShouldBe(SKColors.White);
    }

    [Fact]
    public void BackgroundImage_SizeContain_ShouldFitWithinArea()
    {
        var bg = BackgroundImage.FromBitmap(CreateTestBitmap(40, 20, SKColors.Red));
        bg.Repeat = BackgroundRepeat.NoRepeat;
        bg.Size = BackgroundSize.Contain;

        var root = CreateRootWithBackground(bg, 100, 100);
        RenderElement(root);

        GetPixelColor(50, 30).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void BackgroundImage_SizeCover_ShouldCoverEntireArea()
    {
        var bg = BackgroundImage.FromBitmap(CreateTestBitmap(40, 20, SKColors.Red));
        bg.Repeat = BackgroundRepeat.NoRepeat;
        bg.Size = BackgroundSize.Cover;

        var root = CreateRootWithBackground(bg, 100, 100);
        RenderElement(root);

        GetPixelColor(10, 10).ShouldBe(SKColors.Red);
        GetPixelColor(90, 90).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void BackgroundImage_ExplicitSize_ShouldScaleToSpecifiedDimensions()
    {
        var bg = BackgroundImage.FromBitmap(CreateTestBitmap(40, 40, SKColors.Red));
        bg.Repeat = BackgroundRepeat.NoRepeat;
        bg.Size = BackgroundSize.Px(16, 16);

        var root = CreateRootWithBackground(bg, 100, 100);
        RenderElement(root);

        GetPixelColor(8, 8).ShouldBe(SKColors.Red);
        GetPixelColor(50, 50).ShouldBe(SKColors.White);
    }

    [Fact]
    public void BackgroundImage_ExplicitSize_WithRepeat_ShouldTileAtSpecifiedSize()
    {
        var bg = BackgroundImage.FromBitmap(CreateTestBitmap(40, 40, SKColors.Blue));
        bg.Repeat = BackgroundRepeat.Repeat;
        bg.Size = BackgroundSize.Px(20, 20);

        var root = CreateRootWithBackground(bg, 100, 100);
        RenderElement(root);

        GetPixelColor(10, 10).ShouldBe(SKColors.Blue);
        GetPixelColor(30, 30).ShouldBe(SKColors.Blue);
        GetPixelColor(70, 70).ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void BackgroundImage_ExplicitSize_WithLength_ShouldResolvePercentage()
    {
        var bg = BackgroundImage.FromBitmap(CreateTestBitmap(40, 40, SKColors.Red));
        bg.Repeat = BackgroundRepeat.NoRepeat;
        bg.Size = BackgroundSize.From(Length.Percent(50), Length.Percent(50));

        var root = CreateRootWithBackground(bg, 100, 100);
        RenderElement(root);

        // 50% of 100px = 50px, so image fills top-left 50x50 area
        GetPixelColor(25, 25).ShouldBe(SKColors.Red);
        GetPixelColor(75, 75).ShouldBe(SKColors.White);
    }

    [Fact]
    public void BackgroundImage_ExplicitSize_WithMixedUnits()
    {
        var bg = BackgroundImage.FromBitmap(CreateTestBitmap(40, 40, SKColors.Green));
        bg.Repeat = BackgroundRepeat.NoRepeat;
        bg.Size = BackgroundSize.From(Length.Px(30), Length.Percent(100));

        var root = CreateRootWithBackground(bg, 100, 100);
        RenderElement(root);

        // 30px wide, 100% of 100px = 100px tall
        GetPixelColor(15, 50).ShouldBe(SKColors.Green);
        GetPixelColor(50, 50).ShouldBe(SKColors.White);
    }

    [Fact]
    public void BackgroundImage_SvgStream_ShouldRender()
    {
        var svgContent = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 16'><rect width='16' height='16' fill='blue'/></svg>";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgContent));
        var bg = BackgroundImage.FromSvgStream(stream);
        bg.Repeat = BackgroundRepeat.NoRepeat;

        var root = CreateRootWithBackground(bg, 100, 100);
        RenderElement(root);

        GetPixelColor(8, 8).ShouldBe(SKColors.Blue);
    }

    private DivElement CreateRootWithBackground(BackgroundImage bg, int width, int height)
    {
        var root = new DivElement();
        root.Style = new Style
        {
            Width = Length.Px(width),
            Height = Length.Px(height),
            BackgroundImage = bg,
            BackgroundRepeat = bg.Repeat,
            BackgroundSize = bg.Size,
            BackgroundPosition = bg.Position,
        };
        return root;
    }

    private void RenderElement(Element root)
    {
        var engine = new MikoEngine();
        engine.Initialize(root, [], _canvas, 200, 200);
        engine.Render(_canvas);
    }

    private SKColor GetPixelColor(int x, int y) => _canvasBitmap.GetPixel(x, y);

    private static SKBitmap CreateTestBitmap(int width, int height, SKColor color)
    {
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        return bitmap;
    }
}
