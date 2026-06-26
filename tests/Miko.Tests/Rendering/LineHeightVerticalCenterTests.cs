using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// ISSUE-070 回归测试：
/// 当元素的 height 与 line-height 相等（如均为 50px）时，浏览器会把单行文本垂直居中。
/// Miko 之前对非 button 文本一律顶部对齐，导致文本贴在内容盒顶部。
/// 这里通过扫描渲染位图中文本像素的垂直分布，验证文本确实被居中（而非顶对齐）。
/// </summary>
public class LineHeightVerticalCenterTests : IDisposable
{
    private const int Width = 200;
    private const int Height = 100;

    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine;
    private readonly LayoutEngine _layoutEngine;

    public LineHeightVerticalCenterTests()
    {
        _bitmap = new SKBitmap(Width, Height);
        _canvas = new SKCanvas(_bitmap);
        _renderEngine = new RenderEngine();
        _renderEngine.SetCanvas(_canvas);
        _layoutEngine = new LayoutEngine();
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    /// <summary>
    /// 返回位图中存在“非白色”像素（即被绘制的文本）的最小/最大行号，
    /// 用于判断文本墨迹在垂直方向上的分布范围。无文本像素时返回 (-1, -1)。
    /// </summary>
    private (int top, int bottom) FindInkVerticalSpan()
    {
        int top = -1, bottom = -1;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var c = _bitmap.GetPixel(x, y);
                // 背景为透明/白，文本为深色：任何明显非白像素都视为墨迹。
                bool isInk = c.Alpha > 0 && (c.Red < 200 || c.Green < 200 || c.Blue < 200);
                if (isInk)
                {
                    if (top == -1) top = y;
                    bottom = y;
                    break;
                }
            }
        }
        return (top, bottom);
    }

    /// <summary>
    /// height: 50px; line-height: 50px 的 div（含文本）应将文本垂直居中：
    /// 文本墨迹的中心应接近内容盒（50px 高）的垂直中心，而非贴顶。
    /// </summary>
    [Fact]
    public void Div_WithEqualHeightAndLineHeight_ShouldCenterTextVertically()
    {
        _canvas.Clear(SKColors.White);

        var div = new DivElement { Class = "label", TextContent = "Test" };

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("label"), new Style
        {
            Height = Length.Px(50),
            LineHeight = Length.Px(50),
            FontSize = Length.Px(16),
            Color = Color.Black,
        });

        var box = _layoutEngine.Layout(div, new List<StyleSheet> { sheet }, Width, Height);
        box.BoxModel.Content.Height.ShouldBe(50f, 0.01f);

        _renderEngine.Render(box);

        var (inkTop, inkBottom) = FindInkVerticalSpan();
        inkTop.ShouldBeGreaterThanOrEqualTo(0, "Text should have been rendered");

        float inkCenter = (inkTop + inkBottom) / 2f;
        float boxCenter = box.BoxModel.Content.Top + box.BoxModel.Content.Height / 2f; // = 25

        // 墨迹中心应接近内容盒中心（容差覆盖字体上下不对称的视觉重心）。
        inkCenter.ShouldBe(boxCenter, 6f,
            "Text ink center should be near the vertical center of the 50px line box");

        // 并且明显不是顶对齐：顶对齐时墨迹会从盒顶（y≈0）开始。
        inkTop.ShouldBeGreaterThan(10,
            "Text should not be flush to the top of the box");
    }

    /// <summary>
    /// 嵌套场景（ISSUE-070 原始复现）：外层 div 设置 height/line-height，
    /// 内层 span 继承 line-height，其文本也应垂直居中。
    /// </summary>
    [Fact]
    public void NestedSpan_InheritingLineHeight_ShouldCenterTextVertically()
    {
        _canvas.Clear(SKColors.White);

        var span = new SpanElement { TextContent = "Test" };
        var div = new DivElement { Class = "label" };
        div.AddChild(span);

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("label"), new Style
        {
            Height = Length.Px(50),
            LineHeight = Length.Px(50),
            FontSize = Length.Px(16),
            Color = Color.Black,
        });

        var box = _layoutEngine.Layout(div, new List<StyleSheet> { sheet }, Width, Height);

        _renderEngine.Render(box);

        var (inkTop, inkBottom) = FindInkVerticalSpan();
        inkTop.ShouldBeGreaterThanOrEqualTo(0, "Text should have been rendered");

        float inkCenter = (inkTop + inkBottom) / 2f;
        inkCenter.ShouldBe(25f, 6f,
            "Inherited line-height should center the span text within the 50px line box");
        inkTop.ShouldBeGreaterThan(10,
            "Span text should not be flush to the top of the box");
    }

    /// <summary>
    /// 无回归：未显式设置 line-height 时，文本按自然行高渲染，墨迹应靠近内容盒顶部
    /// （行盒高 ≈ 字体高，居中与顶对齐等价）。
    /// </summary>
    [Fact]
    public void Div_WithoutLineHeight_TextStaysNearTop()
    {
        _canvas.Clear(SKColors.White);

        var div = new DivElement { Class = "plain", TextContent = "Test" };

        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("plain"), new Style
        {
            FontSize = Length.Px(16),
            Color = Color.Black,
        });

        var box = _layoutEngine.Layout(div, new List<StyleSheet> { sheet }, Width, Height);

        _renderEngine.Render(box);

        var (inkTop, _) = FindInkVerticalSpan();
        inkTop.ShouldBeGreaterThanOrEqualTo(0, "Text should have been rendered");
        // 自然行高下文本应靠近顶部（不会被推到盒子中部）。
        inkTop.ShouldBeLessThan(10,
            "Without explicit line-height, text ink should start near the content top");
    }
}
