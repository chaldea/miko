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
/// 位图绘制的重采样质量（ISSUE-137 后续）：桌面 1:1 渲染下暴露出的两处模糊，
/// 以及修它们时必须守住的两条底线。
///
/// <para>四组指标都是像素统计，不依赖人眼判断：</para>
/// <list type="bullet">
///   <item><b>梯度能量</b>：相邻像素差的平方和。越大＝边缘过渡越陡＝越锐。</item>
///   <item><b>过渡像素数</b>：既非纯图形色也非纯背景色的像素数。越少＝越锐。</item>
///   <item><b>摩尔纹得分</b>：逐行灰度标准差。1px 条纹降采样的理想结果是均匀灰，
///         得分应接近 0；出现明暗条带说明高频内容折叠成了走样。</item>
///   <item><b>缝隙像素数</b>：本该纯黑的平铺区域里的偏亮像素数。</item>
/// </list>
/// </summary>
public class ImageSamplingQualityTests
{
    // ---- 被测图形 ----------------------------------------------------------

    /// <summary>圆：边缘全是斜的，对重采样与采样相位最敏感。</summary>
    private const string CircleSvg =
        "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 512 512'>" +
        "<circle cx='256' cy='256' r='240' fill='black'/></svg>";

    /// <summary>填满 viewBox 的实心块：平铺后不该有任何缝隙。</summary>
    private const string SolidSvg =
        "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 10'>" +
        "<rect width='10' height='10' fill='black'/></svg>";

