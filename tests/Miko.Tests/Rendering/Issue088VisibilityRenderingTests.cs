using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// ISSUE-088：visibility 绘制行为 —— hidden 元素自身不绘制，但仍占布局空间且子元素可用
/// visibility: visible 覆盖显示。
/// </summary>
public class Issue088VisibilityRenderingTests : IDisposable
{
    private readonly SKBitmap _canvasBitmap;
    private readonly SKCanvas _canvas;

    public Issue088VisibilityRenderingTests()
    {
        _canvasBitmap = new SKBitmap(400, 400);
        _canvas = new SKCanvas(_canvasBitmap);
        _canvas.Clear(SKColors.White);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _canvasBitmap.Dispose();
    }

    private void Render(Element root)
    {
        var engine = new MikoEngine();
        engine.Initialize(root, [], _canvas, 400, 400);
        engine.Render(_canvas);
    }

    private SKColor GetPixel(int x, int y) => _canvasBitmap.GetPixel(x, y);

    [Fact]
    public void VisibilityHidden_DoesNotPaintBackground()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(200),
                Height = Length.Px(200),
                BackgroundColor = Color.Red,
                Visibility = Visibility.Hidden
            }
        };

        Render(root);

        // hidden → 红色背景不绘制，保持画布白色。
        GetPixel(100, 100).ShouldBe(SKColors.White);
    }

    [Fact]
    public void VisibilityVisible_PaintsBackground()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(200),
                Height = Length.Px(200),
                BackgroundColor = Color.Red
            }
        };

        Render(root);

        GetPixel(100, 100).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void VisibleChild_OfHiddenParent_StillPaints()
    {
        // 父元素 hidden，子元素 visible → 子元素背景仍绘制。
        var child = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(80),
                Height = Length.Px(80),
                BackgroundColor = Color.Blue,
                Visibility = Visibility.Visible
            }
        };
        var parent = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(200),
                Height = Length.Px(200),
                BackgroundColor = Color.Red,
                Visibility = Visibility.Hidden
            },
            Children = { child }
        };

        Render(parent);

        // 父背景（红）不绘制 → 父区域内、子盒之外为白色。
        GetPixel(150, 150).ShouldBe(SKColors.White);
        // 子背景（蓝）仍绘制。
        GetPixel(40, 40).ShouldBe(SKColors.Blue);
    }
}
