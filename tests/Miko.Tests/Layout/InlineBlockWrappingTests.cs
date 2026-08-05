using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-116：连续 inline-block 兄弟盒无视宽度约束，溢出父容器而不换行。
///
/// Razor 编译期会剥离纯空白节点，因此 DOM 里相邻 inline-block 之间没有空白文本节点。
/// 断行机会必须由「原子盒之间」这一位置本身提供（浏览器行为：inline-block 之间是
/// 允许软换行的断行机会），而不是依赖空白单元。
/// </summary>
public class InlineBlockWrappingTests
{
    private readonly LayoutEngine _layoutEngine = new();

    private static List<StyleSheet> ItemStyleSheets(float itemWidth = 100f) => new()
    {
        new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new()
                {
                    Selector = new ClassSelector("root"),
                    Style = new Style { Width = Length.Px(500), Height = Length.Px(500) }
                },
                new()
                {
                    Selector = new ClassSelector("padding"),
                    Style = new Style { Padding = new Padding(Length.Px(0), Length.Px(16)) }
                },
                new()
                {
                    Selector = new ClassSelector("item"),
                    Style = new Style
                    {
                        BoxSizing = BoxSizing.BorderBox,
                        Display = Display.InlineBlock,
                        Width = Length.Px(itemWidth),
                        Height = Length.Px(30),
                        Border = new Border(Length.Px(1), BorderStyle.Solid, (Color)"#eee"),
                        MarginBottom = Length.Px(5),
                    }
                },
            }
        }
    };

    /// <summary>构建 .root &gt; .padding &gt; .item * count（子项之间无空白节点）。</summary>
    private static (DivElement Root, DivElement Padding, List<DivElement> Items) BuildItems(int count)
    {
        var items = new List<DivElement>(count);
        var padding = new DivElement { Class = "padding" };
        for (int i = 0; i < count; i++)
        {
            var item = new DivElement { Class = "item" };
            item.AddChild(new TextNode((i + 1).ToString()));
            padding.AddChild(item);
            items.Add(item);
        }

        var root = new DivElement { Class = "root" };
        root.AddChild(padding);
        return (root, padding, items);
    }

    private static LayoutBox FindBox(LayoutBox root, Element element)
        => FindBoxOrNull(root, element) ?? throw new System.InvalidOperationException("element not in layout tree");

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

    [Fact]
    public void InlineBlockSiblings_ShouldWrapWithinContainerWidth()
    {
        // .root(500) > .padding(0 16) > .item(100) * 6 → 每行最多 4 项（468 内容宽）。
        var (root, padding, items) = BuildItems(6);
        var layout = _layoutEngine.Layout(root, ItemStyleSheets(), 500, 500);

        var paddingBox = FindBox(layout, padding);
        float contentRight = paddingBox.BoxModel.Content.Right;

        foreach (var item in items)
        {
            var box = FindBox(layout, item).BoxModel.MarginBox;
            box.Right.ShouldBeLessThanOrEqualTo(contentRight + 0.5f,
                $"item \"{item.Children[0]}\" 溢出了容器内容宽度");
        }
    }

    [Fact]
    public void InlineBlockSiblings_ShouldPackFourPerLineThenWrap()
    {
        // 468 内容宽 / 100 每项 → 前 4 项同一行，第 5 项换到下一行。
        var (root, _, items) = BuildItems(6);
        var layout = _layoutEngine.Layout(root, ItemStyleSheets(), 500, 500);

        var boxes = items.ConvertAll(i => FindBox(layout, i).BoxModel.MarginBox);

        for (int i = 1; i < 4; i++)
        {
            boxes[i].Top.ShouldBe(boxes[0].Top, 0.5f, $"第 {i + 1} 项应与第 1 项同行");
            boxes[i].Left.ShouldBe(boxes[i - 1].Right, 0.5f, $"第 {i + 1} 项应紧随前一项水平排列");
        }

        boxes[4].Top.ShouldBeGreaterThan(boxes[0].Bottom - 0.5f, "第 5 项应换到下一行");
        boxes[4].Left.ShouldBe(boxes[0].Left, 0.5f, "换行后应回到行首");
        boxes[5].Top.ShouldBe(boxes[4].Top, 0.5f, "第 6 项应与第 5 项同行");
    }

    [Fact]
    public void InlineBlockSiblings_ShouldGrowContainerHeightForEachLine()
    {
        // 两行 × (30 + 5 margin-bottom) = 70。
        var (root, padding, _) = BuildItems(6);
        var layout = _layoutEngine.Layout(root, ItemStyleSheets(), 500, 500);

        FindBox(layout, padding).BoxModel.Content.Height.ShouldBe(70f, 0.5f,
            "换行后容器高度应容纳两行");
    }

    [Fact]
    public void SingleInlineBlockWiderThanContainer_ShouldNotWrapAlone()
    {
        // 单个超宽 inline-block 无处可断，整体溢出（与浏览器一致）。
        var (root, padding, items) = BuildItems(1);
        var layout = _layoutEngine.Layout(root, ItemStyleSheets(itemWidth: 600f), 500, 500);

        var box = FindBox(layout, items[0]).BoxModel.MarginBox;
        var paddingBox = FindBox(layout, padding);
        box.Left.ShouldBe(paddingBox.BoxModel.Content.Left, 0.5f);
        box.Width.ShouldBe(600f, 0.5f);
    }
}
