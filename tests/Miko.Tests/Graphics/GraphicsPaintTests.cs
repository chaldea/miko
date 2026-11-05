using Miko.Common;
using Miko.Graphics;
using Miko.Hosting;
using Shouldly;

namespace Miko.Tests.Graphics;

/// <summary>
/// The paint-based drawing of <see cref="IDrawingContext"/>: <see cref="GraphicsPath"/>, <see cref="GraphicsPaint"/>
/// (fill and stroke, gradients, dashes, shadows), <see cref="IDrawingContext.Transform"/> and
/// <see cref="IDrawingContext.ClipBounds"/>. Public API only, as an outside caller (Miko.Charts) uses it.
/// </summary>
public class GraphicsPaintTests
{
    private static readonly IGraphicsFactory Graphics = new MikoEngineBuilder().Build().Graphics;

    private static RenderSurface White(int width, int height)
    {
        var surface = Graphics.CreateSurface(width, height);
        surface.Context.Clear(Color.White);
        return surface;
    }

    private static void ShouldBeClose(Color actual, Color expected, int tolerance = 10)
    {
        Math.Abs(actual.R - expected.R).ShouldBeLessThanOrEqualTo(tolerance, $"R of {actual}, expected {expected}");
        Math.Abs(actual.G - expected.G).ShouldBeLessThanOrEqualTo(tolerance, $"G of {actual}, expected {expected}");
        Math.Abs(actual.B - expected.B).ShouldBeLessThanOrEqualTo(tolerance, $"B of {actual}, expected {expected}");
    }

    [Fact]
    public void Should_fill_a_path_and_leave_the_outside_untouched()
    {
        using var surface = White(40, 40);
        using var path = new GraphicsPath();
        path.MoveTo(0, 0);
        path.LineTo(40, 0);
        path.LineTo(0, 40);
        path.Close();
        using var paint = new GraphicsPaint(new Color(255, 0, 0));

        surface.Context.DrawPath(path, paint);

        surface.GetPixel(5, 5).ShouldBe(new Color(255, 0, 0));
        surface.GetPixel(35, 35).ShouldBe(Color.White);
    }

    [Fact]
    public void Should_stroke_with_the_paint_width_instead_of_filling()
    {
        using var surface = White(40, 40);
        using var paint = new GraphicsPaint(Color.Black) { IsStroke = true, StrokeWidth = 4 };

        surface.Context.DrawRect(new RectF(10, 10, 20, 20), paint);

        surface.GetPixel(10, 20).ShouldBe(Color.Black);
        surface.GetPixel(20, 20).ShouldBe(Color.White, "a stroke must not fill");
    }

    [Fact]
    public void Should_report_the_tight_bounds_of_a_path()
    {
        using var path = new GraphicsPath();
        path.MoveTo(10, 20);
        path.CubicTo(40, 0, 60, 40, 90, 20);

        var bounds = path.TightBounds;

        bounds.X.ShouldBe(10, 0.01);
        bounds.Right.ShouldBe(90, 0.01);
        bounds.Y.ShouldBeGreaterThan(5);      // the control point at y = 0 is not on the curve
        bounds.Bottom.ShouldBeLessThan(35);
    }

    [Fact]
    public void Should_parse_svg_path_data_and_reject_garbage()
    {
        using var path = GraphicsPath.FromSvgPathData("M0 0 L10 0 L10 10 Z").ShouldNotBeNull();
        path.IsEmpty.ShouldBeFalse();
        path.TightBounds.Width.ShouldBe(10, 0.01);

        GraphicsPath.FromSvgPathData("this is not a path").ShouldBeNull();
    }

    [Fact]
    public void Should_append_arcs_in_degrees_clockwise_from_the_right()
    {
        // a quarter arc from 0° to 90° of the circle inscribed in (0,0,40,40) runs from (40,20) to (20,40)
        using var path = new GraphicsPath();
        path.ArcTo(new RectF(0, 0, 40, 40), 0, 90, forceMoveTo: true);

        var bounds = path.TightBounds;
        bounds.X.ShouldBe(20, 0.5);
        bounds.Y.ShouldBe(20, 0.5);
        bounds.Right.ShouldBe(40, 0.5);
        bounds.Bottom.ShouldBe(40, 0.5);
    }

    [Fact]
    public void Should_fill_with_a_linear_gradient_between_its_points()
    {
        using var surface = White(100, 10);
        using var paint = new GraphicsPaint();
        paint.SetLinearGradient(0, 0, 100, 0, [new Color(255, 0, 0), new Color(0, 0, 255)]);

        surface.Context.DrawRect(new RectF(0, 0, 100, 10), paint);

        ShouldBeClose(surface.GetPixel(1, 5), new Color(255, 0, 0), 8);
        ShouldBeClose(surface.GetPixel(50, 5), new Color(128, 0, 128), 12);
        ShouldBeClose(surface.GetPixel(98, 5), new Color(0, 0, 255), 8);
    }

    [Fact]
    public void Should_fill_with_a_radial_gradient_around_its_center()
    {
        using var surface = White(100, 100);
        using var paint = new GraphicsPaint();
        paint.SetRadialGradient(50, 50, 50, [Color.Black, Color.White]);

        surface.Context.DrawRect(new RectF(0, 0, 100, 100), paint);

        ShouldBeClose(surface.GetPixel(50, 50), Color.Black, 8);
        ShouldBeClose(surface.GetPixel(1, 1), Color.White, 8);
    }

