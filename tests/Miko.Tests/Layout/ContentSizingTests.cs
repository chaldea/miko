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
