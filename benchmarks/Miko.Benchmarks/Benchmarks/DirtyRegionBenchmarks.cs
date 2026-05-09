using BenchmarkDotNet.Attributes;
using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;

namespace Miko.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class DirtyRegionBenchmarks
{
    private DirtyRegionManager _manager = null!;
    private DivElement[] _nonOverlappingElements = null!;
    private DivElement[] _overlappingElements = null!;
    private DivElement[] _adjacentElements = null!;

    [GlobalSetup]
    public void Setup()
    {
        var layoutEngine = new LayoutEngine();
        var styles = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules =
                [
                    new StyleRule
                    {
                        Selector = new Miko.Styling.Selectors.TagSelector("div"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(50),
                            Height = Length.Px(30)
                        }
                    }
                ]
            }
        };

        _nonOverlappingElements = CreateElementsWithLayout(50, layoutEngine, styles, spacing: 100);
        _overlappingElements = CreateElementsWithLayout(50, layoutEngine, styles, spacing: 20);
        _adjacentElements = CreateElementsWithLayout(50, layoutEngine, styles, spacing: 50);
    }

    private static DivElement[] CreateElementsWithLayout(
        int count, LayoutEngine engine, List<StyleSheet> styles, int spacing)
    {
        var root = new DivElement { Id = "root" };
        var elements = new DivElement[count];
        for (int i = 0; i < count; i++)
        {
            var el = new DivElement { Id = $"el-{i}" };
            root.AddChild(el);
            elements[i] = el;
        }

        var layoutRoot = engine.Layout(root, styles, 800, spacing * count);
        AssignLayoutBoxes(root, layoutRoot);
        return elements;
    }

    private static void AssignLayoutBoxes(Miko.Core.Element element, LayoutBox layoutBox)
    {
        element.LayoutBox = layoutBox;
        for (int i = 0; i < element.Children.Count && i < layoutBox.Children.Count; i++)
        {
            AssignLayoutBoxes(element.Children[i], layoutBox.Children[i]);
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _manager = new DirtyRegionManager();
    }

    [Benchmark]
    public List<RectF> AddRegions_NonOverlapping()
    {
        foreach (var el in _nonOverlappingElements)
            _manager.MarkDirty(el);
        return _manager.GetDirtyRegions();
    }

    [Benchmark]
    public List<RectF> AddRegions_Overlapping()
    {
        foreach (var el in _overlappingElements)
            _manager.MarkDirty(el);
        return _manager.GetDirtyRegions();
    }

    [Benchmark]
    public List<RectF> AddRegions_Adjacent()
    {
        foreach (var el in _adjacentElements)
            _manager.MarkDirty(el);
        return _manager.GetDirtyRegions();
    }
}
