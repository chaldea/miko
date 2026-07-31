using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-110：行内流中文字排版问题（复现 examples/DebugDemo）。
///
/// 问题2：span 内文本与 &lt;code&gt; 等行内元素交错时，排版无视宽度约束、显示顺序错乱。
/// 问题3：中英文混排时，中文长串没有断行机会，宽度约束计算错误（中文宽度与英文不同）。
/// </summary>
public class InlineRunWrappingTests
{
    private readonly LayoutEngine _layoutEngine = new();

    private const float DescriptionWidth = 300f;
    private const float FontSize = 18f; // Rem(1.125) at 16px root

    private static List<StyleSheet> DemoStyleSheets() => new()
    {
        new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new() { Selector = new ClassSelector("component-details"), Style = new Style { Width = Length.Px(DescriptionWidth) } },
                new()
                {
                    Selector = new ClassSelector("component-description"),
                    Style = new Style
                    {
                        Color = (Color)"#3e4a58",
                        FontSize = Length.Px(FontSize),
                        LineHeight = Length.Number(1.4f),
                        WhiteSpace = WhiteSpace.Normal,
                        PaddingBottom = Length.Px(16),
                    }
                },
            }
        }
    };

    private static DivElement BuildDescription(params Element[] children)
    {
        var description = new DivElement { Class = "component-description" };
        foreach (var child in children)
        {
            description.AddChild(child);
        }

        var details = new DivElement { Class = "component-details" };
        details.AddChild(description);
        return details;
    }

    private static LayoutBox FindBox(LayoutBox root, Element element)
    {
        return FindBoxOrNull(root, element)
            ?? throw new System.InvalidOperationException("element not in layout tree");
    }

    private static LayoutBox? FindBoxOrNull(LayoutBox root, Element element)
    {
        if (ReferenceEquals(root.Element, element)) return root;
        foreach (var child in root.Children)
        {
            var found = FindBoxOrNull(child, element);
            if (found != null) return found;
        }
        return null;
    }

    [Fact]
    public void SpanWithInlineCode_ShouldWrapWithinContainerWidth()
    {
        // <span>Accordions provide ... All <code>ion-accordion</code> components should be
        // grouped inside <code>ion-accordion-group</code> components.</span>
        var span = new SpanElement();
        span.AddChild(new TextNode("Accordions provide collapsible sections in your content to reduce vertical space while providing a way of organizing and grouping information. All "));
        var code1 = new CodeElement();
        code1.AddChild(new TextNode("ion-accordion"));
        span.AddChild(code1);
        span.AddChild(new TextNode(" components should be grouped inside "));
        var code2 = new CodeElement();
        code2.AddChild(new TextNode("ion-accordion-group"));
        span.AddChild(code2);
        span.AddChild(new TextNode(" components."));

        var details = BuildDescription(span);
        var layout = _layoutEngine.Layout(details, DemoStyleSheets(), 500, 500);

        var spanBox = FindBox(layout, span);

        // 所有在流子盒（文本片段与 code）的并集不应超出 span 内容宽度（300px）。
        float contentLeft = spanBox.BoxModel.Content.Left;
        foreach (var child in spanBox.Children)
        {
            if (child.Element is TextNode) continue; // 文本节点的包围盒在下方单独断言
            child.BoxModel.MarginBox.Right.ShouldBeLessThanOrEqualTo(contentLeft + DescriptionWidth + 0.5f,
                $"{child.Element.TagName} 溢出了容器宽度");
        }

        // code 元素不应与任何文本片段重叠（顺序不错乱）。文本节点经行内断行后
        // 内容盒是全部行片段的并集（天然包含后续行的行内元素），须逐片段断言。
        var code1Box = FindBox(layout, code1).BoxModel.MarginBox;
        var code2Box = FindBox(layout, code2).BoxModel.MarginBox;
        foreach (var child in spanBox.Children)
        {
            if (child.Element is not TextNode node || string.IsNullOrEmpty(node.Text)) continue;
            var label = node.Text[..System.Math.Min(12, node.Text.Length)];
            node.LayoutFragments.ShouldNotBeNull("行内流中的文本节点应已被行内格式化上下文断行");
            foreach (var frag in node.LayoutFragments)
            {
                var fragRect = new RectF(
                    child.BoxModel.Content.X + frag.X,
                    child.BoxModel.Content.Y + frag.Y,
                    frag.Width,
                    frag.Height);
                Overlaps(fragRect, code1Box).ShouldBeFalse($"文本 \"{label}...\" 的片段与 code1 重叠");
                Overlaps(fragRect, code2Box).ShouldBeFalse($"文本 \"{label}...\" 的片段与 code2 重叠");
            }
        }
    }

    [Fact]
    public void SpanWithInlineCode_ShouldKeepVisualOrder()
    {
        // text1 <code>c1</code> text2：三者应按 DOM 顺序从左到右排列（同一行内）。
        var span = new SpanElement();
        span.AddChild(new TextNode("All "));
        var code1 = new CodeElement();
        code1.AddChild(new TextNode("ion-accordion"));
        span.AddChild(code1);
        span.AddChild(new TextNode(" components."));

        var details = BuildDescription(span);
        var layout = _layoutEngine.Layout(details, DemoStyleSheets(), 500, 500);

        var spanBox = FindBox(layout, span);
        var text1 = spanBox.Children[0].BoxModel.Content;
        var code1Box = FindBox(layout, code1).BoxModel.MarginBox;
        var text2 = spanBox.Children[2].BoxModel.Content;

        // 同一行：顶部对齐。
        code1Box.Top.ShouldBe(text1.Top, 1f);
        text2.Top.ShouldBe(text1.Top, 1f);
        // 顺序：text1 → code1 → text2，互不重叠。
        code1Box.Left.ShouldBeGreaterThanOrEqualTo(text1.Right - 0.5f);
        text2.Left.ShouldBeGreaterThanOrEqualTo(code1Box.Right - 0.5f);
    }

    [Fact]
    public void CjkMixedText_ShouldWrapWithinContainerWidth()
    {
        // 中英文混排：中文之间、中文与英文之间都应允许断行，整体不超出 300px。
        var span = new SpanElement();
        span.AddChild(new TextNode("Linux 桌面软件的打包并不像 Windows 的 MSI/EXE 那样统一，而是每个发行版都有自己的包格式。如果你希望支持 Ubuntu、UOS、麒麟、Deepin 等国产 Linux，一般有下面几种方案。"));

        var details = BuildDescription(span);
        var layout = _layoutEngine.Layout(details, DemoStyleSheets(), 500, 500);

        var textBox = FindBox(layout, span).Children[0];

        // 文本盒宽度不得超过容器内容宽度（允许少量测量误差）。
        textBox.BoxModel.Content.Width.ShouldBeLessThanOrEqualTo(DescriptionWidth + 0.5f,
            "中文混排文本应换行收敛在容器宽度内，而不是整串溢出");

        // 文本应换为多行（行高 18 × 1.4 = 25.2）。
        float lineHeight = FontSize * 1.4f;
        textBox.BoxModel.Content.Height.ShouldBeGreaterThan(lineHeight * 2,
            "长文本应至少换出三行");
    }

    [Fact]
    public void PureCjkText_ShouldWrapPerCharacter()
    {
        // 纯中文（无空格）：每个汉字都是一个断行机会。
        var span = new SpanElement();
        span.AddChild(new TextNode("桌面软件的打包并不像想象中那样统一而是每个发行版都有自己的包格式"));

        var details = BuildDescription(span);
        var layout = _layoutEngine.Layout(details, DemoStyleSheets(), 500, 500);

        var textBox = FindBox(layout, span).Children[0];
        textBox.BoxModel.Content.Width.ShouldBeLessThanOrEqualTo(DescriptionWidth + 0.5f);
        textBox.BoxModel.Content.Height.ShouldBeGreaterThan(FontSize * 1.4f * 1.5f,
            "纯中文长串应换行为多行");
    }

    private static bool Overlaps(RectF a, RectF b)
        => a.Left < b.Right - 0.5f && a.Right > b.Left + 0.5f
        && a.Top < b.Bottom - 0.5f && a.Bottom > b.Top + 0.5f;
}
