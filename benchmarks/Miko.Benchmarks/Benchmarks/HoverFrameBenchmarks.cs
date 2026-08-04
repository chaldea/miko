using BenchmarkDotNet.Attributes;
using Miko.Benchmarks.Helpers;
using Miko.Core;
using Miko.Layout;
using Miko.Styling;

namespace Miko.Benchmarks.Benchmarks;

/// <summary>
/// ISSUE-113：指针在长列表上滑过时的「每帧重排」成本。
///
/// <para>复现场景：DebugDemo 里一个 IonList 中若干带 <c>Href</c> 的 IonItem，指针扫过时
/// 悬停链不断变化。只要有任一 :hover 相关规则（或任何其他 DOM/状态变更）使布局输入失效，
/// 引擎就会执行一次完整的样式重解析 + 布局树重建——该帧的成本与分配量直接决定 UI 是否卡顿。</para>
///
/// <para>关键的被测量：<b>每帧分配量</b>。渲染帧中的堆分配会推高 gen0 回收频率，GC 暂停
/// 表现为鼠标移动时 :hover 跟不上指针（issue 中记录的内存锯齿与延迟）。因此
/// <c>[MemoryDiagnoser]</c> 报告的 Allocated 列是本组基准的首要指标，其次才是耗时。</para>
///
/// <para>基线（优化前，287 元素 × 1868 条 Ionic 规则）：约 62 MB、45 ms/帧，
/// 其中约 96% 的分配来自 <c>ClassSelector</c> → <c>Element.HasClass</c> 的
/// <c>Split(' ')</c>；见 ISSUE-113 实现总结。</para>
/// </summary>
[MemoryDiagnoser]
public class HoverFrameBenchmarks
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly StyleResolver _styleResolver = new();

    private Element _listPage = null!;
    private List<StyleSheet> _componentLibraryStyles = null!;
    private List<Element> _items = null!;
    private int _hoverCursor;

    [Params(20, 60)]
    public int ItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _listPage = DomBuilder.CreateComponentListPage(ItemCount);
        _componentLibraryStyles = DomBuilder.CreateComponentLibraryStyleSheet();

        // 收集各列表项的可悬停元素（item host），供逐帧轮转模拟指针滑过。
        _items = [];
        CollectItems(_listPage, _items);

        // 预热：文本度量缓存等一次性成本不应计入逐帧数据。
        _layoutEngine.InvalidateCache();
        _layoutEngine.Layout(_listPage, _componentLibraryStyles, 390, 844);
    }

    private static void CollectItems(Element element, List<Element> sink)
    {
        if (element.HasClass("ion-item")) sink.Add(element);
        foreach (var child in element.Children) CollectItems(child, sink);
    }

    /// <summary>
    /// 一次「悬停变化帧」：把悬停状态从上一项移到下一项，然后执行整页重排——
    /// 即指针跨过一个列表项边界时引擎实际要做的工作。
    /// </summary>
    [Benchmark(Description = "Hover move + full relayout (component-library stylesheet)")]
    public LayoutBox HoverMove_FullRelayout()
    {
        var previous = _items[_hoverCursor];
        _hoverCursor = (_hoverCursor + 1) % _items.Count;
        var current = _items[_hoverCursor];

        previous.ClearState(ElementState.Hover);
        current.SetState(ElementState.Hover);

        _layoutEngine.InvalidateCache();
        return _layoutEngine.Layout(_listPage, _componentLibraryStyles, 390, 844);
    }

    /// <summary>
    /// 纯样式解析的每帧成本（不含布局算法），用于定位回归发生在级联还是布局。
    /// </summary>
    [Benchmark(Description = "Style resolve only (all elements)")]
    public int StyleResolveOnly()
    {
        var viewport = new ViewportInfo(390, 844);
        int count = 0;
        ResolveRecursive(_listPage, _styleResolver, _componentLibraryStyles, viewport, ref count);
        return count;
    }

    private static void ResolveRecursive(Element element, StyleResolver resolver,
        List<StyleSheet> styleSheets, ViewportInfo viewport, ref int count)
    {
        if (resolver.Resolve(element, styleSheets, viewport) != null) count++;
        foreach (var child in element.Children)
            ResolveRecursive(child, resolver, styleSheets, viewport, ref count);
    }
}
