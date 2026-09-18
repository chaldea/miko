using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Rendering;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

/// <summary>
/// ISSUE-137（原 ISSUE-111）：矢量背景（SVG）必须按<b>设备像素</b>栅格化，
/// 否则在高密度设备（Android/iOS 真机、模拟器）上会经历
/// 「1× 栅格化 → 画布放大 → 合成」的二次重采样，图标边缘明显变糊。
///
/// <para>度量方式：画一个圆形 SVG（唯一的信息都在斜边上），统计整幅画面里
/// 「既非纯图形色也非纯背景色」的中间灰阶像素数量。1× 栅格化再放大时，
/// 每一个 1× 源像素的边缘会被线性插值抹成 Scale 宽的渐变带；直接按设备像素
/// 栅格化则边缘只有亚像素级的抗锯齿过渡，中间色像素数量显著更少。</para>
/// </summary>
public class VectorBackgroundDeviceScaleTests
{
    private const float DeviceScale = 3f;
    private const int LogicalSize = 24;
    private const int CanvasSize = (int)(LogicalSize * DeviceScale);

    /// <summary>一个填满 viewBox 的黑色圆 —— 边缘全是斜的，对重采样最敏感。</summary>
    private const string CircleSvg =
        "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'>" +
        "<circle cx='12' cy='12' r='11' fill='black'/></svg>";

    [Fact]
    public void Should_rasterize_svg_background_at_device_pixels()
    {
        // 设备像素栅格化的参考画面：直接在 1× 画布上画 72×72 的同一个 SVG，
        // 这是「没有二次重采样」时应当得到的边缘锐度上限。
        using var reference = RenderSvgDirect(CanvasSize);
        int referenceBlend = CountBlendedPixels(reference);

        // 引擎在 3× 画布上以 24×24 逻辑尺寸绘制该 SVG 背景。
        using var actual = RenderSvgBackgroundOnScaledCanvas();
        int engineBlend = CountBlendedPixels(actual);

        // 1× 栅格化后放大会把边缘抹成约 DeviceScale 像素宽的渐变带，
        // 中间色像素数量成倍增长。允许 1.5 倍余量吸收抗锯齿实现差异。
        engineBlend.ShouldBeLessThan((int)(referenceBlend * 1.5f));
    }

    [Fact]
    public void Should_keep_drawn_geometry_at_logical_size()
    {
        // 过采样只改变位图分辨率，不得改变绘制几何：圆仍然只占左上 24×24 逻辑区域
        // （= 设备像素 0..72），其外（逻辑 30px → 设备 90px）必须仍是背景色。
        using var surface = RenderSvgBackgroundOnScaledCanvas(canvasSize: 120, boxSize: 40);

        // 盒子 40×40 逻辑像素，背景图 24×24 居左上：逻辑 (12,12) 在圆心 → 黑。
        surface.GetPixel((int)(12 * DeviceScale), (int)(12 * DeviceScale)).ShouldBe(SKColors.Black);
        // 逻辑 (30,30) 在 24×24 背景图之外 → 白。
        surface.GetPixel((int)(30 * DeviceScale), (int)(30 * DeviceScale)).ShouldBe(SKColors.White);
    }

    [Fact]
    public void DeviceScale_ShouldBeOne_OnUnscaledCanvas()
    {
        using var bitmap = new SKBitmap(64, 64);
        using var canvas = new SKCanvas(bitmap);

        new Painter(canvas).DeviceScale.ShouldBe(1f);
    }

    [Fact]
    public void DeviceScale_ShouldReportCanvasScale()
    {
        using var bitmap = new SKBitmap(64, 64);
        using var canvas = new SKCanvas(bitmap);
        canvas.Translate(17, 9); // 平移不是缩放，不得计入。
        canvas.Scale(3f);

        new Painter(canvas).DeviceScale.ShouldBe(3f);
    }

    [Fact]
    public void DeviceScale_ShouldSurviveRotation()
    {
        // CSS transform 的旋转与设备缩放并入同一个矩阵。此时 ScaleX 不再是缩放系数
        // （旋转 90° 时为 0），单取它会把栅格目标尺寸算成 0，图标直接消失。
        using var bitmap = new SKBitmap(64, 64);
        using var canvas = new SKCanvas(bitmap);
        canvas.Scale(3f);
        canvas.RotateDegrees(90f);

        new Painter(canvas).DeviceScale.ShouldBe(3f, 0.001f);
    }

    [Theory]
    // 桌面（1×）不放大：位图尺寸 = 绘制尺寸。
    [InlineData(1f, 1f)]
    // 常见密度档原样透传。
    [InlineData(2f, 2f)]
    [InlineData(3f, 3f)]
    // 非整数密度（如 2.625 的 Android xhdpi 变体）向上取整到台阶，绝不欠采样。
    [InlineData(2.625f, 3f)]
    [InlineData(1.1f, 1.5f)]
    // 小于 1（元素被 transform 缩小）不降低栅格分辨率：缩小采样本就无损。
    [InlineData(0.5f, 1f)]
    // 超过上限时按上限，避免瞬时大倍率分配超大位图。
    [InlineData(12f, 4f)]
    // 非法值退化为 1，而不是把目标尺寸算成 0/NaN。
    [InlineData(float.NaN, 1f)]
    public void QuantizeRasterScale_ShouldSnapToStepsWithoutUndersampling(float deviceScale, float expected)
    {
        RenderEngine.QuantizeRasterScale(deviceScale).ShouldBe(expected);
    }

    /// <summary>
    /// 在 <c>Scale(DeviceScale)</c> 的画布上，用引擎绘制一个带 SVG 背景的盒子。
    /// 背景图按 <c>background-size</c> 固定为 24×24 逻辑像素。
    /// </summary>
    private static SKBitmap RenderSvgBackgroundOnScaledCanvas(
        int canvasSize = CanvasSize, int boxSize = LogicalSize)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(CircleSvg));
        var bg = BackgroundImage.FromSvgStream(stream);

        var root = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(boxSize),
                Height = Length.Px(boxSize),
                BackgroundImage = bg,
                BackgroundRepeat = BackgroundRepeat.NoRepeat,
                BackgroundSize = BackgroundSize.Px(LogicalSize, LogicalSize),
            }
        };

        var bitmap = new SKBitmap(canvasSize, canvasSize);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            canvas.Scale(DeviceScale);

            float logical = canvasSize / DeviceScale;
            var engine = new MikoEngineBuilder().Build();
            engine.Initialize(root, [], canvas, logical, logical);
            engine.Render(canvas);
        }
        return bitmap;
    }

    /// <summary>无中间位图的参考渲染：SVG 直接按目标设备像素尺寸栅格化。</summary>
    private static SKBitmap RenderSvgDirect(int size)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(CircleSvg));
        var bg = BackgroundImage.FromSvgStream(stream);
        var rendered = bg.RenderAtSize(size, size)!;

        var bitmap = new SKBitmap(size, size);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(rendered, 0, 0);
        }
        return bitmap;
    }

    /// <summary>统计既非纯黑也非纯白的过渡像素数（边缘模糊程度的代理指标）。</summary>
    private static int CountBlendedPixels(SKBitmap bitmap)
    {
        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var p = bitmap.GetPixel(x, y);
                bool pureBlack = p.Red < 8 && p.Green < 8 && p.Blue < 8;
                bool pureWhite = p.Red > 247 && p.Green > 247 && p.Blue > 247;
                if (!pureBlack && !pureWhite) count++;
            }
        }
        return count;
    }
}
