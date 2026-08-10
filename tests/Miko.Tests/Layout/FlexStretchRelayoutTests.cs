using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-122 回归测试：stretch 拉伸出的交叉尺寸必须对被拉伸项自身的布局生效——项目要以
/// 拉伸后的确定交叉尺寸重新布局其子树（CSS Flexbox §9.4 步骤 8；grid 的 align/justify-items
/// stretch 同理）。
///
/// 缺陷表现：拉伸只改写了项目的 content 盒尺寸，子树仍停留在按拉伸前的自然交叉尺寸算出的
/// 位置上。于是 stretch 行内一个 <c>align-items:center</c> 的 flex 子项，其内容按内容高
/// （17.9px）而非拉伸后的行高（44px）居中，表现为贴顶。
/// </summary>
public class FlexStretchRelayoutTests
{
    private readonly LayoutEngine _layoutEngine = new();

    /// <summary>
    /// ISSUE-122 原始复现：min-height 撑出的 stretch 行内，两个 align-items:center 的
    /// flex 子项中的 span 都应相对 44px 行高垂直居中。
    /// </summary>
    [Fact]
    public void RowStretch_CenterAlignedFlexItem_CentersItsContentInStretchedHeight()
    {
        var labelSpan = new SpanElement { TextContent = "Test" };
        var nativeSpan = new SpanElement { TextContent = "Test" };
        var label = new DivElement { Class = "input-label", Children = { labelSpan } };
        var native = new DivElement { Class = "input-native", Children = { nativeSpan } };
        var wrapper = new DivElement { Class = "input-wrapper", Children = { label, native } };
        var input = new DivElement { Class = "input", Children = { wrapper } };
        var root = new DivElement { Class = "root", Children = { input } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["*"] = new() { BoxSizing = BoxSizing.BorderBox },
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".input"] = new() { Display = Display.Block, MinHeight = Length.Px(44) },
            [".input-wrapper"] = new()
            {
                Display = Display.Flex,
                MinHeight = Length.Px(44),
                AlignItems = AlignItems.Stretch,
            },
            [".input-label"] = new() { Display = Display.Flex, AlignItems = AlignItems.Center },
            [".input-native"] = new() { Display = Display.Flex, AlignItems = AlignItems.Center },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var wrapperBox = layout.Children[0].Children[0];
        var labelBox = wrapperBox.Children[0];
        var nativeBox = wrapperBox.Children[1];

        // 前提：min-height 把 wrapper 抬到 44，stretch 把两个子项也拉到 44。
        wrapperBox.BoxModel.Content.Height.ShouldBe(44f, 0.5f);
        labelBox.BoxModel.Content.Height.ShouldBe(44f, 0.5f);
        nativeBox.BoxModel.Content.Height.ShouldBe(44f, 0.5f);

        // 核心断言：span 相对被拉伸到 44 的父级内容盒垂直居中，而非贴顶（修复前 Y == 父 Y）。
        foreach (var (parent, span) in new[] { (labelBox, labelBox.Children[0]), (nativeBox, nativeBox.Children[0]) })
        {
            var spanBox = span.BoxModel.MarginBox;
            float expectedY = parent.BoxModel.Content.Y + (44f - spanBox.Height) / 2f;
            spanBox.Y.ShouldBe(expectedY, 0.5f);
            spanBox.Y.ShouldBeGreaterThan(parent.BoxModel.Content.Y + 1f);
        }
    }

    /// <summary>
    /// 列方向对称守卫：stretch 沿宽度拉伸，子项内部 justify-content:center 应相对拉伸后的
    /// 宽度水平居中。该用例在修复前即通过（列向 stretch 已通过约束把交叉宽度传给子项），
    /// 保留以防重排逻辑破坏该路径。
    /// </summary>
    [Fact]
    public void ColumnStretch_CenterAlignedFlexItem_CentersItsContentInStretchedWidth()
    {
        var span = new SpanElement { TextContent = "Test" };
        var item = new DivElement { Class = "item", Children = { span } };
        var container = new DivElement { Class = "container", Children = { item } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = Length.Px(300),
                AlignItems = AlignItems.Stretch,
            },
            // 行方向 flex 项：主轴（水平）居中，交叉轴（垂直）由 stretch 拉伸。
            [".item"] = new() { Display = Display.Flex, JustifyContent = JustifyContent.Center },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var itemBox = layout.Children[0].Children[0];
        var spanBox = itemBox.Children[0];

        // stretch 把 item 宽度拉到容器的 300。
        itemBox.BoxModel.Content.Width.ShouldBe(300f, 0.5f);

        // span 在拉伸后的 300 宽内水平居中（修复前按内容宽居中，等价于贴左）。
        float expectedX = itemBox.BoxModel.Content.X + (300f - spanBox.BoxModel.MarginBox.Width) / 2f;
        spanBox.BoxModel.MarginBox.X.ShouldBe(expectedX, 0.5f);
        spanBox.BoxModel.MarginBox.X.ShouldBeGreaterThan(itemBox.BoxModel.Content.X + 1f);
    }

