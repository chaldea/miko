using BenchmarkDotNet.Attributes;
using Miko.Benchmarks.Helpers;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;

namespace Miko.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class StyleResolutionBenchmarks
{
    private readonly LayoutEngine _layoutEngine = new();

    private Element _smallTree = null!;
    private Element _largeTree = null!;
    private Element _deepTree = null!;

    private List<StyleSheet> _fewRules = null!;
    private List<StyleSheet> _manyRules = null!;
    private List<StyleSheet> _complexSelectors = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallTree = DomBuilder.CreateFlatTree(10);
        _largeTree = DomBuilder.CreateFlatTree(100);
        _deepTree = DomBuilder.CreateDeepTree(30);

        _fewRules = DomBuilder.CreateBlockStyleSheet();
        _manyRules = DomBuilder.CreateLargeStyleSheet(100);
        _complexSelectors = CreateComplexSelectorStyleSheet();
    }

    [Benchmark]
    public LayoutBox Resolve_FewRules()
        { _layoutEngine.InvalidateCache(); return _layoutEngine.Layout(_smallTree, _fewRules, 800, 600); }

    [Benchmark]
    public LayoutBox Resolve_ManyRules()
        { _layoutEngine.InvalidateCache(); return _layoutEngine.Layout(_largeTree, _manyRules, 800, 600); }

    [Benchmark]
    public LayoutBox Resolve_ComplexSelectors()
        { _layoutEngine.InvalidateCache(); return _layoutEngine.Layout(_largeTree, _complexSelectors, 800, 600); }

    [Benchmark]
    public LayoutBox Resolve_DeepInheritance()
        { _layoutEngine.InvalidateCache(); return _layoutEngine.Layout(_deepTree, _manyRules, 800, 600); }

    private static List<StyleSheet> CreateComplexSelectorStyleSheet()
    {
        var rules = new List<StyleRule>();
        for (int i = 0; i < 50; i++)
        {
            rules.Add(new StyleRule
            {
                Selector = new IdSelector($"child-{i}"),
                Style = new Style { Width = Length.Px(200), Padding = Length.Px(5) }
            });
            rules.Add(new StyleRule
            {
                Selector = new ClassSelector(i % 2 == 0 ? "even" : "odd"),
                Style = new Style { Height = Length.Px(30), MarginBottom = Length.Px(2) }
            });
            rules.Add(new StyleRule
            {
                Selector = new TagSelector("div"),
                Style = new Style { Display = Display.Block }
            });
        }
        return [new StyleSheet { Rules = rules }];
    }
}