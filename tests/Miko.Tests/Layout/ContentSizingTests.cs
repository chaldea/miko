using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// 内容尺寸计算测试 - 验证元素没有设置 Width/Height 时，应该根据内容计算尺寸
/// </summary>
public class ContentSizingTests
{
    private readonly LayoutEngine _layoutEngine = new();

    #region Block Layout Content Sizing

    [Fact]
    public void BlockLayout_WithTextContent_NoWidthHeight_ShouldHaveNonZeroHeight()
    {
        // Arrange: 一个只有文本内容、没有设置宽高的 block 元素
        var root = new DivElement { TextContent = "Hello World" };
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

        // Assert: 高度应该大于 0（根据文本内容计算）
        layoutRoot.BoxModel.Content.Height.ShouldBeGreaterThan(0,
            "Block element with text content should have non-zero height when Height is auto");
    }

    [Fact]
    public void BlockLayout_EmptyElement_NoWidthHeight_ShouldHaveZeroHeight()
    {
        // Arrange: 一个没有子元素和文本内容的空 block 元素
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

        // Assert: 空元素高度应该为 0
        layoutRoot.BoxModel.Content.Height.ShouldBe(0,
            "Empty block element should have zero height when Height is auto");
    }

    [Fact]
    public void BlockLayout_NestedChildWithText_ParentShouldShrinkToFitChild()
    {
        // Arrange: 父元素包含一个有文本内容的子元素，父元素没有设置高度
        var parent = new DivElement { Class = "parent" };
        var child = new DivElement { Class = "child", TextContent = "Hello World" };
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
                        Style = new Style { Display = Display.Block }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style { Display = Display.Block }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(parent, styleSheets, 800, 600);

        // Assert: 父元素高度应该等于子元素高度
        var childBox = layoutRoot.Children[0];
        childBox.BoxModel.Content.Height.ShouldBeGreaterThan(0,
            "Child element with text content should have non-zero height");
        layoutRoot.BoxModel.Content.Height.ShouldBe(childBox.BoxModel.MarginBox.Height,
            "Parent should shrink-to-fit its children when Height is auto");
    }

