using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.Motion;
using LiveChartsCore.MikoView;
using LiveChartsCore.MikoView.Drawing;
using LiveChartsCore.MikoView.Drawing.Geometries;
using LiveChartsCore.MikoView.Painting;
using LiveChartsCore.MikoView.Painting.Effects;
using LiveChartsCore.MikoView.Painting.ImageFilters;
using LiveChartsCore.Painting;
using Miko.Common;
using Miko.Graphics;
using Miko.Hosting;
using Shouldly;
using Padding = LiveChartsCore.Drawing.Padding;

namespace Miko.Charts.Tests;

/// <summary>
/// The Miko render layer on its own: geometries and paints drawn on a motion canvas into an offscreen surface.
/// </summary>
[Collection(ChartsCollection.Name)]
public class DrawingLayerTests : IDisposable
{
    private const int Size = 200;
    private readonly RenderSurface _surface;
    private readonly CoreMotionCanvas _canvas = new();

    public DrawingLayerTests()
    {
        var engine = new MikoEngineBuilder().Build();
        LiveChartsMiko.UseGraphics(engine.Graphics);
        _ = LiveChartsMiko.EnsureInitialized();
        _surface = engine.Graphics.CreateSurface(Size, Size);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _surface.Dispose();
    }

    private void Add(Paint paint, PaintStyle style, params IDrawnElement[] geometries)
    {
        paint.PaintStyle = style;
        foreach (var geometry in geometries) paint.AddGeometryToPaintTask(_canvas, geometry);
        _canvas.AddDrawableTask(paint);
    }

    private void Draw()
    {
        _surface.Context.Clear(Color.White);
        _canvas.DrawFrame(new MikoDrawingContext(_canvas, _surface.Context, new LvcColor(255, 255, 255), clearCanvasOnNewFrame: false));
    }

    private Color Pixel(int x, int y) => _surface.GetPixel(x, y);

    private static void ShouldBeClose(Color actual, Color expected, int tolerance = 12)
    {
        Math.Abs(actual.R - expected.R).ShouldBeLessThanOrEqualTo(tolerance, $"R of {actual}");
        Math.Abs(actual.G - expected.G).ShouldBeLessThanOrEqualTo(tolerance, $"G of {actual}");
        Math.Abs(actual.B - expected.B).ShouldBeLessThanOrEqualTo(tolerance, $"B of {actual}");
    }

    [Fact]
    public void Should_fill_a_rectangle_with_a_solid_color_paint()
    {
        Add(new SolidColorPaint(new LvcColor(255, 0, 0)), PaintStyle.Fill,
            new RectangleGeometry { X = 20, Y = 20, Width = 60, Height = 40 });

        Draw();

        ShouldBeClose(Pixel(50, 40), Color.FromRgb(255, 0, 0));
        ShouldBeClose(Pixel(100, 100), Color.White);
    }

    [Fact]
    public void Should_stroke_without_filling_when_the_paint_is_a_stroke()
    {
        Add(new SolidColorPaint(new LvcColor(0, 0, 255), 4), PaintStyle.Stroke,
            new RectangleGeometry { X = 20, Y = 20, Width = 100, Height = 100 });

        Draw();

        ShouldBeClose(Pixel(20, 70), Color.FromRgb(0, 0, 255));
        ShouldBeClose(Pixel(70, 70), Color.White);
    }

    [Fact]
    public void Should_apply_the_geometry_opacity()
    {
        Add(new SolidColorPaint(new LvcColor(0, 0, 0)), PaintStyle.Fill,
            new RectangleGeometry { X = 0, Y = 0, Width = 100, Height = 100, Opacity = 0.5f });

        Draw();

        // black at half opacity over white
        ShouldBeClose(Pixel(50, 50), Color.FromRgb(128, 128, 128), tolerance: 8);
    }

    [Fact]
    public void Should_lay_out_a_linear_gradient_across_the_area_drawn()
    {
        Add(new LinearGradientPaint(new LvcColor(255, 0, 0), new LvcColor(0, 0, 255)), PaintStyle.Fill,
            new RectangleGeometry { X = 0, Y = 0, Width = Size, Height = Size });

        Draw();

        // default direction: left (start color) to right (end color)
        ShouldBeClose(Pixel(2, 100), Color.FromRgb(255, 0, 0), tolerance: 16);
        ShouldBeClose(Pixel(Size - 3, 100), Color.FromRgb(0, 0, 255), tolerance: 16);
    }

