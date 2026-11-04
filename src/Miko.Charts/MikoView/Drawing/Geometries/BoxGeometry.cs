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
using Miko.Common;

namespace LiveChartsCore.MikoView.Drawing.Geometries;

/// <summary>
/// Defines a box (box and whiskers) geometry.
/// </summary>
public class BoxGeometry : BaseBoxGeometry, IDrawnElement<MikoDrawingContext>
{
    /// <inheritdoc cref="IDrawnElement{TDrawingContext}.Draw(TDrawingContext)"/>
    public virtual void Draw(MikoDrawingContext context)
    {
        var paint = context.ActiveMikoPaint!;
        var g = context.Context;

        var w = Width;
        var cx = X + w * 0.5f;
        var h = Y;
        var o = Third;
        var c = First;
        var l = Min;
        var m = Median;
        var x = X;

        float yi, yj;

        if (o > c)
        {
            yi = c;
            yj = o;
        }
        else
        {
            yi = o;
            yj = c;
        }

        if (paint.IsStroke)
        {
            g.DrawLine(cx, h, cx, yi, paint);
            g.DrawLine(x, m, x + w, m, paint);
            g.DrawLine(cx, yj, cx, l, paint);
        }
        else
        {
            g.DrawRect(new RectF(x, yi, w, Math.Abs(o - c)), paint);
        }

        g.DrawRect(new RectF(x, yi, w, Math.Abs(o - c)), paint);
    }
}
