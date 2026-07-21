using Miko.Common;
using Miko.Core.DomElements;
using Miko.Utils;

namespace Miko.Layout.LayoutAlgorithms;

/// <summary>
/// 文本节点布局算法（<see cref="TextNode"/>）。
///
/// 文本节点是匿名的行内级盒：没有 margin / border / padding，其内容盒即测量出的文本外接矩形。
/// 文本样式（字体、字号、字重、行高、空白处理）由样式继承从父元素获得。
/// 当存在宽度约束且允许换行时使用多行测量，否则单行测量。
///
/// 文本节点作为普通子盒进入父级的 Block / Inline / Flex 布局，交错顺序由子节点列表表达
/// （见 ISSUE-086）。这取代了历史上把父元素 <c>TextContent</c> 作为「排在子元素之前的匿名盒」
/// 的特例处理。
/// </summary>
public class TextLayout
{
    public void Layout(LayoutBox box, LayoutConstraints constraints, float x, float y)
    {
        var style = box.ComputedStyle;
        // 应用 text-transform 后再测量，确保尺寸与绘制一致（大写通常更宽）。
        var text = TextTransformer.Apply(box.Element.TextContent, style.TextTransform);

        // 文本节点无 margin / border / padding。
        box.BoxModel.Margin = new EdgeSizes(0, 0, 0, 0);
        box.BoxModel.Border = new EdgeSizes(0, 0, 0, 0);
        box.BoxModel.Padding = new EdgeSizes(0, 0, 0, 0);

        if (string.IsNullOrEmpty(text))
        {
            box.BoxModel.Content = new RectF(x, y, 0, 0);
            return;
        }

        // 行高优先使用显式 line-height，否则取字体自然度量（与其它布局算法一致）。
        float resolvedLineHeight = BlockLayout.ResolveLineHeight(style);

        // letter-spacing 与长单词断行（word-break / overflow-wrap）参与测量。
        float letterSpacing = style.LetterSpacing.ToPixels(0, style.FontSize.Value);
        bool breakLongWords = TextWrapper.ShouldBreakLongWords(style.WordBreak, style.OverflowWrap);

        float width;
        float height;

        bool canWrap = style.WhiteSpace == WhiteSpace.Normal
                    || style.WhiteSpace == WhiteSpace.PreWrap
                    || style.WhiteSpace == WhiteSpace.PreLine;

        // pre 不做软换行，但保留的显式换行符（及 Tab 展开）需要多行测量路径，
        // 否则多行 pre 文本会被按单行测量（高度塌缩为一行、宽度含换行符）。
        bool needsMultilineMeasure = style.WhiteSpace == WhiteSpace.Pre;

        if ((canWrap && constraints.AvailableWidth.HasValue && constraints.AvailableWidth.Value > 0)
            || needsMultilineMeasure)
        {
            // pre 无换行宽度概念（不软换行），可用宽度缺省时给一个足够大的值；
            // MeasureTextWithWrap 的非换行路径只按显式换行符分行，不使用该宽度。
            float availWidth = constraints.AvailableWidth is > 0 ? constraints.AvailableWidth.Value : float.MaxValue;
            var textNode = (TextNode)box.Element;

            // 测量缓存（ISSUE-096）：以全部测量输入为键，命中则跳过换行算法的全部分配。
            if (textNode.McText == text
                && textNode.McFontFamily == style.FontFamily
                && textNode.McFontSize == style.FontSize.Value
                && textNode.McFontWeight == style.FontWeight
                && textNode.McAvailWidth == availWidth
                && textNode.McLineHeight == resolvedLineHeight
                && textNode.McWhiteSpace == style.WhiteSpace
                && textNode.McBreakLongWords == breakLongWords
                && textNode.McLetterSpacing == letterSpacing)
            {
                (width, height) = textNode.McResult;
            }
            else
            {
                var (wrappedWidth, wrappedHeight) = TextMeasurer.MeasureTextWithWrap(
                    text,
                    style.FontFamily,
                    style.FontSize.Value,
                    style.FontWeight,
                    availWidth,
                    resolvedLineHeight,
                    style.WhiteSpace,
                    breakLongWords,
                    letterSpacing);

                textNode.McText = text;
                textNode.McFontFamily = style.FontFamily;
                textNode.McFontSize = style.FontSize.Value;
                textNode.McFontWeight = style.FontWeight;
                textNode.McAvailWidth = availWidth;
                textNode.McLineHeight = resolvedLineHeight;
                textNode.McWhiteSpace = style.WhiteSpace;
                textNode.McBreakLongWords = breakLongWords;
                textNode.McLetterSpacing = letterSpacing;
                textNode.McResult = (wrappedWidth, wrappedHeight);

                width = wrappedWidth;
                height = wrappedHeight;
            }
        }
        else
        {
            var (textWidth, _) = TextMeasurer.MeasureText(
                text,
                style.FontFamily,
                style.FontSize.Value,
                style.FontWeight);
            width = textWidth + TextMeasurer.LetterSpacingExtent(text, letterSpacing);
            height = resolvedLineHeight;
        }

        box.BoxModel.Content = new RectF(x, y, width, height);
    }
}
