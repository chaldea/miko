using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Utils;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-099 回归测试：FlexWrap 分行漏算 auto 尺寸 flex 项的 padding/border，
/// 多行 wrap 容器中 align-items 误用容器交叉尺寸导致首行位置偏移，
/// 以及 flex 对齐属性缺少 CSS normal 初始值导致默认行为与浏览器不一致。
///
/// 问题1（分行）：ComputeFlexBasis 把 flex-basis 折算为 outer size（margin-box）时，对
/// border-box 一律不再追加 padding/border——这只对"显式尺寸"成立（显式 border-box 宽度
/// 已含 padding/border）；而 auto 尺寸经内容测量得到的是 content 宽度，无论 box-sizing
/// 都必须补上 padding/border。漏算导致主轴 outer size 偏小、每行多放项目并溢出容器。
///
/// 问题2（行内对齐）：多行（wrap 分行后）容器中，align-items 应在"本行"自然交叉尺寸内
/// 对齐，行级分布由 align-content 负责；修复前对齐范围错用容器内容交叉尺寸，
/// align-items:center 时首行距容器顶部多出 (containerCross - lineCross)/2 的大间距，
/// stretch 项则被拉到容器交叉尺寸而侵入相邻行。
///
/// 问题3（默认值）：CSS 中 align-items/align-content/justify-content 的初始值为 normal
/// （最终行为分别为 stretch / stretch / flex-start）；修复前 Miko 无 Normal 枚举值，
/// align-content 缺省按 FlexStart 紧密排布，与浏览器的 stretch（等分剩余空间增大各行
/// 交叉尺寸，行内子项在增大后的行内重新对齐/拉伸）不一致。
/// </summary>
public class FlexWrapAutoSizeBasisTests
{
    private readonly LayoutEngine _layoutEngine = new();

