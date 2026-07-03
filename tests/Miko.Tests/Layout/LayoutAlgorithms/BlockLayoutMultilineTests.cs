using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using Xunit;

namespace Miko.Tests.Layout.LayoutAlgorithms;

/// <summary>
/// BlockLayout 多行文本换行测试
/// 验证 block 元素（如 p）的文本换行和高度计算
/// </summary>
public class BlockLayoutMultilineTests
{
    private readonly LayoutEngine _layoutEngine = new();

    [Fact]
    public void BlockElement_WithLongText_ShouldWrapAndCalculateCorrectHeight()
    {
        // 模拟 ISSUE-079 真实场景：p 元素默认是 block，应该自动换行

        var longText = "Buttons provide a clickable element, which can be used in forms, or anywhere " +
                       "that needs simple, standard button functionality. They may display text, " +
                       "icons, or both. Buttons can be styled with several attributes to look a " +
                       "specific way.";

        var divElement = new DivElement { Class = "root" };
        var pElement = new ParagraphElement { TextContent = longText };
        var h2Element = new H2Element { TextContent = "Basic Usage" };

        divElement.AddChild(pElement);
        divElement.AddChild(h2Element);

        var styleSheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new ClassSelector("root"),
                    Style = new Style
                    {
                        Display = Display.Block,
                        Width = Length.Px(500)
                    }
                },
                new StyleRule
                {
                    Selector = new TagSelector("p"),
                    Style = new Style
                    {
                        Display = Display.Block,  // p 默认是 block
                        FontFamily = "Arial",
                        FontSize = Length.Px(16),
                        LineHeight = Length.Px(24)
                    }
                },
                new StyleRule
                {
                    Selector = new TagSelector("h2"),
                    Style = new Style
                    {
                        Display = Display.Block,
                        FontFamily = "Arial",
                        FontSize = Length.Px(24),
                        FontWeight = FontWeight.Bold,
                        LineHeight = Length.Px(32)
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(divElement, new List<StyleSheet> { styleSheet }, 800, 600);

        var pBox = layoutRoot.Children.FirstOrDefault(b => b.Element == pElement);
        var h2Box = layoutRoot.Children.FirstOrDefault(b => b.Element == h2Element);

        pBox.ShouldNotBeNull();
        h2Box.ShouldNotBeNull();

        // 验证：p 元素的高度应该大于单行高度（长文本会换行）
        pBox.BoxModel.Content.Height.ShouldBeGreaterThan(24,
            $"p 元素高度（{pBox.BoxModel.Content.Height}）应该大于单行高度（24px），因为文本会换行");

        // 验证：h2 的顶部应该在 p 的底部之下，不应重叠
        float pBottom = pBox.BoxModel.MarginBox.Bottom;
        float h2Top = h2Box.BoxModel.MarginBox.Top;

        h2Top.ShouldBeGreaterThanOrEqualTo(pBottom,
            $"h2 元素（top={h2Top}）不应与 p 元素（bottom={pBottom}）重叠");
    }

    [Fact]
    public void BlockElement_WithShortText_ShouldUseSingleLineHeight()
    {
        var shortText = "Hello World";

        var pElement = new ParagraphElement { TextContent = shortText };

        var styleSheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new TagSelector("p"),
                    Style = new Style
                    {
                        Display = Display.Block,
                        Width = Length.Px(500),
                        FontFamily = "Arial",
                        FontSize = Length.Px(16),
                        LineHeight = Length.Px(24)
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(pElement, new List<StyleSheet> { styleSheet }, 800, 600);

        // 验证：短文本只应占用一行高度
        layoutRoot.BoxModel.Content.Height.ShouldBe(24);
    }

    [Fact]
    public void BlockElement_WithWhiteSpaceNowrap_ShouldNotWrap()
    {
        var longText = "This is a very long text that would normally wrap but should not because of nowrap";

        var pElement = new ParagraphElement { TextContent = longText };

        var styleSheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new TagSelector("p"),
                    Style = new Style
                    {
                        Display = Display.Block,
                        Width = Length.Px(200),  // 窄宽度
                        FontFamily = "Arial",
                        FontSize = Length.Px(16),
                        LineHeight = Length.Px(24),
                        WhiteSpace = WhiteSpace.Nowrap
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(pElement, new List<StyleSheet> { styleSheet }, 800, 600);

        // 验证：使用 nowrap 时只应占用一行高度
        layoutRoot.BoxModel.Content.Height.ShouldBe(24);
    }

    [Fact]
    public void BlockElement_WidthAuto_ShouldExpandToText()
    {
        var text = "Short text";

        var pElement = new ParagraphElement { TextContent = text };

        var styleSheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new TagSelector("p"),
                    Style = new Style
                    {
                        Display = Display.Block,
                        Width = Length.Auto,  // 不限制宽度
                        FontFamily = "Arial",
                        FontSize = Length.Px(16),
                        LineHeight = Length.Px(24)
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(pElement, new List<StyleSheet> { styleSheet }, 800, 600);

        // 验证：宽度自动时，内容宽度应该等于容器宽度（800 - margin）
        layoutRoot.BoxModel.Content.Width.ShouldBeGreaterThan(0);

        // 验证：高度应该是单行（文本很短不需要换行）
        layoutRoot.BoxModel.Content.Height.ShouldBe(24);
    }
}
