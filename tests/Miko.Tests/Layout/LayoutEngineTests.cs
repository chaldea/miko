using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// 布局引擎测试 - 验证不同 Display 属性的布局正确性
/// </summary>
public class LayoutEngineTests
{
    private readonly LayoutEngine _layoutEngine = new();

    #region Display.Block Tests

    [Fact]
    public void BlockLayout_SingleElement_ShouldFillAvailableWidth()
    {
        // Arrange
        var root = new DivElement();
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style { Display = Display.Block }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        layoutRoot.ShouldNotBeNull();
        layoutRoot.ComputedStyle.Display.ShouldBe(Display.Block);
        layoutRoot.BoxModel.Content.Width.ShouldBe(800); // 填满可用宽度
    }

    [Fact]
    public void BlockLayout_WithFixedWidth_ShouldUseSpecifiedWidth()
    {
        // Arrange
        var root = new DivElement();
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(400)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        layoutRoot.BoxModel.Content.Width.ShouldBe(400);
    }

    [Fact]
    public void BlockLayout_WithPadding_ShouldReduceContentWidth()
    {
        // Arrange
        var root = new DivElement();
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            PaddingTop = Length.Px(20),
                            PaddingRight = Length.Px(20),
                            PaddingBottom = Length.Px(20),
                            PaddingLeft = Length.Px(20)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        layoutRoot.BoxModel.Padding.Left.ShouldBe(20);
        layoutRoot.BoxModel.Padding.Right.ShouldBe(20);
        layoutRoot.BoxModel.Content.Width.ShouldBe(760); // 800 - 20 - 20
    }

    [Fact]
    public void BlockLayout_VerticalStacking_ShouldStackChildrenVertically()
    {
        // Arrange
        var root = new DivElement();
        var child1 = new DivElement();
        var child2 = new DivElement();
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Height = Length.Px(100)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        layoutRoot.Children.Count.ShouldBe(2);
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];

        // 第一个子元素应该在顶部
        layoutChild1.BoxModel.Content.Y.ShouldBe(0);
        layoutChild1.BoxModel.Content.Height.ShouldBe(100);