    /// <summary>
    /// 拉伸后的交叉尺寸是"确定尺寸"，后代的百分比高度应以它为基准解析
    /// （拉伸前项目高度为 auto，height:100% 会退化为内容高）。
    /// </summary>
    [Fact]
    public void RowStretch_DescendantPercentHeight_ResolvesAgainstStretchedHeight()
    {
        var fill = new DivElement { Class = "fill" };
        var item = new DivElement { Class = "item", Children = { fill } };
        var container = new DivElement { Class = "container", Children = { item } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Display = Display.Flex,
                Height = Length.Px(80),
                AlignItems = AlignItems.Stretch,
            },
            [".item"] = new() { Display = Display.Block, Width = Length.Px(100) },
            [".fill"] = new() { Height = Length.Percent(100) },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var itemBox = layout.Children[0].Children[0];

        itemBox.BoxModel.Content.Height.ShouldBe(80f, 0.5f);
        itemBox.Children[0].BoxModel.Content.Height.ShouldBe(80f, 0.5f);
    }

    /// <summary>
    /// 回归保护：显式交叉尺寸的项目不参与 stretch，重排不得改变其高度，
    /// 且其内部对齐仍相对自身高度求值。
    /// </summary>
    [Fact]
    public void RowStretch_ExplicitCrossSizeItem_IsNotStretchedOrRelaidOut()
    {
        var span = new SpanElement { TextContent = "Test" };
        var fixedItem = new DivElement { Class = "fixed", Children = { span } };
        var container = new DivElement { Class = "container", Children = { fixedItem } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Display = Display.Flex,
                Height = Length.Px(200),
                AlignItems = AlignItems.Stretch,
            },
            [".fixed"] = new()
            {
                Display = Display.Flex,
                Height = Length.Px(40),
                AlignItems = AlignItems.Center,
            },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var fixedBox = layout.Children[0].Children[0];
        var spanBox = fixedBox.Children[0];

        // 显式高度不被拉伸到 200。
        fixedBox.BoxModel.Content.Height.ShouldBe(40f, 0.5f);

        // 内部 center 相对自身 40 高居中。
        float expectedY = fixedBox.BoxModel.Content.Y + (40f - spanBox.BoxModel.MarginBox.Height) / 2f;
        spanBox.BoxModel.MarginBox.Y.ShouldBe(expectedY, 0.5f);
    }

    /// <summary>
    /// align-content: stretch（多行）路径同样需要重排：行被等分增大后，
    /// 行内 stretch 项的子树要相对增大后的行交叉尺寸重新对齐。
    /// </summary>
    [Fact]
    public void AlignContentStretch_GrownLine_RelaysOutStretchedItemContent()
    {
        var spanA = new SpanElement { TextContent = "A" };
        var spanB = new SpanElement { TextContent = "B" };
        var itemA = new DivElement { Class = "item", Children = { spanA } };
        var itemB = new DivElement { Class = "item", Children = { spanB } };
        var container = new DivElement { Class = "container", Children = { itemA, itemB } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Display = Display.Flex,
                FlexWrap = FlexWrap.Wrap,
                Width = Length.Px(100),
                Height = Length.Px(200),
                AlignContent = AlignContent.Stretch,
                AlignItems = AlignItems.Stretch,
            },
            // 每项宽 80 > 容器 100 的一半，强制分两行；内部 center 依赖被增大后的行高。
            [".item"] = new()
            {
                Display = Display.Flex,
                Width = Length.Px(80),
                AlignItems = AlignItems.Center,
            },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];
        containerBox.Children.Count.ShouldBe(2);

        // 两行等分 200 高：每行 100，两项都被拉到 100。
        foreach (var itemBox in containerBox.Children)
        {
            itemBox.BoxModel.Content.Height.ShouldBe(100f, 0.5f);

            var childBox = itemBox.Children[0].BoxModel.MarginBox;
            float expectedY = itemBox.BoxModel.Content.Y + (100f - childBox.Height) / 2f;
            childBox.Y.ShouldBe(expectedY, 0.5f);
        }
    }

    /// <summary>
    /// Grid 的 align-items/justify-items:stretch 走同一套"拉伸后重排"逻辑：
    /// 被拉满 area 的子项内部对齐应相对拉伸后的 area 尺寸求值，而非拉伸前的内容尺寸。
    /// </summary>
    [Fact]
    public void GridStretch_CenterAlignedFlexItem_CentersItsContentInStretchedArea()
    {
        var span = new SpanElement { TextContent = "Test" };
        var item = new DivElement { Class = "item", Children = { span } };
        var container = new DivElement { Class = "container", Children = { item } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100) },
                GridTemplateRows = new List<GridTrackSize> { GridTrackSize.Px(80) },
            },
            // 默认 align/justify-items 为 normal（表现为 stretch）：item 被拉满 100x80。
            [".item"] = new() { Display = Display.Flex, AlignItems = AlignItems.Center },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var itemBox = layout.Children[0].Children[0];
        var spanBox = itemBox.Children[0].BoxModel.MarginBox;

        itemBox.BoxModel.Content.Width.ShouldBe(100f, 0.5f);
        itemBox.BoxModel.Content.Height.ShouldBe(80f, 0.5f);

        // span 相对拉伸后的 80 高居中（修复前贴顶）。
        float expectedY = itemBox.BoxModel.Content.Y + (80f - spanBox.Height) / 2f;
        spanBox.Y.ShouldBe(expectedY, 0.5f);
        spanBox.Y.ShouldBeGreaterThan(itemBox.BoxModel.Content.Y + 1f);
    }
}
