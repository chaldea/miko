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
using Miko.Graphics;

namespace LiveChartsCore.MikoView.Drawing.Geometries;

/// <summary>
/// Draws SVG paths scaled into a rectangle.
/// </summary>
public static class Svg
{
    /// <summary>
    /// Draws a path centered in the rectangle (<paramref name="x"/>, <paramref name="y"/>, <paramref name="width"/>,
    /// <paramref name="height"/>), scaled to fit.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    /// <param name="paint">The paint.</param>
    /// <param name="path">The path.</param>
    /// <param name="x">The x location.</param>
    /// <param name="y">The y location.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="fitToSize">Fits both axes to the rectangle instead of preserving the aspect ratio.</param>
    public static void Draw(
        MikoDrawingContext context,
        GraphicsPaint paint,
        GraphicsPath path,
        float x,
        float y,
        float width,
        float height,
        bool fitToSize = false)
    {
        var g = context.Context;
        _ = g.Save();

        var bounds = path.TightBounds;
        var midX = bounds.X + bounds.Width / 2f;
        var midY = bounds.Y + bounds.Height / 2f;

        if (fitToSize)
        {
            // fit to both axes
            g.Translate(x + width / 2f, y + height / 2f);
            g.Scale(
                width / (bounds.Width + paint.StrokeWidth),
                height / (bounds.Height + paint.StrokeWidth));
            g.Translate(-midX, -midY);
        }
        else
        {
            // fit to the max dimension, preserving the scale in the min axis.
            var maxB = bounds.Width < bounds.Height ? bounds.Height : bounds.Width;

            g.Translate(x + width / 2f, y + height / 2f);
            g.Scale(
                width / (maxB + paint.StrokeWidth),
                height / (maxB + paint.StrokeWidth));
            g.Translate(-midX, -midY);
        }

        g.DrawPath(path, paint);
        g.Restore();
    }
}