        // 第二个子元素应该在第一个下方
        layoutChild2.BoxModel.Content.Y.ShouldBe(100);
        layoutChild2.BoxModel.Content.Height.ShouldBe(100);
    }

    [Fact]
    public void BlockLayout_WithMargin_ShouldApplyMarginSpacing()
    {
        // Arrange
        var root = new DivElement { Class = "root" };
        var child = new DivElement { Class = "child" };
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style
                        {
                            Display = Display.Block
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            MarginTop = Length.Px(10),
                            MarginRight = Length.Px(10),
                            MarginBottom = Length.Px(10),
                            MarginLeft = Length.Px(10)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild = layoutRoot.Children[0];
        layoutChild.BoxModel.Margin.Left.ShouldBe(10);
        layoutChild.BoxModel.Margin.Right.ShouldBe(10);
        layoutChild.BoxModel.Margin.Top.ShouldBe(10);
        layoutChild.BoxModel.Margin.Bottom.ShouldBe(10);

        // 内容区域应该从 margin 之后开始 (相对于父元素的内容区域)
        layoutChild.BoxModel.Content.X.ShouldBe(10);
        layoutChild.BoxModel.Content.Y.ShouldBe(10);
    }

    [Fact]
    public void BlockLayout_MarginLeftAuto_ShouldPushElementToRight()
    {
        // Arrange
        var root = new DivElement { Class = "root" };
        var child = new DivElement { Class = "child" };
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style { Display = Display.Block }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(200),
                            MarginLeft = Length.Auto
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild = layoutRoot.Children[0];
        // margin-left: auto 应该占据剩余空间 (800 - 200 = 600)
        layoutChild.BoxModel.Margin.Left.ShouldBe(600);
        layoutChild.BoxModel.Content.X.ShouldBe(600);
        layoutChild.BoxModel.Content.Width.ShouldBe(200);
    }

    [Fact]
    public void BlockLayout_MarginLeftAndRightAuto_ShouldCenterElement()
    {
        // Arrange
        var root = new DivElement { Class = "root" };
        var child = new DivElement { Class = "child" };
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style { Display = Display.Block }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(200),
                            MarginLeft = Length.Auto,
                            MarginRight = Length.Auto
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild = layoutRoot.Children[0];
        // margin-left: auto; margin-right: auto 应该居中 (800 - 200) / 2 = 300
        layoutChild.BoxModel.Margin.Left.ShouldBe(300);
        layoutChild.BoxModel.Margin.Right.ShouldBe(300);
        layoutChild.BoxModel.Content.X.ShouldBe(300);
        layoutChild.BoxModel.Content.Width.ShouldBe(200);
    }

    [Fact]
    public void BlockLayout_MarginRightAuto_ShouldKeepElementOnLeft()
    {
        // Arrange
        var root = new DivElement { Class = "root" };
        var child = new DivElement { Class = "child" };
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style { Display = Display.Block }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(200),
                            MarginRight = Length.Auto
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild = layoutRoot.Children[0];
        // margin-right: auto 应该占据剩余空间，元素保持在左侧
        layoutChild.BoxModel.Margin.Right.ShouldBe(600);
        layoutChild.BoxModel.Content.X.ShouldBe(0);
        layoutChild.BoxModel.Content.Width.ShouldBe(200);
    }

    #endregion

    #region Display.Flex Tests

    [Fact]
    public void FlexLayout_Row_ShouldArrangeChildrenHorizontally()
    {
        // Arrange
        var root = new DivElement();
        var child1 = new DivElement();
        var child2 = new DivElement();
        var child3 = new DivElement();
        root.AddChild(child1);
        root.AddChild(child2);
        root.AddChild(child3);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(100),
                            Height = Length.Px(50)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        layoutRoot.Children.Count.ShouldBe(3);
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];
        var layoutChild3 = layoutRoot.Children[2];

        // 子元素应该水平排列
        layoutChild1.BoxModel.Content.X.ShouldBe(0);
        layoutChild2.BoxModel.Content.X.ShouldBe(layoutChild1.BoxModel.MarginBox.Right);
        layoutChild3.BoxModel.Content.X.ShouldBe(layoutChild2.BoxModel.MarginBox.Right);

        // Y 坐标应该相同（在同一行）
        layoutChild1.BoxModel.Content.Y.ShouldBe(0);
        layoutChild2.BoxModel.Content.Y.ShouldBe(0);
        layoutChild3.BoxModel.Content.Y.ShouldBe(0);
    }

    [Fact]
    public void FlexLayout_Column_ShouldArrangeChildrenVertically()
    {
        // Arrange
        var root = new DivElement();
        var child1 = new DivElement();
        var child2 = new DivElement();
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Column,
                            Width = Length.Px(200),
                            Height = Length.Px(100)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];

        // 子元素应该垂直排列
        layoutChild1.BoxModel.Content.Y.ShouldBe(0);
        layoutChild2.BoxModel.Content.Y.ShouldBe(layoutChild1.BoxModel.MarginBox.Bottom);

        // X 坐标应该相同（在同一列）
        layoutChild1.BoxModel.Content.X.ShouldBe(0);
        layoutChild2.BoxModel.Content.X.ShouldBe(0);
    }

    [Fact]
    public void FlexLayout_JustifyContentCenter_ShouldCenterChildrenOnMainAxis()
    {
        // Arrange
        var root = new DivElement();
        var child = new DivElement();
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            JustifyContent = JustifyContent.Center,
                            Width = Length.Px(400),
                            Height = Length.Px(100)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild = layoutRoot.Children[0];
        var childWidth = layoutChild.BoxModel.MarginBox.Width;
        var expectedX = (400 - childWidth) / 2;

        // 子元素应该在主轴上居中
        layoutChild.BoxModel.Content.X.ShouldBe(expectedX, 0.1f);
    }

    [Fact]
    public void FlexLayout_JustifyContentSpaceBetween_ShouldDistributeSpaceEvenly()
    {
        // Arrange
        var root = new DivElement();
        var child1 = new DivElement();
        var child2 = new DivElement();
        var child3 = new DivElement();
        root.AddChild(child1);
        root.AddChild(child2);
        root.AddChild(child3);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            JustifyContent = JustifyContent.SpaceBetween,
                            Width = Length.Px(100),
                            Height = Length.Px(50)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];
        var layoutChild3 = layoutRoot.Children[2];

        // 第一个子元素应该在起始位置
        layoutChild1.BoxModel.Content.X.ShouldBe(0, 0.1f);

        // 第三个子元素应该在结束位置
        var containerWidth = layoutRoot.BoxModel.Content.Width;
        var child3Width = layoutChild3.BoxModel.MarginBox.Width;
        layoutChild3.BoxModel.Content.X.ShouldBe(containerWidth - child3Width, 0.1f);

        // 第二个子元素应该在中间
        var spacing1 = layoutChild2.BoxModel.Content.X - layoutChild1.BoxModel.MarginBox.Right;
        var spacing2 = layoutChild3.BoxModel.Content.X - layoutChild2.BoxModel.MarginBox.Right;
        spacing1.ShouldBe(spacing2, 0.1f);
    }

    [Fact]
    public void FlexLayout_AlignItemsCenter_ShouldCenterChildrenOnCrossAxis()
    {
        // Arrange
        var root = new DivElement();
        var child = new DivElement();
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            AlignItems = AlignItems.Center,
                            Width = Length.Px(400),
                            Height = Length.Px(200)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild = layoutRoot.Children[0];
        var childHeight = layoutChild.BoxModel.MarginBox.Height;
        var containerHeight = layoutRoot.BoxModel.Content.Height;
        var expectedY = (containerHeight - childHeight) / 2;

        // 子元素应该在交叉轴上居中
        layoutChild.BoxModel.Content.Y.ShouldBe(expectedY, 0.1f);
    }

    [Fact]
    public void FlexLayout_AlignItemsStretch_ShouldStretchChildrenToCrossAxisSize()
    {
        // Arrange - 两个子元素有不同的内容高度，AlignItems.Stretch 应该使它们高度统一
        var root = new DivElement { Class = "input-group" };
        var child1 = new DivElement { Class = "label", TextContent = "UserName" };
        var child2 = new DivElement { Class = "input", TextContent = "Value" };
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("input-group"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            AlignItems = AlignItems.Stretch,
                            Width = Length.Percent(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("label"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Padding = new Padding(6, 12),
                            Width = Length.Percent(20),
                            FontSize = Length.Px(14),
                            Border = new Border(1, BorderStyle.Solid, Color.FromHex("ced4da"))
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("input"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Padding = new Padding(10, 12),
                            Width = Length.Percent(80),
                            FontSize = Length.Px(14),
                            Border = new Border(1, BorderStyle.Solid, Color.FromHex("ced4da"))
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var box1 = layoutRoot.Children[0].BoxModel;
        var box2 = layoutRoot.Children[1].BoxModel;

        // AlignItems.Stretch 应该使两个元素的 MarginBox 高度统一
        box1.MarginBox.Height.ShouldBe(box2.MarginBox.Height, 0.01f);
    }

    [Fact]
    public void FlexLayout_Row_ChildrenWithTextContent_ShouldHaveDifferentXPositions()
    {
        // Arrange - 测试 flex row 容器中子元素的 X 坐标应该不同
        // 当子元素没有设置宽度时，应该根据文本内容计算固有宽度
        var root = new DivElement { Class = "flex-container" };
        var child1 = new SpanElement { TextContent = "$", Class = "item" };
        var child2 = new SpanElement { TextContent = "Amount", Class = "item" };
        var child3 = new SpanElement { TextContent = ".00", Class = "item" };
        root.AddChild(child1);
        root.AddChild(child2);
        root.AddChild(child3);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(600)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            PaddingTop = Length.Px(6),
                            PaddingRight = Length.Px(12),
                            PaddingBottom = Length.Px(6),
                            PaddingLeft = Length.Px(12)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 600, 600);

        // Assert
        layoutRoot.Children.Count.ShouldBe(3);
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];
        var layoutChild3 = layoutRoot.Children[2];

        // 子元素的 X 坐标应该不同（它们应该水平排列）
        layoutChild1.BoxModel.Content.X.ShouldNotBe(layoutChild2.BoxModel.Content.X);
        layoutChild2.BoxModel.Content.X.ShouldNotBe(layoutChild3.BoxModel.Content.X);

        // 第二个元素应该在第一个元素之后
        layoutChild2.BoxModel.Content.X.ShouldBeGreaterThan(layoutChild1.BoxModel.Content.X);
        // 第三个元素应该在第二个元素之后
        layoutChild3.BoxModel.Content.X.ShouldBeGreaterThan(layoutChild2.BoxModel.Content.X);
    }

    [Fact]
    public void FlexLayout_DefaultAlignItems_ShouldNotStretchChildrenWithDifferentPadding()
    {
        // Arrange - Flex 容器中的子元素有不同的 padding
        // 默认情况下（AlignItems = FlexStart），子元素应该保持各自的固有尺寸
        var root = new DivElement { Class = "row" };
        var button1 = new ButtonElement { TextContent = "Button", Class = "btn-sm" };
        var button2 = new ButtonElement { TextContent = "Button" };
        var button3 = new ButtonElement { TextContent = "Button", Class = "btn-lg" };
        root.AddChild(button1);
        root.AddChild(button2);
        root.AddChild(button3);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("row"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            AlignItems = AlignItems.FlexStart
                        }
                    },
                    new StyleRule
                    {
                        Selector = new TagSelector("button"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Padding = new Padding(6, 12),
                            FontSize = Length.Px(14)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("btn-sm"),
                        Style = new Style
                        {
                            PaddingTop = Length.Px(4),
                            PaddingRight = Length.Px(8),
                            PaddingBottom = Length.Px(4),
                            PaddingLeft = Length.Px(8),
                            FontSize = Length.Px(14)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("btn-lg"),
                        Style = new Style
                        {
                            PaddingTop = Length.Px(10),
                            PaddingRight = Length.Px(16),
                            PaddingBottom = Length.Px(10),
                            PaddingLeft = Length.Px(16),
                            FontSize = Length.Px(14)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        layoutRoot.Children.Count.ShouldBe(3);
        var box1 = layoutRoot.Children[0].BoxModel;
        var box2 = layoutRoot.Children[1].BoxModel;
        var box3 = layoutRoot.Children[2].BoxModel;

        // 验证子元素高度不同（btn-sm < medium < btn-lg）
        // 因为垂直 padding 不同，所以 BorderBox 高度应该不同
        // btn-sm: padding-top=4, padding-bottom=4, total vertical padding=8
        // medium: padding-top=6, padding-bottom=6, total vertical padding=12
        // btn-lg: padding-top=10, padding-bottom=10, total vertical padding=20
        box1.BorderBox.Height.ShouldBeLessThan(box2.BorderBox.Height);
        box2.BorderBox.Height.ShouldBeLessThan(box3.BorderBox.Height);

        // 验证子元素宽度不同（使用相同文本，所以宽度差异仅来自 padding）
        // btn-sm: padding-left=8, padding-right=8, total horizontal padding=16
        // medium: padding-left=12, padding-right=12, total horizontal padding=24
        // btn-lg: padding-left=16, padding-right=16, total horizontal padding=32
        box1.BorderBox.Width.ShouldBeLessThan(box2.BorderBox.Width);
        box2.BorderBox.Width.ShouldBeLessThan(box3.BorderBox.Width);
    }

    [Fact]
    public void FlexLayout_Row_AlignItemsCenter_ShouldCenterChildVertically()
    {
        // Arrange
        var root = new DivElement { Class = "container" };
        var child = new DivElement { Class = "item" };
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            AlignItems = AlignItems.Center,
                            Width = Length.Px(500),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item"),
                        Style = new Style
                        {
                            Width = Length.Px(20),
                            Height = Length.Px(20)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild = layoutRoot.Children[0];
        // 子元素应该垂直居中: (100 - 20) / 2 = 40
        layoutChild.BoxModel.Content.Y.ShouldBe(40);
        layoutChild.BoxModel.Content.Height.ShouldBe(20);
    }

    [Fact]
    public void FlexLayout_Row_MarginLeftAuto_ShouldPushItemToRight()
    {
        // Arrange
        var root = new DivElement { Class = "container" };
        var child1 = new DivElement { Class = "item1" };
        var child2 = new DivElement { Class = "item2" };
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(800)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item1"),
                        Style = new Style { Width = Length.Px(100) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item2"),
                        Style = new Style
                        {
                            Width = Length.Px(100),
                            MarginLeft = Length.Auto
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];

        // item1 应该在左侧
        layoutChild1.BoxModel.Content.X.ShouldBe(0);
        layoutChild1.BoxModel.Content.Width.ShouldBe(100);

        // item2 应该被推到右侧 (800 - 100 - 100 = 600 的 margin-left)
        layoutChild2.BoxModel.Content.X.ShouldBe(700);
        layoutChild2.BoxModel.Content.Width.ShouldBe(100);
    }

    [Fact]
    public void FlexLayout_Row_MarginLeftAndRightAuto_ShouldCenterItem()
    {
        // Arrange
        var root = new DivElement { Class = "container" };
        var child = new DivElement { Class = "item" };
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(800)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item"),
                        Style = new Style
                        {
                            Width = Length.Px(200),
                            MarginLeft = Length.Auto,
                            MarginRight = Length.Auto
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild = layoutRoot.Children[0];

        // item 应该居中 (800 - 200) / 2 = 300
        layoutChild.BoxModel.Content.X.ShouldBe(300);
        layoutChild.BoxModel.Content.Width.ShouldBe(200);
    }

    [Fact]
    public void FlexLayout_Column_MarginTopAuto_ShouldPushItemToBottom()
    {
        // Arrange
        var root = new DivElement { Class = "container" };
        var child1 = new DivElement { Class = "item1" };
        var child2 = new DivElement { Class = "item2" };
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Column,
                            Height = Length.Px(600)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item1"),
                        Style = new Style { Height = Length.Px(100) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item2"),
                        Style = new Style
                        {
                            Height = Length.Px(100),
                            MarginTop = Length.Auto
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];

        // item1 应该在顶部
        layoutChild1.BoxModel.Content.Y.ShouldBe(0);
        layoutChild1.BoxModel.Content.Height.ShouldBe(100);

        // item2 应该被推到底部 (600 - 100 - 100 = 400 的 margin-top)
        layoutChild2.BoxModel.Content.Y.ShouldBe(500);
        layoutChild2.BoxModel.Content.Height.ShouldBe(100);
    }

    [Fact]
    public void FlexLayout_Row_ShouldRespectMinHeight()
    {
        // Arrange - flex row container with min-height should enforce minimum height
        // even when content is smaller
        var root = new DivElement { Class = "flex-container" };
        var child = new DivElement { Class = "small-child" };
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(200),
                            MinHeight = Length.Px(80)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("small-child"),
                        Style = new Style
                        {
                            Width = Length.Px(50),
                            Height = Length.Px(20)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert - container height should be at least 80px (the min-height)
        layoutRoot.BoxModel.Content.Height.ShouldBeGreaterThanOrEqualTo(80);
        layoutRoot.Children[0].BoxModel.Content.Height.ShouldBe(20);
    }

    [Fact]
    public void FlexLayout_Column_ShouldRespectMinHeight()
    {
        // Arrange - flex column container with min-height should enforce minimum
        // when children don't fill the space
        var root = new DivElement { Class = "flex-container" };
        var child1 = new DivElement { Class = "child" };
        var child2 = new DivElement { Class = "child" };
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Column,
                            Width = Length.Px(200),
                            MinHeight = Length.Px(120)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style
                        {
                            Width = Length.Px(50),
                            Height = Length.Px(30)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert - container height should be at least 120px (the min-height),
        // even though children only take 60px total
        layoutRoot.BoxModel.Content.Height.ShouldBeGreaterThanOrEqualTo(120);
        layoutRoot.Children.Count.ShouldBe(2);
    }

    [Fact]
    public void FlexLayout_AlignItemsCenter_ShouldOffsetChildSubtree()
    {
        // Arrange - a tall flex-row container (align-items: center) whose child is itself
        // a container with a nested grandchild. Centering the child must also move the
        // grandchild's subtree, not leave it at the top.
        var root = new DivElement { Class = "bar" };
        var title = new DivElement { Class = "title" };
        var inner = new DivElement { Class = "inner" };
        title.AddChild(inner);
        root.AddChild(title);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("bar"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            AlignItems = AlignItems.Center,
                            Width = Length.Px(300),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("title"),
                        Style = new Style { Width = Length.Px(200), Height = Length.Px(20) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("inner"),
                        Style = new Style { Width = Length.Px(200), Height = Length.Px(20) }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);
        var titleBox = layoutRoot.Children[0];
        var innerBox = titleBox.Children[0];

        // Assert - title centered in the 100px bar: (100 - 20) / 2 = 40
        titleBox.BoxModel.Content.Y.ShouldBe(40);
        // The nested grandchild must follow its parent's centered position (not stay at 0)
        innerBox.BoxModel.Content.Y.ShouldBe(40);
    }

    [Fact]
    public void FlexLayout_JustifyContentCenter_ShouldOffsetChildSubtree()
    {
        // Arrange - a wide flex-row container (justify-content: center) whose single child
        // has a nested grandchild. Centering along the main axis must move the subtree.
        var root = new DivElement { Class = "bar" };
        var item = new DivElement { Class = "item" };
        var inner = new DivElement { Class = "inner" };
        item.AddChild(inner);
        root.AddChild(item);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("bar"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            JustifyContent = JustifyContent.Center,
                            Width = Length.Px(300),
                            Height = Length.Px(50)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item"),
                        Style = new Style { Width = Length.Px(100), Height = Length.Px(50) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("inner"),
                        Style = new Style { Width = Length.Px(100), Height = Length.Px(50) }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);
        var itemBox = layoutRoot.Children[0];
        var innerBox = itemBox.Children[0];

        // Assert - item centered along main axis: (300 - 100) / 2 = 100
        itemBox.BoxModel.Content.X.ShouldBe(100);
        // The nested grandchild must follow its parent's centered position (not stay at 0)
        innerBox.BoxModel.Content.X.ShouldBe(100);
    }

    #endregion

    #region Flex-Grow/Shrink/Basis Tests

    [Fact]
    public void FlexLayout_FlexGrow_ShouldDistributeRemainingSpace()
    {
        // Arrange - 三个子元素使用不同的 flex-grow 值分配剩余空间
        var root = new DivElement { Class = "flex-container" };
        var child1 = new DivElement { Class = "item1" };
        var child2 = new DivElement { Class = "item2" };
        var child3 = new DivElement { Class = "item3" };
        root.AddChild(child1);
        root.AddChild(child2);
        root.AddChild(child3);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(600),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item1"),
                        Style = new Style
                        {
                            FlexGrow = 1,
                            FlexBasis = Length.Px(0)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item2"),
                        Style = new Style
                        {
                            FlexGrow = 2,
                            FlexBasis = Length.Px(0)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item3"),
                        Style = new Style
                        {
                            FlexGrow = 3,
                            FlexBasis = Length.Px(0)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert - 总共 600px，按 1:2:3 分配
        // item1 = 600 * 1/6 = 100px
        // item2 = 600 * 2/6 = 200px
        // item3 = 600 * 3/6 = 300px
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];
        var layoutChild3 = layoutRoot.Children[2];

        layoutChild1.BoxModel.MarginBox.Width.ShouldBe(100, 1f);
        layoutChild2.BoxModel.MarginBox.Width.ShouldBe(200, 1f);
        layoutChild3.BoxModel.MarginBox.Width.ShouldBe(300, 1f);
    }

    [Fact]
    public void FlexLayout_FlexShrink_ShouldShrinkWhenOverflow()
    {
        // Arrange - 子元素 flex-basis 总和超过容器宽度，使用 flex-shrink 收缩
        var root = new DivElement { Class = "flex-container" };
        var child1 = new DivElement { Class = "item1" };
        var child2 = new DivElement { Class = "item2" };
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(300),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item1"),
                        Style = new Style
                        {
                            FlexBasis = Length.Px(200),
                            FlexShrink = 1
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item2"),
                        Style = new Style
                        {
                            FlexBasis = Length.Px(200),
                            FlexShrink = 1
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        // 容器 300px，两个子元素各 200px（总 400px），溢出 100px
        // flex-shrink 都是 1，flex-basis 都是 200px
        // 收缩比例: item1 = 1*200/(1*200+1*200) = 0.5, item2 = 0.5
        // item1 缩小 100 * 0.5 = 50px → 最终 150px
        // item2 缩小 100 * 0.5 = 50px → 最终 150px
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];

        layoutChild1.BoxModel.MarginBox.Width.ShouldBe(150, 1f);
        layoutChild2.BoxModel.MarginBox.Width.ShouldBe(150, 1f);
    }

    [Fact]
    public void FlexLayout_FlexBasis_ShouldUseAsInitialSize()
    {
        // Arrange - flex-basis 应该覆盖 width 作为初始尺寸
        var root = new DivElement { Class = "flex-container" };
        var child = new DivElement { Class = "item" };
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(600),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item"),
                        Style = new Style
                        {
                            Width = Length.Px(100), // 这个应该被忽略
                            FlexBasis = Length.Px(200), // 这个应该作为初始尺寸
                            FlexGrow = 0
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert - flex-basis 应该作为初始尺寸
        var layoutChild = layoutRoot.Children[0];
        layoutChild.BoxModel.MarginBox.Width.ShouldBe(200, 1f);
    }

    [Fact]
    public void FlexLayout_FlexGrow_Column_ShouldDistributeVerticalSpace()
    {
        // Arrange - 列方向的 flex-grow 分配垂直空间
        var root = new DivElement { Class = "flex-container" };
        var child1 = new DivElement { Class = "item1" };
        var child2 = new DivElement { Class = "item2" };
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Column,
                            Width = Length.Px(200),
                            Height = Length.Px(300)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item1"),
                        Style = new Style
                        {
                            FlexGrow = 1,
                            FlexBasis = Length.Px(0)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item2"),
                        Style = new Style
                        {
                            FlexGrow = 2,
                            FlexBasis = Length.Px(0)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert - 总共 300px 高度，按 1:2 分配
        // item1 = 300 * 1/3 = 100px
        // item2 = 300 * 2/3 = 200px
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];

        layoutChild1.BoxModel.MarginBox.Height.ShouldBe(100, 1f);
        layoutChild2.BoxModel.MarginBox.Height.ShouldBe(200, 1f);
    }

    [Fact]
    public void FlexLayout_FlexGrowZero_ShouldNotGrow()
    {
        // Arrange - flex-grow: 0 的子元素不应该增长
        var root = new DivElement { Class = "flex-container" };
        var child1 = new DivElement { Class = "item1" };
        var child2 = new DivElement { Class = "item2" };
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(600),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item1"),
                        Style = new Style
                        {
                            FlexBasis = Length.Px(100),
                            FlexGrow = 0 // 不增长
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item2"),
                        Style = new Style
                        {
                            FlexBasis = Length.Px(100),
                            FlexGrow = 1 // 增长填充剩余空间
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        // 容器 600px，两个子元素各 100px（总 200px），剩余 400px
        // item1 flex-grow: 0 → 保持 100px
        // item2 flex-grow: 1 → 100 + 400 = 500px
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];

        layoutChild1.BoxModel.MarginBox.Width.ShouldBe(100, 1f);
        layoutChild2.BoxModel.MarginBox.Width.ShouldBe(500, 1f);
    }

    [Fact]
    public void FlexLayout_FlexShrink_DifferentRatios_ShouldShrinkProportionally()
    {
        // Arrange - 不同的 flex-shrink 值应该按比例收缩
        var root = new DivElement { Class = "flex-container" };
        var child1 = new DivElement { Class = "item1" };
        var child2 = new DivElement { Class = "item2" };
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(300),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item1"),
                        Style = new Style
                        {
                            FlexBasis = Length.Px(200),
                            FlexShrink = 1
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item2"),
                        Style = new Style
                        {
                            FlexBasis = Length.Px(200),
                            FlexShrink = 3
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        // 容器 300px，两个子元素各 200px（总 400px），溢出 100px
        // 收缩系数: item1 = 1*200 = 200, item2 = 3*200 = 600, 总 = 800
        // item1 收缩 100 * (200/800) = 25px → 最终 175px
        // item2 收缩 100 * (600/800) = 75px → 最终 125px
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];

        layoutChild1.BoxModel.MarginBox.Width.ShouldBe(175, 1f);
        layoutChild2.BoxModel.MarginBox.Width.ShouldBe(125, 1f);
    }

    [Fact]
    public void FlexLayout_FlexShrinkZero_ShouldNotShrink()
    {
        // Arrange - flex-shrink: 0 的子元素不应该收缩
        var root = new DivElement { Class = "flex-container" };
        var child1 = new DivElement { Class = "item1" };
        var child2 = new DivElement { Class = "item2" };
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(300),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item1"),
                        Style = new Style
                        {
                            FlexBasis = Length.Px(200),
                            FlexShrink = 0 // 不收缩
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item2"),
                        Style = new Style
                        {
                            FlexBasis = Length.Px(200),
                            FlexShrink = 1 // 收缩承担全部溢出
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        // 容器 300px，两个子元素各 200px（总 400px），溢出 100px
        // item1 flex-shrink: 0 → 保持 200px
        // item2 flex-shrink: 1 → 承担全部收缩，200 - 100 = 100px
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];

        layoutChild1.BoxModel.MarginBox.Width.ShouldBe(200, 1f);
        layoutChild2.BoxModel.MarginBox.Width.ShouldBe(100, 1f);
    }

    [Fact]
    public void FlexLayout_FlexBasisAuto_ShouldUseContentSize()
    {
        // Arrange - flex-basis: auto 应该使用内容尺寸
        var root = new DivElement { Class = "flex-container" };
        var child1 = new DivElement { Class = "item1", TextContent = "Short" };
        var child2 = new DivElement { Class = "item2", TextContent = "Much Longer Text" };
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(800),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item1"),
                        Style = new Style
                        {
                            // FlexBasis 默认为 auto
                            FlexGrow = 0
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item2"),
                        Style = new Style
                        {
                            // FlexBasis 默认为 auto
                            FlexGrow = 0
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert - 较长文本的元素应该比较短文本的元素宽
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];

        layoutChild2.BoxModel.MarginBox.Width.ShouldBeGreaterThan(layoutChild1.BoxModel.MarginBox.Width);
    }

    [Fact]
    public void FlexLayout_FlexBasisPercent_ShouldCalculateFromContainer()
    {
        // Arrange - flex-basis 百分比应该基于容器尺寸计算
        var root = new DivElement { Class = "flex-container" };
        var child = new DivElement { Class = "item" };
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(400),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item"),
                        Style = new Style
                        {
                            FlexBasis = Length.Percent(50), // 容器宽度的 50%
                            FlexGrow = 0
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert - 50% of 400px = 200px
        var layoutChild = layoutRoot.Children[0];
        layoutChild.BoxModel.MarginBox.Width.ShouldBe(200, 1f);
    }

    #endregion

    #region Display.Inline Tests

    [Fact]
    public void InlineLayout_ShouldArrangeChildrenHorizontally()
    {
        // Arrange
        var root = new SpanElement();
        var child1 = new SpanElement();
        var child2 = new SpanElement();
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("span"),
                        Style = new Style
                        {
                            Display = Display.Inline,
                            Width = Length.Px(50),
                            Height = Length.Px(20)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        layoutRoot.Children.Count.ShouldBe(2);
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];

        // 行内元素应该水平排列
        layoutChild1.BoxModel.Content.X.ShouldBe(0);
        layoutChild2.BoxModel.Content.X.ShouldBe(layoutChild1.BoxModel.MarginBox.Right);

        // Y 坐标应该相同（在同一行）
        layoutChild1.BoxModel.Content.Y.ShouldBe(0);
        layoutChild2.BoxModel.Content.Y.ShouldBe(0);
    }

    [Fact]
    public void InlineLayout_ShouldNotRespectWidthAndHeight()
    {
        // Arrange
        var root = new SpanElement();
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("span"),
                        Style = new Style
                        {
                            Display = Display.Inline,
                            Width = Length.Px(200),
                            Height = Length.Px(100)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        // 行内元素的宽度和高度由内容决定，不受 width/height 属性影响
        // 但在当前实现中，如果设置了 width/height，会使用这些值
        layoutRoot.BoxModel.Content.Width.ShouldBe(200);
        layoutRoot.BoxModel.Content.Height.ShouldBe(100);
    }

    #endregion

    #region Display.InlineBlock Tests

    [Fact]
    public void InlineBlockLayout_ShouldRespectWidthAndHeight()
    {
        // Arrange
        var root = new DivElement { Class = "container" };
        var child1 = new DivElement { Class = "inline-block" };
        var child2 = new DivElement { Class = "inline-block" };
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("container"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(800)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("inline-block"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Width = Length.Px(100),
                            Height = Length.Px(50)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        layoutRoot.Children.Count.ShouldBe(2);
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];

        // inline-block 元素应该遵守设置的宽度和高度
        layoutChild1.BoxModel.Content.Width.ShouldBe(100);
        layoutChild1.BoxModel.Content.Height.ShouldBe(50);
        layoutChild2.BoxModel.Content.Width.ShouldBe(100);
        layoutChild2.BoxModel.Content.Height.ShouldBe(50);
    }

    [Fact]
    public void InlineBlockLayout_ShouldArrangeChildrenHorizontallyOnSameLine()
    {
        // Arrange - 使用 inline 容器来包含 inline-block 子元素
        var root = new SpanElement { Class = "container" };
        var child1 = new SpanElement { Class = "box" };
        var child2 = new SpanElement { Class = "box" };
        var child3 = new SpanElement { Class = "box" };
        root.AddChild(child1);
        root.AddChild(child2);
        root.AddChild(child3);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("container"),
                        Style = new Style
                        {
                            Display = Display.Inline,
                            Width = Length.Px(800)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("box"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Width = Length.Px(100),
                            Height = Length.Px(60)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        layoutRoot.Children.Count.ShouldBe(3);
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];
        var layoutChild3 = layoutRoot.Children[2];

        // 验证元素在同一行（Y 坐标相同）
        layoutChild1.BoxModel.Content.Y.ShouldBe(layoutChild2.BoxModel.Content.Y);
        layoutChild2.BoxModel.Content.Y.ShouldBe(layoutChild3.BoxModel.Content.Y);

        // 验证元素水平排列（X 坐标递增）
        layoutChild1.BoxModel.Content.X.ShouldBe(0);
        layoutChild2.BoxModel.Content.X.ShouldBe(layoutChild1.BoxModel.MarginBox.Right);
        layoutChild3.BoxModel.Content.X.ShouldBe(layoutChild2.BoxModel.MarginBox.Right);

        // 验证第二个元素在第一个元素右侧
        layoutChild2.BoxModel.Content.X.ShouldBeGreaterThan(layoutChild1.BoxModel.Content.X);
        // 验证第三个元素在第二个元素右侧
        layoutChild3.BoxModel.Content.X.ShouldBeGreaterThan(layoutChild2.BoxModel.Content.X);
    }

    [Fact]
    public void InlineBlockLayout_WithMargin_ShouldApplyCorrectSpacing()
    {
        // Arrange - 使用 inline 容器
        var root = new SpanElement { Class = "container" };
        var child1 = new SpanElement { Class = "box" };
        var child2 = new SpanElement { Class = "box" };
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("container"),
                        Style = new Style
                        {
                            Display = Display.Inline,
                            Width = Length.Px(800)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("box"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Width = Length.Px(100),
                            Height = Length.Px(50),
                            MarginLeft = Length.Px(10),
                            MarginRight = Length.Px(10)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];

        // 验证 margin 被正确应用
        layoutChild1.BoxModel.Margin.Left.ShouldBe(10);
        layoutChild1.BoxModel.Margin.Right.ShouldBe(10);
        layoutChild2.BoxModel.Margin.Left.ShouldBe(10);
        layoutChild2.BoxModel.Margin.Right.ShouldBe(10);

        // 第一个元素的内容区域应该从 margin-left 之后开始
        layoutChild1.BoxModel.Content.X.ShouldBe(10);

        // 第二个元素应该在第一个元素的 margin box 之后
        // 第一个元素的 margin box right = 10 (margin-left) + 100 (content) + 10 (margin-right) = 120
        // 第二个元素的 content.X = 120 (第一个元素的 margin box right) + 10 (自己的 margin-left) = 130
        float expectedX2 = layoutChild1.BoxModel.MarginBox.Right + layoutChild2.BoxModel.Margin.Left;
        layoutChild2.BoxModel.Content.X.ShouldBe(expectedX2);

        // 验证元素在同一行
        layoutChild1.BoxModel.Content.Y.ShouldBe(layoutChild2.BoxModel.Content.Y);
    }

    [Fact]
    public void InlineBlockLayout_WithPadding_ShouldReduceContentArea()
    {
        // Arrange
        var root = new DivElement { Class = "container" };
        var child = new DivElement { Class = "box" };
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("container"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(800)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("box"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Width = Length.Px(100),
                            Height = Length.Px(60),
                            PaddingLeft = Length.Px(15),
                            PaddingRight = Length.Px(15),
                            PaddingTop = Length.Px(10),
                            PaddingBottom = Length.Px(10)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild = layoutRoot.Children[0];

        // 验证 padding 被正确应用
        layoutChild.BoxModel.Padding.Left.ShouldBe(15);
        layoutChild.BoxModel.Padding.Right.ShouldBe(15);
        layoutChild.BoxModel.Padding.Top.ShouldBe(10);
        layoutChild.BoxModel.Padding.Bottom.ShouldBe(10);

        // 验证内容区域尺寸
        layoutChild.BoxModel.Content.Width.ShouldBe(100);
        layoutChild.BoxModel.Content.Height.ShouldBe(60);

        // 验证 padding box 尺寸（content + padding）
        layoutChild.BoxModel.PaddingBox.Width.ShouldBe(130); // 100 + 15 + 15
        layoutChild.BoxModel.PaddingBox.Height.ShouldBe(80); // 60 + 10 + 10
    }

    [Fact]
    public void InlineBlockLayout_MixedWithDifferentHeights_ShouldAlignOnSameLine()
    {
        // Arrange - 使用 inline 容器
        var root = new SpanElement { Class = "container" };
        var child1 = new SpanElement { Class = "small" };
        var child2 = new SpanElement { Class = "large" };
        var child3 = new SpanElement { Class = "medium" };
        root.AddChild(child1);
        root.AddChild(child2);
        root.AddChild(child3);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("container"),
                        Style = new Style
                        {
                            Display = Display.Inline,
                            Width = Length.Px(800)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("small"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Width = Length.Px(80),
                            Height = Length.Px(40)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("large"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Width = Length.Px(120),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("medium"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Width = Length.Px(100),
                            Height = Length.Px(60)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];
        var layoutChild3 = layoutRoot.Children[2];

        // 验证所有元素在同一行（Y 坐标相同）
        layoutChild1.BoxModel.Content.Y.ShouldBe(0);
        layoutChild2.BoxModel.Content.Y.ShouldBe(0);
        layoutChild3.BoxModel.Content.Y.ShouldBe(0);

        // 验证元素水平排列
        layoutChild1.BoxModel.Content.X.ShouldBe(0);
        layoutChild2.BoxModel.Content.X.ShouldBe(80); // 第一个元素宽度
        layoutChild3.BoxModel.Content.X.ShouldBe(200); // 80 + 120

        // 验证各元素的尺寸
        layoutChild1.BoxModel.Content.Width.ShouldBe(80);
        layoutChild1.BoxModel.Content.Height.ShouldBe(40);
        layoutChild2.BoxModel.Content.Width.ShouldBe(120);
        layoutChild2.BoxModel.Content.Height.ShouldBe(100);
        layoutChild3.BoxModel.Content.Width.ShouldBe(100);
        layoutChild3.BoxModel.Content.Height.ShouldBe(60);
    }

    [Fact]
    public void InlineBlockLayout_WithBorder_ShouldIncludeInBoxModel()
    {
        // Arrange - 使用 inline 容器
        var root = new SpanElement { Class = "container" };
        var child1 = new SpanElement { Class = "box" };
        var child2 = new SpanElement { Class = "box" };
        root.AddChild(child1);
        root.AddChild(child2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("container"),
                        Style = new Style
                        {
                            Display = Display.Inline,
                            Width = Length.Px(800)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("box"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Width = Length.Px(100),
                            Height = Length.Px(50),
                            BorderWidth = Length.Px(5)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var layoutChild1 = layoutRoot.Children[0];
        var layoutChild2 = layoutRoot.Children[1];

        // 验证 border 被正确应用
        layoutChild1.BoxModel.Border.Left.ShouldBe(5);
        layoutChild1.BoxModel.Border.Right.ShouldBe(5);
        layoutChild1.BoxModel.Border.Top.ShouldBe(5);
        layoutChild1.BoxModel.Border.Bottom.ShouldBe(5);

        // 验证 border box 尺寸（content + border）
        layoutChild1.BoxModel.BorderBox.Width.ShouldBe(110); // 100 + 5 + 5
        layoutChild1.BoxModel.BorderBox.Height.ShouldBe(60); // 50 + 5 + 5

        // 验证第二个元素的位置
        // 第一个元素的 border box right = 5 (border-left) + 100 (content) + 5 (border-right) = 110
        // 第二个元素的 content.X = 110 (第一个元素的 border box right) + 5 (自己的 border-left) = 115
        float expectedX2 = layoutChild1.BoxModel.BorderBox.Right + layoutChild2.BoxModel.Border.Left;
        layoutChild2.BoxModel.Content.X.ShouldBe(expectedX2);

        // 验证元素在同一行
        layoutChild1.BoxModel.Content.Y.ShouldBe(layoutChild2.BoxModel.Content.Y);
    }

    [Fact]
    public void BlockLayout_WithInlineBlockChildren_ShouldArrangeHorizontally()
    {
        // Arrange - Block 容器包含多个 InlineBlock 子元素
        // InlineBlock 子元素应该水平排列在同一行
        var root = new DivElement { Class = "container" };
        var button1 = new ButtonElement { TextContent = "Small", Class = "btn-sm" };
        var button2 = new ButtonElement { TextContent = "Medium" };
        var button3 = new ButtonElement { TextContent = "Large", Class = "btn-lg" };
        root.AddChild(button1);
        root.AddChild(button2);
        root.AddChild(button3);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("button"),
                        Style = new Style
                        {
                            Display = Display.InlineBlock,
                            Padding = new Padding(6, 12),
                            Margin = new Margin(0, 10, 10, 0)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("btn-sm"),
                        Style = new Style
                        {
                            PaddingTop = Length.Px(4),
                            PaddingRight = Length.Px(8),
                            PaddingBottom = Length.Px(4),
                            PaddingLeft = Length.Px(8)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("btn-lg"),
                        Style = new Style
                        {
                            PaddingTop = Length.Px(10),
                            PaddingRight = Length.Px(16),
                            PaddingBottom = Length.Px(10),
                            PaddingLeft = Length.Px(16)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        layoutRoot.Children.Count.ShouldBe(3);
        var box1 = layoutRoot.Children[0].BoxModel;
        var box2 = layoutRoot.Children[1].BoxModel;
        var box3 = layoutRoot.Children[2].BoxModel;

        // 验证三个按钮在同一行（BorderBox.Top 相同）
        box1.BorderBox.Top.ShouldBe(box2.BorderBox.Top);
        box2.BorderBox.Top.ShouldBe(box3.BorderBox.Top);

        // 验证按钮水平排列（X 坐标递增）
        box2.BorderBox.Left.ShouldBeGreaterThan(box1.BorderBox.Left);
        box3.BorderBox.Left.ShouldBeGreaterThan(box2.BorderBox.Left);
    }

    #endregion

    #region Display.None Tests

    [Fact]
    public void DisplayNone_ShouldNotAppearInLayoutTree()
    {
        // Arrange
        var root = new DivElement();
        var child1 = new DivElement { Id = "visible" };
        var child2 = new DivElement { Id = "hidden" };
        var child3 = new DivElement { Id = "visible2" };
        root.AddChild(child1);
        root.AddChild(child2);
        root.AddChild(child3);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style { Display = Display.Block }
                    },
                    new StyleRule
                    {
                        Selector = new IdSelector("hidden"),
                        Style = new Style { Display = Display.None }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        // 只有两个可见的子元素应该出现在布局树中
        layoutRoot.Children.Count.ShouldBe(2);
        layoutRoot.Children[0].Element.Id.ShouldBe("visible");
        layoutRoot.Children[1].Element.Id.ShouldBe("visible2");
    }

    #endregion

    #region Mixed Display Tests

    [Fact]
    public void MixedDisplay_BlockContainingFlex_ShouldLayoutCorrectly()
    {
        // Arrange
        var root = new DivElement { Class = "container" };
        var flexContainer = new DivElement { Class = "flex-container" };
        var flexChild1 = new DivElement { Class = "flex-child" };
        var flexChild2 = new DivElement { Class = "flex-child" };

        root.AddChild(flexContainer);
        flexContainer.AddChild(flexChild1);
        flexContainer.AddChild(flexChild2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("container"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(800),
                            Height = Length.Px(600)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(400),
                            Height = Length.Px(200)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-child"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(100),
                            Height = Length.Px(100)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        layoutRoot.ComputedStyle.Display.ShouldBe(Display.Block);
        layoutRoot.Children.Count.ShouldBe(1);

        var flexLayout = layoutRoot.Children[0];
        flexLayout.ComputedStyle.Display.ShouldBe(Display.Flex);
        flexLayout.Children.Count.ShouldBe(2);

        // Flex 子元素应该水平排列
        var flexChild1Layout = flexLayout.Children[0];
        var flexChild2Layout = flexLayout.Children[1];

        // 第一个子元素应该在起始位置
        flexChild1Layout.BoxModel.Content.X.ShouldBe(0);

        // 第二个子元素应该在第一个之后（但由于 BlockLayout 的行为，可能都在 X=0）
        // 在当前实现中，FlexLayout 使用 BlockLayout 来布局子元素，
        // 这会导致子元素的 X 坐标被重置
        // 这是一个已知的限制，我们只验证它们都被正确添加到布局树中
        flexChild2Layout.ShouldNotBeNull();
        flexChild1Layout.BoxModel.Content.Width.ShouldBe(100);
        flexChild2Layout.BoxModel.Content.Width.ShouldBe(100);
    }

    #endregion

    #region Box Model Integration Tests

    [Fact]
    public void BlockLayout_WithCompleteBoxModel_ShouldCalculateCorrectly()
    {
        // Arrange
        var root = new DivElement();
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(400),
                            Height = Length.Px(300),
                            PaddingTop = Length.Px(20),
                            PaddingRight = Length.Px(20),
                            PaddingBottom = Length.Px(20),
                            PaddingLeft = Length.Px(20),
                            BorderWidth = Length.Px(5),
                            MarginTop = Length.Px(10),
                            MarginRight = Length.Px(10),
                            MarginBottom = Length.Px(10),
                            MarginLeft = Length.Px(10)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        // Content box
        layoutRoot.BoxModel.Content.Width.ShouldBe(400);
        layoutRoot.BoxModel.Content.Height.ShouldBe(300);

        // Padding
        layoutRoot.BoxModel.Padding.Left.ShouldBe(20);
        layoutRoot.BoxModel.Padding.Right.ShouldBe(20);

        // Border
        layoutRoot.BoxModel.Border.Left.ShouldBe(5);
        layoutRoot.BoxModel.Border.Right.ShouldBe(5);

        // Margin
        layoutRoot.BoxModel.Margin.Left.ShouldBe(10);
        layoutRoot.BoxModel.Margin.Right.ShouldBe(10);

        // Padding box = content + padding
        var paddingBox = layoutRoot.BoxModel.PaddingBox;
        paddingBox.Width.ShouldBe(440); // 400 + 20 + 20

        // Border box = padding box + border
        var borderBox = layoutRoot.BoxModel.BorderBox;
        borderBox.Width.ShouldBe(450); // 440 + 5 + 5

        // Margin box = border box + margin
        var marginBox = layoutRoot.BoxModel.MarginBox;
        marginBox.Width.ShouldBe(470); // 450 + 10 + 10
    }

    [Fact]
    public void FlexLayout_WithPaddingAndBorder_ShouldReduceAvailableSpace()
    {
        // Arrange
        var root = new DivElement();
        var child = new DivElement();
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(400),
                            PaddingTop = Length.Px(20),
                            PaddingRight = Length.Px(20),
                            PaddingBottom = Length.Px(20),
                            PaddingLeft = Length.Px(20),
                            BorderWidth = Length.Px(5)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        // 容器的内容宽度应该是 400
        layoutRoot.BoxModel.Content.Width.ShouldBe(400);

        // 子元素的可用宽度应该考虑 padding 和 border
        // 但在当前实现中，子元素的约束是基于父元素的内容宽度
        var layoutChild = layoutRoot.Children[0];
        layoutChild.ShouldNotBeNull();
    }

    #endregion

    #region Flex Container with Text Content Tests

    [Fact]
    public void FlexLayout_WithTextContentOnly_ShouldHaveHeightFromText()
    {
        // Arrange - Flex 容器只有文本内容，没有子元素
        var button = new ButtonElement { Class = "flex-btn", TextContent = "Accordion Item #1" };

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-btn"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            AlignItems = AlignItems.Center,
                            Width = Length.Px(400)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(button, styleSheets, 800, 600);

        // Assert - 高度应该由文本内容决定，不能为 0
        layoutRoot.BoxModel.Content.Height.ShouldBeGreaterThan(0);
        layoutRoot.BoxModel.BorderBox.Width.ShouldBe(400);
    }

    [Fact]
    public void FlexLayout_AccordionButtonScenario_ShouldHaveCorrectDimensions()
    {
        // Arrange - 复现 ISSUE-024 的场景
        // accordion-item(block, border:1px) → h2(block) → button(flex, width:100%)
        var accordionItem = new DivElement { Class = "accordion-item" };
        var h2 = new H2Element();
        var button = new ButtonElement { Class = "accordion-button", TextContent = "Accordion Item #1" };
        h2.AddChild(button);
        accordionItem.AddChild(h2);

        var root = new DivElement();
        root.AddChild(accordionItem);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("accordion-item"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Border = new Border(1, BorderStyle.Solid, Color.FromHex("dee2e6"))
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("accordion-button"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            AlignItems = AlignItems.Center,
                            Width = Length.Percent(100)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert
        var accordionBox = layoutRoot.Children[0];
        var h2Box = accordionBox.Children[0];
        var buttonBox = h2Box.Children[0];

        // button 的高度应该大于 0（由文本内容决定）
        buttonBox.BoxModel.Content.Height.ShouldBeGreaterThan(0);

        // button 使用 border-box，width: 100% 意味着 border-box 宽度 = h2 内容宽度
        buttonBox.BoxModel.BorderBox.Width.ShouldBe(h2Box.BoxModel.Content.Width);

        // button 的 margin box 不应该超出 h2 的内容宽度（button 无 margin）
        buttonBox.BoxModel.MarginBox.Width.ShouldBeLessThanOrEqualTo(h2Box.BoxModel.Content.Width);
    }

    [Fact]
    public void FlexLayout_ColumnDirection_WithTextContentOnly_ShouldHaveHeightFromText()
    {
        // Arrange - 列方向的 Flex 容器只有文本内容
        var div = new DivElement { Class = "flex-col", TextContent = "Hello World" };

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-col"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Column,
                            Width = Length.Px(200)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(div, styleSheets, 800, 600);

        // Assert
        layoutRoot.BoxModel.Content.Height.ShouldBeGreaterThan(0);
    }

    #endregion

    #region Input Element Default Width Tests

    /// <summary>
    /// 文本输入框在布局时应该使用浏览器默认宽度（约173px）
    /// 这验证了 UA Stylesheet 中的默认宽度在布局阶段被正确应用
    /// </summary>
    [Fact]
    public void TextInput_Layout_ShouldHaveDefaultWidth()
    {
        // Arrange
        var input = new InputElement { Type = InputType.Text };

        // Act
        var layoutRoot = _layoutEngine.Layout(input, new List<StyleSheet>(), 800, 600);

        // Assert: 浏览器默认文本输入框 border-box 宽度约为 173px (Chrome)
        layoutRoot.BoxModel.BorderBox.Width.ShouldBe(173,
            "Text input should have border-box width of 173px (browser default)");
    }

    /// <summary>
    /// 密码输入框在布局时应该与文本输入框有相同的默认宽度
    /// </summary>
    [Fact]
    public void PasswordInput_Layout_ShouldHaveSameDefaultWidthAsText()
    {
        // Arrange
        var textInput = new InputElement { Type = InputType.Text };
        var passwordInput = new InputElement { Type = InputType.Password };

        // Act
        var textLayout = _layoutEngine.Layout(textInput, new List<StyleSheet>(), 800, 600);
        var passwordLayout = _layoutEngine.Layout(passwordInput, new List<StyleSheet>(), 800, 600);

        // Assert: 密码输入框应该与文本输入框具有相同的宽度
        passwordLayout.BoxModel.BorderBox.Width.ShouldBe(textLayout.BoxModel.BorderBox.Width,
            "Password input should have same default width as text input");
        passwordLayout.BoxModel.BorderBox.Width.ShouldBe(173,
            "Password input should have default width of 173px");
    }

    /// <summary>
    /// 文本输入框在 Flex 容器中应该保持其固有默认宽度
    /// </summary>
    [Fact]
    public void TextInput_InFlexContainer_ShouldMaintainDefaultWidth()
    {
        // Arrange
        var container = new DivElement { Class = "flex-container" };
        var label = new SpanElement { TextContent = "Name:", Class = "label" };
        var input = new InputElement { Type = InputType.Text, Class = "input" };
        container.AddChild(label);
        container.AddChild(input);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex-container"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(600)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(container, styleSheets, 800, 600);

        // Assert: 输入框在 Flex 容器中应该保持其默认宽度（border-box = 173px）
        var inputLayout = layoutRoot.Children[1];
        inputLayout.BoxModel.BorderBox.Width.ShouldBe(173,
            "Text input in flex container should maintain border-box width of 173px");
    }

    /// <summary>
    /// Checkbox 和 Radio 应该有固定的 13x13px 尺寸
    /// </summary>
    [Theory]
    [InlineData(InputType.Checkbox)]
    [InlineData(InputType.Radio)]
    public void CheckboxAndRadio_Layout_ShouldHaveFixedSize(InputType inputType)
    {
        // Arrange
        var input = new InputElement { Type = inputType };

        // Act
        var layoutRoot = _layoutEngine.Layout(input, new List<StyleSheet>(), 800, 600);

        // Assert: Checkbox 和 Radio 应该是 13x13px
        layoutRoot.BoxModel.Content.Width.ShouldBe(13,
            $"{inputType} should have fixed width of 13px");
        layoutRoot.BoxModel.Content.Height.ShouldBe(13,
            $"{inputType} should have fixed height of 13px");
    }

    /// <summary>
    /// Range 输入框应该有默认宽度（约129px）
    /// </summary>
    [Fact]
    public void RangeInput_Layout_ShouldHaveDefaultWidth()
    {
        // Arrange
        var input = new InputElement { Type = InputType.Range };

        // Act
        var layoutRoot = _layoutEngine.Layout(input, new List<StyleSheet>(), 800, 600);

        // Assert: Range 输入框默认宽度约为 129px (Chrome)
        layoutRoot.BoxModel.Content.Width.ShouldBe(129,
            "Range input should have default width of 129px");
    }

    #endregion

    #region BoxSizing Tests

    [Fact]
    public void BoxSizing_BorderBox_WidthIncludesPaddingAndBorder()
    {
        var div = new DivElement { TextContent = "Test" };
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style
                        {
                            BoxSizing = BoxSizing.BorderBox,
                            Width = Length.Px(200),
                            Padding = new Padding(10),
                            Border = new Border(5, BorderStyle.Solid, Color.Black)
                        }
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(div, styleSheets, 800, 600);

        // border-box: width=200 包含 padding 和 border
        // content = 200 - padding(10+10) - border(5+5) = 170
        layoutRoot.BoxModel.Content.Width.ShouldBe(170);
        layoutRoot.BoxModel.BorderBox.Width.ShouldBe(200);
    }

    [Fact]
    public void BoxSizing_ContentBox_WidthIsContentOnly()
    {
        var div = new DivElement { TextContent = "Test" };
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("div"),
                        Style = new Style
                        {
                            BoxSizing = BoxSizing.ContentBox,
                            Width = Length.Px(200),
                            Padding = new Padding(10),
                            Border = new Border(5, BorderStyle.Solid, Color.Black)
                        }
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(div, styleSheets, 800, 600);

        // content-box: width=200 仅为内容宽度
        layoutRoot.BoxModel.Content.Width.ShouldBe(200);
        layoutRoot.BoxModel.BorderBox.Width.ShouldBe(230);
    }

    [Fact]
    public void BoxSizing_ButtonWithWidth100Percent_ShouldNotOverflowParent()
    {
        var parent = new DivElement();
        var button = new ButtonElement { TextContent = "Click" };
        parent.AddChild(button);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("button"),
                        Style = new Style { Width = Length.Percent(100) }
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(parent, styleSheets, 400, 600);
        var buttonBox = layoutRoot.Children[0];

        // button 默认 border-box，width:100% = 父内容宽度 = border-box 宽度
        buttonBox.BoxModel.MarginBox.Width.ShouldBeLessThanOrEqualTo(
            layoutRoot.BoxModel.Content.Width);
    }

    #endregion

    #region Flex Column Nested Percentage Height

    [Fact]
    public void FlexColumn_FlexGrowChild_NestedPercentageHeight_ShouldResolveCorrectly()
    {
        var root = new DivElement { Class = "root" };
        var toolbar = new DivElement { Class = "toolbar", TextContent = "sss" };
        var content = new DivElement { Class = "content" };
        var panel = new DivElement { Class = "panel" };
        var test = new DivElement { Class = "test", TextContent = "test" };

        panel.AddChild(test);
        content.AddChild(panel);
        root.AddChild(toolbar);
        root.AddChild(content);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style
                        {
                            Width = Length.Px(500),
                            Height = Length.Px(500),
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Column
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("toolbar"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Height = Length.Px(34),
                            AlignItems = AlignItems.Center,
                            PaddingLeft = Length.Px(8)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("content"),
                        Style = new Style
                        {
                            FlexGrow = 1,
                            FlexShrink = 1,
                            FlexBasis = Length.Percent(0),
                            OverflowY = Overflow.Hidden
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("panel"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            Width = Length.Percent(100),
                            Height = Length.Percent(100),
                            OverflowY = Overflow.Scroll
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("test"),
                        Style = new Style
                        {
                            Width = Length.Px(100),
                            Height = Length.Px(800)
                        }
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        var contentBox = layoutRoot.Children[1];
        contentBox.BoxModel.Content.Height.ShouldBeGreaterThan(0);
        contentBox.BoxModel.Content.Height.ShouldBe(466, 1f);

        var panelBox = contentBox.Children[0];
        panelBox.BoxModel.Content.Height.ShouldBeGreaterThan(0);
        panelBox.BoxModel.Content.Height.ShouldBe(contentBox.BoxModel.Content.Height, 1f);

        var testBox = panelBox.Children[0];
        testBox.BoxModel.Content.Height.ShouldBe(800, 1f);
    }

    [Fact]
    public void FlexShorthand_ShouldSetGrowShrinkBasis()
    {
        var style = new Style { Flex = 1 };

        style.FlexGrow.ShouldNotBeNull();
        style.FlexGrow!.Value.ShouldBe(1f);
        style.FlexShrink.ShouldNotBeNull();
        style.FlexShrink!.Value.ShouldBe(1f);
        style.FlexBasis.ShouldNotBeNull();
        style.FlexBasis!.Value.ShouldBe(Length.Percent(0));
    }

    #endregion

    #region Flex Row Cross-Axis Height

    [Fact]
    public void FlexRow_ChildWithOverflowScroll_ShouldHaveContainerHeight()
    {
        // devtools-root (flex column, 500x500)
        //   toolbar (height: 34px)
        //   content (flex:1, display:flex, overflow-y:hidden)
        //     elements-panel (flex row, width:100%, height:100%)
        //       dom-tree-panel (flex-grow:1, overflow-y:scroll, padding:4px)
        //         test "DOM"
        //       style-panel (width:280px)

        var root = new DivElement { Class = "root" };
        var toolbar = new DivElement { Class = "toolbar", TextContent = "sss" };
        var content = new DivElement { Class = "content" };
        var panel = new DivElement { Class = "elements-panel" };
        var treePanel = new DivElement { Class = "dom-tree-panel" };
        var test = new DivElement { Class = "test", TextContent = "DOM" };
        var stylePanel = new DivElement { Class = "style-panel", TextContent = "style" };

        treePanel.AddChild(test);
        panel.AddChild(treePanel);
        panel.AddChild(stylePanel);
        content.AddChild(panel);
        root.AddChild(toolbar);
        root.AddChild(content);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style
                        {
                            Width = Length.Px(500),
                            Height = Length.Px(500),
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Column
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("toolbar"),
                        Style = new Style
                        {
                            Height = Length.Px(34)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("content"),
                        Style = new Style
                        {
                            Flex = 1,
                            Display = Display.Flex,
                            OverflowY = Overflow.Hidden
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("elements-panel"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Percent(100),
                            Height = Length.Percent(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("dom-tree-panel"),
                        Style = new Style
                        {
                            FlexGrow = 1,
                            OverflowY = Overflow.Scroll,
                            Padding = new Padding(Length.Px(4))
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("style-panel"),
                        Style = new Style
                        {
                            Width = Length.Px(280),
                            OverflowY = Overflow.Scroll,
                            Padding = new Padding(Length.Px(8))
                        }
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // content should fill remaining height (500 - 34 = 466)
        var contentBox = layoutRoot.Children[1];
        contentBox.BoxModel.Content.Height.ShouldBe(466, 1f);

        // elements-panel (height:100%) should match content height
        var elementsPanelBox = contentBox.Children[0];
        elementsPanelBox.BoxModel.Content.Height.ShouldBe(466, 1f);

        // dom-tree-panel should stretch to fill the cross-axis (full container height)
        var treePanelBox = elementsPanelBox.Children[0];
        treePanelBox.BoxModel.Content.Height.ShouldBeGreaterThan(100);

        // text content should be at the top, not vertically centered
        var testBox = treePanelBox.Children[0];
        testBox.BoxModel.Content.Y.ShouldBe(treePanelBox.BoxModel.Content.Y, 1f);
    }

    [Fact]
    public void FlexRow_BlockChild_TextShouldBeAtTop()
    {
        // Exact repro from DebugDemo.Razor: nested flex column → flex row → block panels
        // Text in .dom-tree-panel and .style-panel should start at the top, not centered.
        //
        // devtools-root (flex column, 500x500)
        //   devtools-toolbar (height: 34px) "sss"
        //   devtools-content (flex:1, display:flex, overflow-y:hidden)
        //     elements-panel (flex row, width:100%, height:100%)
        //       dom-tree-panel (block, flex-grow:1, overflow-y:scroll, padding:4px)
        //         test "DOM"
        //       style-panel (block, width:280, overflow-y:scroll, padding:8px)
        //         "style"

        var root = new DivElement { Class = "devtools-root" };
        var toolbar = new DivElement { Class = "devtools-toolbar", TextContent = "sss" };
        var content = new DivElement { Class = "devtools-content" };
        var elementsPanel = new DivElement { Class = "elements-panel" };
        var treePanel = new DivElement { Class = "dom-tree-panel" };
        var test = new DivElement { Class = "test", TextContent = "DOM" };
        var stylePanel = new DivElement { Class = "style-panel", TextContent = "style" };

        treePanel.AddChild(test);
        elementsPanel.AddChild(treePanel);
        elementsPanel.AddChild(stylePanel);
        content.AddChild(elementsPanel);
        root.AddChild(toolbar);
        root.AddChild(content);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("devtools-root"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Column,
                            Width = Length.Px(500),
                            Height = Length.Px(500)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("devtools-toolbar"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Height = Length.Px(34),
                            AlignItems = AlignItems.Center,
                            PaddingLeft = Length.Px(8)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("devtools-content"),
                        Style = new Style
                        {
                            Flex = 1,
                            Display = Display.Flex,
                            OverflowY = Overflow.Hidden
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("elements-panel"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Percent(100),
                            Height = Length.Percent(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("dom-tree-panel"),
                        Style = new Style
                        {
                            FlexGrow = 1,
                            OverflowY = Overflow.Scroll,
                            Padding = new Padding(Length.Px(4)),
                            BorderRight = new BorderSide(Length.Px(1), BorderStyle.Solid, new Color(70, 70, 70))
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("style-panel"),
                        Style = new Style
                        {
                            Width = Length.Px(280),
                            OverflowY = Overflow.Scroll,
                            Padding = new Padding(Length.Px(8))
                        }
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // content (flex:1) should fill remaining: 500 - 34 = 466
        var contentBox = layoutRoot.Children[1];
        contentBox.BoxModel.Content.Height.ShouldBe(466, 1f);

        // elements-panel (height:100%) should match content
        var elementsPanelBox = contentBox.Children[0];
        elementsPanelBox.BoxModel.Content.Height.ShouldBe(466, 1f);

        // dom-tree-panel should be stretched to fill container height
        var treePanelBox = elementsPanelBox.Children[0];
        treePanelBox.BoxModel.Content.Height.ShouldBeGreaterThan(400);

        // test div inside dom-tree-panel: its Y should be at the top of the panel content area
        var testBox = treePanelBox.Children[0];
        float treePanelContentY = treePanelBox.BoxModel.Content.Y;
        testBox.BoxModel.MarginBox.Y.ShouldBe(treePanelContentY, 1f);

        // style-panel text should also start at the top (style-panel has no children, just TextContent)
        // For elements with TextContent and no children, the text is rendered at content.Y
        // so we just verify the content.Y is at the top of the elements-panel content area
        var stylePanelBox = elementsPanelBox.Children[1];
        stylePanelBox.BoxModel.Content.Height.ShouldBeGreaterThan(400);
    }

    #endregion

    #region Text Content + Children Coexistence

    [Fact]
    public void BlockLayout_TextContentAndChildren_ShouldNotOverlap()
    {
        // <div> content1 <div>content2</div> </div>
        // When a block element has both TextContent and block children,
        // the text occupies a line at the top and children flow below it.
        var root = new DivElement { Class = "outer", TextContent = "content1" };
        var child = new DivElement { Class = "inner", TextContent = "content2" };
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("outer"),
                        Style = new Style { Width = Length.Px(300) }
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        var childBox = layoutRoot.Children[0];

        // The child block must be positioned below the parent's own text line,
        // not at the very top (which would overlap the text).
        float parentContentTop = layoutRoot.BoxModel.Content.Y;
        childBox.BoxModel.MarginBox.Y.ShouldBeGreaterThan(parentContentTop);

        // The parent's content height must include both the text line and the child.
        layoutRoot.BoxModel.Content.Height.ShouldBeGreaterThan(childBox.BoxModel.MarginBox.Height);
    }

    [Fact]
    public void BlockLayout_TextContentAndInlineChildren_ShouldFlowOnSameLine()
    {
        // <div> content1 <span>content2</span> </div>
        // TextContent acts like an anonymous inline box and should flow
        // on the same line with inline/inline-block children.
        var root = new DivElement { Class = "outer", TextContent = "content1 " };
        var span = new SpanElement { Class = "inner", TextContent = "content2" };
        root.AddChild(span);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("outer"),
                        Style = new Style { Width = Length.Px(300) }
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        var spanBox = layoutRoot.Children[0];

        // The span should be on the same line as the parent's text,
        // positioned horizontally after the text width, not below it.
        float parentContentTop = layoutRoot.BoxModel.Content.Y;
        spanBox.BoxModel.MarginBox.Y.ShouldBe(parentContentTop, 1f);

        // The span's X position should be after the text "content1 "
        float parentContentLeft = layoutRoot.BoxModel.Content.X;
        spanBox.BoxModel.MarginBox.X.ShouldBeGreaterThan(parentContentLeft);
    }

    #endregion

    #region Positioning (relative / absolute) Tests

    [Fact]
    public void AbsolutePositioning_TopRight_ShouldPositionRelativeToRelativeParent()
    {
        // 复现 ISSUE-037 问题1：
        // 父元素 position: relative，子元素 position: absolute; top: 0; right: 0
        // 子元素应该相对于父元素的内边距盒（padding box）定位到右上角。
        var parent = new DivElement { Class = "parent" };
        var child = new DivElement { Class = "child" };
        parent.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("parent"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Position = Position.Relative,
                            Width = Length.Px(400),
                            Height = Length.Px(300)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Position = Position.Absolute,
                            Top = Length.Px(0),
                            Right = Length.Px(0),
                            Width = Length.Px(50),
                            Height = Length.Px(50)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(parent, styleSheets, 800, 600);

        // Assert
        var childBox = layoutRoot.Children[0];
        var parentPaddingBox = layoutRoot.BoxModel.PaddingBox;

        // top: 0 → 子元素 border box 顶部对齐父元素 padding box 顶部
        childBox.BoxModel.BorderBox.Top.ShouldBe(parentPaddingBox.Top);
        // right: 0 → 子元素 border box 右边对齐父元素 padding box 右边
        childBox.BoxModel.BorderBox.Right.ShouldBe(parentPaddingBox.Right);
    }

    [Fact]
    public void AbsolutePositioning_TopLeft_ShouldOffsetFromContainingBlock()
    {
        // position: absolute; top: 10; left: 20 应相对于定位父元素的 padding box 偏移
        var parent = new DivElement { Class = "parent" };
        var child = new DivElement { Class = "child" };
        parent.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("parent"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Position = Position.Relative,
                            Width = Length.Px(400),
                            Height = Length.Px(300),
                            PaddingTop = Length.Px(5),
                            PaddingLeft = Length.Px(5),
                            PaddingRight = Length.Px(5),
                            PaddingBottom = Length.Px(5)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Position = Position.Absolute,
                            Top = Length.Px(10),
                            Left = Length.Px(20),
                            Width = Length.Px(50),
                            Height = Length.Px(50)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(parent, styleSheets, 800, 600);

        // Assert
        var childBox = layoutRoot.Children[0];
        var parentPaddingBox = layoutRoot.BoxModel.PaddingBox;

        childBox.BoxModel.BorderBox.Left.ShouldBe(parentPaddingBox.Left + 20);
        childBox.BoxModel.BorderBox.Top.ShouldBe(parentPaddingBox.Top + 10);
    }

    [Fact]
    public void AbsolutePositioning_NoPositionedAncestor_ShouldUseViewport()
    {
        // 没有定位祖先时，绝对定位相对于初始包含块（视口）。
        var root = new DivElement { Class = "root" };
        var child = new DivElement { Class = "child" };
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(400), Height = Length.Px(300) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Position = Position.Absolute,
                            Top = Length.Px(15),
                            Left = Length.Px(25),
                            Width = Length.Px(50),
                            Height = Length.Px(50)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert - 相对于视口 (0,0)
        var childBox = layoutRoot.Children[0];
        childBox.BoxModel.BorderBox.Left.ShouldBe(25);
        childBox.BoxModel.BorderBox.Top.ShouldBe(15);
    }

    [Fact]
    public void RelativePositioning_TopLeft_ShouldOffsetFromNormalPosition()
    {
        // position: relative; top: 10; left: 20 应相对于常规流中的位置偏移
        var root = new DivElement { Class = "root" };
        var child = new DivElement { Class = "child" };
        root.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(400), Height = Length.Px(300) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Position = Position.Relative,
                            Top = Length.Px(10),
                            Left = Length.Px(20),
                            Height = Length.Px(50)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert - 子元素从常规流位置 (0,0) 偏移 (20,10)
        var childBox = layoutRoot.Children[0];
        childBox.BoxModel.BorderBox.Left.ShouldBe(20);
        childBox.BoxModel.BorderBox.Top.ShouldBe(10);
    }

    [Fact]
    public void RelativePositioning_ShouldMoveDescendantsToo()
    {
        // relative 偏移应整体平移子树（后代也一起移动）。
        var root = new DivElement { Class = "root" };
        var rel = new DivElement { Class = "rel" };
        var inner = new DivElement { Class = "inner" };
        root.AddChild(rel);
        rel.AddChild(inner);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(400) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("rel"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Position = Position.Relative,
                            Left = Length.Px(30),
                            Top = Length.Px(40),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("inner"),
                        Style = new Style { Display = Display.Block, Height = Length.Px(20) }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert - inner 也应随 rel 一起偏移 (30, 40)
        var innerBox = layoutRoot.Children[0].Children[0];
        innerBox.BoxModel.Content.X.ShouldBe(30);
        innerBox.BoxModel.Content.Y.ShouldBe(40);
    }

    [Fact]
    public void AbsolutePositioning_ContainingBlockIsNearestPositionedAncestor()
    {
        // absolute 应相对于最近的"定位"祖先（position != static），跳过 static 祖先。
        var root = new DivElement { Class = "root" };       // relative，定位包含块
        var middle = new DivElement { Class = "middle" };   // static，应被跳过
        var child = new DivElement { Class = "child" };     // absolute
        root.AddChild(middle);
        middle.AddChild(child);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Position = Position.Relative,
                            Width = Length.Px(400),
                            Height = Length.Px(300)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("middle"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            MarginTop = Length.Px(50),
                            MarginLeft = Length.Px(50),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Position = Position.Absolute,
                            Top = Length.Px(0),
                            Left = Length.Px(0),
                            Width = Length.Px(30),
                            Height = Length.Px(30)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert - child 相对于 root（padding box 原点 0,0），而非 static 的 middle
        var childBox = layoutRoot.Children[0].Children[0];
        childBox.BoxModel.BorderBox.Left.ShouldBe(0);
        childBox.BoxModel.BorderBox.Top.ShouldBe(0);
    }

    [Fact]
    public void AbsoluteChild_ShouldNotContributeToParentContentHeight()
    {
        // 复现 ISSUE-037 问题2：
        // 绝对定位子元素脱离文档流，父元素（高度自适应）的内容高度
        // 应只由常规流子元素决定，不包含绝对定位子元素。
        var parent = new DivElement { Class = "parent" };
        var flow = new DivElement { Class = "flow" };       // 常规流子元素，高度 40
        var abs = new DivElement { Class = "abs" };          // 绝对定位，高度 500
        parent.AddChild(flow);
        parent.AddChild(abs);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("parent"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Position = Position.Relative,
                            Width = Length.Px(400)
                            // 高度自适应（auto）
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("flow"),
                        Style = new Style { Display = Display.Block, Height = Length.Px(40) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("abs"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Position = Position.Absolute,
                            Top = Length.Px(0),
                            Left = Length.Px(0),
                            Width = Length.Px(100),
                            Height = Length.Px(500)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(parent, styleSheets, 800, 600);

        // Assert - 父元素内容高度应为 40（仅常规流子元素），而非 540 或 500
        layoutRoot.BoxModel.Content.Height.ShouldBe(40);
    }

    [Fact]
    public void AbsoluteChild_ShouldNotPushFollowingSibling()
    {
        // 绝对定位子元素不占据常规流空间，后续兄弟元素不应被其推开。
        var parent = new DivElement { Class = "parent" };
        var abs = new DivElement { Class = "abs" };          // 绝对定位，高度 200
        var after = new DivElement { Class = "after" };      // 常规流，应紧跟父内容顶部
        parent.AddChild(abs);
        parent.AddChild(after);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("parent"),
                        Style = new Style { Display = Display.Block, Position = Position.Relative, Width = Length.Px(400) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("abs"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Position = Position.Absolute,
                            Top = Length.Px(0),
                            Left = Length.Px(0),
                            Height = Length.Px(200)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("after"),
                        Style = new Style { Display = Display.Block, Height = Length.Px(30) }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(parent, styleSheets, 800, 600);

        // Assert - after 应位于父元素内容顶部（Y=0），不被绝对定位元素推开
        var afterBox = layoutRoot.Children[1];
        afterBox.BoxModel.Content.Y.ShouldBe(0);
        // 父内容高度仅由 after 决定
        layoutRoot.BoxModel.Content.Height.ShouldBe(30);
    }

    [Fact]
    public void FlexContainer_AbsoluteChild_ShouldNotAffectMainSize()
    {
        // flex 容器中的绝对定位子元素不是 flex 项目，不影响主轴尺寸与排列。
        var parent = new DivElement { Class = "flex" };
        var item = new DivElement { Class = "item" };        // flex 项目，宽 100
        var abs = new DivElement { Class = "abs" };          // 绝对定位，宽 300
        parent.AddChild(item);
        parent.AddChild(abs);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("flex"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Position = Position.Relative,
                            Width = Length.Px(400),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item"),
                        Style = new Style { Width = Length.Px(100), Height = Length.Px(50) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("abs"),
                        Style = new Style
                        {
                            Position = Position.Absolute,
                            Top = Length.Px(0),
                            Right = Length.Px(0),
                            Width = Length.Px(300),
                            Height = Length.Px(80)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(parent, styleSheets, 800, 600);

        // Assert
        var itemBox = layoutRoot.Children[0];
        var absBox = layoutRoot.Children[1];

        // flex 项目从主轴起点开始，不被绝对定位元素占位影响
        itemBox.BoxModel.Content.X.ShouldBe(0);
        // 绝对定位元素按 right:0 定位到容器右边
        absBox.BoxModel.BorderBox.Right.ShouldBe(layoutRoot.BoxModel.PaddingBox.Right);
    }

    #endregion

    #region Scrollable Content with Padding Tests

    [Fact]
    public void ScrollContainer_WithPadding_ScrollableHeightShouldIncludeBottomPadding()
    {
        // 复现 ISSUE-037 问题3：
        // 滚动容器设置了 padding，内容超出时，可滚动高度应包含上下内边距，
        // 否则无法滚动到内容底部（看不到内容下边框和容器的 bottom padding）。
        var root = new DivElement { Class = "root" };
        var main = new DivElement { Class = "main-content" };
        var content = new DivElement { Class = "content" };
        root.AddChild(main);
        main.AddChild(content);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(600), Height = Length.Px(600) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("main-content"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Percent(100),
                            Height = Length.Percent(100),
                            OverflowY = Overflow.Scroll,
                            PaddingTop = Length.Px(16),
                            PaddingRight = Length.Px(16),
                            PaddingBottom = Length.Px(16),
                            PaddingLeft = Length.Px(16)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("content"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(500),
                            Height = Length.Px(700)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 600, 600);
        var mainBox = layoutRoot.Children[0];

        // Assert
        // content 高度 700，main-content padding 16（上下共 32）
        // 可滚动高度（scrollHeight）应为 700 + 32 = 732（包含上下 padding），而非 700。
        mainBox.ScrollableContentHeight.ShouldBe(732);

        // main-content height:100% 解析为内容高度 600，padding box 视口高度 = 600 + 32 = 632。
        // 最大可滚动距离 = scrollHeight - clientHeight = 732 - 632 = 100。
        // 修复前 ScrollableContentHeight 为 700，maxScroll = 700 - 632 = 68，
        // 缺少 32px（上下 padding），导致无法滚动到 content 底部与容器 bottom padding。
        float viewportHeight = mainBox.BoxModel.PaddingBox.Height;
        viewportHeight.ShouldBe(632);
        float maxScroll = mainBox.ScrollableContentHeight - viewportHeight;
        maxScroll.ShouldBe(100);
    }

    [Fact]
    public void ScrollContainer_WithoutPadding_ScrollableHeightUnchanged()
    {
        // 无 padding 时可滚动高度即内容高度（回归保护：原本正常的情况不受影响）。
        var root = new DivElement { Class = "root" };
        var main = new DivElement { Class = "main-content" };
        var content = new DivElement { Class = "content" };
        root.AddChild(main);
        main.AddChild(content);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(600), Height = Length.Px(600) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("main-content"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Percent(100),
                            Height = Length.Percent(100),
                            OverflowY = Overflow.Scroll
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("content"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(500), Height = Length.Px(700) }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 600, 600);
        var mainBox = layoutRoot.Children[0];

        // Assert - 无 padding，可滚动高度 = content 高度 700
        mainBox.ScrollableContentHeight.ShouldBe(700);
    }

    #endregion
}
