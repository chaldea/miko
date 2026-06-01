using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// 行内/行内块元素同时包含文本内容与子元素时的布局测试。
/// 复现并验证 ISSUE-037：button 同时包含文本和子元素时宽度异常。
/// </summary>
public class InlineMixedContentTests
{
    private readonly LayoutEngine _layoutEngine = new();

    private static List<StyleSheet> DefaultStyleSheets() => new()
    {
        new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new TagSelector("button"),
                    Style = new Style { Display = Display.InlineBlock }
                },
                new StyleRule
                {
                    Selector = new TagSelector("span"),
                    Style = new Style { Display = Display.Inline }
                }
            }
        }
    };

    /// <summary>
    /// 测量一段文本在默认按钮字体下的宽度，用于断言比较。
    /// </summary>
    private float MeasureWidth(string text)
    {
        var probe = new SpanElement { TextContent = text };
        var layout = _layoutEngine.Layout(probe, DefaultStyleSheets(), 800, 600);
        return layout.BoxModel.Content.Width;
    }

    [Fact]
    public void Button_WithOnlyText_ShouldSizeToText()
    {
        // <button>Notifications</button>
        var button = new ButtonElement { TextContent = "Notifications" };

        var layout = _layoutEngine.Layout(button, DefaultStyleSheets(), 800, 600);

        layout.BoxModel.Content.Width.ShouldBeGreaterThan(0,
            "Button with only text should size to its text content");
    }

    [Fact]
    public void Button_WithOnlyChildSpan_ShouldSizeToChild()
    {
        // <button><span>99+</span></button>
        var button = new ButtonElement();
        button.AddChild(new SpanElement { TextContent = "99+" });

        var layout = _layoutEngine.Layout(button, DefaultStyleSheets(), 800, 600);

        layout.BoxModel.Content.Width.ShouldBeGreaterThan(0,
            "Button with only a child should size to the child");
    }

    [Fact]
    public void Button_WithTextAndChild_ShouldIncludeBothInWidth()
    {
        // <button>Notifications<span>99+</span></button>
        var button = new ButtonElement { TextContent = "Notifications" };
        var span = new SpanElement { TextContent = "99+" };
        button.AddChild(span);

        var layout = _layoutEngine.Layout(button, DefaultStyleSheets(), 800, 600);

        float textWidth = MeasureWidth("Notifications");
        var spanBox = layout.Children[0];

        // ISSUE-037: 之前 button 宽度只等于 span 的宽度，忽略了 "Notifications" 文本。
        // 正确行为：内容宽度应当同时包含文本宽度与 span 宽度。
        layout.BoxModel.Content.Width.ShouldBeGreaterThan(spanBox.BoxModel.MarginBox.Width,
            "Button content width must include both the own text and the child span");
        layout.BoxModel.Content.Width.ShouldBe(textWidth + spanBox.BoxModel.MarginBox.Width, 0.5f,
            "Button content width should be the sum of its text width and the child span width");
    }

    [Fact]
    public void Button_WithTextAndChild_ChildShouldNotOverlapText()
    {
        // 子元素应当排在文本之后，而不是从内容区左上角开始（否则会与文本重叠）。
        var button = new ButtonElement { TextContent = "Notifications" };
        var span = new SpanElement { TextContent = "99+" };
        button.AddChild(span);

        var layout = _layoutEngine.Layout(button, DefaultStyleSheets(), 800, 600);

        float textWidth = MeasureWidth("Notifications");
        var spanBox = layout.Children[0];

        spanBox.BoxModel.MarginBox.Left.ShouldBe(
            layout.BoxModel.Content.Left + textWidth, 0.5f,
            "Child span should start after the button's own text, not overlap it");
    }
}
