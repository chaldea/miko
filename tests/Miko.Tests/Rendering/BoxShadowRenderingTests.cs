using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

public class BoxShadowRenderingTests : IDisposable
{
    private readonly SKBitmap _canvasBitmap;
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine;
    private readonly LayoutEngine _layoutEngine;

    public BoxShadowRenderingTests()
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
    public void BoxShadow_SingleLayer_ShouldRender()
    {
        // Arrange - box at (50, 50) 50x50 with opaque shadow offset (10, 10)
        var root = new DivElement { Class = "box" };
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("box"),
                        Style = new Style
                        {
                            Position = Position.Absolute,
                            Left = Length.Px(50),
                            Top = Length.Px(50),
                            Width = Length.Px(50),
                            Height = Length.Px(50),
                            BackgroundColor = Color.White,
                            BoxShadow = new List<BoxShadow>
                            {
                                new BoxShadow(10, 10, 4, 0, new Color(0, 0, 0, 255))
                            }
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 200, 200);
        _renderEngine.Render(layoutRoot);

        // Assert - box occupies (50,50)-(100,100); shadow offset (10,10) extends it to
        // (60,60)-(110,110). The bottom-right strip beyond the box (e.g. x=105, y=105)
        // should show the dark shadow.
        var shadowPixel = GetPixelColor(105, 105);
        ((int)shadowPixel.Red).ShouldBeLessThan(200); // dark shadow

        // The box interior stays white (background drawn on top of the shadow)
        GetPixelColor(70, 70).ShouldBe(SKColors.White);

        // Far corner (top-left, away from the offset shadow) stays white
        GetPixelColor(20, 20).ShouldBe(SKColors.White);
    }

    [Fact]
    public void BoxShadow_MultipleLayers_ShouldStackForDarkerShadow()
    {
        // Arrange - two boxes: one with a single semi-transparent shadow, one with the
        // same shadow stacked three times. The stacked one should be darker.
        var single = RenderShadowAndSample(new List<BoxShadow>
        {
            new BoxShadow(10, 10, 0, 0, new Color(0, 0, 0, 60))
        });
        var triple = RenderShadowAndSample(new List<BoxShadow>
        {
            new BoxShadow(10, 10, 0, 0, new Color(0, 0, 0, 60)),
            new BoxShadow(10, 10, 0, 0, new Color(0, 0, 0, 60)),
            new BoxShadow(10, 10, 0, 0, new Color(0, 0, 0, 60))
        });

        // Assert - stacking multiple shadow layers produces a darker result
        triple.ShouldBeLessThan(single);
    }

    [Fact]
    public void BoxShadow_IonicHeaderShadow_ShouldRender()
    {
        // Arrange - the exact Ionic MD header 3-layer shadow
        var sample = RenderShadowAndSample(new List<BoxShadow>
        {
            new BoxShadow(0, 2, 4, -1, new Color(0, 0, 0, 51)),  // 0.2 alpha
            new BoxShadow(0, 4, 5, 0, new Color(0, 0, 0, 36)),   // 0.14 alpha
            new BoxShadow(0, 1, 10, 0, new Color(0, 0, 0, 31))   // 0.12 alpha
        });

        // Assert - the multi-layer shadow is visible below the box (darker than white)
        sample.ShouldBeLessThan(255);
    }

    /// <summary>
    /// Renders a 50x50 box at (50,50) with the given shadow layers and returns the
    /// red channel of a pixel just below-right of the box (in the shadow region).
    /// </summary>
    private int RenderShadowAndSample(List<BoxShadow> shadows)
    {
        using var bmp = new SKBitmap(200, 200);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);
        var re = new RenderEngine();
        re.SetCanvas(canvas);
        var le = new LayoutEngine();

        var root = new DivElement { Class = "box" };
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("box"),
                        Style = new Style
                        {
                            Position = Position.Absolute,
                            Left = Length.Px(50),
                            Top = Length.Px(50),
                            Width = Length.Px(50),
                            Height = Length.Px(50),
                            BackgroundColor = Color.White,
                            BoxShadow = shadows
                        }
                    }
                }
            }
        };

        var layoutRoot = le.Layout(root, styleSheets, 200, 200);
        re.Render(layoutRoot);

        // Sample at the bottom edge below the box where shadows accumulate
        return bmp.GetPixel(75, 105).Red;
    }

    [Fact]
    public void BoxShadow_WithBorderRadius_ShouldFollowShape()
    {
        // Arrange - rounded box at (30, 30) with shadow
        var root = new DivElement { Class = "box" };
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("box"),
                        Style = new Style
                        {
                            Position = Position.Absolute,
                            Left = Length.Px(30),
                            Top = Length.Px(30),
                            Width = Length.Px(60),
                            Height = Length.Px(60),
                            BackgroundColor = Color.White,
                            BorderRadius = Length.Px(10),
                            BoxShadow = new List<BoxShadow>
                            {
                                new BoxShadow(3, 3, 6, 0, new Color(0, 0, 0, 150))
                            }
                        }
                    }
                }
            }
        };

        // Act & Assert - should render rounded shadow without crashing
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 200, 200);
        Should.NotThrow(() => _renderEngine.Render(layoutRoot));
    }

    [Fact]
    public void BoxShadow_EmptyList_ShouldNotCrash()
    {
        // Arrange - box with empty shadow list
        var root = new DivElement { Class = "box" };
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("box"),
                        Style = new Style
                        {
                            Width = Length.Px(50),
                            Height = Length.Px(50),
                            BackgroundColor = Color.White,
                            BoxShadow = new List<BoxShadow>()
                        }
                    }
                }
            }
        };

        // Act & Assert - should not throw
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 200, 200);
        Should.NotThrow(() => _renderEngine.Render(layoutRoot));
    }

    private SKColor GetPixelColor(int x, int y)
    {
        return _canvasBitmap.GetPixel(x, y);
    }
}
