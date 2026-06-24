using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// Flex 布局测试：换行 (FlexWrap.Wrap) 与间距 (Gap) 的正确性。
/// </summary>
public class FlexLayoutTests
{
    private readonly LayoutEngine _layoutEngine = new();

    [Fact]
    public void FlexRow_NoWrap_AllItemsOnOneLine()
    {
        // 容器 500px 宽，3 个子元素各 100px → 不换行，单行布局。
        var container = new DivElement();
        for (int i = 0; i < 3; i++) container.AddChild(new DivElement());

        var styleSheets = new List<StyleSheet>
        {
            new()
            {
                Rules = new List<StyleRule>
                {
                    new() { Selector = new TagSelector("div"), Style = new Style { Display = Display.Flex } },
                    new()
                    {
                        Selector = new DescendantSelector(new TagSelector("div"), new TagSelector("div")),
                        Style = new Style { Width = Length.Px(100), Height = Length.Px(60) }
                    }
                }
            }
        };

        var root = _layoutEngine.Layout(container, styleSheets, 500, 600);

        // 3 个子元素全在一行，容器高度 = 行高 60。
        root.Children.Count.ShouldBe(3);
        root.BoxModel.Content.Height.ShouldBe(60);
        root.Children[0].BoxModel.Content.X.ShouldBe(0);
        root.Children[1].BoxModel.Content.X.ShouldBe(100);
        root.Children[2].BoxModel.Content.X.ShouldBe(200);
    }

    [Fact]
    public void FlexRow_Wrap_BreaksIntoMultipleLines()
    {
        // 容器 500px 宽，12 个子元素各 100px + FlexWrap.Wrap → 每行 5 个 (5×100=500)，共 3 行。
        var container = new DivElement();
        for (int i = 0; i < 12; i++) container.AddChild(new DivElement());

        var styleSheets = new List<StyleSheet>
        {
            new()
            {
                Rules = new List<StyleRule>
                {
                    new()
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style { Display = Display.Flex, FlexWrap = FlexWrap.Wrap }
                    },
                    new()
                    {
                        Selector = new DescendantSelector(new TagSelector("div"), new TagSelector("div")),
                        Style = new Style { Width = Length.Px(100), Height = Length.Px(60) }
                    }
                }
            }
        };

        var root = _layoutEngine.Layout(container, styleSheets, 500, 600);

        root.Children.Count.ShouldBe(12);

        // 第 1 行：item 0..4 (X: 0, 100, 200, 300, 400; Y: 0)
        for (int i = 0; i < 5; i++)
        {
            root.Children[i].BoxModel.Content.X.ShouldBe(i * 100);
            root.Children[i].BoxModel.Content.Y.ShouldBe(0);
        }

        // 第 2 行：item 5..9 (X: 0..400; Y: 60)
        for (int i = 5; i < 10; i++)
        {
            root.Children[i].BoxModel.Content.X.ShouldBe((i - 5) * 100);
            root.Children[i].BoxModel.Content.Y.ShouldBe(60);
        }

        // 第 3 行：item 10..11 (X: 0, 100; Y: 120)
        root.Children[10].BoxModel.Content.X.ShouldBe(0);
        root.Children[10].BoxModel.Content.Y.ShouldBe(120);
        root.Children[11].BoxModel.Content.X.ShouldBe(100);
        root.Children[11].BoxModel.Content.Y.ShouldBe(120);

