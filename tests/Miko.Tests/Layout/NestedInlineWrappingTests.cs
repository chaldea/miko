using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-126：嵌套行内盒的换行与 block-in-inline。
///
/// 问题1：<c>&lt;span&gt;&lt;span&gt;长文本&lt;/span&gt;&lt;/span&gt;</c> 在定宽容器中不换行、
/// 直接溢出父容器；单层 span 换行正常。根因是行内格式化上下文把非替换 inline 盒当作
/// 原子盒（整体不可断），而 CSS 中 inline 盒是「透明」的——其内容直接参与父级的行内流，
/// 可以跨父级的多行断开。
///
/// 一并处理：inline 盒内的块级子元素（block-in-inline）应独占整行，而不是当作原子行内盒。
/// </summary>
public class NestedInlineWrappingTests
{
    private readonly LayoutEngine _layoutEngine = new();

    private const float RootWidth = 300f;
    private const float FontSize = 16f;
    private const float LineHeight = 24f; // 16 × 1.5

    private const string LongText =
        "Lorem ipsum dolor sit amet, Lorem ipsum dolor sit amet, consectetur adipiscing elit.";

    private static List<StyleSheet> Sheets() => new()
    {
        new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new()
                {
                    Selector = new ClassSelector("root"),
                    Style = new Style
                    {
                        Display = Display.Block,
                        Width = Length.Px(RootWidth),
                        FontSize = Length.Px(FontSize),
                        LineHeight = Length.Number(1.5f),
                        WhiteSpace = WhiteSpace.Normal,
                    }
                },
            }
        }
    };

    private static DivElement Root(params Element[] children)
    {
        var root = new DivElement { Class = "root" };
        foreach (var child in children) root.AddChild(child);
        return root;
    }

    private static LayoutBox FindBox(LayoutBox root, Element element)
        => FindBoxOrNull(root, element)
           ?? throw new InvalidOperationException("element not in layout tree");

    private static LayoutBox? FindBoxOrNull(LayoutBox root, Element element)
    {
        if (ReferenceEquals(root.Element, element)) return root;
        foreach (var child in root.Children)
        {
            var found = FindBoxOrNull(child, element);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>文本节点的行片段，换算为绝对坐标矩形。</summary>
    private static List<RectF> AbsFragments(LayoutBox root, TextNode node)
    {
        var box = FindBox(root, node);
        node.LayoutFragments.ShouldNotBeNull("行内流中的文本节点应已被行内格式化上下文断行");
        var content = box.BoxModel.Content;
        return node.LayoutFragments
            .Select(f => new RectF(content.X + f.X, content.Y + f.Y, f.Width, f.Height))
            .ToList();
    }

    [Fact]
    public void NestedSpan_ShouldWrapWithinContainerWidth()
    {
        // <div class="root"><span><span>长文本</span></span></div>
        // 内层 span 的文本应在 300px 处断行，而不是整串溢出。
        var text = new TextNode(LongText);
        var inner = new SpanElement();
        inner.AddChild(text);
        var outer = new SpanElement();
        outer.AddChild(inner);

        var root = Root(outer);
        var layout = _layoutEngine.Layout(root, Sheets(), 800, 600);

        float contentRight = layout.BoxModel.Content.Right;
        var frags = AbsFragments(layout, text);

        frags.Count.ShouldBeGreaterThan(1, "嵌套 span 内的长文本应换行为多行");
        foreach (var frag in frags)
        {
            frag.Right.ShouldBeLessThanOrEqualTo(contentRight + 0.5f,
                "嵌套 span 内的文本片段不应溢出容器宽度");
        }

        // 内外层 span 的盒也应收敛在容器内。
        FindBox(layout, inner).BoxModel.MarginBox.Right
            .ShouldBeLessThanOrEqualTo(contentRight + 0.5f, "内层 span 溢出了容器宽度");
        FindBox(layout, outer).BoxModel.MarginBox.Right
            .ShouldBeLessThanOrEqualTo(contentRight + 0.5f, "外层 span 溢出了容器宽度");
    }

    [Fact]
    public void NestedSpan_ShouldShareLineBoxWithSiblingText()
    {
        // <span>aaa <span>bbb</span> ccc</span>
        // 内层 span 的内容应与前后文本共享同一行盒（不独占新行）。
        var before = new TextNode("alpha ");
        var innerText = new TextNode("bravo");
        var after = new TextNode(" charlie");

        var inner = new SpanElement();
        inner.AddChild(innerText);
        var outer = new SpanElement();
        outer.AddChild(before);
        outer.AddChild(inner);
        outer.AddChild(after);

        var layout = _layoutEngine.Layout(Root(outer), Sheets(), 800, 600);

        var beforeFrags = AbsFragments(layout, before);
        var innerFrags = AbsFragments(layout, innerText);
        var afterFrags = AbsFragments(layout, after);

        // 三段短文本总宽远小于 300px，应全部落在同一行。
        beforeFrags.Count.ShouldBe(1);
        innerFrags.Count.ShouldBe(1);
        afterFrags.Count.ShouldBe(1);

        innerFrags[0].Top.ShouldBe(beforeFrags[0].Top, 0.5f, "内层 span 应与前文同行");
        afterFrags[0].Top.ShouldBe(beforeFrags[0].Top, 0.5f, "后文应与内层 span 同行");

        // 顺序：before → inner → after，互不重叠。
        innerFrags[0].Left.ShouldBeGreaterThanOrEqualTo(beforeFrags[0].Right - 0.5f);
        afterFrags[0].Left.ShouldBeGreaterThanOrEqualTo(innerFrags[0].Right - 0.5f);
    }

    [Fact]
    public void WrappedInlineBox_ShouldHavePerLineFragments()
    {
        // 跨行的内层 span：其盒几何应为逐行片段矩形（背景/边框逐行绘制），
        // 片段数等于它占据的行数，各片段依次相差一个行高。
        var text = new TextNode(LongText);
        var inner = new SpanElement();
        inner.AddChild(text);
        var outer = new SpanElement();
        outer.AddChild(inner);

        var layout = _layoutEngine.Layout(Root(outer), Sheets(), 800, 600);

        var innerBox = FindBox(layout, inner);
        var textFrags = AbsFragments(layout, text);

        innerBox.InlineFragments.ShouldNotBeNull("跨行的 inline 盒应有逐行片段矩形");
        innerBox.InlineFragments.Count.ShouldBe(textFrags.Count,
            "inline 盒的片段数应等于它占据的行数");

        for (int i = 1; i < innerBox.InlineFragments.Count; i++)
        {
            innerBox.InlineFragments[i].Top.ShouldBe(
                innerBox.InlineFragments[i - 1].Top + LineHeight, 0.5f,
                "相邻行片段应相差一个行高");
        }
    }

    /// <summary>
    /// ISSUE-126 复现代码原样：.root(300×600) 内两段 .text，第二段中间夹一个 h1。
    /// 全部行内内容都应收敛在 300px 内，两段之间由两个 br 分隔。
    /// </summary>
    [Fact]
    public void Issue126Repro_AllTextShouldStayWithinRootWidth()
    {
        TextNode Nested(out SpanElement wrapper)
        {
            var t = new TextNode(LongText);
            var s = new SpanElement();
            s.AddChild(t);
            wrapper = s;
            return t;
        }

        var text1 = Nested(out var inner1);
        var block1 = new SpanElement { Class = "text" };
        block1.AddChild(inner1);

        var text2 = Nested(out var inner2);
        var text3 = Nested(out var inner3);
        var heading = new H1Element();
        heading.AddChild(new TextNode("Lorem ipsum dolor sit amet, consectetur adipiscing elit."));

        var block2 = new SpanElement { Class = "text" };
        block2.AddChild(inner2);
        block2.AddChild(heading);
        block2.AddChild(inner3);

        var root = Root(block1, new BrElement(), new BrElement(), block2);
        var layout = _layoutEngine.Layout(root, Sheets(), 800, 600);

        float contentRight = layout.BoxModel.Content.Right;

        foreach (var node in new[] { text1, text2, text3 })
        {
            var frags = AbsFragments(layout, node);
            frags.Count.ShouldBeGreaterThan(1, "每段嵌套文本都应换行");
            foreach (var frag in frags)
            {
                frag.Right.ShouldBeLessThanOrEqualTo(contentRight + 0.5f,
                    "文本片段不应溢出 .root 的 300px 宽度");
            }
        }

        // 第二段在 h1 之下继续，两段整体按 DOM 顺序纵向排列、互不重叠。
        var block1Box = FindBox(layout, block1);
        var block2Box = FindBox(layout, block2);
        block2Box.BoxModel.Content.Top.ShouldBeGreaterThanOrEqualTo(
            block1Box.BoxModel.Content.Bottom - 0.5f, "两个 br 之后第二段应排在第一段下方");
    }

    [Fact]
    public void BlockInsideInline_ShouldOccupyItsOwnLine()
    {
        // <span>text <h1>block</h1> text</span>
        // h1 是块级子元素，应独占整行：与前后行内内容在垂直方向不重叠。
        var before = new TextNode("alpha");
        var after = new TextNode("charlie");

        var heading = new H1Element();
        heading.AddChild(new TextNode("bravo"));

        var outer = new SpanElement();
        outer.AddChild(before);
        outer.AddChild(heading);
        outer.AddChild(after);

        var layout = _layoutEngine.Layout(Root(outer), Sheets(), 800, 600);

        var headingBox = FindBox(layout, heading).BoxModel.MarginBox;
        var beforeFrags = AbsFragments(layout, before);
        var afterFrags = AbsFragments(layout, after);

        foreach (var frag in beforeFrags)
        {
            frag.Bottom.ShouldBeLessThanOrEqualTo(headingBox.Top + 0.5f,
                "h1 之前的行内内容应排在 h1 上方");
        }
        foreach (var frag in afterFrags)
        {
            frag.Top.ShouldBeGreaterThanOrEqualTo(headingBox.Bottom - 0.5f,
                "h1 之后的行内内容应排在 h1 下方");
        }
    }
}
