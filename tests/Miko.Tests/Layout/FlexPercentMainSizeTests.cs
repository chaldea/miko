using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-106：flex 项目的百分比主轴尺寸被二次解析。
/// flex 算法先用 flex-basis/width 的百分比相对"容器主轴尺寸"解析出项目最终主尺寸，
/// 随后又以该"已解析尺寸"作为 AvailableWidth/AvailableHeight 派发子布局——子布局算法
/// （Block/Flex/Grid/Inline/Table）会再次用项目自身的百分比宽度/高度对其求值：
/// width:50% 的项目实际得到 50%×50%，且其后代的百分比尺寸也基于这个错误基准。
/// 修复：flex 派发项目布局时把最终主轴内容尺寸作为"已定型尺寸"传入约束，
/// 子布局直接使用该值，跳过自身 width/height 解析与该轴 min/max 夹取（均已在 flex 完成）。
/// </summary>
public class FlexPercentMainSizeTests
{
    private readonly LayoutEngine _layoutEngine = new();

    /// <summary>复刻 ISSUE-106 的 DOM 与样式（DebugDemo 场景）。</summary>
    private static (DivElement root, List<StyleSheet> sheets) BuildIssueScene()
    {
        var root = new DivElement { Class = "root" };
        var list = new DivElement { Class = "list" };
        var item = new DivElement { Class = "item" };
        root.AddChild(list);
        list.AddChild(item);

        var sheet = new StyleSheet();
        sheet.Add(new CssObject()
        {
            ["*"] = new()
            {
                BoxSizing = BoxSizing.BorderBox,
            },
            [".root"] = new()
            {
                Width = Length.Px(500),
                Height = Length.Px(250),
                Display = Display.Flex,
            }
        });
        sheet.Add(new CssObject()
        {
            [".list"] = new()
            {
                Display = Display.Block,
                Width = Length.Percent(50),
            },
            [".item"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Px(40),
                Display = Display.Block,
                Border = new Border(Length.Px(1), BorderStyle.Solid, Color.Black)
            }
        });
        return (root, new List<StyleSheet> { sheet });
    }

    [Fact]
    public void Should_NotDoubleApplyPercent_WhenFlexItemHasPercentWidth()
    {
        var (root, sheets) = BuildIssueScene();

        var rootBox = _layoutEngine.Layout(root, sheets, 500, 250);

        var listBox = rootBox.Children[0];
        var itemBox = listBox.Children[0];

        // .list：500 的 50% = 250（border-box，无 padding/border → 内容宽即 250）。
        listBox.BoxModel.Content.Width.ShouldBe(250f, 0.01f);
        // .item：width:100% 应等于 .list 的内容宽 250（border-box 含 2px 边框 → 内容 248）。
        // 修复前百分比被二次解析：.list 自身内容宽被算成 125，.item 只有 125。
        itemBox.BoxModel.MarginBox.Width.ShouldBe(250f, 0.01f);
        itemBox.BoxModel.Content.Width.ShouldBe(248f, 0.01f);
    }