        // 容器高度 = 3 行 × 60 = 180。
        root.BoxModel.Content.Height.ShouldBe(180);
    }

    [Fact]
    public void FlexRow_WrapWithGap_AppliesSpacingBetweenItems()
    {
        // 容器 500px，12 items × 100px，FlexWrap.Wrap + Gap(10) → 每行 4 个 (4×100 + 3×10 = 430 ≤ 500)。
        var container = new DivElement();
        for (int i = 0; i < 12; i++) container.AddChild(new DivElement());

        var styleSheets = new List<StyleSheet>
        {
            new()
            {
                Rules = new List<StyleRule>
                {
                    new()
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style { Display = Display.Flex, FlexWrap = FlexWrap.Wrap, Gap = Length.Px(10) }
                    },
                    new()
                    {
                        Selector = new DescendantSelector(new TagSelector("div"), new TagSelector("div")),
                        Style = new Style { Width = Length.Px(100), Height = Length.Px(60) }
                    }
                }
            }
        };

        var root = _layoutEngine.Layout(container, styleSheets, 500, 600);

        root.Children.Count.ShouldBe(12);

        // 第 1 行：item 0..3 (X: 0, 110, 220, 330; Y: 0)
        for (int i = 0; i < 4; i++)
            root.Children[i].BoxModel.Content.X.ShouldBe(i * 110);

        // 第 2 行：item 4..7 (Y: 70 = 60 + 10 row-gap)
        for (int i = 4; i < 8; i++)
        {
            root.Children[i].BoxModel.Content.X.ShouldBe((i - 4) * 110);
            root.Children[i].BoxModel.Content.Y.ShouldBe(70);
        }

        // 第 3 行：item 8..11 (Y: 140)
        for (int i = 8; i < 12; i++)
        {
            root.Children[i].BoxModel.Content.X.ShouldBe((i - 8) * 110);
            root.Children[i].BoxModel.Content.Y.ShouldBe(140);
        }

        // 容器高度 = 3×60 + 2×10 = 200。
        root.BoxModel.Content.Height.ShouldBe(200);
    }

    [Fact]
    public void FlexRow_WrapWithGap_BorderBox_MatchesDebugDemoRepro()
    {
        // ISSUE 复现：* { box-sizing: border-box } + .container { flex; wrap; gap:10; width:500 }
        // + .item { width:100; height:60 }，12 个 item → 每行 4 个，3 行。
        var container = new DivElement { Class = "container" };
        for (int i = 0; i < 12; i++) container.AddChild(new DivElement { Class = "item" });

        var styleSheets = new List<StyleSheet>
        {
            new()
            {
                Rules = new List<StyleRule>
                {
                    new() { Selector = new UniversalSelector(), Style = new Style { BoxSizing = BoxSizing.BorderBox } },
                    new()
                    {
                        Selector = new ClassSelector("container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexWrap = FlexWrap.Wrap,
                            Gap = Length.Px(10),
                            Width = Length.Px(500),
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("item"),
                        Style = new Style { Width = Length.Px(100), Height = Length.Px(60) }
                    },
                }
            }
        };

        var root = _layoutEngine.Layout(container, styleSheets, 800, 600);

        // 每行 4 个，X: 0,110,220,330；3 行，Y: 0,70,140。
        root.Children[0].BoxModel.Content.X.ShouldBe(0);
        root.Children[3].BoxModel.Content.X.ShouldBe(330);
        root.Children[4].BoxModel.Content.Y.ShouldBe(70);
        root.Children[8].BoxModel.Content.Y.ShouldBe(140);
        root.BoxModel.Content.Height.ShouldBe(200); // 3×60 + 2×10
    }

    [Fact]
    public void FlexColumn_NoWrap_AllItemsInOneColumn()
    {
        // 列方向，3 个子元素，无换行 → 单列，高度累加。
        var container = new DivElement();
        for (int i = 0; i < 3; i++) container.AddChild(new DivElement());

        var styleSheets = new List<StyleSheet>
        {
            new()
            {
                Rules = new List<StyleRule>
                {
                    new()
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column }
                    },
                    new()
                    {
                        Selector = new DescendantSelector(new TagSelector("div"), new TagSelector("div")),
                        Style = new Style { Width = Length.Px(80), Height = Length.Px(50) }
                    }
                }
            }
        };

        var root = _layoutEngine.Layout(container, styleSheets, 500, 600);

        root.Children.Count.ShouldBe(3);
        root.Children[0].BoxModel.Content.Y.ShouldBe(0);
        root.Children[1].BoxModel.Content.Y.ShouldBe(50);
        root.Children[2].BoxModel.Content.Y.ShouldBe(100);
        root.BoxModel.Content.Height.ShouldBe(150); // 3×50
    }

    [Fact]
    public void FlexColumn_WrapWithGap_BreaksIntoMultipleColumns()
    {
        // 列方向 wrap：容器高 150，6 items × 60，gap 10 → 每列 2 个 (2×60+10=130 ≤ 150；3 个=200 >150)，共 3 列。
        var container = new DivElement();
        for (int i = 0; i < 6; i++) container.AddChild(new DivElement());

        var styleSheets = new List<StyleSheet>
        {
            new()
            {
                Rules = new List<StyleRule>
                {
                    new()
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Column,
                            FlexWrap = FlexWrap.Wrap,
                            Gap = Length.Px(10),
                            Height = Length.Px(150)
                        }
                    },
                    new()
                    {
                        Selector = new DescendantSelector(new TagSelector("div"), new TagSelector("div")),
                        Style = new Style { Width = Length.Px(80), Height = Length.Px(60) }
                    }
                }
            }
        };

        var root = _layoutEngine.Layout(container, styleSheets, 500, 150);

        root.Children.Count.ShouldBe(6);

        // 第 1 列：item 0,1 (X: 0, Y: 0, 70)
        root.Children[0].BoxModel.Content.X.ShouldBe(0);
        root.Children[0].BoxModel.Content.Y.ShouldBe(0);
        root.Children[1].BoxModel.Content.X.ShouldBe(0);
        root.Children[1].BoxModel.Content.Y.ShouldBe(70);

        // 第 2 列：item 2,3 (X: 90 = 80 + 10 column-gap, Y: 0, 70)
        root.Children[2].BoxModel.Content.X.ShouldBe(90);
        root.Children[2].BoxModel.Content.Y.ShouldBe(0);
        root.Children[3].BoxModel.Content.X.ShouldBe(90);
        root.Children[3].BoxModel.Content.Y.ShouldBe(70);

        // 第 3 列：item 4,5 (X: 180, Y: 0, 70)
        root.Children[4].BoxModel.Content.X.ShouldBe(180);
        root.Children[4].BoxModel.Content.Y.ShouldBe(0);
        root.Children[5].BoxModel.Content.X.ShouldBe(180);
        root.Children[5].BoxModel.Content.Y.ShouldBe(70);
    }

    // ISSUE-064 §1.2: a flex COLUMN whose height comes from min-height (not an explicit height)
    // must still vertically center its children (justify-content: center on the main axis) and
    // horizontally center them (align-items: center on the cross axis). This mirrors
    // ion-segment-button: a flex-column with min-height + align/justify center wrapping a small
    // label. The bug was that justify-content was skipped because the main-axis size was still 0
    // (the min-height was applied to the box only AFTER the children were placed).
    [Fact]
    public void FlexColumn_MinHeight_CentersChildVerticallyAndHorizontally()
    {
        // <button flex-col W=200 minH=40 align/justify center> <label 50x20/> </button>
        // The button's height comes from min-height (40), taller than its 20px label, so the
        // label must be vertically centered within the 40px (justify-content: center) and
        // horizontally centered within the 200px (align-items: center).
        var button = new DivElement { Class = "button" };
        var label = new DivElement { Class = "label" };
        button.AddChild(label);

        var styleSheets = new List<StyleSheet>
        {
            new()
            {
                Rules = new List<StyleRule>
                {
                    new()
                    {
                        Selector = new ClassSelector("button"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Column,
                            AlignItems = AlignItems.Center,
                            JustifyContent = JustifyContent.Center,
                            Width = Length.Px(200),
                            MinHeight = Length.Px(40),
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("label"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(50), Height = Length.Px(20) }
                    },
                }
            }
        };

        var root = _layoutEngine.Layout(button, styleSheets, 800, 600);

        var labelBox = root.Children[0];

        // The button is 40px tall (min-height floors the 20px content).
        root.BoxModel.Content.Height.ShouldBe(40);

        // Horizontal centering (align-items: center, cross axis): the 50px label is centered
        // in the 200px button → left edge at (200 - 50) / 2 = 75.
        labelBox.BoxModel.Content.X.ShouldBe(75);

        // Vertical centering (justify-content: center, main axis): the 20px label is centered
        // in the 40px button → top edge at (40 - 20) / 2 = 10.
        labelBox.BoxModel.Content.Y.ShouldBe(10);
    }

    // Same centering, but the column has an EXPLICIT height (control case). This already worked,
    // so it guards against a regression in the explicit-height path.
    [Fact]
    public void FlexColumn_ExplicitHeight_CentersChildVerticallyAndHorizontally()
    {
        var button = new DivElement { Class = "button" };
        var label = new DivElement { Class = "label" };
        button.AddChild(label);

        var styleSheets = new List<StyleSheet>
        {
            new()
            {
                Rules = new List<StyleRule>
                {
                    new()
                    {
                        Selector = new ClassSelector("button"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Column,
                            AlignItems = AlignItems.Center,
                            JustifyContent = JustifyContent.Center,
                            Width = Length.Px(200),
                            Height = Length.Px(40),
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("label"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(50), Height = Length.Px(20) }
                    },
                }
            }
        };

        var root = _layoutEngine.Layout(button, styleSheets, 800, 600);
        var labelBox = root.Children[0];

        labelBox.BoxModel.Content.X.ShouldBe(75);  // (200 - 50) / 2
        labelBox.BoxModel.Content.Y.ShouldBe(10);  // (40 - 20) / 2
    }
}
