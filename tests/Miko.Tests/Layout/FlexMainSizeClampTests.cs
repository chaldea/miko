using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-102：flex 项目在主轴方向的 min/max-width（列方向为 min/max-height）约束
/// 必须参与 flex 布局——假设主尺寸（flex-basis）先被夹取，grow/shrink 分配时命中
/// 约束的项目冻结在夹取值，剩余空间在未冻结项目间重分配；全部冻结后仍有的剩余
/// 空间交由 justify-content 分布（与浏览器一致）。
/// 修复前：grow 分配完全忽略 max-width，flex: 1 1 0 + max-width: 168 的项目被
/// 均分到 266.67（内容 242.67），且强制覆写宽度使子元素自身的 max-width 夹取失效。
/// </summary>
public class FlexMainSizeClampTests
{
    private readonly LayoutEngine _layoutEngine = new();

    /// <summary>复刻 ISSUE-102 的 DOM 与样式（DebugDemo 场景：3 个 flex:1 1 0 + max-width:168 的项目）。</summary>
    private static (DivElement root, List<StyleSheet> sheets) BuildIssueScene()
    {
        var root = new DivElement { Class = "root" };
        var container = new DivElement { Class = "container" };
        root.AddChild(container);
        for (int i = 0; i < 3; i++)
            container.AddChild(new DivElement { Class = "item" });

        var sheet = new StyleSheet();
        sheet.Add(new CssObject()
        {
            ["*"] = new()
            {
                BoxSizing = BoxSizing.BorderBox,
            },
            [".root"] = new()
            {
                Width = Length.Px(800),
                Height = Length.Px(800),
            },
            [".container"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
                Height = Length.Px(56),
            },
            [".item"] = new()
            {
                FlexGrow = 1,
                FlexShrink = 1,
                FlexBasis = Length.Px(0),
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Height = Length.Percent(100),
                MaxWidth = Length.Px(168),
                PaddingLeft = Length.Px(12),
                PaddingRight = Length.Px(12),
            }
        });
        return (root, new List<StyleSheet> { sheet });
    }

    [Fact]
    public void Should_ClampGrownItemsToMaxWidth()
    {
        var (root, sheets) = BuildIssueScene();

        var rootBox = _layoutEngine.Layout(root, sheets, 800, 800);

        var containerBox = rootBox.Children[0];
        containerBox.BoxModel.Content.Width.ShouldBe(800f, 0.01f);
        containerBox.Children.Count.ShouldBe(3);

        foreach (var item in containerBox.Children)
        {
            // border-box：max-width:168 约束边框盒 → margin-box 宽 168、内容宽 144（减去 padding 24）。
            item.BoxModel.MarginBox.Width.ShouldBe(168f, 0.01f);
            item.BoxModel.Content.Width.ShouldBe(144f, 0.01f);
        }
    }

    [Fact]
    public void Should_DistributeLeftoverAfterClamp_ByJustifyContent()
    {
        var (root, sheets) = BuildIssueScene();

        var rootBox = _layoutEngine.Layout(root, sheets, 800, 800);
        var containerBox = rootBox.Children[0];

        // 3 项均被夹取在 168（共 504），剩余 296 由 justify-content: center 分布 → 首项偏移 148。
        containerBox.Children[0].BoxModel.MarginBox.Left.ShouldBe(148f, 0.01f);
        containerBox.Children[1].BoxModel.MarginBox.Left.ShouldBe(316f, 0.01f);
        containerBox.Children[2].BoxModel.MarginBox.Left.ShouldBe(484f, 0.01f);
    }

    [Fact]
    public void Should_RedistributeClampedSpaceToUnclampedItems()
    {
        // 容器 600：两项均 flex: 1 1 0，A 有 max-width: 100。
        // 均分 300/300 时 A 命中上限冻结在 100，释放出的 200 应继续分给 B → B = 500。
        var container = new DivElement
        {
            Style = new Style { Display = Display.Flex, Width = Length.Px(600), Height = Length.Px(50) }
        };
        container.AddChild(new DivElement
        {
            Style = new Style
            {
                FlexGrow = 1, FlexShrink = 1, FlexBasis = Length.Px(0),
                MaxWidth = Length.Px(100), Height = Length.Px(20)
            }
        });
        container.AddChild(new DivElement
        {
            Style = new Style { FlexGrow = 1, FlexShrink = 1, FlexBasis = Length.Px(0), Height = Length.Px(20) }
        });

        var root = _layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        root.Children[0].BoxModel.MarginBox.Width.ShouldBe(100f, 0.01f);
        root.Children[1].BoxModel.MarginBox.Width.ShouldBe(500f, 0.01f);
    }

