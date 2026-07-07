using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// ISSUE-086：文本以 TextNode 子盒承载后，命中测试应对文本节点透明——点击文本时命中目标
/// 解析为其包含元素（如 button），而非匿名文本节点，从而不破坏事件处理与冒泡。
/// </summary>
public class TextNodeHitTestTests
{
    private const float W = 400f;
    private const float H = 300f;

    private static MikoEngine CreateEngine(Element root, List<StyleSheet>? sheets = null)
    {
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)W, (int)H));
        engine.Initialize(root, sheets ?? new List<StyleSheet>(), surface.Canvas, W, H);
        return engine;
    }

    [Fact]
    public void ClickOnButtonText_HitsButtonNotTextNode()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".btn"] = new()
            {
                Width = Length.Px(200),
                Height = Length.Px(60),
                FontSize = Length.Px(16),
            }
        });

        var button = new ButtonElement { Class = "btn", TextContent = "Click me" };
        var root = new DivElement { Children = { button } };

        var engine = CreateEngine(root, new List<StyleSheet> { sheet });

        // 点击按钮内部（文本所在区域）。
        var hit = engine.HitTest(20f, 20f);

        hit.ShouldBe(button);
        hit.ShouldNotBeOfType<TextNode>();
    }

    [Fact]
    public void ClickOnMixedContentText_HitsContainingElement()
    {
        // <p>we can use <a>stopPropagation</a> ...</p>：点击首段文本应命中 p，而非文本节点。
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".para"] = new()
            {
                Width = Length.Px(400),
                Height = Length.Px(40),
                FontSize = Length.Px(16),
            }
        });

        var p = new ParagraphElement { Class = "para" };
        p.AddChild(new TextNode("we can use "));
        p.AddChild(new AnchorElement { Children = { new TextNode("stopPropagation") } });
        p.AddChild(new TextNode(" to prevent bubbling."));
        var root = new DivElement { Children = { p } };

        var engine = CreateEngine(root, new List<StyleSheet> { sheet });

        // 命中第一段文本区域（靠近左上角）。
        var hit = engine.HitTest(5f, 8f);
        hit.ShouldNotBeNull();
        hit.ShouldNotBeOfType<TextNode>();
    }
}
