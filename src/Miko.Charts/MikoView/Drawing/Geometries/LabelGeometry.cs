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

using System.Text;
using LiveChartsCore.Drawing;
using LiveChartsCore.MikoView.Painting;
using Miko.Common;
using Padding = LiveChartsCore.Drawing.Padding;
using Miko.Graphics;

namespace LiveChartsCore.MikoView.Drawing.Geometries;

/// <summary>
/// Defines a label (text) geometry.
/// </summary>
/// <remarks>
/// The text is laid out by Miko's text pipeline (font fallback, bidi, shaping), one <see cref="TextBlock"/>
/// per line. Lines break at '\n' and, when <see cref="BaseLabelGeometry.MaxWidth"/> is set, between words.
/// The layout is cached until the text, size, padding, max width or font change.
/// </remarks>
public class LabelGeometry : BaseLabelGeometry, IDrawnElement<MikoDrawingContext>
{
    private (string Text, float Size, Padding Padding, float MaxWidth, string Family, FontWeight Weight) _previousKey =
        (string.Empty, 0, new(), 0, string.Empty, FontWeight.Normal);

    private TextLayout _layout = TextLayout.Empty;

    /// <summary>
    /// Gets the laid out text, rebuilt when anything that affects it changed.
    /// </summary>
    internal TextLayout Layout
    {
        get
        {
            var key = BuildKey();
            if (key == _previousKey || string.IsNullOrEmpty(Text)) return _layout;

            _layout = TextLayout.Create(Text, TextSize, key.Family, key.Weight, MaxWidth, Padding);
            _previousKey = key;
            return _layout;
        }
    }

    /// <inheritdoc cref="IDrawnElement{TDrawingContext}.Draw(TDrawingContext)" />
    public virtual void Draw(MikoDrawingContext context)
    {
        var paint = context.ActiveMikoPaint
            ?? throw new InvalidOperationException("A paint is required to draw a label.");
        var opacity = Opacity * context.ActiveOpacity;
        var layout = Layout;
        var size = layout.Size;

        var x = X;
        var y = Y;

        // the alignment is relative to the x and y coordinates
        var rax = x + GetRelativeX(HorizontalAlign, size);
        var ray = y + GetRelativeY(VerticalAlign, size);

        var g = context.Context;

        if (Background != LvcColor.Empty)
        {
            var bg = Background;
            using var background = new GraphicsPaint(new Color(bg.R, bg.G, bg.B, (byte)(bg.A * opacity)));
            g.DrawRect(new RectF(rax, ray, size.Width, size.Height), background);
        }

        var horizontalPadding = Padding.Left + Padding.Right;

        foreach (var line in layout.Lines)
        {
            // right to left text is anchored to the right edge of the label
            var lineOffset = layout.IsRtl ? size.Width - horizontalPadding - line.Width : 0f;

            // truncate to avoid subpixel rendering issues
            g.DrawText(line.Text, (int)(rax + line.X + lineOffset), (int)(ray + line.Baseline), paint);
        }
    }

    /// <inheritdoc cref="IDrawnElement.Measure"/>
    public override LvcSize Measure()
    {
        return Paint is null
            ? throw new InvalidOperationException(
                $"A paint is required to measure a label, please set the {nameof(Paint)} " +
                $"property with the paint that is drawing the label.")
            : Layout.Size.GetRotatedSize(RotateTransform);
    }

    private (string Text, float Size, Padding Padding, float MaxWidth, string Family, FontWeight Weight) BuildKey()
    {
        var paint = Paint as MikoPaint;
        return (Text, TextSize, Padding, MaxWidth, MikoPaint.ResolveFontFamily(paint), paint?.FontWeight ?? FontWeight.Normal);
    }

    private static float GetRelativeX(Align align, LvcSize size) => align switch
    {
        Align.Start => 0f,
        Align.Middle => -size.Width / 2f,
        Align.End => -size.Width,
        _ => throw new ArgumentOutOfRangeException(nameof(align), align, null)
    };

    private static float GetRelativeY(Align align, LvcSize size) => align switch
    {
        Align.Start => 0f,
        Align.Middle => -size.Height / 2f,
        Align.End => -size.Height,
        _ => throw new ArgumentOutOfRangeException(nameof(align), align, null)
    };

    /// <summary>One positioned line of a label.</summary>
    internal readonly record struct TextLine(TextBlock Text, float X, float Baseline, float Width);

    /// <summary>The lines of a label and the size they take, padding included.</summary>
    internal sealed class TextLayout
    {
        public static readonly TextLayout Empty = new([], new LvcSize(0, 0), false);

        private TextLayout(TextLine[] lines, LvcSize size, bool isRtl)
        {
            Lines = lines;
            Size = size;
            IsRtl = isRtl;
        }

        public TextLine[] Lines { get; }

        public LvcSize Size { get; }

        public bool IsRtl { get; }

        public static TextLayout Create(
            string text, float fontSize, string family, FontWeight weight, float maxWidth, Padding padding)
        {
            var factory = LiveChartsMiko.Graphics;
            var style = new TextStyle(family, fontSize, weight);
            var contentWidth = maxWidth - padding.Left - padding.Right;

            var lines = new List<(TextBlock Block, float Width)>();
            var isRtl = false;

            foreach (var paragraph in text.Replace("\r", string.Empty).Split('\n'))
            {
                foreach (var line in Wrap(paragraph, style, factory, contentWidth))
                {
                    var block = factory.CreateText(line, style);
                    lines.Add((block, block.Width));
                    isRtl |= ContainsRtl(line);
                }
            }

            // line height from the first font; an empty line still takes a line
            var probe = factory.CreateText("A", style);
            var lineHeight = probe.Height;

            var positioned = new TextLine[lines.Count];
            var width = 0f;
            for (var i = 0; i < lines.Count; i++)
            {
                var (block, w) = lines[i];
                positioned[i] = new TextLine(block, padding.Left, padding.Top + i * lineHeight + probe.Ascent, w);
                if (w > width) width = w;
            }

            var size = new LvcSize(
                width + padding.Left + padding.Right,
                lines.Count * lineHeight + padding.Top + padding.Bottom);

            return new TextLayout(positioned, size, isRtl);
        }

        /// <summary>Splits a paragraph into lines no wider than <paramref name="maxWidth"/> at spaces.</summary>
        private static IEnumerable<string> Wrap(string paragraph, TextStyle style, IGraphicsFactory factory, float maxWidth)
        {
            if (paragraph.Length == 0 || maxWidth >= float.MaxValue / 2 || maxWidth <= 0)
            {
                yield return paragraph;
                yield break;
            }

            var line = new StringBuilder();
            foreach (var word in paragraph.Split(' '))
            {
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && factory.CreateText(candidate, style).Width > maxWidth)
                {
                    yield return line.ToString();
                    line.Clear();
                    line.Append(word);
                }
                else
                {
                    line.Clear();
                    line.Append(candidate);
                }
            }

            yield return line.ToString();
        }

        private static bool ContainsRtl(string text)
        {
            foreach (var c in text)
            {
                if (c is >= '\u0590' and <= '\u08FF' or >= '\uFB1D' and <= '\uFDFF' or >= '\uFE70' and <= '\uFEFC')
                    return true;
            }

            return false;
        }
    }
}
