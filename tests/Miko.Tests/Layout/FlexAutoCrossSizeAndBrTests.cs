using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Layout.LayoutAlgorithms;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-093 回归测试。
///
/// 问题1：Flex 容器中，交叉轴尺寸为 auto 且未 stretch 的子元素应 shrink-to-fit（内容尺寸），
///        不应撑满容器交叉轴。仅 align-items/align-self: stretch 时才填满交叉轴。
///
/// 问题2：<c>&lt;br&gt;</c> 作为 flex 项应生成一个行盒（占一行行高），使其在列方向产生
///        可见换行（对齐浏览器：br flex 项是一个空的行盒）。
/// </summary>
public class FlexAutoCrossSizeAndBrTests
{
    private readonly LayoutEngine _layoutEngine = new();

    // ── 问题1：交叉轴 auto 的非 stretch 项应 shrink-to-fit ─────────────────────

    [Fact]
    public void FlexColumn_AutoWidthChild_NotStretch_ShrinksToFitAndCenters()
    {
        // <root W=500 H=500> <container flex-col center> <div1 (auto width, text)/> </container> </root>
        // align-items: center（非 stretch）：div1 宽度为 auto，应收缩到文本宽度而非撑满 500，
        // 并在交叉轴（水平）居中。
        var div1 = new DivElement { Class = "div1", TextContent = "div1" };
        var container = new DivElement { Class = "container", Children = { div1 } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                FontSize = Length.Px(16),
            },
            [".div1"] = new() { Height = Length.Px(30) },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];
        var div1Box = containerBox.Children[0];

        float textWidth = Miko.Utils.TextMeasurer.MeasureTextWidth(
            "div1", div1Box.ComputedStyle.FontFamily, 16f, div1Box.ComputedStyle.FontWeight);

        // shrink-to-fit：宽度为文本宽度，远小于容器 500。
        div1Box.BoxModel.Content.Width.ShouldBe(textWidth, 0.5f);
        div1Box.BoxModel.Content.Width.ShouldBeLessThan(500f);

        // 交叉轴（水平）居中：左缘 = (500 - 宽) / 2。
        float expectedX = containerBox.BoxModel.Content.X + (500f - div1Box.BoxModel.MarginBox.Width) / 2f;
        div1Box.BoxModel.Content.X.ShouldBe(expectedX, 0.5f);
    }

    [Fact]
    public void FlexColumn_AutoWidthChild_DefaultStretch_FillsContainer()
    {
        // 回归保护：默认 align-items 为 stretch，auto 宽度子元素应撑满容器交叉轴（500）。
        var child = new DivElement { Class = "child" };
        var container = new DivElement { Class = "container", Children = { child } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                // 不设置 AlignItems → 默认 Stretch。
            },
            [".child"] = new() { Height = Length.Px(30) },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var childBox = layout.Children[0].Children[0];

        // stretch：撑满容器内容宽度 500。
        childBox.BoxModel.Content.Width.ShouldBe(500f, 0.5f);
    }

    [Fact]
    public void FlexColumn_ExplicitWidthChild_IsUnaffected()
    {
        // 显式宽度子元素不受 shrink-to-fit 影响，仍为其显式宽度并居中。
        var child = new DivElement { Class = "child" };
        var container = new DivElement { Class = "container", Children = { child } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.Center,
            },
            [".child"] = new() { Width = Length.Px(100), Height = Length.Px(30) },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];
        var childBox = containerBox.Children[0];

        childBox.BoxModel.Content.Width.ShouldBe(100f, 0.5f);
        // 居中：(500 - 100) / 2 = 200。
        childBox.BoxModel.Content.X.ShouldBe(containerBox.BoxModel.Content.X + 200f, 0.5f);
    }

    // ── 问题2：<br> 在 flex 列中生成一个行盒并产生换行 ──────────────────────────

    [Fact]
    public void FlexColumn_Br_OccupiesOneLineAndPushesNextItemDown()
    {
        // <container flex-col> <div1 H=30/> <br/> <div2 H=30/> </container>
        // br 应占一行行高（此处显式 line-height=20 保证确定性），div2 起点被下推。
        var div1 = new DivElement { Class = "box", TextContent = "div1" };
        var br = new BrElement();
        var div2 = new DivElement { Class = "box", TextContent = "div2" };
        var container = new DivElement { Class = "container", Children = { div1, br, div2 } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                FontSize = Length.Px(16),
            },
            [".box"] = new() { Height = Length.Px(30) },
            // 显式 line-height 让 br 的行高确定（否则依赖字体度量）。
            ["br"] = new() { LineHeight = Length.Px(20) },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];

        containerBox.Children.Count.ShouldBe(3);
        var div1Box = containerBox.Children[0];
        var brBox = containerBox.Children[1];
        var div2Box = containerBox.Children[2];

        brBox.Element.ShouldBeOfType<BrElement>();

        // br 占据一行行高（20），且不产生宽度。
        brBox.BoxModel.Content.Height.ShouldBe(20f, 0.5f);
        brBox.BoxModel.Content.Width.ShouldBe(0f, 0.5f);

        // 列方向堆叠：div1 在顶部，br 紧随，div2 被 br 下推。
        div1Box.BoxModel.Content.Y.ShouldBe(containerBox.BoxModel.Content.Y, 0.5f);
        brBox.BoxModel.Content.Y.ShouldBe(div1Box.BoxModel.MarginBox.Bottom, 0.5f);
        div2Box.BoxModel.Content.Y.ShouldBe(brBox.BoxModel.MarginBox.Bottom, 0.5f);

        // 容器高度累加：30 (div1) + 20 (br) + 30 (div2) = 80。
        containerBox.BoxModel.Content.Height.ShouldBe(80f, 0.5f);
    }

    [Fact]
    public void FlexColumn_WithoutBr_ItemsAreAdjacent()
    {
        // 对照组：无 br 时 div1 与 div2 相邻（无换行间隙）。
        var div1 = new DivElement { Class = "box", TextContent = "div1" };
        var div2 = new DivElement { Class = "box", TextContent = "div2" };
        var container = new DivElement { Class = "container", Children = { div1, div2 } };
        var root = new DivElement { Class = "root", Children = { container } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".root"] = new() { Width = Length.Px(500), Height = Length.Px(500) },
            [".container"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                FontSize = Length.Px(16),
            },
            [".box"] = new() { Height = Length.Px(30) },
        });

        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 800, 600);
        var containerBox = layout.Children[0];
        var div1Box = containerBox.Children[0];
        var div2Box = containerBox.Children[1];

        // 无 br：div2 紧贴 div1 底部，容器高度 = 60。
        div2Box.BoxModel.Content.Y.ShouldBe(div1Box.BoxModel.MarginBox.Bottom, 0.5f);
        containerBox.BoxModel.Content.Height.ShouldBe(60f, 0.5f);
    }
}
