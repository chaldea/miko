using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// Tests for the incremental-render dirty-region threshold (ISSUE-036, benchmark report §2/§4).
/// When the number of dirty regions exceeds the threshold, the engine should fall back to a
/// full render rather than performing many full-tree traversals.
/// </summary>
public class IncrementalRenderThresholdTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;

    public IncrementalRenderThresholdTests()
    {
        _bitmap = new SKBitmap(400, 1200);
        _canvas = new SKCanvas(_bitmap);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    /// <summary>
    /// Builds a vertical stack of separated colored rows so that each row produces a
    /// distinct (non-mergeable) dirty region.
    /// </summary>
    private static DivElement BuildRows(int count, out List<DivElement> rows)
    {
        rows = new List<DivElement>();
        var root = new DivElement
        {
            Style = new Style { Display = Display.Block, Width = Length.Px(400), Height = Length.Px(1200) }
        };

        for (int i = 0; i < count; i++)
        {
            var row = new DivElement
            {
                Class = "row",
                Style = new Style
                {
                    Display = Display.Block,
                    Width = Length.Px(100),
                    Height = Length.Px(10),
                    MarginBottom = Length.Px(10),
                    BackgroundColor = new Color(10, 20, 30)
                }
            };
            rows.Add(row);
            root.AddChild(row);
        }

        return root;
    }

    [Fact]
    public void Update_ManyDirtyRegions_FallsBackToFullRenderWithSameResult()
    {
        var root = BuildRows(40, out var rows);

        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet>(), _canvas, 400, 1200);

        // Mark every row dirty (40 > default threshold of 30) -> full-render fallback path
        foreach (var row in rows)
        {
            engine.InvalidateElement(row);
        }
        engine.Update(_canvas);

        // Capture the incremental/fallback result
        var fallbackPixels = _bitmap.Copy();

        // Now produce an authoritative full render and compare — results must be identical
        _canvas.Clear(SKColors.Transparent);
        engine.Render(_canvas);

        try
        {
            for (int y = 0; y < _bitmap.Height; y += 37)
            {
                for (int x = 0; x < _bitmap.Width; x += 41)
                {
                    fallbackPixels.GetPixel(x, y).ShouldBe(_bitmap.GetPixel(x, y),
                        $"Pixel ({x},{y}) differs between many-dirty-region fallback and full render");
                }
            }
        }
        finally
        {
            fallbackPixels.Dispose();
        }
    }

    [Fact]
    public void Update_FewDirtyRegions_DoesNotThrow()
    {
        var root = BuildRows(3, out var rows);

        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet>(), _canvas, 400, 1200);

        foreach (var row in rows)
        {
            engine.InvalidateElement(row);
        }

        Should.NotThrow(() => engine.Update(_canvas));
    }

    [Fact]
    public void RenderEngine_MaxIncrementalDirtyRegions_DefaultsTo30()
    {
        var renderEngine = new Miko.Rendering.RenderEngine();
        renderEngine.MaxIncrementalDirtyRegions.ShouldBe(30);
    }
}