    [Fact]
    public void BlockLayout_PercentWidthChild_InShrinkToFitParent_ShouldContributeContentWidth()
    {
        // Arrange: flex row > auto 宽的 block 项目 > width:100% 的 block 子盒 > 定宽孙盒。
        // 回归（ion-animation）：百分比宽度针对不确定包含块应退化为 auto（与 Flex/Grid/InlineLayout
        // 的 ISSUE-077 处理一致）。退化缺失时，shrink-to-fit 测量遍把 width:100% 子盒按 0 求值，
        // 父盒内容宽度塌缩为 0（Ionic iOS back-button 的 .button-native 因此不可命中——
        // 兄弟标题盒占满整行盖住 0 宽按钮）。
        var grandchild = new DivElement { Class = "grandchild" };
        var child = new DivElement { Class = "child" };
        child.AddChild(grandchild);
        var item = new DivElement { Class = "item" };
        item.AddChild(child);
        var root = new DivElement { Class = "root" };
        root.AddChild(item);

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
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Width = Length.Px(400),
                            Height = Length.Px(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item"),
                        Style = new Style { Display = Display.Block }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style { Display = Display.Block, Width = Length.Percent(100) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("grandchild"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(62), Height = Length.Px(20) }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert: auto 宽的 flex 项目收缩到内容宽（62），width:100% 的子盒再填满定型后的父宽。
        var itemBox = layoutRoot.Children[0];
        var childBox = itemBox.Children[0];
        itemBox.BoxModel.Content.Width.ShouldBe(62f, 0.5f,
            "Percent-width child must contribute its content width to the shrink-to-fit parent");
        childBox.BoxModel.Content.Width.ShouldBe(62f, 0.5f,
            "width:100% resolves against the parent's definite width on the real layout pass");
    }

    [Fact]
    public void BlockLayout_PercentHeightChild_AgainstAutoHeightParent_ShouldDegradeToAuto()
    {
        // Arrange: 复现 ISSUE-109（ion-back-button 高度链）：
        //   container(48×48 定高) → host(height:auto, min-height:48) → native(height:100%, min-height:48)
        //   → inner(display:flex, height:100%) → icon(24×24)。
        // 百分比高度针对不确定包含块（auto 高度的 host）应退化为 auto——由内容决定高度，
        // 而不是折算为 0 并把 0 作为"确定基准"继续传给子孙（Flex/Grid/InlineLayout 已有同款
        // 退化，见 ISSUE-077；BlockLayout 此前只有宽度方向的退化）。
        // 浏览器审计结果：native 48 高（min-height 抬升），inner 24 高（内容高度）。
        var icon = new DivElement { Class = "icon" };
        var inner = new DivElement { Class = "inner" };
        inner.AddChild(icon);
        var native = new DivElement { Class = "native" };
        native.AddChild(inner);
        var host = new DivElement { Class = "host" };
        host.AddChild(native);
        var container = new DivElement { Class = "container" };
        container.AddChild(host);

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
                            Width = Length.Px(48),
                            Height = Length.Px(48)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("host"),
                        Style = new Style { Display = Display.Block, MinHeight = Length.Px(48) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("native"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Height = Length.Percent(100),
                            MinHeight = Length.Px(48)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("inner"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Height = Length.Percent(100)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("icon"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(24),
                            Height = Length.Px(24)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(container, styleSheets, 800, 600);

        // Assert
        var hostBox = layoutRoot.Children[0];
        var nativeBox = hostBox.Children[0];
        var innerBox = nativeBox.Children[0];

        innerBox.BoxModel.Content.Height.ShouldBe(24f, 0.5f,
            "height:100% against an auto-height (indefinite) ancestor must degrade to auto — content height");
        nativeBox.BoxModel.Content.Height.ShouldBe(48f, 0.5f,
            "native height degrades to auto (content 24) then min-height clamps it to 48");
        hostBox.BoxModel.Content.Height.ShouldBe(48f, 0.5f,
            "host height is content (48) — its min-height is already satisfied");
    }

    [Fact]
    public void BlockLayout_PercentHeightChild_AgainstDefiniteParent_ShouldResolvePercent()
    {
        // Arrange: 定高链上的百分比高度必须照常解析（守护退化只在不确定包含块时触发）：
        //   container(48 定高) → native(height:100%) → inner(height:100%, display:block)。
        var inner = new DivElement { Class = "inner" };
        var native = new DivElement { Class = "native" };
        native.AddChild(inner);
        var container = new DivElement { Class = "container" };
        container.AddChild(native);

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
                            Width = Length.Px(48),
                            Height = Length.Px(48)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("native"),
                        Style = new Style { Display = Display.Block, Height = Length.Percent(100) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("inner"),
                        Style = new Style { Display = Display.Block, Height = Length.Percent(100) }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(container, styleSheets, 800, 600);

        // Assert
        var nativeBox = layoutRoot.Children[0];
        var innerBox = nativeBox.Children[0];
        nativeBox.BoxModel.Content.Height.ShouldBe(48f, 0.5f,
            "height:100% resolves against the definite containing block");
        innerBox.BoxModel.Content.Height.ShouldBe(48f, 0.5f,
            "the resolved definite height is passed down as the percentage base for descendants");
    }

    #endregion

    #region Inline Layout Content Sizing

    [Fact]
    public void InlineLayout_WithTextContent_NoWidthHeight_ShouldHaveNonZeroSize()
    {
        // Arrange: 一个只有文本内容、没有设置宽高的 inline 元素
        var root = new SpanElement { TextContent = "Hello World" };
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("span"),
                        Style = new Style { Display = Display.Inline }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert: 宽度和高度都应该大于 0（根据文本内容计算）
        layoutRoot.BoxModel.Content.Width.ShouldBeGreaterThan(0,
            "Inline element with text content should have non-zero width when Width is auto");
        layoutRoot.BoxModel.Content.Height.ShouldBeGreaterThan(0,
            "Inline element with text content should have non-zero height when Height is auto");
    }

    [Fact]
    public void InlineLayout_EmptyElement_NoWidthHeight_ShouldHaveZeroSize()
    {
        // Arrange: 一个没有子元素和文本内容的空 inline 元素
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
                        Style = new Style { Display = Display.Inline }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert: 空元素宽高应该为 0
        layoutRoot.BoxModel.Content.Width.ShouldBe(0,
            "Empty inline element should have zero width when Width is auto");
        layoutRoot.BoxModel.Content.Height.ShouldBe(0,
            "Empty inline element should have zero height when Height is auto");
    }

    [Fact]
    public void InlineLayout_LongText_ShouldHaveWidthMatchingText()
    {
        // Arrange: 一个有较长文本的 inline 元素
        var shortText = new SpanElement { TextContent = "Hi", Class = "short" };
        var longText = new SpanElement { TextContent = "Hello World, this is a longer text", Class = "long" };

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("span"),
                        Style = new Style { Display = Display.Inline }
                    }
                }
            }
        };

        // Act
        var shortLayout = _layoutEngine.Layout(shortText, styleSheets, 800, 600);
        var longLayout = _layoutEngine.Layout(longText, styleSheets, 800, 600);

        // Assert: 长文本的宽度应该大于短文本
        longLayout.BoxModel.Content.Width.ShouldBeGreaterThan(shortLayout.BoxModel.Content.Width,
            "Longer text should have greater width");
    }

    #endregion

    #region Flex Layout Content Sizing

    [Fact]
    public void FlexLayout_ChildWithTextContent_NoWidthHeight_ShouldHaveNonZeroSize()
    {
        // Arrange: Flex 容器包含一个有文本内容的子元素
        var container = new DivElement { Class = "container" };
        var child = new DivElement { Class = "child", TextContent = "Hello World" };
        container.AddChild(child);

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
                            FlexDirection = FlexDirection.Row
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style { Display = Display.Block }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(container, styleSheets, 800, 600);

        // Assert: 子元素宽高应该大于 0
        var childBox = layoutRoot.Children[0];
        childBox.BoxModel.Content.Width.ShouldBeGreaterThan(0,
            "Flex child with text content should have non-zero width when Width is auto");
        childBox.BoxModel.Content.Height.ShouldBeGreaterThan(0,
            "Flex child with text content should have non-zero height when Height is auto");
    }

    [Fact]
    public void FlexLayout_Row_ChildrenWithText_ShouldHaveDifferentWidthsBasedOnContent()
    {
        // Arrange: Flex 行容器包含两个不同文本长度的子元素
        var container = new DivElement { Class = "container" };
        var child1 = new DivElement { Class = "child", TextContent = "Hi" };
        var child2 = new DivElement { Class = "child", TextContent = "Hello World, this is longer" };
        container.AddChild(child1);
        container.AddChild(child2);

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
                            FlexDirection = FlexDirection.Row
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style { Display = Display.Block }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(container, styleSheets, 800, 600);

        // Assert: 第二个子元素（长文本）的宽度应该大于第一个子元素（短文本）
        var childBox1 = layoutRoot.Children[0];
        var childBox2 = layoutRoot.Children[1];

        childBox1.BoxModel.Content.Width.ShouldBeGreaterThan(0,
            "First child should have non-zero width");
        childBox2.BoxModel.Content.Width.ShouldBeGreaterThan(0,
            "Second child should have non-zero width");
        childBox2.BoxModel.Content.Width.ShouldBeGreaterThan(childBox1.BoxModel.Content.Width,
            "Child with longer text should have greater width");
    }

    [Fact]
    public void FlexLayout_Column_ContainerShouldShrinkToFitChildren()
    {
        // Arrange: Flex 列容器包含两个有文本内容的子元素
        var container = new DivElement { Class = "container" };
        var child1 = new DivElement { Class = "child", TextContent = "Line 1" };
        var child2 = new DivElement { Class = "child", TextContent = "Line 2" };
        container.AddChild(child1);
        container.AddChild(child2);

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
                            FlexDirection = FlexDirection.Column
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("child"),
                        Style = new Style { Display = Display.Block }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(container, styleSheets, 800, 600);

        // Assert: 容器高度应该等于所有子元素高度之和
        var childBox1 = layoutRoot.Children[0];
        var childBox2 = layoutRoot.Children[1];

        var totalChildrenHeight = childBox1.BoxModel.MarginBox.Height + childBox2.BoxModel.MarginBox.Height;
        layoutRoot.BoxModel.Content.Height.ShouldBe(totalChildrenHeight,
            "Flex column container height should equal sum of children heights when Height is auto");
    }

    #endregion

    #region InlineBlock Content Sizing

    [Fact]
    public void InlineBlockLayout_WithTextContent_NoWidthHeight_ShouldHaveNonZeroSize()
    {
        // Arrange: 一个只有文本内容、没有设置宽高的 inline-block 元素
        var root = new SpanElement { TextContent = "Hello World" };
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("span"),
                        Style = new Style { Display = Display.InlineBlock }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // Assert: 宽度和高度都应该大于 0（根据文本内容计算）
        layoutRoot.BoxModel.Content.Width.ShouldBeGreaterThan(0,
            "InlineBlock element with text content should have non-zero width when Width is auto");
        layoutRoot.BoxModel.Content.Height.ShouldBeGreaterThan(0,
            "InlineBlock element with text content should have non-zero height when Height is auto");
    }

    #endregion

    #region Font Size Affecting Content Size

    [Fact]
    public void ContentSizing_LargerFontSize_ShouldHaveLargerHeight()
    {
        // Arrange: 两个相同文本的元素，一个字体大、一个字体小
        var smallFont = new SpanElement { TextContent = "Hello", Class = "small" };
        var largeFont = new SpanElement { TextContent = "Hello", Class = "large" };

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("small"),
                        Style = new Style
                        {
                            Display = Display.Inline,
                            FontSize = Length.Px(12)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("large"),
                        Style = new Style
                        {
                            Display = Display.Inline,
                            FontSize = Length.Px(32)
                        }
                    }
                }
            }
        };

        // Act
        var smallLayout = _layoutEngine.Layout(smallFont, styleSheets, 800, 600);
        var largeLayout = _layoutEngine.Layout(largeFont, styleSheets, 800, 600);

        // Assert: 大字体的高度应该大于小字体
        largeLayout.BoxModel.Content.Height.ShouldBeGreaterThan(smallLayout.BoxModel.Content.Height,
            "Larger font size should result in larger content height");
        largeLayout.BoxModel.Content.Width.ShouldBeGreaterThan(smallLayout.BoxModel.Content.Width,
            "Larger font size should result in larger content width");
    }

    #endregion
}