    [Fact]
    public void Should_NotDoubleApplyPercent_WhenColumnFlexItemHasPercentHeight()
    {
        // 列方向对称场景：主轴为高度，百分比 height 同样被二次解析。
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = Length.Px(250),
                Height = Length.Px(500),
            }
        };
        var list = new DivElement
        {
            Style = new Style { Display = Display.Block, Height = Length.Percent(50) }
        };
        var item = new DivElement
        {
            Style = new Style { Display = Display.Block, Height = Length.Percent(100) }
        };
        root.AddChild(list);
        list.AddChild(item);

        var rootBox = _layoutEngine.Layout(root, new List<StyleSheet>(), 250, 500);

        var listBox = rootBox.Children[0];
        var itemBox = listBox.Children[0];

        listBox.BoxModel.Content.Height.ShouldBe(250f, 0.01f);
        // 修复前：.list 内容高被算成 125，.item 的 100% 也只有 125。
        itemBox.BoxModel.Content.Height.ShouldBe(250f, 0.01f);
    }

    [Fact]
    public void Should_NotDoubleApplyPercent_WhenFlexItemIsItselfFlex()
    {
        // flex 项目自身也是 flex 容器（嵌套 flex）：宽度同样经百分比二次解析。
        var root = new DivElement
        {
            Style = new Style { Display = Display.Flex, Width = Length.Px(500), Height = Length.Px(250) }
        };
        var list = new DivElement
        {
            Style = new Style { Display = Display.Flex, Width = Length.Percent(50) }
        };
        var item = new DivElement
        {
            Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Px(40) }
        };
        root.AddChild(list);
        list.AddChild(item);

        var rootBox = _layoutEngine.Layout(root, new List<StyleSheet>(), 500, 250);

        var listBox = rootBox.Children[0];
        var itemBox = listBox.Children[0];

        listBox.BoxModel.Content.Width.ShouldBe(250f, 0.01f);
        itemBox.BoxModel.Content.Width.ShouldBe(250f, 0.01f);
    }

    [Fact]
    public void Should_UseFinalGrownSize_AsDescendantPercentBase()
    {
        // grow 使项目最终主尺寸（500）大于 flex-basis（250）时，
        // 后代的百分比尺寸必须基于"最终尺寸"而非自身百分比的二次解析结果。
        var root = new DivElement
        {
            Style = new Style { Display = Display.Flex, Width = Length.Px(500), Height = Length.Px(250) }
        };
        var list = new DivElement
        {
            Style = new Style { Display = Display.Block, Width = Length.Percent(50), FlexGrow = 1 }
        };
        var item = new DivElement
        {
            Style = new Style { Display = Display.Block, Width = Length.Percent(50), Height = Length.Px(40) }
        };
        root.AddChild(list);
        list.AddChild(item);

        var rootBox = _layoutEngine.Layout(root, new List<StyleSheet>(), 500, 250);

        var listBox = rootBox.Children[0];
        var itemBox = listBox.Children[0];

        // .list grow 后占满 500；.item 的 50% 应基于 500 → 250。
        listBox.BoxModel.Content.Width.ShouldBe(500f, 0.01f);
        itemBox.BoxModel.Content.Width.ShouldBe(250f, 0.01f);
    }

    [Fact]
    public void Should_UseClampedSize_AsDescendantPercentBase()
    {
        // 百分比 max-width 夹取：flex 已相对容器（500）把项目夹到 250，
        // 子布局不得再相对"已夹取尺寸"二次应用百分比 min/max 与 width。
        var root = new DivElement
        {
            Style = new Style { Display = Display.Flex, Width = Length.Px(500), Height = Length.Px(250) }
        };
        var list = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Percent(80),
                MaxWidth = Length.Percent(50),
            }
        };
        var item = new DivElement
        {
            Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Px(40) }
        };
        root.AddChild(list);
        list.AddChild(item);

        var rootBox = _layoutEngine.Layout(root, new List<StyleSheet>(), 500, 250);

        var listBox = rootBox.Children[0];
        var itemBox = listBox.Children[0];

        // basis 80%×500=400 被 max-width:50% 夹取为 250；.item 的 100% 应等于 250。
        listBox.BoxModel.Content.Width.ShouldBe(250f, 0.01f);
        itemBox.BoxModel.Content.Width.ShouldBe(250f, 0.01f);
    }

    [Fact]
    public void Should_ResolveItemPercentAgainstListContentWidth_WithPadding()
    {
        // .list 带 padding：border-box 50% = 250，内容宽 230；.item 的 100% 应解析为 230。
        var root = new DivElement
        {
            Style = new Style { Display = Display.Flex, Width = Length.Px(500), Height = Length.Px(250) }
        };
        var list = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Percent(50),
                BoxSizing = BoxSizing.BorderBox,
                Padding = new Padding(Length.Px(10)),
            }
        };
        var item = new DivElement
        {
            Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Px(40) }
        };
        root.AddChild(list);
        list.AddChild(item);

        var rootBox = _layoutEngine.Layout(root, new List<StyleSheet>(), 500, 250);

        var listBox = rootBox.Children[0];
        var itemBox = listBox.Children[0];

        listBox.BoxModel.MarginBox.Width.ShouldBe(250f, 0.01f);
        listBox.BoxModel.Content.Width.ShouldBe(230f, 0.01f);
        itemBox.BoxModel.Content.Width.ShouldBe(230f, 0.01f);
        itemBox.BoxModel.Content.X.ShouldBe(10f, 0.01f);
    }
}
