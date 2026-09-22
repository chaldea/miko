using Miko.Core;
using Miko.Diagnostics;
using Miko.Ionic.Benchmarks.Pages;
using Miko.Layout;
using Miko.Rendering;
using SkiaSharp;

namespace Miko.Ionic.Benchmarks.Helpers;

/// <summary>
/// Reports one relayout frame split into the same stages the on-device frame probe reports
/// (style / layout / paint), so a desktop benchmark number can be lined up against a device
/// measurement without converting between them (ISSUE-145).
/// </summary>
internal static class StageBreakdown
{
    public static void Run(int iterations = 40)
    {
        const int width = 390, height = 844;

        using var harness = IonicAppHarness.Create<CatalogHomePage>();
        var page = harness.Build<CatalogHomePage>();
        var styles = harness.StyleSheets;

        var layoutEngine = new LayoutEngine();
        var renderEngine = new RenderEngine();
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        renderEngine.SetCanvas(surface.Canvas);

        // Warm the caches AND let the JIT reach steady state before the reported frame. The
        // style stage is a very large amount of straight-line generated code (one branch per CSS
        // property, per matched rule); tiered compilation needs a few hundred calls to promote
        // it, and a short warm-up reports a frame several times slower than the app ever sees.
        for (int i = 0; i < 300; i++)
        {
            layoutEngine.InvalidateCache();
            renderEngine.Render(layoutEngine.Layout(page, styles, width, height));
        }

        FrameTimingDiagnostics.Begin();
        for (int i = 0; i < iterations; i++)
        {
            layoutEngine.InvalidateCache();
            surface.Canvas.Clear(SKColors.White);
            renderEngine.Render(layoutEngine.Layout(page, styles, width, height));
        }
        var stages = FrameTimingDiagnostics.End();

        var elements = Count(page);
        Console.WriteLine($"elements={elements} rules={harness.RuleCount} frames={iterations}");
        Console.WriteLine($"  style  = {stages.StyleMicroseconds / iterations / 1000:F2} ms/frame  ({stages.StyleMicroseconds / iterations / elements:F1} us/element)");
        Console.WriteLine($"  layout = {stages.LayoutMicroseconds / iterations / 1000:F2} ms/frame");
        Console.WriteLine($"  paint  = {stages.PaintMicroseconds / iterations / 1000:F2} ms/frame");
        Console.WriteLine($"  total  = {stages.TotalMicroseconds / iterations / 1000:F2} ms/frame");
        Console.WriteLine($"  boxes={stages.BoxCount / iterations} text={stages.TextDrawCount / iterations}");

        LayoutAllocationDiagnostics.Begin();
        for (int i = 0; i < iterations; i++)
        {
            layoutEngine.InvalidateCache();
            layoutEngine.Layout(page, styles, width, height);
        }
        var alloc = LayoutAllocationDiagnostics.End();
        Console.WriteLine($"  ComputedStyle rent new/reused = {alloc.ComputedStyleRentNew / iterations}/{alloc.ComputedStyleRentReused / iterations} per frame");
        Console.WriteLine($"  LayoutBox created = {alloc.LayoutBoxCreated / iterations} per frame");
        Console.WriteLine($"  allocated = {alloc.AllocatedBytes / iterations / 1024} KB/frame");
    }

    private static int Count(Element element)
    {
        int total = 1;
        foreach (var child in element.Children) total += Count(child);
        return total;
    }
}
