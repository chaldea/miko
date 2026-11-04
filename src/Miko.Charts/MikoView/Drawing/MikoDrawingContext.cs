// The MIT License(MIT)
//
// Copyright(c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using LiveChartsCore.Drawing;
using LiveChartsCore.Motion;
using LiveChartsCore.Painting;
using LiveChartsCore.MikoView.Painting;
using Miko.Common;
using Miko.Graphics;

namespace LiveChartsCore.MikoView.Drawing;

/// <summary>
/// Drawing context that wraps Miko.Graphics.IDrawingContext.
/// </summary>
public class MikoDrawingContext : DrawingContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MikoDrawingContext"/> class.
    /// </summary>
    /// <param name="motionCanvas">The motion canvas.</param>
    /// <param name="context">The Miko drawing context.</param>
    /// <param name="background">The background color.</param>
    /// <param name="clearCanvasOnNewFrame">Whether the canvas should be cleared at the beginning of each frame.</param>
    public MikoDrawingContext(
        CoreMotionCanvas motionCanvas,
        IDrawingContext context,
        LvcColor background,
        bool clearCanvasOnNewFrame = true)
    {
        MotionCanvas = motionCanvas;
        Context = context;
        Background = background;
        ClearCanvasOnNewFrame = clearCanvasOnNewFrame;
    }

    /// <summary>
    /// Gets a value indicating whether the canvas is cleared at the beginning of each frame.
    /// </summary>
    public bool ClearCanvasOnNewFrame { get; }

    /// <summary>
    /// Gets or sets the motion canvas.
    /// </summary>
    public CoreMotionCanvas MotionCanvas { get; }

    /// <summary>
    /// Gets or sets the Miko drawing context.
    /// </summary>
    public IDrawingContext Context { get; }

    /// <summary>
    /// Gets or sets the active paint.
    /// </summary>
    public GraphicsPaint? ActiveMikoPaint { get; internal set; }

    /// <summary>
    /// Gets or sets the background.
    /// </summary>
    public LvcColor Background { get; }

    /// <inheritdoc cref="DrawingContext.LogOnCanvas(string)"/>
    public override void LogOnCanvas(string log)
    {
        var textStyle = new TextStyle("Arial", 14);
        var lines = log.Split('`');

        var bgColor = new Color(0, 0, 0, 180);
        Context.FillRect(new RectF(10, 0, 400, (14 + 4f) * lines.Length), bgColor);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            var textBlock = Context.Graphics.CreateText(line, textStyle);
            Context.DrawText(textBlock, 10, 10 + 2 + (14 + 4f) * i + 14, Color.FromRgb(255, 255, 255));
        }
    }

    internal override void OnBeginDraw()
    {
        if (!ClearCanvasOnNewFrame)
            return;

        var bg = Background;
        Context.Clear(new Color(bg.R, bg.G, bg.B, bg.A));
    }

    internal override void OnEndDraw()
    {
        // No cleanup is needed at the end of the draw operation.
    }

    internal override void OnBeginZone(CanvasZone zone)
    {
        if (zone.Clip == LvcRectangle.Empty) return;

        zone.StateId = Context.Save();
        Context.ClipRect(new RectF(zone.Clip.X, zone.Clip.Y, zone.Clip.Width, zone.Clip.Height));
    }

    internal override void OnEndZone(CanvasZone zone)
    {
        if (zone.Clip == LvcRectangle.Empty) return;

        Context.RestoreToCount(zone.StateId);
    }

    internal override void Draw(IDrawnElement drawable)
    {
        var opacity = ActiveOpacity;

        var element = (IDrawnElement<MikoDrawingContext>)drawable;

        var canvasState = 0;
        if (element.HasTransform)
        {
            canvasState = Context.Save();
            var transform = BuildTransform(element);
            Context.Transform(transform.A, transform.B, transform.C, transform.D, transform.E, transform.F);
        }

        if (ActiveLvcPaint is null)
        {
            // if the active paint is null, we need to draw by the element paint

            var elementFill = element.Fill;
            var elementStroke = element.Stroke;
            var elementPaint = element.Paint;

            if (elementFill is not null)
                DrawByPaint(elementFill, element, opacity);

            if (elementStroke is not null)
                DrawByPaint(elementStroke, element, opacity);

            if (elementPaint is not null)
                DrawByPaint(elementPaint, element, opacity);
        }
        else
        {
            // we will draw using the active paint while the element paint is null

            if (ActiveLvcPaint.PaintStyle.HasFlag(PaintStyle.Fill))
            {
                var elementFill = element.Fill;

                if (elementFill is null)
                    DrawElement(element, opacity);
                else
                    DrawByPaint(elementFill, element, opacity);
            }

            if (ActiveLvcPaint.PaintStyle.HasFlag(PaintStyle.Stroke))
            {
                var elementStroke = element.Stroke;

                if (elementStroke is null)
                    DrawElement(element, opacity);
                else
                    DrawByPaint(elementStroke, element, opacity);
            }
        }

        if (element.HasTransform)
        {
            Context.RestoreToCount(canvasState);
        }
    }

    internal override void SelectPaint(Paint paint)
    {
        ActiveLvcPaint = paint;
        PaintMotionProperty.s_activePaint = paint;

        paint.OnPaintStarted(this, null);
    }

    internal override void ClearPaintSelection(Paint paint)
    {
        paint.OnPaintFinished(this, null);

        ActiveLvcPaint = null!;
        ActiveMikoPaint = null!;
        PaintMotionProperty.s_activePaint = null!;
    }

    private void DrawByPaint(Paint paint, IDrawnElement<MikoDrawingContext> element, float opacity)
    {
        var originalPaint = ActiveMikoPaint;
        var originalTask = ActiveLvcPaint;

        // hack for now...
        // ActiveLvcPaint must be null for this kind of draw method...
        // normally used to draw tooltips and legends
        // Improve this? maybe a cleaner way?
        if (paint != MeasureTask.Instance)
        {
            ActiveLvcPaint = paint;
            paint.OnPaintStarted(this, element);
        }

        DrawElement(element, opacity);

        paint.OnPaintFinished(this, element);

        ActiveMikoPaint = originalPaint;
        ActiveLvcPaint = originalTask;
    }

    private void DrawElement(IDrawnElement<MikoDrawingContext> element, float opacity)
    {
        var hasGeometryOpacity =
            ActiveLvcPaint is not null &&
            opacity < 1;

        var hasShadow =
            ActiveLvcPaint is not null &&
            element.DropShadow is not null &&
            element.DropShadow != LvcDropShadow.Empty;

        if (hasGeometryOpacity)
        {
            ActiveLvcPaint!.ApplyOpacityMask(this, opacity, element);
        }

        if (hasShadow && ActiveMikoPaint is not null)
        {
            var shadow = element.DropShadow!;
            var color = new Color(shadow.Color.R, shadow.Color.G, shadow.Color.B, shadow.Color.A);
            ActiveMikoPaint.SetDropShadow(shadow.Dx, shadow.Dy, shadow.SigmaX, shadow.SigmaY, color);
        }

        element.Draw(this);

        if (hasShadow && ActiveMikoPaint is not null)
        {
            // put back the paint's own filter (a tooltip background has a drop shadow of its own)
            if (ActiveLvcPaint is MikoPaint mikoPaint) mikoPaint.RestoreImageFilter();
            else ActiveMikoPaint.ClearFilter();
        }

        if (hasGeometryOpacity)
        {
            ActiveLvcPaint!.RestoreOpacityMask(this, opacity, element);
        }
    }

    private static Affine BuildTransform(IDrawnElement<MikoDrawingContext> element)
    {
        var m = element.Measure();
        var o = element.TransformOrigin;
        var xo = m.Width * o.X;
        var yo = m.Height * o.Y;

        var originX = element.X + xo;
        var originY = element.Y + yo;
        var matrix = Affine.Identity;

        // same composition order as the SkiaSharp view: matrix = matrix * next.
        if (element.HasTranslate)
        {
            var t = element.TranslateTransform;
            matrix = Affine.Concat(matrix, Affine.Translation(t.X, t.Y));
        }

        if (element.HasRotation)
        {
            matrix = Affine.Concat(matrix, Affine.Rotation(element.RotateTransform, originX, originY));
        }

        if (element.HasScale)
        {
            var s = element.ScaleTransform;
            matrix = Affine.Concat(matrix, Affine.Scale(s.X, s.Y, originX, originY));
        }

        if (element.HasSkew)
        {
            var skew = element.SkewTransform;
            var skewMatrix = new Affine(
                1, (float)Math.Tan(skew.Y * Math.PI / 180),
                (float)Math.Tan(skew.X * Math.PI / 180), 1,
                0, 0);

            matrix = Affine.Concat(matrix, Affine.Translation(originX, originY));
            matrix = Affine.Concat(matrix, skewMatrix);
            matrix = Affine.Concat(matrix, Affine.Translation(-originX, -originY));
        }

        return matrix;
    }

    /// <summary>
    /// A 2D affine matrix in the SVG/Canvas layout [a c e; b d f], applied to column vectors, so
    /// <c>Concat(first, second)</c> applies <c>second</c> to a point before <c>first</c> (as <c>SKMatrix.Concat</c>).
    /// </summary>
    private readonly record struct Affine(float A, float B, float C, float D, float E, float F)
    {
        public static Affine Identity => new(1, 0, 0, 1, 0, 0);

        public static Affine Translation(float dx, float dy) => new(1, 0, 0, 1, dx, dy);

        public static Affine Rotation(float degrees, float px, float py)
        {
            var radians = degrees * Math.PI / 180;
            var sin = (float)Math.Sin(radians);
            var cos = (float)Math.Cos(radians);

            // rotation about (px, py): T(p) * R * T(-p)
            return new(
                cos, sin,
                -sin, cos,
                px - cos * px + sin * py,
                py - sin * px - cos * py);
        }

        public static Affine Scale(float sx, float sy, float px, float py) =>
            new(sx, 0, 0, sy, px - sx * px, py - sy * py);

        public static Affine Concat(Affine first, Affine second) => new(
            first.A * second.A + first.C * second.B,
            first.B * second.A + first.D * second.B,
            first.A * second.C + first.C * second.D,
            first.B * second.C + first.D * second.D,
            first.A * second.E + first.C * second.F + first.E,
            first.B * second.E + first.D * second.F + first.F);
    }
}
