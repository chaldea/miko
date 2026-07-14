using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// 复现并验证 ISSUE-081：inline-block 元素上的 min-width / min-height 应被正确应用。
/// </summary>
public class InlineBlockMinSizeTests
{
    private readonly LayoutEngine _layoutEngine = new();

    /// <summary>
    /// 构建 ISSUE-081 中描述的 DOM 与样式：
    /// .root(500×500) > .button(inline-block, auto, min 40×40) > .button-native(flex, 100%) > .button-inner(flex, 100%, 文本 "AU")
    /// </summary>
    private LayoutBox LayoutIssueTree()
    {
        var inner = new SpanElement { Class = "button-inner", TextContent = "AU" };
        var native = new SpanElement { Class = "button-native", Children = { inner } };
        var button = new DivElement { Class = "button", Children = { native } };
        var root = new DivElement { Class = "root", Children = { button } };

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new()
                    {
                        Selector = new UniversalSelector(),
                        Style = new Style { BoxSizing = BoxSizing.BorderBox }
                    },
                    new()
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style { Width = Length.Px(500), Height = Length.Px(500) }
                    },
                    new()
                    {
                        Selector = new ClassSelector("button"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Width = Length.Auto,
                            Height = Length.Auto,
                            MinWidth = Length.Px(40),
                            MinHeight = Length.Px(40),
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("button-native"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            Width = Length.Percent(100),
                            Height = Length.Percent(100),
                            MinHeight = Length.Px(36),
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("button-inner"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            Width = Length.Percent(100),
                            Height = Length.Percent(100),
                            FontSize = Length.Px(14),
                            LineHeight = Length.Number(1),
                        }
                    },
                }
            }
        };

        return _layoutEngine.Layout(root, styleSheets, 800, 600);
    }

    private static LayoutBox FindByClass(LayoutBox box, string className)
    {
        if (box.Element.Class == className) return box;
        foreach (var child in box.Children)
        {
            var found = FindByClass(child, className);
            if (found != null) return found;
        }
        return null!;
    }

    [Fact]
    public void InlineBlock_MinWidthMinHeight_ShouldBeApplied()
    {
        var layoutRoot = LayoutIssueTree();
        var button = FindByClass(layoutRoot, "button");

        // .button 是 inline-block，宽高均为 auto，应被 min-width/min-height 撑到 40×40。
        button.BoxModel.Content.Width.ShouldBe(40f);
        button.BoxModel.Content.Height.ShouldBe(40f);
    }

    [Fact]
    public void InlineBlock_MinWidth_ShouldWidenNarrowContent()
    {
        var layoutRoot = LayoutIssueTree();
        var button = FindByClass(layoutRoot, "button");

        // "AU" 文本宽度远小于 40px，min-width 应把宽度抬到 40px。
        button.BoxModel.Content.Width.ShouldBe(40f);
    }

    [Fact]
    public void InlineBlock_MinSize_ShouldPropagateToPercentChildren()
    {
        var layoutRoot = LayoutIssueTree();
        var button = FindByClass(layoutRoot, "button");
        var native = FindByClass(layoutRoot, "button-native");
        var inner = FindByClass(layoutRoot, "button-inner");

        // ISSUE-081 理论值（与 Demo 一致，设置了 line-height: 1）：
        // .button        40 × 40  (auto，被 min-width/min-height 撑起)
        // .button-native 40 × 36  (width:100% 解析为父确定宽度 40；height:100% 不确定退化为内容，
        //                          被自身 min-height:36 撑起)
        // .button-inner  40 × 14  (width:100% 解析为 40；height:100% 退化为一行文本高度，
        //                          line-height:1 使其精确等于 font-size 14px)

        button.BoxModel.Content.Width.ShouldBe(40f);
        button.BoxModel.Content.Height.ShouldBe(40f);

        native.BoxModel.Content.Width.ShouldBe(40f, "button-native width:100% should resolve to parent's 40px");
        native.BoxModel.Content.Height.ShouldBe(36f, "button-native height should be min-height 36px");

        inner.BoxModel.Content.Width.ShouldBe(40f, "button-inner width:100% should resolve to native's 40px");
        // height:100% 退化为一行文本高度，line-height:1 × font-size:14px = 14px
        inner.BoxModel.Content.Height.ShouldBe(14f, "button-inner height:100% degrades to line-height (1 × 14px)");
    }
}
