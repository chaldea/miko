using Miko.Common;
using Miko.Core.DomElements;
using Miko.Utils;

namespace Miko.Layout.LayoutAlgorithms;

/// <summary>
/// 行内格式化上下文（ISSUE-110）：对一段连续的在流行内级子盒（文本节点、inline、
/// inline-block、inline-flex）做统一断行。
///
/// 背景：此前 BlockLayout / InlineLayout 把行内子盒排在同一行、不处理换行——文本节点
/// 各自按容器全宽独立换行，行内元素（code、a、span 等）则从行光标处水平堆放。文本与
/// 行内元素交错时（"text &lt;code&gt;x&lt;/code&gt; text"），后续文本盒与元素盒互相
/// 重叠、越过容器宽度，视觉顺序错乱。
///
/// 本类把整段行内流切成断行单元（拉丁按单词、CJK 按字符、行内元素为原子盒、br 为
/// 强制换行），按可用宽度贪心装箱成行盒，再逐行定位：
/// - 原子盒（行内元素、pre/nowrap 文本）整体落在一行，放不下则移到下一行；
/// - 参与断行的文本节点（white-space: normal）被切分为若干行片段
///   （<see cref="TextNode.LayoutFragments"/>），片段与元素盒共享行盒、顺序与浏览器一致；
/// - 行级水平对齐（text-align）在行盒宽度内作用于整行内容。
///
/// 块容器（BlockLayout）与行内盒（InlineLayout）共用本实现。
/// </summary>
internal static class InlineFormattingContext
{
    /// <summary>行内流的布局结果。</summary>
    internal readonly struct RunResult
    {
        /// <summary>全部行盒的累计高度。</summary>
        public float TotalHeight { get; init; }

        /// <summary>最宽行盒的内容宽度（shrink-to-fit 与滚动度量使用）。</summary>
        public float MaxLineWidth { get; init; }
    }

    private enum ItemKind
    {
        /// <summary>文本节点的一个断行单元（单词 / CJK 字符 / 空格）。</summary>
        TextPiece,
        /// <summary>原子盒：行内元素或 pre/nowrap 文本节点，内部不参与本行断行。</summary>
        AtomicBox,
        /// <summary>强制换行（br）。</summary>
        ForcedBreak,
    }

    private sealed class InlineItem
    {
        public ItemKind Kind;
        public bool IsSpace;

        /// <summary>CJK 单字符单元（或其前一单元为 CJK）时，其前允许断行（UAX#14）。</summary>
        public bool IsCjk;

        // TextPiece
        public TextNode? Node;
        public string Text = "";

        // AtomicBox / ForcedBreak
        public LayoutBox? Box;

        // 测量结果（TextPiece：文本宽 / 行高；AtomicBox：margin box 尺寸）。
        public float Width;
        public float Height;
    }

    private sealed class Line
    {
        public List<InlineItem> Items { get; } = new();
        public float Width;
        public float Height;
    }

    private sealed class FragmentAcc
    {
        public string Text = "";
        public float X;
        public float Y;
        public float Width;
        public float Height;
    }

