using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// ISSUE-088：outline 绘制测试 —— 轮廓画在边框盒之外，并遵循 outline-offset。
/// </summary>
public class Issue088OutlineRenderingTests : IDisposable
{
    private readonly SKBitmap _canvasBitmap;
    private readonly SKCanvas _canvas;

    public Issue088OutlineRenderingTests()
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
    public void Outline_DrawsOutsideBorderBox()
    {
        // 根含 50px padding，子盒 100×100，outline 4px（offset 0）。
        // 子盒 BorderBox 从 (50,50) 到 (150,150)。轮廓描边中心在 borderBox 外扩 width/2=2 处，
        // 即 x≈48 附近应出现绿色轮廓，而 borderBox 内部（内容区）保持白色。
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(400), Height = Length.Px(400), Padding = Length.Px(50) }
        };
        var box = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(100),
                Height = Length.Px(100),
                Outline = Outline.Solid(Length.Px(4), Color.FromRgb(0, 200, 0))
            }
        };
        root.AddChild(box);

        Render(root);

        // 轮廓在 borderBox（左缘 x=50）之外：检查 x=48、y=100 处为绿色。
        var outlinePixel = GetPixel(48, 100);
        outlinePixel.Green.ShouldBeGreaterThan((byte)150);
        outlinePixel.Red.ShouldBeLessThan((byte)100);

        // 内容中心（100,100）应为白色（轮廓不覆盖内部）。
        GetPixel(100, 100).ShouldBe(SKColors.White);
    }

    [Fact]
    public void OutlineOffset_PushesOutlineFurtherOut()
    {
        // offset=10 时轮廓离 borderBox 更远：borderBox 左缘 x=50，轮廓内缘约在 x=40，
        // 描边中心约在 x=38。检查 x=38 附近为绿色，而紧贴 borderBox 的 x=49 处应为白色间隙。
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(400), Height = Length.Px(400), Padding = Length.Px(60) }
        };
        var box = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(100),
                Height = Length.Px(100),
                Outline = Outline.Solid(Length.Px(4), Color.FromRgb(0, 200, 0)),
                OutlineOffset = Length.Px(10)
            }
        };
        root.AddChild(box);

        Render(root);

        // borderBox 左缘 x=60；offset 10 → 轮廓内缘约 x=50，描边中心约 x=48。
        var outlinePixel = GetPixel(48, 110);
        outlinePixel.Green.ShouldBeGreaterThan((byte)150);

        // 紧贴 borderBox 外侧的间隙（x=58，在 offset 区间内）应为白色。
        GetPixel(58, 110).ShouldBe(SKColors.White);
    }

    [Fact]
    public void NoOutline_LeavesNoStroke()
    {
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(400), Height = Length.Px(400), Padding = Length.Px(50) }
        };
        var box = new DivElement
        {
            Style = new Style { Width = Length.Px(100), Height = Length.Px(100) }
        };
        root.AddChild(box);

        Render(root);

        // 没有 outline → borderBox 之外区域保持白色。
        GetPixel(48, 100).ShouldBe(SKColors.White);
    }
}