    [Fact]
    public void Should_RaiseGrownItemToMinWidth()
    {
        // 容器 600：两项均 flex: 1 1 0，A 有 min-width: 500。
        // 假设主尺寸 A 被抬升到 500 → 剩余 100 由两项均分 → A = 550、B = 50
        // （min-width 只在目标尺寸低于它时夹取，550 不再触发冻结）。
        var container = new DivElement
        {
            Style = new Style { Display = Display.Flex, Width = Length.Px(600), Height = Length.Px(50) }
        };
        container.AddChild(new DivElement
        {
            Style = new Style
            {
                FlexGrow = 1, FlexShrink = 1, FlexBasis = Length.Px(0),
                MinWidth = Length.Px(500), Height = Length.Px(20)
            }
        });
        container.AddChild(new DivElement
        {
            Style = new Style { FlexGrow = 1, FlexShrink = 1, FlexBasis = Length.Px(0), Height = Length.Px(20) }
        });

        var root = _layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        root.Children[0].BoxModel.MarginBox.Width.ShouldBe(550f, 0.01f);
        root.Children[1].BoxModel.MarginBox.Width.ShouldBe(50f, 0.01f);
    }

    [Fact]
    public void Should_ClampShrunkItemsToMinWidth()
    {
        // 容器 100：两项 width: 200、flex-shrink: 1、min-width: 80。
        // 溢出 300，等权收缩各 -150 → 目标 50 命中下限，均冻结在 80。
        var container = new DivElement
        {
            Style = new Style { Display = Display.Flex, Width = Length.Px(100), Height = Length.Px(50) }
        };
        for (int i = 0; i < 2; i++)
        {
            container.AddChild(new DivElement
            {
                Style = new Style { Width = Length.Px(200), MinWidth = Length.Px(80), Height = Length.Px(20) }
            });
        }

        var root = _layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        root.Children[0].BoxModel.MarginBox.Width.ShouldBe(80f, 0.01f);
        root.Children[1].BoxModel.MarginBox.Width.ShouldBe(80f, 0.01f);
    }

    [Fact]
    public void Should_ClampExplicitWidthToMaxWidth_WithoutGrow()
    {
        // 无 grow/shrink 调整时，flex 项目的假设主尺寸（显式 width 作为 flex-basis）
        // 也应被 max-width 夹取（CSS Flexbox §9.7 步骤 3）。
        var container = new DivElement
        {
            Style = new Style { Display = Display.Flex, Width = Length.Px(600), Height = Length.Px(50) }
        };
        container.AddChild(new DivElement
        {
            Style = new Style { Width = Length.Px(300), MaxWidth = Length.Px(100), Height = Length.Px(20) }
        });

        var root = _layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        root.Children[0].BoxModel.MarginBox.Width.ShouldBe(100f, 0.01f);
        root.Children[0].BoxModel.Content.Width.ShouldBe(100f, 0.01f);
    }

    [Fact]
    public void Should_ClampGrownItemsToMaxHeight_InColumnDirection()
    {
        // 列方向与行方向对称：max-height 约束 grow 分配。
        var container = new DivElement
        {
            Style = new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Width = Length.Px(200),
                Height = Length.Px(600)
            }
        };
        for (int i = 0; i < 2; i++)
        {
            container.AddChild(new DivElement
            {
                Style = new Style
                {
                    FlexGrow = 1, FlexShrink = 1, FlexBasis = Length.Px(0),
                    MaxHeight = Length.Px(100), Width = Length.Px(50)
                }
            });
        }

        var root = _layoutEngine.Layout(container, new List<StyleSheet>(), 800, 600);

        root.Children[0].BoxModel.MarginBox.Height.ShouldBe(100f, 0.01f);
        root.Children[1].BoxModel.MarginBox.Height.ShouldBe(100f, 0.01f);
    }
}
