using BenchmarkDotNet.Attributes;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Ionic.Benchmarks.Helpers;
using Miko.Ionic.Benchmarks.Pages;
using Miko.Styling;
using SkiaSharp;

namespace Miko.Ionic.Benchmarks.Benchmarks;

/// <summary>
/// ISSUE-146: the frames a user actually sees change, measured through <see cref="MikoEngine"/> so
/// the engine's own invalidation model decides what runs.
///
/// <para><see cref="AppPageFrameBenchmarks"/> drives a standalone <c>LayoutEngine</c> with its
/// layout cache deliberately disabled, so every frame there is a full restyle. That is the right
/// harness for the style/layout algorithms, but it cannot show an optimisation that works by
/// <em>not</em> running a stage — the engine is the only thing that knows a frame's mutations were
/// text-only, or that a rebuild's first paint is about to be cleared.</para>
///
/// <list type="bullet">
/// <item><b>Text tick</b>: one text node changes (a player's clock label). Before ISSUE-146 this
/// restyled the whole page; now it relayouts with the previous computed styles.</item>
/// <item><b>Class toggle</b>: one class changes. Still a full restyle — the control arm that shows
/// the text tick's saving is not "the page got cheaper" but "the stage no longer runs".</item>
/// <item><b>Route rebuild</b>: a fresh component tree through <c>Initialize</c> + <c>Render</c>,
/// the exact pair a navigation frame performs.</item>
/// </list>
/// </summary>
[MemoryDiagnoser]
public class EngineFrameBenchmarks
{
    private const int ViewportWidth = 390;
    private const int ViewportHeight = 844;

    private IonicAppHarness _harness = null!;
    private SKSurface _surface = null!;
    private MikoEngine _engine = null!;
    private Element _label = null!;
    private Element _toggled = null!;
    private string _toggledClass = null!;
    private int _tick;

    [GlobalSetup]
    public void Setup()
    {
        _surface = SKSurface.Create(new SKImageInfo(ViewportWidth, ViewportHeight));
        _harness = IonicAppHarness.Create<CatalogHomePage>();
        _engine = new MikoEngineBuilder().Build();

        var page = _harness.Build<CatalogHomePage>();
        _engine.Initialize(page, _harness.StyleSheets, _surface.Canvas, ViewportWidth, ViewportHeight);

        _label = FindFirst(page, e => e.Children.Count == 1 && e.Children[0] is TextNode)
            ?? throw new InvalidOperationException("The benchmark page has no text label.");
        _toggled = FindFirst(page, e => e.HasClass("ion-page"))
            ?? throw new InvalidOperationException("The benchmark page has no ion-page host.");
        // Keep every original class (the Ionic mode and theme-scope classes among them) and only
        // add/remove an extra one; replacing the list would strip the page's styling and turn the
        // control arm into a cheaper page instead of the same page restyled.
        _toggledClass = _toggled.Class!;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _surface.Dispose();
        _harness.Dispose();
    }

    [Benchmark(Description = "Engine frame: one text node changes (relayout, styles reused)")]
    public void TextTick()
    {
        _label.TextContent = (_tick++ & 1) == 0 ? "0:01 / 0:03" : "0:02 / 0:03";
        _engine.Render(_surface.Canvas);
    }

    [Benchmark(Description = "Engine frame: one class changes (full restyle)")]
    public void ClassToggle()
    {
        _toggled.Class = (_tick++ & 1) == 0 ? _toggledClass + " benchmark-toggle" : _toggledClass;
        _engine.Render(_surface.Canvas);
    }

    [Benchmark(Description = "Engine frame: route rebuild (Initialize + Render, one paint)")]
    public void RouteRebuild()
    {
        var page = _harness.Build<CatalogHomePage>();
        _engine.Initialize(page, _harness.StyleSheets, _surface.Canvas, ViewportWidth, ViewportHeight,
            transition: null, paint: false);
        _engine.Render(_surface.Canvas);
    }

    private static Element? FindFirst(Element element, Func<Element, bool> predicate)
    {
        if (predicate(element)) return element;
        foreach (var child in element.Children)
        {
            if (FindFirst(child, predicate) is { } found) return found;
        }
        return null;
    }
}
