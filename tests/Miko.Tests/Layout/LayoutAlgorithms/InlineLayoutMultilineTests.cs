using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using Xunit;

namespace Miko.Tests.Layout.LayoutAlgorithms;

/// <summary>
/// InlineLayout 多行文本换行测试
/// 复现并验证 ISSUE-079：p 元素自动换行导致元素高度计算错误
/// </summary>
public class InlineLayoutMultilineTests
{
    private readonly LayoutEngine _layoutEngine = new();

    [Fact]
    public void Should_NotOverlapWithNextElement_WhenMultilineTextWraps()
    {
        // 模拟 ISSUE-079 场景：p 元素和 h2 元素不应重叠

        // 创建父容器
        var divElement = new DivElement { Class = "root" };

        var longText = "Buttons provide a clickable element, which can be used in forms, or anywhere " +
                       "that needs simple, standard button functionality. They may display text, " +
                       "icons, or both. Buttons can be styled with several attributes to look a " +
                       "specific way.";

        var pElement = new ParagraphElement { TextContent = longText };
        var h2Element = new H2Element { TextContent = "Basic Usage" };

        divElement.AddChild(pElement);
        divElement.AddChild(h2Element);

        // 样式配置
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
                        Width = Length.Px(500),
                        Height = Length.Auto
                    }
                },
                new StyleRule
                {
                    Selector = new TagSelector("p"),
                    Style = new Style
                    {
                        Display = Display.Inline,
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

        // 获取 p 和 h2 的布局盒子
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
    public void Should_CalculateSingleLineHeight_WhenTextFitsInOneRow()
    {
        // 短文本应该只占一行
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
                        Display = Display.Inline,
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
    public void Should_WrapTextAndCalculateCorrectHeight_WhenTextExceedsContainerWidth()
    {
        // 准备长文本
        var longText = "Buttons provide a clickable element, which can be used in forms, or anywhere " +
                       "that needs simple, standard button functionality. They may display text, " +
                       "icons, or both. Buttons can be styled with several attributes to look a " +
                       "specific way.";

        // 创建父容器限制宽度
        var divElement = new DivElement();
        var pElement = new ParagraphElement { TextContent = longText };
        divElement.AddChild(pElement);

        var styleSheet = new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new TagSelector("div"),
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
                        Display = Display.Inline,
                        FontFamily = "Arial",
                        FontSize = Length.Px(16),
                        LineHeight = Length.Px(24)
                    }
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(divElement, new List<StyleSheet> { styleSheet }, 800, 600);
        var pBox = layoutRoot.Children.FirstOrDefault(b => b.Element == pElement);

        pBox.ShouldNotBeNull();

        // 验证：文本应该换行，高度应该大于单行高度（24px）
        // 长文本在 500px 宽度下至少需要 3-4 行
        pBox.BoxModel.Content.Height.ShouldBeGreaterThan(24 * 2,
            $"长文本高度（{pBox.BoxModel.Content.Height}）应该至少是 2 行（48px）");
    }
}
