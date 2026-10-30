using Miko.Core;
using Miko.Graphics.Skia;
using Miko.Platform;
using Miko.Rendering;
using Miko.Routing;
using Miko.Styling;
using SkiaSharp;

/// <summary>
/// White-box tests and benchmarks drive the engine with a raw <see cref="SKCanvas"/>
/// (an <c>SKBitmap</c> they then read pixels from). The engine's public entry points take an
/// <see cref="Miko.Graphics.IDrawingContext"/> since ISSUE-162, so these overloads wrap the canvas in the
/// engine's own context (cached per canvas). Only assemblies with internals access can build it,
/// which keeps Skia out of the public surface.
///
/// <para>Linked into Miko.Tests, Miko.Ionic.Tests and the benchmark projects; in the global namespace so no
/// using directive is needed.</para>
/// </summary>
internal static class SkiaCanvasExtensions
{
    public static void Initialize(this MikoEngine engine, Element root, List<StyleSheet> styleSheets, SKCanvas canvas,
        float viewportWidth, float viewportHeight, NavigationTransitionInfo? transition = null)
        => engine.Initialize(root, styleSheets, SkiaDrawingContext.For(canvas), viewportWidth, viewportHeight, transition);

    public static void Initialize(this MikoEngine engine, Element root, List<StyleSheet> styleSheets, SKCanvas canvas,
        float viewportWidth, float viewportHeight, NavigationTransitionInfo? transition, bool paint)
        => engine.Initialize(root, styleSheets, SkiaDrawingContext.For(canvas), viewportWidth, viewportHeight, transition, paint);

    public static void Render(this MikoEngine engine, SKCanvas canvas) => engine.Render(SkiaDrawingContext.For(canvas));

    public static void Update(this MikoEngine engine, SKCanvas canvas) => engine.Update(SkiaDrawingContext.For(canvas));

    public static void Tick(this MikoEngine engine, float deltaTime, SKCanvas canvas)
        => engine.Tick(deltaTime, SkiaDrawingContext.For(canvas));

    public static void Initialize(this MikoInteractionController controller, SKCanvas canvas, float width, float height)
        => controller.Initialize(SkiaDrawingContext.For(canvas), width, height);

    public static void Rebuild(this MikoInteractionController controller, SKCanvas canvas, float width, float height)
        => controller.Rebuild(SkiaDrawingContext.For(canvas), width, height);

    public static void RenderFrame(this MikoInteractionController controller, SKCanvas canvas, float width, float height,
        float deltaTime, Action<SKCanvas> render)
        => controller.RenderFrame(SkiaDrawingContext.For(canvas), width, height, deltaTime, _ => render(canvas));

    public static void SetCanvas(this RenderEngine renderEngine, SKCanvas canvas)
        => renderEngine.SetCanvas(SkiaDrawingContext.For(canvas));
}