    private static BackgroundImage LoadSvg(string svg)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svg));
        return BackgroundImage.FromSvgStream(stream);
    }

    /// <summary>1px 宽黑白竖条：降采样走样的最强激励信号。</summary>
    private static SKBitmap Stripes(int size)
    {
        var bitmap = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        using var paint = new SKPaint { Color = SKColors.Black };
        for (int x = 0; x < size; x += 2)
            canvas.DrawRect(new SKRect(x, 0, x + 1, size), paint);
        return bitmap;
    }

    // ---- 1. 轻度缩小不得走 mipmap 的二次重采样 -----------------------------

    [Theory]
    // 缩放比 ≥ 1/2（cubic 区间）时，一次到位重采样应当不逊于直接按目标尺寸栅格化。
    // 只在源分辨率 ≥ 目标的情形下比较：源比目标小时（放大）位图本身就没有那么多细节，
    // 拿它跟"按目标尺寸新栅格化的矢量图"比不是采样器的锅，那个上限任何采样器都达不到。
    [InlineData(96, 48)]   // 1/2，阈值边界（1:1 另见 DrawImage_UnitScale_*）
    public void DrawImage_ModerateScale_ShouldResampleInOneStep(int srcSize, int dstSize)
    {
        // 曾经一律用 SKMipmapMode.Linear —— 先把源图逐级减半到最近的 mip 层，再从该层线性
        // 插值到目标尺寸，又是一次二次重采样（方向与 ISSUE-137 相反，发生在缩小侧）。
        // 这些比例下 mipmap 的抗走样无收益（见 LargeDownscale 的对照），只剩模糊代价。
        var bg = LoadSvg(CircleSvg);
        var source = bg.RenderAtSize(srcSize, srcSize)!;

        using var actual = DrawOnce(source, new RectF(0, 0, dstSize, dstSize), dstSize);
        // 参考：直接从矢量按目标尺寸栅格化（无中间位图）的边缘锐度。
        using var reference = RasterizeDirect(bg, dstSize);

        Grad(actual).ShouldBeGreaterThan(Grad(reference) * 0.9);
    }

    [Fact]
    public void SamplingFor_ShouldSwitchToMipmapOnlyBelowHalfScale()
    {
        // 阈值本身就是被测契约：它划定了"锐度优先"与"抗走样优先"的分界。
        // 实测依据见 LargeDownscale（cubic 在 <1/2 处走样）与 ModerateScale（≥1/2 处 cubic 更锐）。
        Painter.UsesMipmapForRatio(2.0f).ShouldBeFalse();   // 放大
        Painter.UsesMipmapForRatio(0.5f).ShouldBeFalse();   // 边界含在 cubic 一侧
        Painter.UsesMipmapForRatio(0.49f).ShouldBeTrue();
        // 512 位图 → 48 逻辑像素（桌面头像）确实落在 mipmap 段：它的柔和是抗走样的代价。
        Painter.UsesMipmapForRatio(48f / 512f).ShouldBeTrue();
    }

    [Fact]
    public void SamplingFor_UnitScale_ShouldBypassFiltering()
    {
        // 1:1 的正确行为是逐像素拷贝，任何滤波都是纯损失。ISSUE-137 之后背景 SVG 恰好按
        // 绘制的设备像素尺寸栅格化，1:1 是最常见的情形。
        // 注意判据用 UseCubic 而非 Filter：SKFilterMode.Nearest 是枚举的零值，
        // 用 cubic resampler 构造 SKSamplingOptions 时 Filter 也读回 Nearest，区分不出两条路径。
        var unit = Painter.SamplingFor(1f, 1f);
        Painter.IsUnitScale(1f, 1f).ShouldBeTrue();
        unit.UseCubic.ShouldBeFalse();
        unit.Filter.ShouldBe(SKFilterMode.Nearest);
        unit.Mipmap.ShouldBe(SKMipmapMode.None);

        // 非等比拉伸不是 1:1：只看较小轴会把 64×64 画进 64×128 误判成逐像素拷贝，
        // 走 Nearest 会出现块状锯齿。
        Painter.IsUnitScale(1f, 2f).ShouldBeFalse();
        Painter.SamplingFor(1f, 2f).UseCubic.ShouldBeTrue();

        // 轻度缩小走 cubic（锐度优先），大幅缩小走 mipmap（抗走样优先）。
        Painter.SamplingFor(0.75f, 0.75f).UseCubic.ShouldBeTrue();
        var large = Painter.SamplingFor(0.3f, 0.3f);
        large.UseCubic.ShouldBeFalse();
        large.Mipmap.ShouldBe(SKMipmapMode.Linear);
    }

    [Fact]
    public void DrawImage_UnitScale_ShouldMatchExactPixelCopy()
    {
        // 回归闸门：曾用 Mitchell cubic 兜住这一段，它的 B 参数非零，无需重采样时也滤波，
        // 把 64→64 的边缘过渡像素从 190 抹到 347 —— 比改动之前更糊。
        var bg = LoadSvg(CircleSvg);
        var source = bg.RenderAtSize(64, 64)!;

        using var drawn = DrawOnce(source, new RectF(0, 0, 64, 64), 64);

        // 基准：Skia 的 DrawBitmap 直通。
        using var baseline = new SKBitmap(64, 64);
        using (var canvas = new SKCanvas(baseline))
        {
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(source, 0, 0);
        }

        // 1:1 应当与逐像素拷贝完全一致，而不只是"接近"。
        Mid(drawn).ShouldBe(Mid(baseline));
        Grad(drawn).ShouldBe(Grad(baseline), tolerance: Grad(baseline) * 0.001);
    }

    // ---- 2. 大幅缩小必须保留 mipmap 抗走样 ---------------------------------

    [Theory]
    [InlineData(48)]  // 1/10.7
    [InlineData(24)]  // 1/21.3
    public void DrawImage_LargeDownscale_ShouldNotAlias(int dstSize)
    {
        // 关掉 mipmap 换来的锐度是有代价的：cubic 的采样核只覆盖 4×4 源像素，
        // 落不到的高频内容会折叠成摩尔纹。这条守住"不能为了锐而一律关掉 mipmap"。
        using var source = Stripes(512);
        using var result = DrawOnce(source, new RectF(0, 0, dstSize, dstSize), dstSize);

        // 1px 条纹降到这个比例，理想结果是均匀中灰：逐行方差应接近 0。
        Moire(result).ShouldBeLessThan(5.0);
        // 且整体亮度守恒（黑白各半 → 约 128），不应偏向某一色。
        Mean(result).ShouldBeInRange(100, 156);
    }

    // ---- 3. 采样相位：小数位置不得让整幅图变糊 -----------------------------

    [Fact]
    public void DrawImage_FractionalPosition_ShouldNotBlur()
    {
        // 图标盒尺寸来自 font-size（.ion-icon 是 width: 1em），1.125rem = 18px 这类值经
        // flex 居中很容易落在 x.5 上，因此小数原点不是边缘情况。位图落在半像素位置时每个
        // 源像素都要在两个目标像素间插值，整幅图均匀变糊。
        var bg = LoadSvg(CircleSvg);
        var source = bg.RenderAtSize(24, 24)!;

        var measured = new List<double>();
        foreach (float offset in new[] { 0f, 0.25f, 0.5f, 0.75f })
        {
            using var bmp = DrawOnce(source, new RectF(10 + offset, 10 + offset, 24, 24), 64);
            measured.Add(Grad(bmp));
        }

        // 吸附后各相位的锐度应当一致（此前 0.5px 偏移会让梯度掉约 40%）。
        double min = measured.Min(), max = measured.Max();
        max.ShouldBeGreaterThan(0);
        ((max - min) / max).ShouldBeLessThan(0.02);
    }

    [Fact]
    public void DrawImage_FractionalPosition_OnScaledCanvas_ShouldSnapInDeviceSpace()
    {
        // 吸附必须在设备空间做：3× 画布上逻辑 1/6 px 恰好是 0.5 设备像素，
        // 按逻辑空间取整会把它当成"接近 0"而放过，模糊照旧。
        var bg = LoadSvg(CircleSvg);
        var source = bg.RenderAtSize(72, 72)!;

        var measured = new List<double>();
        foreach (float offset in new[] { 0f, 1f / 6f, 0.5f })
        {
            using var bmp = DrawOnce(
                source, new RectF(4 + offset, 4 + offset, 24, 24), canvasSize: 128, scale: 3f);
            measured.Add(Grad(bmp));
        }

        double min = measured.Min(), max = measured.Max();
        max.ShouldBeGreaterThan(0);
        ((max - min) / max).ShouldBeLessThan(0.02);
    }

    [Fact]
    public void DrawImage_Snapping_ShouldNotMoveImageByMoreThanHalfPixel()
    {
        // 吸附是四舍五入，位移上限半像素——不应造成可感知的错位。
        var bg = LoadSvg(CircleSvg);
        var source = bg.RenderAtSize(24, 24)!;

        foreach (float offset in new[] { 0f, 0.25f, 0.5f, 0.75f })
        {
            using var bmp = DrawOnce(source, new RectF(10 + offset, 10, 24, 24), 64);

            int firstDark = -1;
            for (int x = 0; x < bmp.Width && firstDark < 0; x++)
                if (bmp.GetPixel(x, 22).Red < 200) firstDark = x;

            firstDark.ShouldBeGreaterThanOrEqualTo(0);
            // 请求 10+offset，落点与请求值相差不超过 1 个像素。
            Math.Abs(firstDark - (10 + offset)).ShouldBeLessThanOrEqualTo(1f);
        }
    }

    // ---- 4. 吸附不得在平铺背景上撕出缝隙 -----------------------------------

    [Theory]
    [InlineData(10f)]    // 整数 tile
    [InlineData(7.3f)]   // 小数 tile：逐块吸附会露缝的场景
    [InlineData(11.7f)]
    public void TiledBackground_ShouldHaveNoSeams(float tileSize)
    {
        // 逐块吸附原点却保持小数块尺寸，会在相邻块之间撕出亚像素白缝：
        // 7.3px 的小 tile 上实测缝隙像素 0 → 459（最亮 211），是肉眼可见的亮网格。
        // 平铺的相位由 TileImage 的连续步进决定，不能逐块改动。
        var root = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(100),
                Height = Length.Px(100),
                BackgroundImage = LoadSvg(SolidSvg),
                BackgroundRepeat = BackgroundRepeat.Repeat,
                BackgroundSize = BackgroundSize.Px(tileSize, tileSize),
            }
        };

        using var bitmap = new SKBitmap(100, 100);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            var engine = new MikoEngineBuilder().Build();
            engine.Initialize(root, [], canvas, 100, 100);
            engine.Render(canvas);
        }

        // 只看内部区域，避开右/下边缘本就未铺满的部分。
        int seams = 0;
        for (int y = 2; y < 80; y++)
            for (int x = 2; x < 80; x++)
                if (bitmap.GetPixel(x, y).Red > 40) seams++;

        seams.ShouldBe(0);
    }

    // ---- 绘制与度量 --------------------------------------------------------

    private static SKBitmap DrawOnce(SKBitmap source, RectF dst, int canvasSize, float scale = 1f)
    {
        var bitmap = new SKBitmap(canvasSize, canvasSize);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            if (scale != 1f) canvas.Scale(scale);
            new Painter(canvas).DrawImage(source, dst);
        }
        return bitmap;
    }

    private static SKBitmap RasterizeDirect(BackgroundImage bg, int size)
    {
        var bitmap = new SKBitmap(size, size);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(bg.RenderAtSize(size, size)!, 0, 0);
        }
        return bitmap;
    }

    /// <summary>梯度能量：相邻像素差的平方和，越大越锐。</summary>
    private static double Grad(SKBitmap b)
    {
        double sum = 0;
        for (int y = 0; y < b.Height - 1; y++)
        {
            for (int x = 0; x < b.Width - 1; x++)
            {
                int c = b.GetPixel(x, y).Red;
                sum += Math.Pow(c - b.GetPixel(x + 1, y).Red, 2)
                     + Math.Pow(c - b.GetPixel(x, y + 1).Red, 2);
            }
        }
        return sum;
    }

    /// <summary>逐行灰度标准差的均值：条纹降采样后残留的明暗条带（摩尔纹）强度。</summary>
    private static double Moire(SKBitmap b)
    {
        double total = 0;
        for (int y = 0; y < b.Height; y++)
        {
            double sum = 0, sumSq = 0;
            for (int x = 0; x < b.Width; x++)
            {
                int v = b.GetPixel(x, y).Red;
                sum += v;
                sumSq += (double)v * v;
            }
            double mean = sum / b.Width;
            total += Math.Sqrt(Math.Max(0, sumSq / b.Width - mean * mean));
        }
        return total / b.Height;
    }

    private static double Mean(SKBitmap b)
    {
        double sum = 0;
        for (int y = 0; y < b.Height; y++)
            for (int x = 0; x < b.Width; x++) sum += b.GetPixel(x, y).Red;
        return sum / (b.Width * b.Height);
    }

    /// <summary>过渡像素数：既非纯图形色也非纯背景色的像素数，越少越锐。</summary>
    private static int Mid(SKBitmap b)
    {
        int n = 0;
        for (int y = 0; y < b.Height; y++)
        {
            for (int x = 0; x < b.Width; x++)
            {
                int v = b.GetPixel(x, y).Red;
                if (v >= 8 && v <= 247) n++;
            }
        }
        return n;
    }
}
