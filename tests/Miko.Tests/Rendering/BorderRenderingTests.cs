using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

public class BorderRenderingTests : IDisposable
{
    private readonly SKBitmap _canvasBitmap;
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine;

    public BorderRenderingTests()
    {
        _canvasBitmap = new SKBitmap(600, 600);
        _canvas = new SKCanvas(_canvasBitmap);
        _canvas.Clear(SKColors.White);
        _renderEngine = new RenderEngine();
        _renderEngine.SetCanvas(_canvas);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _canvasBitmap.Dispose();
    }

    [Fact]
    public void AdjacentDivs_WithBorders_ShouldNotOverlap()
    {
        // 模拟 issue 中的场景：三个相邻 div 各有 1px 边框
        // 正常情况下相邻边框之间应该有 2px 宽度（各自 1px）
        var root = new DivElement();
        root.Style = new Style
        {
            Width = Length.Px(500),
            Height = Length.Px(500),
            Padding = Length.Px(10)
        };

        var div1 = new DivElement { Class = "div-1" };
        div1.Style = new Style
        {
            Width = Length.Percent(100),
            Height = Length.Px(30),
            Border = Border.Solid(1, Color.Black)
        };

        var div2 = new DivElement { Class = "div-2" };
        div2.Style = new Style
        {
            Width = Length.Percent(100),
            Height = Length.Px(30),
            Border = Border.Solid(1, Color.Red)
        };
        var div3 = new DivElement { Class = "div-3" };
        div3.Style = new Style
        {
            Width = Length.Percent(100),
            Height = Length.Px(30),
            Border = Border.Solid(1, Color.Blue)
        };

        root.AddChild(div1);
        root.AddChild(div2);
        root.AddChild(div3);

        RenderElement(root);

        // div1 的 BorderBox 从 y=10 开始（padding=10），高度=30+2(border)=32
        // div1 底边框应该在 y=41 (10+32-1=41)
        // div2 的 BorderBox 从 y=42 开始
        // div2 顶边框应该在 y=42
        // 在 y=41 和 y=42 之间应该有明确的分界

        // 验证 div1 底边框区域是黑色
        var div1BottomBorderY = 41;
        var pixelAtDiv1Bottom = GetPixelColor(250, div1BottomBorderY);
        pixelAtDiv1Bottom.ShouldBe(SKColors.Black);

        // 验证 div2 顶边框区域是红色
        var div2TopBorderY = 42;
        var pixelAtDiv2Top = GetPixelColor(250, div2TopBorderY);
        pixelAtDiv2Top.ShouldBe(SKColors.Red);
    }

    [Fact]
    public void Border_ShouldStayWithinBorderBox()
    {
        // 验证边框完全在 BorderBox 内部绘制，不会溢出
        var root = new DivElement();
        root.Style = new Style
        {
            Width = Length.Px(100),
            Height = Length.Px(100),
            Border = Border.Solid(2, Color.Black)
        };

        RenderElement(root);

        // 边框宽度为 2px，BorderBox 从 (0,0) 到 (103,103)
        // 边框应该完全在 BorderBox 内部
        // BorderBox 外部（y=-1 不存在，但 y=0 应该有边框）
        var topEdge = GetPixelColor(50, 0);
        topEdge.ShouldBe(SKColors.Black);

        var insideContent = GetPixelColor(50, 50);
        insideContent.ShouldBe(SKColors.White);
    }

    [Fact]
    public void PerSideBorder_WithTopNone_ShouldRenderOtherSidesCorrectly()
    {
        // 验证取消上边框后，其他三边仍然正确渲染
        var root = new DivElement();
        root.Style = new Style
        {
            Width = Length.Px(100),
            Height = Length.Px(100),
            BorderLeft = new BorderSide(Length.Px(2), BorderStyle.Solid, Color.Blue),
            BorderRight = new BorderSide(Length.Px(2), BorderStyle.Solid, Color.Blue),
            BorderBottom = new BorderSide(Length.Px(2), BorderStyle.Solid, Color.Blue),
            BorderTop = BorderSide.None
        };

        RenderElement(root);

        // 上边框区域应该是白色（无边框）
        var topCenter = GetPixelColor(52, 0);
        topCenter.ShouldBe(SKColors.White);

        // 左边框应该是蓝色
        var leftBorder = GetPixelColor(0, 50);
        leftBorder.ShouldBe(SKColors.Blue);

        // 右边框应该是蓝色
        var rightBorder = GetPixelColor(103, 50);
        rightBorder.ShouldBe(SKColors.Blue);

        // 底边框应该是蓝色
        var bottomBorder = GetPixelColor(52, 101);
        bottomBorder.ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void PerSideBorder_WithTopNone_AndBorderRadius_ShouldRenderBottomCorners()
    {
        // 验证取消上边框 + 有圆角时，底部圆角不会缺失
        var root = new DivElement();
        root.Style = new Style
        {
            Width = Length.Px(100),
            Height = Length.Px(100),
            BorderLeft = new BorderSide(Length.Px(2), BorderStyle.Solid, Color.Blue),
            BorderRight = new BorderSide(Length.Px(2), BorderStyle.Solid, Color.Blue),
            BorderBottom = new BorderSide(Length.Px(2), BorderStyle.Solid, Color.Blue),
            BorderTop = BorderSide.None,
            BorderBottomLeftRadius = Length.Px(8),
            BorderBottomRightRadius = Length.Px(8)
        };

        RenderElement(root);

        // 底部中间应该有蓝色边框
        var bottomCenter = GetPixelColor(52, 101);
        bottomCenter.ShouldBe(SKColors.Blue);

        // 左下角的直角位置应该是白色（因为有圆角）
        var cornerPixel = GetPixelColor(1, 101);
        cornerPixel.ShouldBe(SKColors.White);
    }

    [Fact]
    public void UniformBorder_ShouldInsetByHalfWidth()
    {
        // 验证统一边框向内收缩半个宽度绘制
        var root = new DivElement();
        root.Style = new Style
        {
            Width = Length.Px(50),
            Height = Length.Px(50),
            Border = Border.Solid(4, Color.Red)
        };

        RenderElement(root);

        // 4px 边框，BorderBox 从 (0,0) 到 (57,57)
        // 描边中心线在 (2,2) 到 (55,55)
        // 描边外边缘在 (0,0)，内边缘在 (4,4)
        // 所以 (0,0) 应该有红色，(4,4) 也应该有红色边缘
        var outerEdge = GetPixelColor(0, 28);
        outerEdge.ShouldBe(SKColors.Red);

        // 内容区域中心应该是白色
        var center = GetPixelColor(28, 28);
        center.ShouldBe(SKColors.White);
    }

    private void RenderElement(Element root)
    {
        var engine = new MikoEngine();
        engine.Initialize(root, [], _canvas, 600, 600);
        engine.Render(_canvas);
    }

    private SKColor GetPixelColor(int x, int y) => _canvasBitmap.GetPixel(x, y);
}
