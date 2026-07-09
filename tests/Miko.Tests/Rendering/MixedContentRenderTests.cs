using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// ISSUE-086 端到端渲染验证：文本与标签混排时，各段按 DOM 顺序水平绘制，互不重叠、顺序正确。
/// 通过在离屏画布上着色中间的 span，并检查其像素落在两段文本之间的水平区间内来验证渲染顺序。
/// </summary>
public class MixedContentRenderTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine;
    private readonly LayoutEngine _layoutEngine;

    public MixedContentRenderTests()
    {
        _bitmap = new SKBitmap(600, 200);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
        _renderEngine = new RenderEngine();
        _renderEngine.SetCanvas(_canvas);
        _layoutEngine = new LayoutEngine();
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Fact]
    public void TextSpanText_RendersSpanBetweenTextRuns()
    {
        // <div>test1 <span style="background:red">test2</span> test3</div>
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            [".host"] = new()
            {
                Width = Length.Px(600),
                Height = Length.Px(60),
                FontSize = Length.Px(20),
            },
            [".mid"] = new()
            {
                Display = Display.Inline,
                BackgroundColor = Color.FromRgb(255, 0, 0),
                Color = Color.FromRgb(255, 0, 0),
            }
        });

        var div = new DivElement { Class = "host" };
        div.AddChild(new TextNode("test1 "));
        var span = new SpanElement { Class = "mid" };
        span.AddChild(new TextNode("test2"));
        div.AddChild(span);
        div.AddChild(new TextNode(" test3"));

        var root = new DivElement { Children = { div } };
        var layout = _layoutEngine.Layout(root, new List<StyleSheet> { sheet }, 600, 200);
        _renderEngine.Render(layout);

        // 取三段子盒的水平区间。
        var hostBox = layout.Children[0];
        hostBox.Children.Count.ShouldBe(3);
        var t1 = hostBox.Children[0].BoxModel.Content;
        var spanBox = hostBox.Children[1].BoxModel.BorderBox;
        var t3 = hostBox.Children[2].BoxModel.Content;

        // 顺序：t1 在 span 左，span 在 t3 左。
        spanBox.Left.ShouldBeGreaterThanOrEqualTo(t1.Right - 1f);
        t3.Left.ShouldBeGreaterThanOrEqualTo(spanBox.Right - 1f);

        // 在 span 的红色背景区间内应能采到红色像素。
        int y = (int)(spanBox.Top + spanBox.Height / 2f);
        int spanMidX = (int)(spanBox.Left + spanBox.Width / 2f);
        bool foundRed = false;
        for (int dx = -3; dx <= 3 && !foundRed; dx++)
        {
            var c = _bitmap.GetPixel(Math.Clamp(spanMidX + dx, 0, _bitmap.Width - 1), Math.Clamp(y, 0, _bitmap.Height - 1));
            if (c.Red > 180 && c.Green < 80 && c.Blue < 80) foundRed = true;
        }
        foundRed.ShouldBeTrue("span 的红色应绘制在两段文本之间的区间内");

        // 第一段文本区间内不应出现 span 的红色背景（顺序未错乱）。
        int t1MidX = (int)(t1.Left + Math.Max(2f, t1.Width / 2f));
        var t1Pixel = _bitmap.GetPixel(Math.Clamp(t1MidX, 0, _bitmap.Width - 1), Math.Clamp(y, 0, _bitmap.Height - 1));
        (t1Pixel.Red > 180 && t1Pixel.Green < 80 && t1Pixel.Blue < 80).ShouldBeFalse(
            "第一段文本区间不应被 span 的红色覆盖");
    }

    [Fact]
    public void MixedContent_RendersWithoutError()
    {
        // 示例1：<p>we can use <a>stopPropagation</a> to prevent bubbling.</p>
        var p = new ParagraphElement();
        p.AddChild(new TextNode("we can use "));
        p.AddChild(new AnchorElement { Children = { new TextNode("stopPropagation") } });
        p.AddChild(new TextNode(" to prevent bubbling."));

        var layout = _layoutEngine.Layout(p, new List<StyleSheet>(), 600, 200);
        Should.NotThrow(() => _renderEngine.Render(layout));
    }
}