    /// <summary>
    /// 对 <paramref name="children"/> 的 [startIndex, endIndex) 区段做行内流布局。
    /// 区段内的脱离文档流子盒被跳过（由调用方另行布局）。
    /// </summary>
    /// <param name="availableWidth">行盒可用宽度（内容盒宽）；null 或 ≤0 表示不确定
    /// （shrink-to-fit），此时整段流排为单行。</param>
    /// <param name="atomicWidth">布局原子行内盒时传入的可用宽度（百分比宽度解析基准）。</param>
    /// <param name="atomicHeight">布局原子行内盒时传入的可用高度（百分比高度解析基准）。</param>
    /// <param name="textAlign">行级水平对齐（作用于每个行盒；宽度不确定时不生效）。</param>
    /// <param name="containerLineHeight">br 强制换行的最小行高（取行内流容器的行高）。</param>
    public static RunResult Layout(
        IReadOnlyList<LayoutBox> children,
        int startIndex,
        int endIndex,
        float contentX,
        float startY,
        float? availableWidth,
        float? atomicWidth,
        float? atomicHeight,
        TextAlign textAlign,
        float containerLineHeight)
    {
        bool definite = availableWidth.HasValue && availableWidth.Value > 0;
        float avail = definite ? availableWidth!.Value : float.MaxValue;

        // 1. 构建断行单元序列。
        var items = new List<InlineItem>();
        for (int idx = startIndex; idx < endIndex; idx++)
        {
            var child = children[idx];
            if (BlockLayout.IsOutOfFlow(child))
            {
                continue;
            }

            if (BlockLayout.IsForcedLineBreak(child))
            {
                items.Add(new InlineItem { Kind = ItemKind.ForcedBreak, Box = child });
                continue;
            }

            if (child.Type == LayoutType.Text)
            {
                AddTextItems(child, items, definite ? avail : null, atomicHeight);
            }
            else
            {
                LayoutDispatcher.Dispatch(child, new LayoutConstraints(atomicWidth, atomicHeight), 0, 0);
                items.Add(new InlineItem
                {
                    Kind = ItemKind.AtomicBox,
                    Box = child,
                    Width = child.BoxModel.MarginBox.Width,
                    Height = child.BoxModel.MarginBox.Height,
                });
            }
        }

        // 2. 贪心装箱成行盒。
        var lines = new List<Line>();
        var line = new Line();
        foreach (var item in items)
        {
            if (item.Kind == ItemKind.ForcedBreak)
            {
                // br：结束当前行盒，其后的行内内容排到新的一行；
                // br 自身贡献至少一行的行高（保证空 br 也能换行）。
                StripTrailingSpaces(line);
                line.Height = Math.Max(line.Height, containerLineHeight);
                line.Items.Add(item);
                lines.Add(line);
                line = new Line();
                continue;
            }

            if (item.IsSpace)
            {
                // 行首空白丢弃（CSS 行首空白消除）；放不下的空白随换行消失。
                if (line.Items.Count == 0)
                {
                    continue;
                }
                if (line.Width + item.Width > avail)
                {
                    StripTrailingSpaces(line);
                    lines.Add(line);
                    line = new Line();
                    continue;
                }
                line.Items.Add(item);
                line.Width += item.Width;
                line.Height = Math.Max(line.Height, item.Height);
                continue;
            }

            if (line.Items.Count > 0 && line.Width + item.Width > avail && CanBreakBefore(line.Items[line.Items.Count - 1], item))
            {
                // 换行：行尾空白剥离（不占行宽、不产生可见字形）。
                StripTrailingSpaces(line);
                lines.Add(line);
                line = new Line();
            }

            line.Items.Add(item);
            line.Width += item.Width;
            line.Height = Math.Max(line.Height, item.Height);
        }

        StripTrailingSpaces(line);
        if (line.Items.Count > 0)
        {
            lines.Add(line);
        }

        // 3. 逐行定位：原子盒二次布局到最终位置；文本单元累积为节点行片段。
        var fragments = new Dictionary<TextNode, List<FragmentAcc>>();
        float y = startY;
        float maxLineWidth = 0;

        foreach (var currentLine in lines)
        {
            float x = contentX + AlignOffset(textAlign, avail, currentLine.Width, definite);
            FragmentAcc? open = null;
            TextNode? openNode = null;

            foreach (var item in currentLine.Items)
            {
                switch (item.Kind)
                {
                    case ItemKind.ForcedBreak:
                        LayoutDispatcher.Dispatch(item.Box!, new LayoutConstraints(0, null), x, y);
                        break;

                    case ItemKind.AtomicBox:
                        open = null;
                        openNode = null;
                        LayoutDispatcher.Dispatch(item.Box!, new LayoutConstraints(atomicWidth, atomicHeight), x, y);
                        x = item.Box!.BoxModel.MarginBox.Right;
                        break;

                    case ItemKind.TextPiece:
                        // 同一节点在同一行上的连续单元合并为一条片段（空格也并入——
                        // 文本节点首尾的边界空格参与盒尺寸，与单行测量行为一致）。
                        if (open == null || !ReferenceEquals(openNode, item.Node))
                        {
                            open = new FragmentAcc { X = x, Y = y, Height = currentLine.Height };
                            openNode = item.Node;
                            if (!fragments.TryGetValue(item.Node!, out var nodeFrags))
                            {
                                nodeFrags = new List<FragmentAcc>();
                                fragments[item.Node!] = nodeFrags;
                            }
                            nodeFrags.Add(open);
                        }
                        open.Text += item.Text;
                        open.Width += item.Width;
                        x += item.Width;
                        break;
                }
            }

            y += currentLine.Height;
            maxLineWidth = Math.Max(maxLineWidth, currentLine.Width);
        }

        // 4. 写回文本节点：内容盒 = 全部片段的并集，片段坐标转为相对内容盒原点。
        foreach (var (node, nodeFrags) in fragments)
        {
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (var frag in nodeFrags)
            {
                minX = Math.Min(minX, frag.X);
                minY = Math.Min(minY, frag.Y);
                maxX = Math.Max(maxX, frag.X + frag.Width);
                maxY = Math.Max(maxY, frag.Y + frag.Height);
            }

            var nodeBox = node.LayoutBox;
            if (nodeBox == null) continue;

            nodeBox.BoxModel.Margin = new EdgeSizes(0, 0, 0, 0);
            nodeBox.BoxModel.Border = new EdgeSizes(0, 0, 0, 0);
            nodeBox.BoxModel.Padding = new EdgeSizes(0, 0, 0, 0);
            nodeBox.BoxModel.Content = new RectF(minX, minY, maxX - minX, maxY - minY);

            var result = new List<TextLineFragment>(nodeFrags.Count);
            foreach (var frag in nodeFrags)
            {
                result.Add(new TextLineFragment(frag.Text, frag.X - minX, frag.Y - minY, frag.Width, frag.Height));
            }
            node.LayoutFragments = result;
        }

        return new RunResult
        {
            TotalHeight = y - startY,
            MaxLineWidth = maxLineWidth,
        };
    }

