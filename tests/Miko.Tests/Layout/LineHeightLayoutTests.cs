using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-041 回归测试：
/// 验证 line-height 设置（含无单位 number、px、em、rem）能正确应用到带文本的元素，
/// 使其内容高度按 line-height 解析（如 1.5 × 16px = 24px），而非仅取字体自然度量。
/// </summary>
public class LineHeightLayoutTests
{
    private readonly LayoutEngine _layoutEngine = new();

    /// <summary>
    /// span（display:flex、字体 16px、line-height 1.5）的内容高度应为 24px，
    /// 而非字体度量自然行高（约 18px）。这是 ISSUE-041 中复现的核心场景。
    /// </summary>
    [Fact]
    public void FlexSpan_WithUnitlessLineHeight_ShouldUseLineHeightForOwnText()
    {
        // Arrange
        var span = new SpanElement { Class = "input-group-text", TextContent = "@" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".input-group-text"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                FontSize = Length.Rem(1),         // 16px
                LineHeight = Length.Number(1.5f), // 1.5 × 字体大小 = 24px
            }
        });

        // Act
        var box = _layoutEngine.Layout(span, new List<StyleSheet> { sheet }, 800, 600);

        // Assert: 内容高度应为 1.5 × 16 = 24px
        box.BoxModel.Content.Height.ShouldBe(24f, 0.01f,
            "Span content height should follow line-height 1.5 × font-size 16px = 24px, not font metrics (~18px)");
    }

    /// <summary>
    /// 带 padding 与 border 的 flex span 应正确组合：
    /// line-height 决定内容高度，padding/border 在外撑高 border-box。
    /// </summary>
    [Fact]
    public void FlexSpan_WithLineHeightAndPadding_ShouldComposeBorderBox()
    {
        var span = new SpanElement { Class = "input-group-text", TextContent = "@" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["*"] = new() { BoxSizing = BoxSizing.BorderBox },
            [".input-group-text"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                Padding = new Padding(Length.Rem(0.375f), Length.Rem(0.75f)), // 6px / 12px
                FontSize = Length.Rem(1),         // 16px
                LineHeight = Length.Number(1.5f), // 24px
                BorderWidth = Length.Px(1),
                BorderStyle = BorderStyle.Solid,
                BorderColor = Color.Gray,
            }
        });

        var box = _layoutEngine.Layout(span, new List<StyleSheet> { sheet }, 800, 600);

        // 内容高度 = line-height = 24px
        box.BoxModel.Content.Height.ShouldBe(24f, 0.01f);
        // border-box 高度 = 内容 24 + padding(6+6) + border(1+1) = 38px
        box.BoxModel.BorderBox.Height.ShouldBe(38f, 0.01f);
    }

    /// <summary>
    /// 块级元素（div）的 line-height 也应生效。
    /// </summary>
    [Fact]
    public void BlockDiv_WithLineHeight_ShouldUseLineHeightForOwnText()
    {
        var div = new DivElement { Class = "lh", TextContent = "Hello" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".lh"] = new()
            {
                FontSize = Length.Px(16),
                LineHeight = Length.Px(30), // 显式 30px
            }
        });

        var box = _layoutEngine.Layout(div, new List<StyleSheet> { sheet }, 800, 600);

        box.BoxModel.Content.Height.ShouldBe(30f, 0.01f,
            "Div content height should equal explicit line-height 30px");
    }

    /// <summary>
    /// 行内元素（display:inline）也应支持 line-height。
    /// </summary>
    [Fact]
    public void InlineSpan_WithLineHeight_ShouldUseLineHeightForOwnText()
    {
        var span = new SpanElement { Class = "lh", TextContent = "Hello" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".lh"] = new()
            {
                Display = Display.Inline,
                FontSize = Length.Px(16),
                LineHeight = Length.Number(2f), // 32px
            }
        });

        var box = _layoutEngine.Layout(span, new List<StyleSheet> { sheet }, 800, 600);

        box.BoxModel.Content.Height.ShouldBe(32f, 0.01f,
            "Inline span content height should follow line-height 2 × 16px = 32px");
    }

    /// <summary>
    /// em 单位的 line-height 应按元素自身字体大小解析。
    /// </summary>
    [Fact]
    public void LineHeight_WithEmUnit_ShouldResolveAgainstOwnFontSize()
    {
        var span = new SpanElement { Class = "lh", TextContent = "X" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".lh"] = new()
            {
                Display = Display.Flex,
                FontSize = Length.Px(20),
                LineHeight = Length.Em(1.5f), // 1.5em = 30px at 20px font
            }
        });

        var box = _layoutEngine.Layout(span, new List<StyleSheet> { sheet }, 800, 600);

        box.BoxModel.Content.Height.ShouldBe(30f, 0.01f,
            "line-height 1.5em should resolve against own font-size (20px) = 30px");
    }

    /// <summary>
    /// 不设置 line-height 时回退到字体自然度量，行为不变（无回归）。
    /// </summary>
    [Fact]
    public void LineHeight_NotSet_ShouldFallbackToFontMetrics()
    {
        var span = new SpanElement { Class = "lh", TextContent = "X" };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".lh"] = new()
            {
                Display = Display.Flex,
                FontSize = Length.Px(16),
                // 不设置 LineHeight
            }
        });

        var box = _layoutEngine.Layout(span, new List<StyleSheet> { sheet }, 800, 600);

        // 字体自然度量约为 18.4px（Arial @ 16px），应大于 0 且小于 24（1.5 ratio）
        box.BoxModel.Content.Height.ShouldBeGreaterThan(0f);
        box.BoxModel.Content.Height.ShouldBeLessThan(24f,
            "Without explicit line-height, height should follow font metrics (smaller than 1.5×fs)");
    }

    /// <summary>
    /// ComputedStyle 应保留原始 LineHeight 值（含 number 单位），不在级联阶段折算为 px，
    /// 以便布局阶段按元素自身字体大小解析（与 font-size 的 em 解析时序一致）。
    /// </summary>
    [Fact]
    public void ComputedStyle_LineHeight_ShouldPreserveNumberUnit()
    {
        var resolver = new StyleResolver();
        var span = new SpanElement { Class = "lh" };

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("lh"), new Style
        {
            FontSize = Length.Px(16),
            LineHeight = Length.Number(1.5f),
        });

        var computed = resolver.Resolve(span, new List<StyleSheet> { sheet });

        // LineHeight 在 ComputedStyle 中保留 number 单位（值 = 1.5）
        // ToPixels(0, 16) 应得到 24
        computed.LineHeight.ToPixels(0, computed.FontSize.Value).ShouldBe(24f, 0.01f);
    }
}
