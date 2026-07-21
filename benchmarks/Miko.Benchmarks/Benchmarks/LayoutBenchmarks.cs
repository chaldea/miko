using BenchmarkDotNet.Attributes;
using Miko.Benchmarks.Helpers;
using Miko.Core;
using Miko.Layout;
using Miko.Styling;

namespace Miko.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class LayoutBenchmarks
{
    private readonly LayoutEngine _layoutEngine = new();

    private Element _smallTree = null!;
    private Element _largeTree = null!;
    private Element _deepTree = null!;
    private Element _flexSmall = null!;
    private Element _flexLarge = null!;
    private Element _inlineTree = null!;
    private Element _realisticPage = null!;

    private List<StyleSheet> _blockStyles = null!;
    private List<StyleSheet> _flexStyles = null!;
    private List<StyleSheet> _inlineStyles = null!;
    private List<StyleSheet> _realisticStyles = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallTree = DomBuilder.CreateFlatTree(10);
        _largeTree = DomBuilder.CreateFlatTree(1000);
        _deepTree = DomBuilder.CreateDeepTree(50);
        _flexSmall = DomBuilder.CreateFlexContainer(10);
        _flexLarge = DomBuilder.CreateFlexContainer(200);
        _inlineTree = DomBuilder.CreateInlineContainer(100);
        _realisticPage = DomBuilder.CreateRealisticPage();

        _blockStyles = DomBuilder.CreateBlockStyleSheet();
        _flexStyles = DomBuilder.CreateFlexStyleSheet();
        _inlineStyles = DomBuilder.CreateInlineStyleSheet();
        _realisticStyles = DomBuilder.CreateRealisticStyleSheet();
    }

    [Benchmark]
    public LayoutBox BlockLayout_SmallTree()
        { _layoutEngine.InvalidateCache(); return _layoutEngine.Layout(_smallTree, _blockStyles, 800, 600); }

    [Benchmark]
    public LayoutBox BlockLayout_LargeTree()
        { _layoutEngine.InvalidateCache(); return _layoutEngine.Layout(_largeTree, _blockStyles, 800, 600); }

    [Benchmark]
    public LayoutBox BlockLayout_DeepNesting()
        { _layoutEngine.InvalidateCache(); return _layoutEngine.Layout(_deepTree, _blockStyles, 800, 600); }

    [Benchmark]
    public LayoutBox FlexLayout_FewChildren()
        { _layoutEngine.InvalidateCache(); return _layoutEngine.Layout(_flexSmall, _flexStyles, 800, 600); }

    [Benchmark]
    public LayoutBox FlexLayout_ManyChildren()
        { _layoutEngine.InvalidateCache(); return _layoutEngine.Layout(_flexLarge, _flexStyles, 800, 600); }

    [Benchmark]
    public LayoutBox InlineLayout_ManyElements()
        { _layoutEngine.InvalidateCache(); return _layoutEngine.Layout(_inlineTree, _inlineStyles, 800, 600); }

    [Benchmark]
    public LayoutBox MixedLayout_Realistic()
        { _layoutEngine.InvalidateCache(); return _layoutEngine.Layout(_realisticPage, _realisticStyles, 800, 600); }
}