    /// <summary>
    /// 把文本节点切分为断行单元加入单元序列。
    /// 仅 <c>white-space: normal</c> 参与行内断行；pre / pre-wrap / pre-line / nowrap
    /// 的文本保留其空白语义，作为原子盒走 <see cref="TextLayout"/> 既有路径。
    /// </summary>
    private static void AddTextItems(
        LayoutBox child,
        List<InlineItem> items,
        float? wrapWidth,
        float? atomicHeight)
    {
        var node = (TextNode)child.Element;
        var style = child.ComputedStyle;

        // 默认按无片段处理（行内断行路径会在定位阶段重建片段）；
        // 同时清零内容盒，防止上一帧的几何残留在纯空白节点上。
        node.LayoutFragments = null;
        child.BoxModel.Margin = new EdgeSizes(0, 0, 0, 0);
        child.BoxModel.Border = new EdgeSizes(0, 0, 0, 0);
        child.BoxModel.Padding = new EdgeSizes(0, 0, 0, 0);
        child.BoxModel.Content = new RectF(0, 0, 0, 0);

        var text = TextTransformer.Apply(node.TextContent ?? "", style.TextTransform);

        if (style.WhiteSpace != WhiteSpace.Normal)
        {
            // 原子文本盒：可软换行的模式（pre-wrap / pre-line）在可用宽度内自行换行，
            // 其余（pre / nowrap）单行测量。
            float? w = TextWrapper.ShouldWrap(style.WhiteSpace) && wrapWidth.HasValue
                ? wrapWidth
                : null;
            LayoutDispatcher.Dispatch(child, new LayoutConstraints(w, atomicHeight), 0, 0);
            float width = child.BoxModel.MarginBox.Width;
            float height = child.BoxModel.MarginBox.Height;
            if (width > 0 || height > 0)
            {
                items.Add(new InlineItem
                {
                    Kind = ItemKind.AtomicBox,
                    Box = child,
                    Width = width,
                    Height = height,
                });
            }
            return;
        }

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        float lineHeight = BlockLayout.ResolveLineHeight(style);
        float letterSpacing = style.LetterSpacing.ToPixels(0, style.FontSize.Value);
        float spaceWidth = TextMeasurer.MeasureTextWidth(" ", style.FontFamily, style.FontSize.Value, style.FontWeight)
                         + letterSpacing;

        // 空白折叠但不修剪首尾：边界空格是否消除取决于行首/行尾位置，由装箱阶段决定。
        var collapsed = TextWrapper.CollapseWhitespacePreservingBoundaries(text);
        foreach (var unit in TextWrapper.SplitBreakUnits(collapsed))
        {
            float unitWidth = unit.IsSpace
                ? spaceWidth
                : TextMeasurer.MeasureTextWidth(unit.Text, style.FontFamily, style.FontSize.Value, style.FontWeight)
                  + letterSpacing * unit.Text.Length;

            items.Add(new InlineItem
            {
                Kind = ItemKind.TextPiece,
                IsSpace = unit.IsSpace,
                IsCjk = unit.IsCjk,
                Node = node,
                Text = unit.Text,
                Width = unitWidth,
                Height = lineHeight,
            });
        }
    }

    /// <summary>
    /// 是否允许在 <paramref name="item"/> 之前断行（CSS 软换行机会）：
    /// 仅在空白边界（前一单元是空格）或 CJK 边界（任一侧是 CJK 字符）允许断行。
    /// 两个紧邻的原子盒（如横排滚动的 inline-block 列表）之间没有断行机会，
    /// 超宽时整体溢出而不是换行——与浏览器一致。
    /// </summary>
    private static bool CanBreakBefore(InlineItem previous, InlineItem item)
        => previous.IsSpace || previous.IsCjk || item.IsCjk;

    private static void StripTrailingSpaces(Line line)
    {
        while (line.Items.Count > 0 && line.Items[line.Items.Count - 1].IsSpace)
        {
            line.Width -= line.Items[line.Items.Count - 1].Width;
            line.Items.RemoveAt(line.Items.Count - 1);
        }
    }

    private static float AlignOffset(TextAlign align, float avail, float lineWidth, bool definite)
    {
        if (!definite)
        {
            return 0;
        }
        return align switch
        {
            TextAlign.Center => Math.Max(0, (avail - lineWidth) / 2),
            TextAlign.Right => Math.Max(0, avail - lineWidth),
            _ => 0,
        };
    }
}
