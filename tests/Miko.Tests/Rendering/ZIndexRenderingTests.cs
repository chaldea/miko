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

/// <summary>
/// Verifies that z-index affects sibling paint order for positioned elements. This is what
/// lets a positioned header (z-index &gt; 0) and its box-shadow paint above following
/// in-flow content instead of being covered by it.
/// </summary>
public class ZIndexRenderingTests : IDisposable
{
    private readonly SKBitmap _canvasBitmap;
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine;
    private readonly LayoutEngine _layoutEngine;

    public ZIndexRenderingTests()
    {
        _canvasBitmap = new SKBitmap(100, 100);
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
    public void PositionedHigherZIndex_PaintsAboveLaterSibling()
    {
        // Arrange - two overlapping absolutely-positioned boxes at the same spot.
        // The first (document order) is red with z-index 10; the second is blue with no
        // z-index. Without z-index ordering the later blue box would win; with it, red wins.
        var root = new DivElement { Class = "root" };
        var red = new DivElement { Class = "red" };
        var blue = new DivElement { Class = "blue" };
        root.AddChild(red);
        root.AddChild(blue);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule { Selector = new ClassSelector("root"),
                        Style = new Style { Width = Length.Px(100), Height = Length.Px(100) } },
                    new StyleRule { Selector = new ClassSelector("red"),
                        Style = new Style
                        {
                            Position = Position.Absolute, Left = Length.Px(10), Top = Length.Px(10),
                            Width = Length.Px(50), Height = Length.Px(50),
                            BackgroundColor = new Color(255, 0, 0), ZIndex = 10
                        } },
                    new StyleRule { Selector = new ClassSelector("blue"),
                        Style = new Style
                        {
                            Position = Position.Absolute, Left = Length.Px(10), Top = Length.Px(10),
                            Width = Length.Px(50), Height = Length.Px(50),
                            BackgroundColor = new Color(0, 0, 255)
                        } },
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 100, 100);
        _renderEngine.Render(layoutRoot);

        // Assert - the overlap shows red (higher z-index wins despite being earlier in order)
        var pixel = _canvasBitmap.GetPixel(30, 30);
        pixel.Red.ShouldBe((byte)255);
        pixel.Blue.ShouldBe((byte)0);
    }

    [Fact]
    public void NoZIndex_LaterSiblingPaintsOnTop()
    {
        // Arrange - same overlap but neither box has z-index: document order decides,
        // so the later blue box wins. Guards against the ordering changing default behavior.
        var root = new DivElement { Class = "root" };
        var red = new DivElement { Class = "red" };
        var blue = new DivElement { Class = "blue" };
        root.AddChild(red);
        root.AddChild(blue);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule { Selector = new ClassSelector("root"),
                        Style = new Style { Width = Length.Px(100), Height = Length.Px(100) } },
                    new StyleRule { Selector = new ClassSelector("red"),
                        Style = new Style
                        {
                            Position = Position.Absolute, Left = Length.Px(10), Top = Length.Px(10),
                            Width = Length.Px(50), Height = Length.Px(50),
                            BackgroundColor = new Color(255, 0, 0)
                        } },
                    new StyleRule { Selector = new ClassSelector("blue"),
                        Style = new Style
                        {
                            Position = Position.Absolute, Left = Length.Px(10), Top = Length.Px(10),
                            Width = Length.Px(50), Height = Length.Px(50),
                            BackgroundColor = new Color(0, 0, 255)
                        } },
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 100, 100);
        _renderEngine.Render(layoutRoot);

        // Assert - later sibling (blue) wins
        var pixel = _canvasBitmap.GetPixel(30, 30);
        pixel.Blue.ShouldBe((byte)255);
        pixel.Red.ShouldBe((byte)0);
    }
}