    [Fact]
    public void Should_center_a_radial_gradient()
    {
        Add(new RadialGradientPaint(new LvcColor(255, 255, 0), new LvcColor(0, 128, 0)), PaintStyle.Fill,
            new RectangleGeometry { X = 0, Y = 0, Width = Size, Height = Size });

        Draw();

        ShouldBeClose(Pixel(100, 100), Color.FromRgb(255, 255, 0), tolerance: 16);
        ShouldBeClose(Pixel(2, 2), Color.FromRgb(0, 128, 0), tolerance: 16);
    }

    [Fact]
    public void Should_dash_a_stroke_with_a_dash_effect()
    {
        Add(new SolidColorPaint(new LvcColor(0, 0, 0), 6) { PathEffect = new DashEffect([20, 20]) }, PaintStyle.Stroke,
            new LineGeometry { X = 0, Y = 100, X1 = Size, Y1 = 100 });

        Draw();

        ShouldBeClose(Pixel(10, 100), Color.Black);
        ShouldBeClose(Pixel(30, 100), Color.White);
        ShouldBeClose(Pixel(50, 100), Color.Black);
    }

    [Fact]
    public void Should_draw_a_drop_shadow_beneath_the_shape()
    {
        Add(new SolidColorPaint(new LvcColor(255, 255, 255)) { ImageFilter = new DropShadow(10, 10, 0.1f, 0.1f, new LvcColor(0, 0, 0)) },
            PaintStyle.Fill, new RectangleGeometry { X = 20, Y = 20, Width = 60, Height = 60 });

        Draw();

        // the white shape hides its shadow except along the offset edges
        ShouldBeClose(Pixel(50, 50), Color.White);
        ShouldBeClose(Pixel(85, 85), Color.Black, tolerance: 30);
    }

    [Fact]
    public void Should_draw_a_doughnut_with_a_hole()
    {
        Add(new SolidColorPaint(new LvcColor(0, 160, 0)), PaintStyle.Fill,
            new DoughnutGeometry
            {
                X = 0, Y = 0, Width = Size, Height = Size, CenterX = 100, CenterY = 100,
                InnerRadius = 40, StartAngle = 0, SweepAngle = 359.9f,
            });

        Draw();

        ShouldBeClose(Pixel(100, 100), Color.White);
        ShouldBeClose(Pixel(170, 100), Color.FromRgb(0, 160, 0));
    }

    [Fact]
    public void Should_rotate_a_geometry_about_its_transform_origin()
    {
        // a 100x10 bar centered at (100, 100), rotated 90° about its center, stands up
        Add(new SolidColorPaint(new LvcColor(0, 0, 0)), PaintStyle.Fill,
            new RectangleGeometry { X = 50, Y = 95, Width = 100, Height = 10, RotateTransform = 90 });

        Draw();

        ShouldBeClose(Pixel(100, 60), Color.Black);
        ShouldBeClose(Pixel(60, 100), Color.White);
    }

    [Fact]
    public void Should_measure_a_label_by_its_text()
    {
        var paint = new SolidColorPaint(new LvcColor(0, 0, 0));
        var shortLabel = new LabelGeometry { Text = "1", TextSize = 14, Paint = paint };
        var longLabel = new LabelGeometry { Text = "1234567890", TextSize = 14, Paint = paint };
        var twoLines = new LabelGeometry { Text = "12\n34", TextSize = 14, Paint = paint };
        var padded = new LabelGeometry { Text = "1", TextSize = 14, Paint = paint, Padding = new Padding(10) };

        var one = shortLabel.Measure();

        one.Width.ShouldBeGreaterThan(0);
        one.Height.ShouldBeGreaterThan(10);
        longLabel.Measure().Width.ShouldBeGreaterThan(one.Width * 5);
        twoLines.Measure().Height.ShouldBe(one.Height * 2, 0.5);
        padded.Measure().Width.ShouldBe(one.Width + 20, 0.5);
        padded.Measure().Height.ShouldBe(one.Height + 20, 0.5);
    }

