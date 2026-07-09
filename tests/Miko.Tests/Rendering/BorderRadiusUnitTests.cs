using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// ISSUE-087: border-radius 使用 rem/em/percent 单位时未被解析为像素（旧代码用 Length.Value
/// 直接取数值，把 0.4rem 当成 0.4px）。这些测试通过实际渲染像素验证圆角单位被正确折算。
///
/// 判定原理：给一个纯色方块设置较大的圆角，角落会被圆角裁掉（保持背景白色）；
/// 若单位被错误当成极小的 px 值，角落将不会被裁掉而仍是填充色。
/// </summary>
public class BorderRadiusUnitTests : IDisposable
{
    private readonly SKBitmap _canvasBitmap;
    private readonly SKCanvas _canvas;

    public BorderRadiusUnitTests()
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
    public void BorderRadius_Rem_ShouldResolveAgainstRootFontSize()
    {
        // RootFontSize 默认 16 → 2rem = 32px 圆角。
        // 角点 (3,3) 距圆弧圆心 (32,32) 约 41px > 32px，因此被裁掉 → 白色。
        // 若把 rem 当成 px（2px 圆角），(3,3) 会落在方块内 → 红色（旧 bug 行为）。
        var root = CreateRedBox(100, 100, new BorderRadius(Length.Rem(2)));
        RenderElement(root);

        GetPixelColor(3, 3).ShouldBe(SKColors.White);   // 角落被 32px 圆角裁掉
        GetPixelColor(50, 50).ShouldBe(SKColors.Red);   // 中心仍是填充色
    }

    [Fact]
    public void BorderRadius_Px_ShouldStillWork()
    {
        // 对照组：等效的 32px 圆角应与 2rem 表现一致（防止修复破坏 px 路径）。
        var root = CreateRedBox(100, 100, new BorderRadius(Length.Px(32)));
        RenderElement(root);

        GetPixelColor(3, 3).ShouldBe(SKColors.White);
        GetPixelColor(50, 50).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void BorderRadius_SmallRem_ShouldNotOverClip()
    {
        // 0.4rem = 6.4px 圆角：角点 (3,3) 距圆心 (6.4,6.4) 约 4.8px < 6.4px → 仍在方块内 → 红色。
        // 这正是 ISSUE-087 中 0.4rem 的场景：既要被折算成 6.4px，又不能夸大成极大圆角。
        var root = CreateRedBox(100, 100, new BorderRadius(Length.Rem(0.4f)));
        RenderElement(root);

        GetPixelColor(3, 3).ShouldBe(SKColors.Red);
        // 最外角 (0,0) 一定被 6.4px 圆角裁掉。
        GetPixelColor(0, 0).ShouldBe(SKColors.White);
    }

    [Fact]
    public void BorderRadius_Em_ShouldResolveAgainstFontSize()
    {
        // font-size = 20px，2em = 40px 圆角。角点 (3,3) 距圆心 (40,40) ≈ 52px > 40px → 白色。
        var root = CreateRedBox(100, 100, new BorderRadius(Length.Em(2)));
        root.Style!.FontSize = Length.Px(20);
        RenderElement(root);

        GetPixelColor(3, 3).ShouldBe(SKColors.White);
        GetPixelColor(50, 50).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void BorderRadius_Percent_ShouldResolveAgainstBoxSize()
    {
        // 50% 圆角 → 正方形变圆形。角点被裁掉，中心保留。
        var root = CreateRedBox(100, 100, new BorderRadius(Length.Percent(50)));
        RenderElement(root);

        GetPixelColor(3, 3).ShouldBe(SKColors.White);   // 圆形外的角落
        GetPixelColor(50, 50).ShouldBe(SKColors.Red);   // 圆心仍是填充色
    }

    private static DivElement CreateRedBox(int width, int height, BorderRadius radius)
    {
        var root = new DivElement();
        root.Style = new Style
        {
            Width = Length.Px(width),
            Height = Length.Px(height),
            BackgroundColor = Color.FromRgb(255, 0, 0),
            BorderTopLeftRadius = radius.TopLeft,
            BorderTopRightRadius = radius.TopRight,
            BorderBottomRightRadius = radius.BottomRight,
            BorderBottomLeftRadius = radius.BottomLeft,
        };
        return root;
    }

    private void RenderElement(Element root)
    {
        var engine = new MikoEngine();
        engine.Initialize(root, [], _canvas, 200, 200);
        engine.Render(_canvas);
    }

    private SKColor GetPixelColor(int x, int y) => _canvasBitmap.GetPixel(x, y);
}
