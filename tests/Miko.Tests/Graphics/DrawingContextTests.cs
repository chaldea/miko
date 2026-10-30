using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Graphics;

/// <summary>
/// ISSUE-162: components, hosts and apps draw through <see cref="IDrawingContext"/> and create resources
/// through <see cref="IGraphicsFactory"/>; the rendering backend stays inside the core library. These tests
/// use only the public abstraction (no SkiaSharp), as an outside caller would.
/// </summary>
public class DrawingContextTests
{
    private static readonly IGraphicsFactory Graphics = new MikoEngineBuilder().Build().Graphics;

    private const string SquareSvg =
        "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 10'><rect width='10' height='10' fill='#ff0000'/></svg>";

    [Fact]
    public void Should_fill_and_clear_a_surface()
    {
        using var surface = Graphics.CreateSurface(20, 10);
        var context = surface.Context;
        context.Clear(Color.White);
        context.FillRect(new RectF(10, 0, 10, 10), new Color(0, 0, 255));

        surface.GetPixel(2, 5).ShouldBe(Color.White);
        surface.GetPixel(15, 5).ShouldBe(new Color(0, 0, 255));
    }

    [Fact]
    public void Should_clip_and_restore_state()
    {
        using var surface = Graphics.CreateSurface(20, 20);
        var context = surface.Context;
        context.Clear(Color.White);

        int saved = context.Save();
        context.ClipRect(new RectF(0, 0, 10, 20));
        context.FillRect(new RectF(0, 0, 20, 20), Color.Black);
        context.RestoreToCount(saved);
        context.FillRect(new RectF(15, 15, 5, 5), new Color(0, 255, 0));

        surface.GetPixel(5, 5).ShouldBe(Color.Black);
        surface.GetPixel(15, 5).ShouldBe(Color.White, "the clip leaked past Restore");
        surface.GetPixel(17, 17).ShouldBe(new Color(0, 255, 0));
    }

    [Fact]
    public void Should_apply_translation_and_report_device_scale()
    {
        using var surface = Graphics.CreateSurface(40, 40);
        var context = surface.Context;
        context.Clear(Color.White);
        context.DeviceScale.ShouldBe(1f);

        context.Save();
        context.Scale(2, 2);
        context.DeviceScale.ShouldBe(2f);
        context.Translate(10, 10);
        context.FillRect(new RectF(0, 0, 5, 5), Color.Black);
        context.Restore();

        surface.GetPixel(25, 25).ShouldBe(Color.Black);
        surface.GetPixel(15, 15).ShouldBe(Color.White);
    }

    [Fact]
    public void Should_lay_out_mixed_scripts_with_fallback_fonts()
    {
        // One typeface per string used to turn the kana of "日本語のコメント" into tofu in the danmaku.
        var mixed = Graphics.CreateText("Latin 漢字 かな", new TextStyle("Arial", 20));
        var latin = Graphics.CreateText("Latin", new TextStyle("Arial", 20));

        mixed.Width.ShouldBeGreaterThan(latin.Width);
        mixed.Ascent.ShouldBeGreaterThan(0);
        mixed.Descent.ShouldBeGreaterThan(0);
        mixed.Height.ShouldBe(mixed.Ascent + mixed.Descent);
        // The layout is cached by content: same text and style, same width.
        Graphics.CreateText("Latin", new TextStyle("Arial", 20)).Width.ShouldBe(latin.Width);
    }

    [Fact]
    public void Should_draw_text_at_its_baseline()
    {
        using var surface = Graphics.CreateSurface(120, 40);
        var context = surface.Context;
        context.Clear(Color.White);
        var text = context.Graphics.CreateText("HHHH", new TextStyle("Arial", 24));

        context.DrawText(text, 4, 30, Color.Black);

        CountDark(surface, new RectF(4, 30 - text.Ascent, text.Width, text.Ascent)).ShouldBeGreaterThan(20);
        CountDark(surface, new RectF(0, 31, 120, 9)).ShouldBe(0, "capital H has no descender");
    }

    [Fact]
    public void Should_decode_svg_as_a_vector_image_and_tint_it()
    {
        using var image = Graphics.DecodeImage(System.Text.Encoding.UTF8.GetBytes(SquareSvg));
        image.ShouldNotBeNull();
        image.IsVector.ShouldBeTrue();
        image.Width.ShouldBe(10);
        image.Height.ShouldBe(10);

        using var surface = Graphics.CreateSurface(60, 30);
        var context = surface.Context;
        context.Clear(Color.White);
        context.DrawImage(image, new RectF(0, 0, 30, 30));
        context.DrawImage(image, new RectF(30, 0, 30, 30), tint: new Color(0, 0, 255));

        surface.GetPixel(15, 15).ShouldBe(new Color(255, 0, 0));
        surface.GetPixel(45, 15).ShouldBe(new Color(0, 0, 255), "the tint replaces the image colors");
    }