    [Fact]
    public void Should_wrap_a_label_at_its_max_width()
    {
        var paint = new SolidColorPaint(new LvcColor(0, 0, 0));
        var line = new LabelGeometry { Text = "alpha beta gamma delta", TextSize = 14, Paint = paint };
        var wrapped = new LabelGeometry { Text = "alpha beta gamma delta", TextSize = 14, Paint = paint, MaxWidth = 60 };

        wrapped.Measure().Height.ShouldBeGreaterThan(line.Measure().Height * 2);
        wrapped.Measure().Width.ShouldBeLessThanOrEqualTo(60.5f);
    }

    [Fact]
    public void Should_draw_label_text()
    {
        Add(new SolidColorPaint(new LvcColor(0, 0, 0)), PaintStyle.Text,
            new LabelGeometry { Text = "MIKO", TextSize = 40, X = 100, Y = 100, HorizontalAlign = Align.Middle, VerticalAlign = Align.Middle });

        Draw();

        var ink = 0;
        for (var y = 70; y < 130; y++)
            for (var x = 40; x < 160; x++)
                if (Pixel(x, y).R < 128) ink++;

        ink.ShouldBeGreaterThan(200);
    }

    [Fact]
    public void Should_lay_out_mixed_scripts_through_the_font_fallback()
    {
        var paint = new SolidColorPaint(new LvcColor(0, 0, 0));
        var latin = new LabelGeometry { Text = "ab", TextSize = 16, Paint = paint }.Measure();
        var mixed = new LabelGeometry { Text = "ab 销售 مبيعات", TextSize = 16, Paint = paint }.Measure();

        mixed.Width.ShouldBeGreaterThan(latin.Width * 3);
    }

    [Fact]
    public void Should_interpolate_solid_colors_in_a_transition()
    {
        var from = new SolidColorPaint(new LvcColor(0, 0, 0));
        var to = new SolidColorPaint(new LvcColor(200, 100, 50));

        var mid = (SolidColorPaint)PaintTransition(from, to, 0.5f);

        mid.Color.R.ShouldBe((byte)100);
        mid.Color.G.ShouldBe((byte)50);
        mid.Color.B.ShouldBe((byte)25);
    }

    [Fact]
    public void Should_clone_a_paint_with_its_settings()
    {
        var paint = new SolidColorPaint(new LvcColor(1, 2, 3), 5)
        {
            StrokeCap = StrokeCap.Round,
            FontFamily = "Courier New",
            PathEffect = new DashEffect([2, 2]),
        };

        var clone = (SolidColorPaint)paint.CloneTask();

        clone.ShouldNotBeSameAs(paint);
        clone.Color.ShouldBe(paint.Color);
        // as in LiveCharts, the numeric settings of a clone are the target end of a transition
        // (Map interpolates to = from + 1 * (to - from)), so the stroke thickness is not copied.
        clone.StrokeCap.ShouldBe(StrokeCap.Round);
        clone.FontFamily.ShouldBe("Courier New");
        clone.PathEffect.ShouldNotBeNull();
    }

    [Fact]
    public void Should_parse_paints_and_colors_from_hex_strings()
    {
        var paint = Paint.Parse("#ff8000").ShouldBeOfType<SolidColorPaint>();
        paint.Color.ShouldBe(new LvcColor(255, 128, 0));
    }

    [Fact]
    public void Should_draw_svg_path_geometries_scaled_into_their_box()
    {
        Add(new SolidColorPaint(new LvcColor(0, 0, 0)), PaintStyle.Fill,
            new VariableSVGPathGeometry { SVGPath = "M0 0 L10 0 L10 10 L0 10 Z", X = 50, Y = 50, Width = 100, Height = 100 });

        Draw();

        ShouldBeClose(Pixel(100, 100), Color.Black);
        ShouldBeClose(Pixel(20, 20), Color.White);
    }

    private static Paint PaintTransition(Paint from, Paint to, float progress)
    {
        // Paint.Transitionate is internal to the port; the tests have internals access
        return from.Transitionate(progress, to);
    }
}
