using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-110：行内格式化上下文（InlineFormattingContext）的布局行为测试。
/// 覆盖：行内断行单元共享行盒、断行机会规则（空格/CJK 边界）、行级 text-align、
/// br 强制换行、行片段几何（并集盒 + 相对坐标）。
/// </summary>
public class InlineFormattingContextTests
{
    private readonly LayoutEngine _layoutEngine = new();

    private static List<StyleSheet> Sheets(Style containerStyle) => new()
    {
        new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new() { Selector = new TagSelector("div"), Style = containerStyle },
            }
        }
    };

    private static Style BlockContainer(float width, TextAlign align = TextAlign.Left) => new()
    {
        Display = Display.Block,
        Width = Length.Px(width),
        FontSize = Length.Px(16),
        LineHeight = Length.Number(1.5f),
        TextAlign = align,
        WhiteSpace = WhiteSpace.Normal,
    };

    private const float LineHeight = 24f; // 16 × 1.5

    private static List<TextLineFragment> FragmentsOf(LayoutBox textBox)
        => ((TextNode)textBox.Element).LayoutFragments
           ?? throw new ShouldAssertException("文本节点应有行内断行片段");

    private static RectF AbsRect(LayoutBox textBox, TextLineFragment frag)
    {
        var content = textBox.BoxModel.Content;
        return new RectF(content.X + frag.X, content.Y + frag.Y, frag.Width, frag.Height);
    }

    [Fact]
    public void WrappedText_FragmentsStackByLineHeight_AndUnionBoxCoversAll()
    {
        // 单行装不下的长英文文本：应断为多行片段，并集盒高 = 行数 × 行高。
        var div = new DivElement();
        div.AddChild(new TextNode("one two three four five six seven eight nine ten eleven twelve"));

        var layout = _layoutEngine.Layout(div, Sheets(BlockContainer(120)), 800, 600);
        var textBox = layout.Children[0];

        var frags = FragmentsOf(textBox);
        frags.Count.ShouldBeGreaterThan(1);

        // 片段坐标相对内容盒：首个片段在原点，逐行递增一个行高。
        frags[0].X.ShouldBe(0f, 0.01f);
        frags[0].Y.ShouldBe(0f, 0.01f);
        for (int i = 1; i < frags.Count; i++)
        {
            frags[i].Y.ShouldBe(frags[i - 1].Y + LineHeight, 0.5f, "相邻行片段应相差一个行高");
        }

        // 内容盒 = 全部片段的并集。
        textBox.BoxModel.Content.Height.ShouldBe(frags.Count * LineHeight, 0.5f);
        textBox.BoxModel.Content.Width.ShouldBeLessThanOrEqualTo(120f + 0.5f);
    }

    [Fact]
    public void TextAlignCenter_CentersEachLine()
    {
        var div = new DivElement();
        div.AddChild(new TextNode("alpha beta gamma delta epsilon zeta eta theta iota kappa lambda"));

        var layout = _layoutEngine.Layout(div, Sheets(BlockContainer(150, TextAlign.Center)), 800, 600);
        var textBox = layout.Children[0];
        var content = textBox.BoxModel.Content;

        foreach (var frag in FragmentsOf(textBox))
        {
            var rect = AbsRect(textBox, frag);
            float leftGap = rect.Left - content.Left;
            float rightGap = content.Right - rect.Right;
            // 行级居中：左右间隙相等（行宽不足容器宽时间隙均分）。
            (leftGap - rightGap).ShouldBe(0f, 1.5f, $"片段 \"{frag.Text}\" 应在行内居中");
        }
    }

    [Fact]
    public void TextAlignRight_LastLineAlignsToRightEdge()
    {
        var div = new DivElement();
        div.AddChild(new TextNode("alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu"));

        var layout = _layoutEngine.Layout(div, Sheets(BlockContainer(150, TextAlign.Right)), 800, 600);
        var textBox = layout.Children[0];
        var frags = FragmentsOf(textBox);

        frags.Count.ShouldBeGreaterThan(1);
        // 至少最后一行（通常不满）应贴右缘。
        var last = AbsRect(textBox, frags[frags.Count - 1]);
        last.Right.ShouldBe(textBox.BoxModel.Content.Right, 1f);
    }

    [Fact]
    public void AdjacentInlineBlocks_WithoutSpace_Wrap()
    {
        // 紧邻的 inline-block 之间存在断行机会（UAX#14 LB20：原子盒前后都可断），
        // 即使没有空白文本节点分隔也会换行——与浏览器一致（ISSUE-116）。
        var child1 = new DivElement { Style = new Style { Display = Display.InlineBlock, Width = Length.Px(200), Height = Length.Px(50) } };
        var child2 = new DivElement { Style = new Style { Display = Display.InlineBlock, Width = Length.Px(200), Height = Length.Px(50) } };
        var root = new DivElement { Children = { child1, child2 } };

        var layout = _layoutEngine.Layout(root, Sheets(BlockContainer(300)), 800, 600);

        var box1 = layout.Children[0].BoxModel.MarginBox;
        var box2 = layout.Children[1].BoxModel.MarginBox;
        box2.Left.ShouldBe(box1.Left, 0.5f, "换行后第二个盒应回到行首");
        box2.Top.ShouldBe(box1.Bottom, 0.5f, "放不下的 inline-block 应换到下一行");
    }

    [Fact]
    public void AdjacentInlineBlocks_Nowrap_StayOnOneLine()
    {
        // 横排滚动列表：由容器的 white-space: nowrap 抑制换行，整体溢出到同一行。
        var child1 = new DivElement { Style = new Style { Display = Display.InlineBlock, Width = Length.Px(200), Height = Length.Px(50) } };
        var child2 = new DivElement { Style = new Style { Display = Display.InlineBlock, Width = Length.Px(200), Height = Length.Px(50) } };
        var root = new DivElement { Children = { child1, child2 } };

        var sheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new()
                {
                    Selector = new ClassSelector("container"),
                    Style = new Style { Width = Length.Px(300), WhiteSpace = WhiteSpace.Nowrap }
                }
            }
        };
        root.Class = "container";

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);

        var box1 = layout.Children[0].BoxModel.MarginBox;
        var box2 = layout.Children[1].BoxModel.MarginBox;
        box2.Left.ShouldBe(box1.Right, 0.5f);
        box2.Top.ShouldBe(box1.Top, 0.5f, "nowrap 容器内的 inline-block 不应换行");
    }

    [Fact]
    public void InlineBlock_AfterSpace_WrapsToNextLine()
    {
        // 文本与 inline-block 之间有空格：放不下时 inline-block 移到下一行。
        var div = new DivElement();
        div.AddChild(new TextNode("prefix text here "));
        var box = new DivElement { Style = new Style { Display = Display.InlineBlock, Width = Length.Px(150), Height = Length.Px(30) } };
        div.AddChild(box);

        var layout = _layoutEngine.Layout(div, Sheets(BlockContainer(200)), 800, 600);

        var textBox = layout.Children[0];
        var boxRect = layout.Children[1].BoxModel.MarginBox;
        boxRect.Top.ShouldBe(textBox.BoxModel.Content.Top + LineHeight, 1f,
            "放不下的 inline-block 应换到下一行");
        boxRect.Left.ShouldBe(textBox.BoxModel.Content.Left, 0.5f);
    }

    [Fact]
    public void BoundarySpaces_BetweenTextAndInlineElement_ArePreserved()
    {
        // "All " + code + " components"：文本与行内元素之间的边界空格必须保留为可见间隙。
        var div = new DivElement();
        div.AddChild(new TextNode("All "));
        var code = new CodeElement();
        code.AddChild(new TextNode("x"));
        div.AddChild(code);
        div.AddChild(new TextNode(" components"));

        var layout = _layoutEngine.Layout(div, Sheets(BlockContainer(600)), 800, 600);

        var text1 = layout.Children[0];
        var codeBox = layout.Children[1].BoxModel.MarginBox;
        var text2 = layout.Children[2];

        // 单行排列；text1 片段含尾部空格（其右缘即 code 左缘），
        // text2 片段含头部空格（其左缘即 code 右缘）——字形间隙由此产生。
        var frag1 = FragmentsOf(text1).Single();
        var frag2 = FragmentsOf(text2).Single();
        AbsRect(text1, frag1).Right.ShouldBe(codeBox.Left, 0.5f);
        AbsRect(text2, frag2).Left.ShouldBe(codeBox.Right, 0.5f);

        float spaceWidth = Miko.Utils.TextMeasurer.MeasureTextWidth(" ", "Arial", 16, FontWeight.Normal);
        frag1.Width.ShouldBe(
            Miko.Utils.TextMeasurer.MeasureTextWidth("All", "Arial", 16, FontWeight.Normal) + spaceWidth, 0.5f,
            "text1 片段宽度应包含尾部边界空格");
    }

    [Fact]
    public void Br_InsideInlineRun_StartsNewLine()
    {
        var span1 = new SpanElement();
        span1.AddChild(new TextNode("first"));
        var span2 = new SpanElement();
        span2.AddChild(new TextNode("second"));
        var div = new DivElement { Children = { span1, new BrElement(), span2 } };

        var layout = _layoutEngine.Layout(div, Sheets(BlockContainer(300)), 800, 600);

        var span1Box = layout.Children[0].BoxModel.MarginBox;
        var span2Box = layout.Children[2].BoxModel.MarginBox;
        span2Box.Top.ShouldBeGreaterThanOrEqualTo(span1Box.Top + LineHeight - 1f,
            "br 之后的行内内容应排到新的一行");
        span2Box.Left.ShouldBe(span1Box.Left, 0.5f);
    }

    [Fact]
    public void InlineElement_WithExplicitWidth_WrapsItsTextContent()
    {
        // 定宽行内元素（inline 100px）：其文本内容应在 100px 内换行（重排遍）。
        var span = new SpanElement
        {
            Style = new Style { Display = Display.Inline, Width = Length.Px(100) }
        };
        span.AddChild(new TextNode("alpha beta gamma delta epsilon zeta eta theta"));
        var div = new DivElement { Children = { span } };

        var layout = _layoutEngine.Layout(div, Sheets(BlockContainer(400)), 800, 600);

        var spanBox = layout.Children[0];
        spanBox.BoxModel.Content.Width.ShouldBe(100f, 0.5f);
        spanBox.BoxModel.Content.Height.ShouldBeGreaterThan(LineHeight * 1.5f,
            "定宽行内元素的文本应在其宽度内换行为多行");

        var frags = FragmentsOf(spanBox.Children[0]);
        foreach (var frag in frags)
        {
            frag.Width.ShouldBeLessThanOrEqualTo(100f + 0.5f);
        }
    }

    [Fact]
    public void CjkMixedWithInlineElement_BreaksAroundElement()
    {
        // 中文长文本 + 行内元素混排：中文逐字断行，元素作为原子盒参与，互不重叠。
        var div = new DivElement();
        div.AddChild(new TextNode("桌面软件的打包并不像想象中那样统一而是"));
        var code = new CodeElement();
        code.AddChild(new TextNode("pkg"));
        div.AddChild(code);
        div.AddChild(new TextNode("每个发行版都有自己的包格式"));

        var layout = _layoutEngine.Layout(div, Sheets(BlockContainer(200)), 800, 600);

        var codeBox = layout.Children[1].BoxModel.MarginBox;
        codeBox.Right.ShouldBeLessThanOrEqualTo(layout.BoxModel.Content.Left + 200 + 0.5f,
            "行内元素不应溢出容器宽度");

        foreach (var child in new[] { layout.Children[0], layout.Children[2] })
        {
            foreach (var frag in FragmentsOf(child))
            {
                var rect = AbsRect(child, frag);
                bool overlap = rect.Left < codeBox.Right - 0.5f && rect.Right > codeBox.Left + 0.5f
                            && rect.Top < codeBox.Bottom - 0.5f && rect.Bottom > codeBox.Top + 0.5f;
                overlap.ShouldBeFalse($"中文片段 \"{frag.Text}\" 与行内元素重叠");
            }
        }
    }

    [Fact]
    public void LongLatinWord_WithoutBreakOpportunity_OverflowsOnOwnLine()
    {
        // 无空格超长拉丁单词（未启用 overflow-wrap）：保持整体、独占一行溢出（CSS 行为）。
        var div = new DivElement();
        div.AddChild(new TextNode("short supercalifragilisticexpialidociouslongword end"));

        var layout = _layoutEngine.Layout(div, Sheets(BlockContainer(150)), 800, 600);
        var textBox = layout.Children[0];

        var frags = FragmentsOf(textBox);
        var longFrag = frags.First(f => f.Text.Contains("supercalifragilistic"));
        // 长单词未被拆开……
        longFrag.Text.ShouldBe("supercalifragilisticexpialidociouslongword");
        // ……且独占一行（不与 "short" 同行）。
        var shortFrag = frags.First(f => f.Text.Contains("short"));
        longFrag.Y.ShouldBeGreaterThan(shortFrag.Y, "长单词应移到下一行整体溢出");
    }
}
