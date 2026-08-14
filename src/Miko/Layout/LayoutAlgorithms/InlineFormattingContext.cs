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
        /// <summary>原子盒：inline-block / inline-flex / 替换元素，内部不参与本行断行。</summary>
        AtomicBox,
        /// <summary>强制换行（br）。</summary>
        ForcedBreak,
        /// <summary>透明 inline 盒的左边界（其左侧 margin+border+padding 占据水平空间）。</summary>
        InlineBoxStart,
        /// <summary>透明 inline 盒的右边界（其右侧 margin+border+padding 占据水平空间）。</summary>
        InlineBoxEnd,
        /// <summary>透明 inline 盒内的块级子盒（block-in-inline）：独占整行。</summary>
        BlockBox,
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

        // AtomicBox / ForcedBreak / InlineBoxStart / InlineBoxEnd
        public LayoutBox? Box;

        // 测量结果（TextPiece：文本宽 / 行高；AtomicBox：margin box 尺寸；
        // InlineBoxStart/End：该侧 margin+border+padding 厚度 / 盒自身行高）。
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
    /// <param name="allowWrap">容器是否允许软换行（<c>white-space</c> 非 nowrap/pre）。
    /// false 时整段行内流排为单行（横排滚动列表），仅 br 仍强制换行。</param>
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
        float containerLineHeight,
        bool allowWrap = true)
    {
        bool definite = availableWidth.HasValue && availableWidth.Value > 0;
        float avail = definite ? availableWidth!.Value : float.MaxValue;
        // 装箱上限：不允许软换行时不设限（整段排为单行），但 definite 仍保留——
        // text-align 与百分比解析基准不受 white-space 影响（浏览器行为）。
        float wrapLimit = allowWrap ? avail : float.MaxValue;

        // 1. 构建断行单元序列。非替换 inline 盒在此被「透明化」：其子内容递归展开、
        //    与兄弟内容并入同一序列，从而能跨本上下文的多行断开（ISSUE-126）。
        var items = new List<InlineItem>();
        AddItems(children, startIndex, endIndex, items,
            allowWrap && definite ? avail : null, avail, atomicWidth, atomicHeight);

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

            if (item.Kind == ItemKind.BlockBox)
            {
                // block-in-inline（ISSUE-126）：块级盒前后都强制断行，自身独占一「行」
                // （其高度在定位阶段按实际布局结果确定，故此处行高留 0 由定位阶段回填）。
                StripTrailingSpaces(line);

                // 当前行只有尚未闭合的 inline 盒起始边界（如 <label><div>…）时，不产出
                // 一条空的可视行——把这些边界并入块级行，使外层 inline 盒的片段覆盖该块，
                // 否则 label 之类的包裹盒会得到一条零高度片段而不可命中。
                var blockLine = new Line();
                if (line.Items.Count > 0 && HasOnlyInlineBoxStarts(line))
                {
                    blockLine.Items.AddRange(line.Items);
                    blockLine.Width = line.Width;
                }
                else if (line.Items.Count > 0)
                {
                    lines.Add(line);
                }

                blockLine.Items.Add(item);
                lines.Add(blockLine);
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
                if (line.Width + item.Width > wrapLimit)
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

            if (line.Items.Count > 0 && line.Width + item.Width > wrapLimit
                && CanBreakBefore(line.Items[line.Items.Count - 1], EffectivePrevious(line), item))
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

        // 3. 逐行定位：原子盒二次布局到最终位置；文本单元累积为节点行片段；
        //    透明 inline 盒在每条经过的行盒上累积一段片段矩形（ISSUE-126）。
        var fragments = new Dictionary<TextNode, List<FragmentAcc>>();
        var inlineBoxes = new List<LayoutBox>();
        // 当前行上处于「打开」状态的透明 inline 盒（按嵌套顺序），及其在本行的起始 x。
        var openInlineBoxes = new List<(LayoutBox Box, float StartX)>();
        float y = startY;
        float maxLineWidth = 0;

        foreach (var currentLine in lines)
        {
            float x = contentX + AlignOffset(textAlign, avail, currentLine.Width, definite);
            FragmentAcc? open = null;
            TextNode? openNode = null;

            // 跨行续行：上一行末尾仍处于打开状态的 inline 盒在本行从行首继续。
            for (int i = 0; i < openInlineBoxes.Count; i++)
            {
                openInlineBoxes[i] = (openInlineBoxes[i].Box, x);
            }

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

                    case ItemKind.BlockBox:
                        // block-in-inline：块级盒从行首起排、占满可用宽度（块级流语义），
                        // 行高即其 margin box 高度。
                        open = null;
                        openNode = null;
                        LayoutDispatcher.Dispatch(
                            item.Box!,
                            new LayoutConstraints(definite ? avail : atomicWidth, atomicHeight),
                            contentX, y);
                        currentLine.Height = item.Box!.BoxModel.MarginBox.Height;
                        x = Math.Max(x, item.Box.BoxModel.MarginBox.Right);
                        currentLine.Width = Math.Max(currentLine.Width, item.Box.BoxModel.MarginBox.Width);
                        break;

                    case ItemKind.InlineBoxStart:
                        open = null;
                        openNode = null;
                        if (!inlineBoxes.Contains(item.Box!))
                        {
                            inlineBoxes.Add(item.Box!);
                            item.Box!.InlineFragments = new List<RectF>();
                        }
                        // 片段左边界含左侧 margin（与 MarginBox 语义一致，绘制时再扣除）。
                        openInlineBoxes.Add((item.Box!, x));
                        x += item.Width;
                        break;

                    case ItemKind.InlineBoxEnd:
                        open = null;
                        openNode = null;
                        x += item.Width;
                        CloseInlineBox(openInlineBoxes, item.Box!, x, y, currentLine.Height);
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

            // 行末仍打开的 inline 盒：在本行收一段片段（右边界为行末光标），下一行继续。
            foreach (var (box, startX) in openInlineBoxes)
            {
                box.InlineFragments!.Add(new RectF(startX, y, Math.Max(0, x - startX), currentLine.Height));
            }

            y += currentLine.Height;
            maxLineWidth = Math.Max(maxLineWidth, currentLine.Width);
        }

        // 写回透明 inline 盒的盒模型：内容盒 = 全部片段的并集（扣除边框/内边距）。
        foreach (var box in inlineBoxes)
        {
            WriteInlineBoxGeometry(box);
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
    /// 关闭一个透明 inline 盒：把它在当前行上的这一段收为片段矩形，并移出打开栈。
    /// 若该盒不在打开栈中（跨行且其起始边界在更早的行，已被行末逻辑收尾），则忽略。
    /// </summary>
    private static void CloseInlineBox(
        List<(LayoutBox Box, float StartX)> openInlineBoxes,
        LayoutBox box,
        float endX,
        float y,
        float lineHeight)
    {
        for (int i = openInlineBoxes.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(openInlineBoxes[i].Box, box)) continue;

            float startX = openInlineBoxes[i].StartX;
            box.InlineFragments!.Add(new RectF(startX, y, Math.Max(0, endX - startX), lineHeight));
            openInlineBoxes.RemoveAt(i);
            return;
        }
    }

    /// <summary>
    /// 写回透明 inline 盒的盒模型。定位阶段累积的片段是 margin box 矩形，这里就地扣掉
    /// 首/末片段的左右外边距，使 <see cref="LayoutBox.InlineFragments"/> 成为可直接绘制的
    /// border box；内容盒再取这些片段的并集内缩 border+padding。
    /// </summary>
    private static void WriteInlineBoxGeometry(LayoutBox box)
    {
        var frags = box.InlineFragments;
        if (frags == null || frags.Count == 0)
        {
            box.InlineFragments = null;
            box.BoxModel.Content = new RectF(0, 0, 0, 0);
            return;
        }

        var margin = box.BoxModel.Margin;
        var border = box.BoxModel.Border;
        var padding = box.BoxModel.Padding;

        // margin box → border box：首片段扣左侧 margin、末片段扣右侧 margin
        // （中间片段是断行处的续段，两侧都没有外边距）。
        // 纵向 margin 不参与：CSS 中非替换 inline 盒的上下外边距不影响盒的位置与行高
        // （CSS 2.1 §10.8——垂直方向由行盒决定），故片段的 Y/Height 直接取行盒几何。
        for (int i = 0; i < frags.Count; i++)
        {
            float left = frags[i].X + (i == 0 ? margin.Left : 0);
            float right = frags[i].Right - (i == frags.Count - 1 ? margin.Right : 0);
            frags[i] = new RectF(left, frags[i].Y, Math.Max(0, right - left), frags[i].Height);
        }

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (var frag in frags)
        {
            minX = Math.Min(minX, frag.X);
            minY = Math.Min(minY, frag.Y);
            maxX = Math.Max(maxX, frag.Right);
            maxY = Math.Max(maxY, frag.Bottom);
        }

        // 内容盒 = border box 并集内缩 border+padding，供 ScrollableContent* 度量与
        // 绝对定位包含块等既有消费者使用（BoxModel.BorderBox 会据此反算回并集）。
        box.BoxModel.Content = new RectF(
            minX + border.Left + padding.Left,
            minY + border.Top + padding.Top,
            Math.Max(0, maxX - minX - border.Horizontal - padding.Horizontal),
            Math.Max(0, maxY - minY - border.Vertical - padding.Vertical));
    }

    /// <summary>
    /// 把 <paramref name="children"/> 的 [startIndex, endIndex) 区段展开为断行单元序列。
    /// 透明 inline 盒（<see cref="IsTransparentInline"/>）不作为原子盒整体入列，而是产出
    /// 一对 <see cref="ItemKind.InlineBoxStart"/> / <see cref="ItemKind.InlineBoxEnd"/> 边界单元，
    /// 其子内容递归展开夹在其间——这正是 CSS 中 inline 盒的语义：盒本身不参与断行决策，
    /// 内容直接参与所在的行内格式化上下文（ISSUE-126）。
    /// </summary>
    /// <param name="wrapWidth">文本断行宽度（null 表示不断行，整段排为单行）。</param>
    /// <param name="percentBase">透明 inline 盒解析自身 margin/padding 百分比的基准宽度。</param>
    private static void AddItems(
        IReadOnlyList<LayoutBox> children,
        int startIndex,
        int endIndex,
        List<InlineItem> items,
        float? wrapWidth,
        float percentBase,
        float? atomicWidth,
        float? atomicHeight)
    {
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
                AddTextItems(child, items, wrapWidth, atomicHeight);
                continue;
            }

            // 块级子盒（block-in-inline，ISSUE-126）：不是行内级内容，独占整行。
            // 只可能出现在透明 inline 盒内部——块容器自身的块级子元素由 BlockLayout
            // 在调用本上下文之前就切段排除了。
            if (!BlockLayout.IsInlineOrInlineBlock(child))
            {
                items.Add(new InlineItem { Kind = ItemKind.BlockBox, Box = child });
                continue;
            }

            if (IsTransparentInline(child))
            {
                var style = child.ComputedStyle;
                float fs = style.FontSize.Value;
                // 百分比基准取本行内流的可用宽度（无限宽时按 0，与 CSS 对不确定包含块的处理一致）。
                float pb = float.IsInfinity(percentBase) || percentBase == float.MaxValue ? 0 : percentBase;

                // 非替换 inline 盒的上下外边距在 CSS 中不生效（CSS 2.1 §10.8：垂直方向由行盒决定），
                // 故纵向 margin 记为 0——否则 UA 样式里带 margin 的行内化元素（如 display:inline 的 p）
                // 会把外边距算进 margin box，使块流的纵向推进与盒的可视范围对不上。
                var margin = new EdgeSizes(
                    0,
                    style.MarginRight.ToPixels(pb, fs),
                    0,
                    style.MarginLeft.ToPixels(pb, fs));
                var border = new EdgeSizes(
                    style.BorderTopWidth.ToPixels(pb, fs),
                    style.BorderRightWidth.ToPixels(pb, fs),
                    style.BorderBottomWidth.ToPixels(pb, fs),
                    style.BorderLeftWidth.ToPixels(pb, fs));
                var padding = new EdgeSizes(
                    style.PaddingTop.ToPixels(pb, fs),
                    style.PaddingRight.ToPixels(pb, fs),
                    style.PaddingBottom.ToPixels(pb, fs),
                    style.PaddingLeft.ToPixels(pb, fs));

                child.BoxModel.Margin = margin;
                child.BoxModel.Border = border;
                child.BoxModel.Padding = padding;
                child.InlineFragments = null;

                float boxLineHeight = BlockLayout.ResolveLineHeight(style);

                items.Add(new InlineItem
                {
                    Kind = ItemKind.InlineBoxStart,
                    Box = child,
                    Width = margin.Left + border.Left + padding.Left,
                    Height = boxLineHeight,
                });

                AddItems(child.Children, 0, child.Children.Count, items,
                    wrapWidth, percentBase, atomicWidth, atomicHeight);

                items.Add(new InlineItem
                {
                    Kind = ItemKind.InlineBoxEnd,
                    Box = child,
                    Width = margin.Right + border.Right + padding.Right,
                    Height = boxLineHeight,
                });
                continue;
            }

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

    /// <summary>
    /// 该盒是否为「透明」的非替换 inline 盒：其内容参与父级行内流、可跨父级多行断开。
    ///
    /// inline-block / inline-flex 建立独立的格式化上下文，是原子行内级盒（内部自成一体、
    /// 整体参与父级断行），因此不透明；替换元素（img / video）同样是原子盒。
    /// </summary>
    private static bool IsTransparentInline(LayoutBox box)
    {
        if (box.Type != LayoutType.Inline) return false;
        if (BlockLayout.IsForcedLineBreak(box)) return false;

        // 替换元素按内禀尺寸成盒，是原子行内级盒。
        var (iw, ih) = BlockLayout.GetReplacedIntrinsicSize(box.Element);
        if (iw > 0 && ih > 0) return false;

        // 文本表单控件（input / select / textarea）的内容不是常规行内流（见 BlockLayout），
        // 其盒尺寸由控件度量决定，按原子盒处理。
        if (BlockLayout.IsTextFormControl(box)) return false;

        var style = box.ComputedStyle;

        // 内容会被裁剪或滚动的 inline 盒需要一个确定的矩形视口，按原子盒处理更贴近现实
        // （浏览器中 overflow 非 visible 也会把 inline 盒变为 inline-block 式的原子盒）。
        if (style.OverflowX != Overflow.Visible || style.OverflowY != Overflow.Visible) return false;

        // 显式宽高的 inline 盒：CSS 规范中 width/height 对非替换 inline 无效，但本引擎自
        // ISSUE-079 起按 inline-block 式处理（定宽行内元素在自身宽度内换行，
        // 见 InlineFormattingContextTests.InlineElement_WithExplicitWidth_WrapsItsTextContent）。
        // 保留该既有行为：有显式尺寸时走原子盒路径，由 InlineLayout 解析尺寸并在内部断行。
        //
        // 纯百分比尺寸不算「显式」：它相对包含块解析，而透明 inline 盒（auto 宽）不是确定的
        // 包含块，按 CSS 应退化为 auto、由内容决定（ISSUE-077）。若把它也算作显式尺寸而
        // 走原子路径，父级传下来的 atomicWidth 会成为解析基准，width:100% 被撑满整行。
        bool hasExplicitWidth = !style.Width.IsAuto && !style.Width.HasPercentComponent;
        bool hasExplicitHeight = !style.Height.IsAuto && !style.Height.HasPercentComponent;
        if (hasExplicitWidth || hasExplicitHeight) return false;

        return true;
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
    /// - 空白边界（前一单元是空格）；
    /// - CJK 边界（任一侧是 CJK 字符）；
    /// - 原子盒边界（任一侧是原子行内盒）。原子盒（inline-block / inline-flex / 行内元素、
    ///   替换元素）在 UAX#14 中按「contingent break」(CB) 处理，其前后都是断行机会
    ///   （LB20: <c>÷ CB</c> / <c>CB ÷</c>），因此紧邻的 inline-block 之间即使没有空白
    ///   文本节点也会换行——与浏览器一致（ISSUE-116）。断行机会只存在于原子盒的**边界**，
    ///   盒**内部**不可断，故单个超宽原子盒仍整体溢出。
    ///
    /// 横排滚动列表（一行排开、水平滚动）应由容器的 <c>white-space: nowrap</c> 抑制换行，
    /// 而不是依赖原子盒之间缺少断行机会——见 <paramref name="allowWrap"/>。
    /// </summary>
    /// <param name="previous">行内紧邻的前一单元（含盒边界单元）。</param>
    /// <param name="effectivePrevious">跳过盒边界单元后的前一个「内容」单元：断行机会由内容
    /// 决定，跨越 <c>&lt;/span&gt;</c> 边界时仍应看边界另一侧的空白/CJK 属性（ISSUE-126）。</param>
    private static bool CanBreakBefore(InlineItem previous, InlineItem? effectivePrevious, InlineItem item)
    {
        // 透明 inline 盒的边界本身不是断行机会（ISSUE-126）：
        // - 不能在 InlineBoxStart 之前断，否则本可与前文同行的 <span> 会无谓地被推到下一行；
        // - 更不能在 InlineBoxStart 之后立刻断（把左内边距/边框孤零零留在上一行末尾）；
        // - 也不能在 InlineBoxEnd 之前断（右内边距/边框会与内容分离）。
        // 盒内/盒外的断行机会由内容（空白 / CJK / 原子盒边界）提供，与浏览器一致。
        if (item.Kind == ItemKind.InlineBoxStart || item.Kind == ItemKind.InlineBoxEnd) return false;
        if (previous.Kind == ItemKind.InlineBoxStart) return false;

        var prev = effectivePrevious ?? previous;
        return prev.IsSpace
            || prev.IsCjk || item.IsCjk
            || prev.Kind == ItemKind.AtomicBox || item.Kind == ItemKind.AtomicBox;
    }

    /// <summary>该行是否只包含尚未闭合的透明 inline 盒起始边界（没有任何可视内容）。</summary>
    private static bool HasOnlyInlineBoxStarts(Line line)
    {
        foreach (var item in line.Items)
        {
            if (item.Kind != ItemKind.InlineBoxStart) return false;
        }
        return true;
    }

    /// <summary>
    /// 当前行上最后一个「内容」单元（跳过透明 inline 盒的边界单元）。全是边界单元时返回 null。
    /// </summary>
    private static InlineItem? EffectivePrevious(Line line)
    {
        for (int i = line.Items.Count - 1; i >= 0; i--)
        {
            var kind = line.Items[i].Kind;
            if (kind == ItemKind.InlineBoxStart || kind == ItemKind.InlineBoxEnd) continue;
            return line.Items[i];
        }
        return null;
    }

    /// <summary>
    /// 剥离行尾空白（不占行宽、不产生可见字形）。透明 inline 盒的结束边界会「跨过」——
    /// <c>&lt;span&gt;词 &lt;/span&gt;</c> 断行时，盒内的行尾空格同样应被剥离，
    /// 但边界单元本身（右内边距/边框）必须保留（ISSUE-126）。
    /// </summary>
    private static void StripTrailingSpaces(Line line)
    {
        int i = line.Items.Count - 1;
        while (i >= 0)
        {
            var item = line.Items[i];
            if (item.Kind == ItemKind.InlineBoxEnd)
            {
                i--;
                continue;
            }
            if (!item.IsSpace) break;

            line.Width -= item.Width;
            line.Items.RemoveAt(i);
            i--;
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
