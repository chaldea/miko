using BenchmarkDotNet.Attributes;
using Miko.Core;
using Miko.Ionic.Benchmarks.Helpers;
using Miko.Ionic.Benchmarks.Pages;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using SkiaSharp;

namespace Miko.Ionic.Benchmarks.Benchmarks;

/// <summary>
/// ISSUE-145: the frame cost of a real application page, built from the real component library
/// against the real component stylesheet.
///
/// <para><b>Why this exists.</b> The core suite's "large page (500 elements)" reported ~2.3 ms
/// per full frame — comfortably inside the 16.6 ms budget — while the Anime example's "/home"
/// route, a visually modest page, measured 60-70 ms per frame on an Android emulator. The two
/// numbers are not in conflict; they measure different things. The synthetic page is 500 bare
/// <c>div</c>s resolved against a one-rule stylesheet, so its style stage is nearly free and its
/// cost is dominated by painting. A component-library page inverts that: ~240 elements resolved
/// against ~2,000 rules of compound and descendant selectors, where the style stage dominates.
/// Element count alone does not predict frame time — the product of elements and stylesheet
/// complexity does, and only this benchmark varies the second factor.</para>
///
/// <para>The viewport is phone-portrait (390x844) to match the example app.</para>
/// </summary>
[MemoryDiagnoser]
public class AppPageFrameBenchmarks
{
    private const int ViewportWidth = 390;
    private const int ViewportHeight = 844;

    private readonly LayoutEngine _layoutEngine = new();
    private readonly RenderEngine _renderEngine = new();

    private IonicAppHarness _harness = null!;
    private SKSurface _surface = null!;
    private Element _page = null!;
    private List<StyleSheet> _styles = null!;

    [GlobalSetup]
    public void Setup()
    {
        _surface = SKSurface.Create(new SKImageInfo(ViewportWidth, ViewportHeight));
        _renderEngine.SetCanvas(_surface.Canvas);

        _harness = IonicAppHarness.Create<CatalogHomePage>();
        _styles = _harness.StyleSheets;
        _page = _harness.Build<CatalogHomePage>();

        // Prime the layout and text-measurement caches so the measured frames are steady-state.
        _layoutEngine.InvalidateCache();
        _layoutEngine.Layout(_page, _styles, ViewportWidth, ViewportHeight);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _surface.Dispose();
        _harness.Dispose();
    }

    /// <summary>
    /// A full frame after a state change: styles recomputed, layout recalculated, tree repainted.
    /// This is what every interaction that changes state costs (a segment switch, a filter, a
    /// search keystroke), and it is the frame the example app drops.
    /// </summary>
    [Benchmark(Description = "App page: full frame (style + layout + paint)")]
    public void FullFrame()
    {
        _layoutEngine.InvalidateCache();
        var layout = _layoutEngine.Layout(_page, _styles, ViewportWidth, ViewportHeight);
        _surface.Canvas.Clear(SKColors.White);
        _renderEngine.Render(layout);
    }

    /// <summary>
    /// The style + layout half of the frame, isolated from painting — where the on-device probe
    /// pointed ("box layout and styles").
    /// </summary>
    [Benchmark(Description = "App page: style + layout only")]
    public LayoutBox StyleAndLayout()
    {
        _layoutEngine.InvalidateCache();
        return _layoutEngine.Layout(_page, _styles, ViewportWidth, ViewportHeight);
    }

    /// <summary>
    /// Style resolution alone, with no layout. The difference between this and
    /// <see cref="StyleAndLayout"/> is the layout algorithms' own cost.
    /// </summary>
    [Benchmark(Description = "App page: style resolution only")]
    public int StyleOnly()
    {
        var resolver = new StyleResolver();
        var viewport = new ViewportInfo(ViewportWidth, ViewportHeight);
        return ResolveSubtree(_page, resolver, viewport);
    }

    // Deliberately no "idle frame" benchmark here. A standalone LayoutEngine disables the layout
    // result cache (see its parameterless constructor), because elements built outside an engine
    // have no Owner and so their mutations increment no counter — a cache keyed on that would
    // silently return a stale tree. An "idle" benchmark on this harness would therefore do the
    // full relayout the app's idle frames skip, and report a number the app never pays. Idle
    // frames are covered where the cache is real: the core suite's EngineIdleFrameBenchmarks.

    /// <summary>
    /// Rebuilding the component tree from scratch, as a route navigation does — no layout, no
    /// paint, so the build stage stays visible instead of being masked by them.
    ///
    /// <para>This is the stage that dominates the navigation frame: on the example app a route
    /// change spends far more time here than in style and layout combined, and allocates megabytes
    /// doing it. A steady-state frame never runs it, which is why a benchmark suite that only
    /// measured steady-state frames reported comfortable headroom for a page that visibly
    /// stutters when you switch to it.</para>
    /// </summary>
    [Benchmark(Description = "App page: component build only (route navigation)")]
    public Element BuildOnly() => _harness.Build<CatalogHomePage>();

    private int ResolveSubtree(Element element, StyleResolver resolver, ViewportInfo viewport)
    {
        var computed = resolver.Resolve(element, _styles, viewport);
        int count = 1;
        // The resolved style must stay reachable while descendants resolve — they read it for
        // inheritance — so it is parked on the layout box exactly as ComputeStyles does.
        var previous = element.LayoutBox;
        element.LayoutBox = new LayoutBox { Element = element, ComputedStyle = computed };
        foreach (var child in element.Children)
            count += ResolveSubtree(child, resolver, viewport);
        element.LayoutBox = previous;
        return count;
    }
}