    [Fact]
    public void Should_decode_png_bytes_and_create_images_from_pixels()
    {
        using var source = Graphics.CreateSurface(4, 3);
        source.Context.Clear(new Color(10, 200, 30));
        using var decoded = Graphics.DecodeImage(source.EncodePng());
        decoded.ShouldNotBeNull();
        decoded.IsVector.ShouldBeFalse();
        (decoded.Width, decoded.Height).ShouldBe((4, 3));

        // 2×2 RGBA with rows padded to 16 bytes, as hardware decoders produce them: (0,0) red, (1,1) blue.
        var pixels = new byte[16 * 2];
        byte[] red = [255, 0, 0, 255], blue = [0, 0, 255, 255];
        red.CopyTo(pixels, 0);
        blue.CopyTo(pixels, 16 + 4);
        using var created = Graphics.CreateImage(2, 2, ImagePixelFormat.Rgba8888, pixels, rowBytes: 16);
        created.ShouldNotBeNull();

        using var surface = Graphics.CreateSurface(2, 2);
        surface.Context.DrawImage(created, new RectF(0, 0, 2, 2), ImageSampling.Nearest);
        surface.GetPixel(0, 0).ShouldBe(new Color(255, 0, 0));
        surface.GetPixel(1, 1).ShouldBe(new Color(0, 0, 255));

        Graphics.DecodeImage([1, 2, 3, 4]).ShouldBeNull();
        Graphics.CreateImage(2, 2, ImagePixelFormat.Rgba8888, new byte[8]).ShouldBeNull("not enough pixel data");
    }

    [Fact]
    public void Should_paint_canvas_elements_inside_their_content_box()
    {
        var engine = new MikoEngineBuilder().Build();
        RectF painted = default;
        IDrawingContext? received = null;
        var canvas = new CanvasElement
        {
            Style = new Style { Display = Display.Block, Width = Length.Px(40), Height = Length.Px(20), Padding = new Padding(Length.Px(5)) },
            Paint = (context, bounds) =>
            {
                received = context;
                painted = bounds;
                // Painting past the content box must be clipped away.
                context.FillRect(new RectF(0, 0, 100, 100), Color.Black);
            },
        };
        var root = new DivElement { Style = new Style { Display = Display.Block, BackgroundColor = Color.White } };
        root.Children.Add(canvas);
        using var surface = engine.Graphics.CreateSurface(100, 60);

        engine.Initialize(root, new List<StyleSheet>(), surface.Context, 100, 60);

        received.ShouldNotBeNull();
        received.Graphics.ShouldBeSameAs(engine.Graphics);
        painted.ShouldBe(new RectF(5, 5, 40, 20));
        surface.GetPixel(10, 10).ShouldBe(Color.Black);
        surface.GetPixel(2, 2).ShouldBe(Color.White, "padding is outside the content box");
        surface.GetPixel(60, 10).ShouldBe(Color.White);
    }

    [Fact]
    public void Should_reject_drawing_contexts_it_did_not_create()
    {
        var engine = new MikoEngineBuilder().Build();

        Should.Throw<ArgumentException>(() =>
            engine.Initialize(new DivElement(), new List<StyleSheet>(), new RecordingContext(), 10, 10));
    }

    [Fact]
    public void Software_device_has_no_gpu_context()
    {
        var device = GraphicsDevice.Software;

        device.Backend.ShouldBe(GraphicsBackend.Software);
        device.ResourceCacheLimit.ShouldBe(0);
        device.WrapGlTexture(0x0DE1, 1, 0x8058, 4, 4, ImagePixelFormat.Rgba8888).ShouldBeNull();
        device.WrapMetalTexture(new IntPtr(1), 4, 4, ImagePixelFormat.Rgba8888).ShouldBeNull();
        Should.Throw<InvalidOperationException>(() => device.CreateWindowRenderTarget());
        using var surface = device.CreateSurface(8, 8);
        surface.ShouldNotBeNull();
        device.Dispose();
        GraphicsDevice.Software.CreateSurface(8, 8).ShouldNotBeNull("disposing the shared software device is a no-op");
    }

    private static int CountDark(RenderSurface surface, RectF area)
    {
        int count = 0;
        for (int y = (int)area.Top; y < (int)MathF.Ceiling(area.Bottom) && y < surface.Height; y++)
        for (int x = (int)area.Left; x < (int)MathF.Ceiling(area.Right) && x < surface.Width; x++)
            if (surface.GetPixel(x, y).R < 128) count++;
        return count;
    }

    /// <summary>A context an outside caller could write (e.g. to record a component's drawing calls).</summary>
    private sealed class RecordingContext : IDrawingContext
    {
        public IGraphicsFactory Graphics => DrawingContextTests.Graphics;
        public float DeviceScale => 1;
        public int Save() => 0;
        public void Restore() { }
        public void RestoreToCount(int count) { }
        public void Translate(float dx, float dy) { }
        public void Scale(float sx, float sy) { }
        public void Rotate(float degrees) { }
        public void ClipRect(RectF rect) { }
        public void ClipRoundRect(RectF rect, float radius) { }
        public void Clear(Color color) { }
        public void FillRect(RectF rect, Color color) { }
        public void StrokeRect(RectF rect, Color color, float strokeWidth) { }
        public void FillRoundRect(RectF rect, float radius, Color color) { }
        public void FillCircle(float centerX, float centerY, float radius, Color color) { }
        public void StrokeArc(RectF oval, float startAngle, float sweepAngle, Color color, float strokeWidth) { }
        public void StrokeLine(float x0, float y0, float x1, float y1, Color color, float strokeWidth) { }
        public void DrawText(TextBlock text, float x, float baseline, Color color) { }
        public void StrokeText(TextBlock text, float x, float baseline, Color color, float strokeWidth) { }
        public void DrawImage(Image image, RectF destination, ImageSampling sampling = ImageSampling.Auto, Color? tint = null) { }
    }
}