    [Fact]
    public void Should_go_back_to_the_solid_color_when_the_gradient_is_cleared()
    {
        using var surface = White(10, 10);
        using var paint = new GraphicsPaint(new Color(0, 160, 0));
        paint.SetLinearGradient(0, 0, 10, 0, [Color.Black, Color.Black]);
        paint.ClearGradient();

        surface.Context.DrawRect(new RectF(0, 0, 10, 10), paint);

        surface.GetPixel(5, 5).ShouldBe(new Color(0, 160, 0));
    }

    [Fact]
    public void Should_multiply_the_opacity_into_the_color()
    {
        using var surface = White(10, 10);
        using var paint = new GraphicsPaint(Color.Black) { Opacity = 0.5f };

        surface.Context.DrawRect(new RectF(0, 0, 10, 10), paint);

        ShouldBeClose(surface.GetPixel(5, 5), new Color(128, 128, 128), 4);
    }

    [Fact]
    public void Should_dash_a_stroke()
    {
        using var surface = White(100, 10);
        using var paint = new GraphicsPaint(Color.Black) { IsStroke = true, StrokeWidth = 4 };
        paint.SetDash([10, 10]);

        surface.Context.DrawLine(0, 5, 100, 5, paint);

        surface.GetPixel(5, 5).ShouldBe(Color.Black);
        surface.GetPixel(15, 5).ShouldBe(Color.White);
        surface.GetPixel(25, 5).ShouldBe(Color.Black);

        paint.ClearDash();
        surface.Context.DrawLine(0, 5, 100, 5, paint);
        surface.GetPixel(15, 5).ShouldBe(Color.Black, "clearing the dash makes the line solid again");
    }

    [Fact]
    public void Should_reject_a_dash_pattern_with_an_odd_number_of_lengths()
    {
        using var paint = new GraphicsPaint();
        Should.Throw<ArgumentException>(() => paint.SetDash([4, 2, 4]));
        Should.Throw<ArgumentException>(() => paint.SetDash([4]));
    }

    [Fact]
    public void Should_draw_a_drop_shadow_offset_from_the_shape()
    {
        using var surface = White(60, 60);
        using var paint = new GraphicsPaint(Color.White);
        paint.SetDropShadow(10, 10, 0.01f, 0.01f, Color.Black);

        surface.Context.DrawRect(new RectF(10, 10, 30, 30), paint);

        surface.GetPixel(25, 25).ShouldBe(Color.White, "the shape is drawn above its shadow");
        ShouldBeClose(surface.GetPixel(45, 45), Color.Black, 30);

        paint.ClearFilter();
        surface.Context.Clear(Color.White);
        surface.Context.DrawRect(new RectF(10, 10, 30, 30), paint);
        surface.GetPixel(45, 45).ShouldBe(Color.White, "clearing the filter removes the shadow");
    }

    [Fact]
    public void Should_draw_ovals_and_rounded_rectangles()
    {
        using var surface = White(100, 50);
        using var paint = new GraphicsPaint(Color.Black);

        surface.Context.DrawOval(25, 25, 20, 10, paint);
        surface.Context.DrawRoundRect(new RectF(55, 5, 40, 40), 15, 15, paint);

        surface.GetPixel(25, 25).ShouldBe(Color.Black);
        surface.GetPixel(25, 10).ShouldBe(Color.White, "the oval is 10 high on each side");
        surface.GetPixel(75, 25).ShouldBe(Color.Black);
        surface.GetPixel(56, 6).ShouldBe(Color.White, "the corner is rounded");
    }

    [Fact]
    public void Should_concatenate_an_affine_transform()
    {
        using var surface = White(40, 40);
        using var paint = new GraphicsPaint(Color.Black);

        // scale x2 then move by (10, 10): a 5x5 square at the origin covers (10,10)-(20,20)
        var save = surface.Context.Save();
        surface.Context.Transform(2, 0, 0, 2, 10, 10);
        surface.Context.DrawRect(new RectF(0, 0, 5, 5), paint);
        surface.Context.RestoreToCount(save);

        surface.GetPixel(15, 15).ShouldBe(Color.Black);
        surface.GetPixel(5, 5).ShouldBe(Color.White);
        surface.GetPixel(25, 25).ShouldBe(Color.White);
    }

    [Fact]
    public void Should_report_the_clip_in_the_current_coordinates()
    {
        using var surface = White(100, 100);
        var context = surface.Context;

        context.ClipBounds.Width.ShouldBe(100, 0.5);

        var save = context.Save();
        context.ClipRect(new RectF(20, 30, 40, 50));
        context.Translate(20, 30);
        var clip = context.ClipBounds;
        context.RestoreToCount(save);

        clip.X.ShouldBe(0, 0.5);
        clip.Y.ShouldBe(0, 0.5);
        clip.Width.ShouldBe(40, 0.5);
        clip.Height.ShouldBe(50, 0.5);
    }

    [Fact]
    public void Should_draw_text_with_a_paint()
    {
        using var surface = White(120, 40);
        using var paint = new GraphicsPaint(Color.Black);
        var text = Graphics.CreateText("MIKO", new TextStyle("Arial", 28));

        surface.Context.DrawText(text, 4, 4 + text.Ascent, paint);

        var ink = 0;
        for (var y = 0; y < 40; y++)
            for (var x = 0; x < 120; x++)
                if (surface.GetPixel(x, y).R < 128) ink++;
        ink.ShouldBeGreaterThan(80);
    }

    [Fact]
    public void Should_not_draw_after_a_path_is_disposed()
    {
        var path = new GraphicsPath();
        path.LineTo(10, 10);
        path.Dispose();

        Should.Throw<ObjectDisposedException>(() => path.LineTo(20, 20));
    }
}