    /// <summary>按 issues/ISSUE-099.md 复现布局，返回 container 的布局盒。</summary>
    private LayoutBox LayoutIssueRepro()
    {
        var items = new List<DivElement>();
        for (int i = 1; i <= 9; i++)
            items.Add(new DivElement { Class = "item", TextContent = $"Default{i}" });

        var container = new DivElement { Class = "container" };
        foreach (var item in items) container.AddChild(item);
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["*"] = new() { BoxSizing = BoxSizing.BorderBox },
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Width = Length.Px(440),
                Height = Length.Px(200),
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                FlexDirection = FlexDirection.Row,
                FlexWrap = FlexWrap.Wrap,
            },
            [".item"] = new()
            {
                Display = Display.InlineBlock,
                MinHeight = Length.Px(32),
                Padding = new Padding(Length.Px(6), Length.Px(12)),
                Margin = new Margin(Length.Px(4)),
                FontSize = Length.Px(16),
            },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        return layout.Children[0];
    }

    /// <summary>按内容 Y 坐标把子盒分组为行（同一行内各 item 高度相同，Y 一致）。</summary>
    private static List<List<int>> GroupIntoLines(LayoutBox containerBox) =>
        containerBox.Children
            .Select((c, i) => (y: c.BoxModel.Content.Y, i))
            .GroupBy(t => t.y)
            .OrderBy(g => g.Key)
            .Select(g => g.Select(t => t.i).OrderBy(x => x).ToList())
            .ToList();

    [Fact]
    public void FlexRow_Wrap_AutoWidthBorderBoxItems_PartitionByMarginBoxSize()
    {
        var containerBox = LayoutIssueRepro();
        containerBox.Children.Count.ShouldBe(9);

        // 期望分行：按 margin-box 宽度（文本 + padding 24 + margin 8）贪心装入 440。
        var outerWidths = containerBox.Children.Select(c =>
            TextMeasurer.MeasureTextWidth(
                c.Element.TextContent!, c.ComputedStyle.FontFamily, 16f, c.ComputedStyle.FontWeight)
            + 24f + 8f).ToList();

        var expectedLines = new List<List<int>>();
        var currentLine = new List<int>();
        float lineSize = 0;
        for (int i = 0; i < outerWidths.Count; i++)
        {
            if (currentLine.Count > 0 && lineSize + outerWidths[i] > 440f + 0.01f)
            {
                expectedLines.Add(currentLine);
                currentLine = new List<int>();
                lineSize = 0;
            }
            currentLine.Add(i);
            lineSize += outerWidths[i];
        }
        expectedLines.Add(currentLine);

        var actualLines = GroupIntoLines(containerBox);

        actualLines.Count.ShouldBe(expectedLines.Count);
        for (int i = 0; i < expectedLines.Count; i++)
            actualLines[i].ShouldBe(expectedLines[i]);

        // 任何 item 不得溢出容器内容区（修复前首行 6 个 item 溢出 440 边界）。
        float containerRight = containerBox.BoxModel.Content.X + containerBox.BoxModel.Content.Width;
        foreach (var child in containerBox.Children)
            child.BoxModel.MarginBox.Right.ShouldBeLessThanOrEqualTo(containerRight + 0.5f);
    }

    [Fact]
    public void FlexRow_Wrap_MultiLine_DefaultAlignContent_StretchSpreadsLines()
    {
        var containerBox = LayoutIssueRepro();
        float contentTop = containerBox.BoxModel.Content.Y;

        // align-content 缺省 = normal → stretch：各行等分剩余空间增大交叉尺寸，
        // 行内子项（align-items:center）在增大后的行内居中——与浏览器一致。
        // 修复前（FlexStart 缺省）各行自顶部紧密排布，底部留空，与浏览器渲染不符。
        float lineHeight = containerBox.Children[0].BoxModel.MarginBox.Height;
        var lines = GroupIntoLines(containerBox);
        lines.Count.ShouldBeGreaterThan(1); // 确认为多行场景

        float free = 200f - lines.Count * lineHeight;
        free.ShouldBeGreaterThan(0f);
        float growth = free / lines.Count;

        for (int line = 0; line < lines.Count; line++)
        {
            // 行 L 起点 = L × (自然行高 + 每行增量)；居中项相对行起点偏移 growth/2。
            float expectedTop = contentTop + line * (lineHeight + growth) + growth / 2f;
            foreach (var index in lines[line])
                containerBox.Children[index].BoxModel.MarginBox.Top.ShouldBe(expectedTop, 0.5f);
        }
    }

    [Fact]
    public void FlexRow_Wrap_AlignItemsCenter_AlignsWithinEachLineNotContainer()
    {
        // 行内项高不一：line1 = A(40) + B(100)，line2 = C(40)。
        // align-content 显式 FlexStart（隔离问题2，排除默认 stretch 的行增大）：
        // align-items:center 应在各行自身交叉尺寸内居中：A 在 line1（高 100）内居中 → y=30；
        // C 在 line2（高 40）内 → 紧贴 line2 顶部 y=100。修复前对齐范围为容器高 400，
        // A 会被放到 y=(400-40)/2=180。
        var itemA = new DivElement { Class = "a" };
        var itemB = new DivElement { Class = "b" };
        var itemC = new DivElement { Class = "c" };
        var container = new DivElement { Class = "container", Children = { itemA, itemB, itemC } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Width = Length.Px(210),
                Height = Length.Px(400),
                Display = Display.Flex,
                FlexWrap = FlexWrap.Wrap,
                AlignItems = AlignItems.Center,
                AlignContent = AlignContent.FlexStart,
            },
            [".a"] = new() { Width = Length.Px(100), Height = Length.Px(40) },
            [".b"] = new() { Width = Length.Px(100), Height = Length.Px(100) },
            [".c"] = new() { Width = Length.Px(100), Height = Length.Px(40) },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];

        containerBox.Children[0].BoxModel.Content.Y.ShouldBe(30f, 0.5f);  // A：line1 内居中
        containerBox.Children[1].BoxModel.Content.Y.ShouldBe(0f, 0.5f);   // B：line1 最高项
        containerBox.Children[2].BoxModel.Content.Y.ShouldBe(100f, 0.5f); // C：line2 顶部
    }

    [Fact]
    public void FlexColumn_Wrap_AlignItemsCenter_AlignsWithinEachColumnNotContainer()
    {
        // 列方向对称用例：col1 = A(宽40) + B(宽100)，col2 = C(宽40)。
        // align-content 显式 FlexStart（隔离问题2）：align-items:center（交叉轴为水平）
        // A 在 col1（宽 100）内居中 → x=30；C 在 col2（宽 40）内 → 紧贴 col2 左缘 x=100。
        var itemA = new DivElement { Class = "a" };
        var itemB = new DivElement { Class = "b" };
        var itemC = new DivElement { Class = "c" };
        var container = new DivElement { Class = "container", Children = { itemA, itemB, itemC } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Width = Length.Px(400),
                Height = Length.Px(210),
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                FlexWrap = FlexWrap.Wrap,
                AlignItems = AlignItems.Center,
                AlignContent = AlignContent.FlexStart,
            },
            [".a"] = new() { Width = Length.Px(40), Height = Length.Px(100) },
            [".b"] = new() { Width = Length.Px(100), Height = Length.Px(100) },
            [".c"] = new() { Width = Length.Px(40), Height = Length.Px(100) },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];

        containerBox.Children[0].BoxModel.Content.X.ShouldBe(30f, 0.5f);  // A：col1 内居中
        containerBox.Children[1].BoxModel.Content.X.ShouldBe(0f, 0.5f);   // B：col1 最宽项
        containerBox.Children[2].BoxModel.Content.X.ShouldBe(100f, 0.5f); // C：col2 左缘
    }

    [Fact]
    public void FlexRow_Wrap_DefaultAlignContent_StretchGrowsLinesAndRealignsItems()
    {
        // 问题3：align-content 缺省 = normal → stretch。两行（100、40），容器高 400，
        // free=260，每行增大 130 → 行交叉尺寸 230、170；align-items:center 在增大后的行内
        // 居中：A y=(230-40)/2=95，B y=(230-100)/2=65；line2 起于 230，C y=230+(170-40)/2=295。
        // 修复前缺省为 FlexStart：A y=30、B y=0、C y=100，与浏览器不一致。
        var itemA = new DivElement { Class = "a" };
        var itemB = new DivElement { Class = "b" };
        var itemC = new DivElement { Class = "c" };
        var container = new DivElement { Class = "container", Children = { itemA, itemB, itemC } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Width = Length.Px(210),
                Height = Length.Px(400),
                Display = Display.Flex,
                FlexWrap = FlexWrap.Wrap,
                AlignItems = AlignItems.Center,
            },
            [".a"] = new() { Width = Length.Px(100), Height = Length.Px(40) },
            [".b"] = new() { Width = Length.Px(100), Height = Length.Px(100) },
            [".c"] = new() { Width = Length.Px(100), Height = Length.Px(40) },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];

        containerBox.Children[0].BoxModel.Content.Y.ShouldBe(95f, 0.5f);  // A
        containerBox.Children[1].BoxModel.Content.Y.ShouldBe(65f, 0.5f);  // B
        containerBox.Children[2].BoxModel.Content.Y.ShouldBe(295f, 0.5f); // C
    }

    [Fact]
    public void FlexRow_Wrap_DefaultAlignContent_StretchStretchesAutoCrossSizeItems()
    {
        // 问题3：align-items 缺省 = normal → stretch，align-content 缺省 = normal → stretch。
        // auto 高度的 A/C 在增大后的行内拉伸填满（margin-box == 行交叉尺寸）：
        // line1（自然 100）增大 130 → A 高 230；line2（自然 40）增大 130 → C 高 170；
        // 显式高度的 B 不可拉伸，行为同 flex-start（y=0，高 100 不变）。
        var itemA = new DivElement { Class = "a", Children = { new DivElement { Class = "fill" } } };
        var itemB = new DivElement { Class = "b" };
        var itemC = new DivElement { Class = "c", Children = { new DivElement { Class = "fill" } } };
        var container = new DivElement { Class = "container", Children = { itemA, itemB, itemC } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Width = Length.Px(210),
                Height = Length.Px(400),
                Display = Display.Flex,
                FlexWrap = FlexWrap.Wrap,
            },
            [".a"] = new() { Width = Length.Px(100) },
            [".b"] = new() { Width = Length.Px(100), Height = Length.Px(100) },
            [".c"] = new() { Width = Length.Px(100) },
            [".fill"] = new() { Height = Length.Px(40) },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];

        containerBox.Children[0].BoxModel.Content.Y.ShouldBe(0f, 0.5f);
        containerBox.Children[0].BoxModel.Content.Height.ShouldBe(230f, 0.5f); // A 拉伸填满增大行
        containerBox.Children[1].BoxModel.Content.Y.ShouldBe(0f, 0.5f);
        containerBox.Children[1].BoxModel.Content.Height.ShouldBe(100f, 0.5f); // B 显式高度不变
        containerBox.Children[2].BoxModel.Content.Y.ShouldBe(230f, 0.5f);      // line2 起点
        containerBox.Children[2].BoxModel.Content.Height.ShouldBe(170f, 0.5f); // C 拉伸填满增大行
    }

    [Fact]
    public void FlexColumn_Wrap_DefaultAlignContent_StretchGrowsColumnsAndRealignsItems()
    {
        // 列方向对称用例（问题3）：两列（宽 100、40），容器宽 400，free=260，
        // 每列增大 130 → 列交叉尺寸 230、170；align-items:center 在增大后的列内居中：
        // A x=(230-40)/2=95，B x=(230-100)/2=65；col2 起于 230，C x=230+(170-40)/2=295。
        var itemA = new DivElement { Class = "a" };
        var itemB = new DivElement { Class = "b" };
        var itemC = new DivElement { Class = "c" };
        var container = new DivElement { Class = "container", Children = { itemA, itemB, itemC } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Width = Length.Px(400),
                Height = Length.Px(210),
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                FlexWrap = FlexWrap.Wrap,
                AlignItems = AlignItems.Center,
            },
            [".a"] = new() { Width = Length.Px(40), Height = Length.Px(100) },
            [".b"] = new() { Width = Length.Px(100), Height = Length.Px(100) },
            [".c"] = new() { Width = Length.Px(40), Height = Length.Px(100) },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];

        containerBox.Children[0].BoxModel.Content.X.ShouldBe(95f, 0.5f);  // A
        containerBox.Children[1].BoxModel.Content.X.ShouldBe(65f, 0.5f);  // B
        containerBox.Children[2].BoxModel.Content.X.ShouldBe(295f, 0.5f); // C
    }

    [Fact]
    public void FlexColumn_Wrap_AutoHeightBorderBoxItems_PartitionByMarginBoxSize()
    {
        // 列方向对称用例：auto 高度 item（min-height 32 + padding 6 + margin 4），
        // 容器高 120，验证按 outer 高度分列且不溢出。
        var items = new List<DivElement>();
        for (int i = 1; i <= 9; i++)
            items.Add(new DivElement { Class = "item", TextContent = $"D{i}" });

        var container = new DivElement { Class = "container" };
        foreach (var item in items) container.AddChild(item);
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["*"] = new() { BoxSizing = BoxSizing.BorderBox },
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Width = Length.Px(300),
                Height = Length.Px(120),
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                FlexWrap = FlexWrap.Wrap,
            },
            [".item"] = new()
            {
                Display = Display.InlineBlock,
                MinHeight = Length.Px(32),
                Padding = new Padding(Length.Px(6), Length.Px(12)),
                Margin = new Margin(Length.Px(4)),
                FontSize = Length.Px(16),
            },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];
        containerBox.Children.Count.ShouldBe(9);

        // 任何 item 不得溢出容器主轴边界（修复前首列多装 item 溢出 120 高度）。
        float containerBottom = containerBox.BoxModel.Content.Y + containerBox.BoxModel.Content.Height;
        foreach (var child in containerBox.Children)
            child.BoxModel.MarginBox.Bottom.ShouldBeLessThanOrEqualTo(containerBottom + 0.5f);
    }
}
