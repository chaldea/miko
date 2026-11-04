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
using LiveChartsCore.Generators;
using Miko.Common;

namespace LiveChartsCore.MikoView.Drawing.Geometries;

/// <summary>
/// Defines a rectangle geometry that carries its own color, normally used by heat maps.
/// </summary>
public partial class ColoredRectangleGeometry : BoundedDrawnGeometry, IColoredGeometry, IDrawnElement<MikoDrawingContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColoredRectangleGeometry"/> class.
    /// </summary>
    public ColoredRectangleGeometry()
    {
        _ColorMotionProperty = new(LvcColor.Empty);
    }

    /// <inheritdoc cref="IColoredGeometry.Color"/>
    [MotionProperty]
    public partial LvcColor Color { get; set; }

    /// <inheritdoc cref="IDrawnElement{TDrawingContext}.Draw(TDrawingContext)"/>
    public virtual void Draw(MikoDrawingContext context)
    {
        // this geometry modifies the paint of the context, it is used in heat maps where
        // every cell has its own color, and the paint is reset by the next paint start.
        var c = Color;
        var paint = context.ActiveMikoPaint!;
        paint.Color = new Miko.Common.Color(c.R, c.G, c.B, c.A);
        context.Context.DrawRect(new RectF(X, Y, Width, Height), paint);
    }
}
