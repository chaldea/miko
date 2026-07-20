using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// ion-avatar 圆形裁剪修复：overflow != visible 且带 border-radius 的盒子，其内容（子元素、图片位图）
/// 必须裁剪到圆角形状，而不是方形矩形。旧代码的 overflow 裁剪始终是矩形（<c>ClipRect</c>），
/// 导致方形内容溢出圆角，头像显示为方形。
///
/// 判定原理：让内容完全填满一个 50% 圆角、overflow:hidden 的方块。若裁剪成圆形，四角应被裁掉
/// （露出白色背景），圆心保留内容色；若仍是矩形裁剪（旧 bug），四角仍是内容色。
/// </summary>
public class OverflowRoundedClipTests : IDisposable
{
    private readonly SKBitmap _canvasBitmap;
    private readonly SKCanvas _canvas;

    public OverflowRoundedClipTests()
    {
        _canvasBitmap = new SKBitmap(200, 200);
        _canvas = new SKCanvas(_canvasBitmap);
        _canvas.Clear(SKColors.White);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _canvasBitmap.Dispose();
    }

    [Fact]
    public void RoundedOverflowHidden_ClipsChildContentToCircle()
    {
        // 圆形容器（100x100，50% 圆角，overflow:hidden）里放一个 100%x100% 的红色子块（自身无圆角）。
        // 子块若被矩形裁剪，(3,3) 会是红色（旧 bug）；被圆形裁剪则 (3,3) 是白色。
        var host = new DivElement();
        host.Style = new Style
        {
            Width = Length.Px(100),
            Height = Length.Px(100),
            OverflowX = Overflow.Hidden,
            OverflowY = Overflow.Hidden,
            BorderTopLeftRadius = Length.Percent(50),
            BorderTopRightRadius = Length.Percent(50),
            BorderBottomRightRadius = Length.Percent(50),
            BorderBottomLeftRadius = Length.Percent(50),
        };

        var fill = new DivElement();
        fill.Style = new Style
        {
            Width = Length.Percent(100),
            Height = Length.Percent(100),
            BackgroundColor = Color.FromRgb(255, 0, 0),
        };
        host.AddChild(fill);

        RenderElement(host);

        GetPixelColor(3, 3).ShouldBe(SKColors.White);   // 圆外的角落被裁掉
        GetPixelColor(50, 50).ShouldBe(SKColors.Red);   // 圆心保留内容
    }

    [Fact]
    public void RoundedOverflowHidden_ClipsOwnBackgroundContentUnaffected_WhenNoOverflow()
    {
        // 对照：同样 50% 圆角但 overflow:visible 的方块的背景，仍由 border-radius 自行成形
        // （背景绘制走圆角路径），角落应为白色，圆心为红色。确保修复未破坏普通圆角背景。
        var box = new DivElement();
        box.Style = new Style
        {
            Width = Length.Px(100),
            Height = Length.Px(100),
            BackgroundColor = Color.FromRgb(255, 0, 0),
            BorderTopLeftRadius = Length.Percent(50),
            BorderTopRightRadius = Length.Percent(50),
            BorderBottomRightRadius = Length.Percent(50),
            BorderBottomLeftRadius = Length.Percent(50),
        };

        RenderElement(box);

        GetPixelColor(3, 3).ShouldBe(SKColors.White);
        GetPixelColor(50, 50).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void RectangularOverflowHidden_DoesNotClipCorners()
    {
        // 反向对照：无圆角、overflow:hidden 时，内容仍填满整个矩形，四角保留内容色
        // （确认修复只在有圆角时才改用圆角裁剪，无圆角时保持矩形裁剪）。
        var host = new DivElement();
        host.Style = new Style
        {
            Width = Length.Px(100),
            Height = Length.Px(100),
            OverflowX = Overflow.Hidden,
            OverflowY = Overflow.Hidden,
        };

        var fill = new DivElement();
        fill.Style = new Style
        {
            Width = Length.Percent(100),
            Height = Length.Percent(100),
            BackgroundColor = Color.FromRgb(255, 0, 0),
        };
        host.AddChild(fill);

        RenderElement(host);

        GetPixelColor(3, 3).ShouldBe(SKColors.Red);     // 无圆角 → 角落仍是内容
        GetPixelColor(50, 50).ShouldBe(SKColors.Red);
    }

    private void RenderElement(Element root)
    {
        var engine = new MikoEngine();
        engine.Initialize(root, [], _canvas, 200, 200);
        engine.Render(_canvas);
    }

    private SKColor GetPixelColor(int x, int y) => _canvasBitmap.GetPixel(x, y);
}
