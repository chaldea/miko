using Miko.Platform.Video;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Platform.Video;

/// <summary>
/// NV12→RGB 的 SkSL 合成。这条路径是 SkiaSharp 缺少 <c>SKYUVAInfo</c> / 多平面
/// <c>FromTextures</c> 时的替代实现，所有系统硬解后端都依赖它，故用已知色值锁死其正确性。
/// <para>无 GPU 上下文时在 CPU surface 上合成，因此可在 CI 无显卡环境运行。</para>
/// </summary>
public class Nv12FrameComposerTests
{
    /// <summary>按窄带 NV12 构造一帧纯色的 Y / UV 两张平面图像。</summary>
    private static (SKImage y, SKImage uv) SolidPlanes(byte luma, byte u, byte v, int width = 16, int height = 16)
    {
        var yInfo = new SKImageInfo(width, height, SKColorType.R8Unorm, SKAlphaType.Opaque);
        var yPixels = new byte[yInfo.RowBytes * height];
        Array.Fill(yPixels, luma);

        int uvWidth = width / 2;
        int uvHeight = height / 2;
        var uvInfo = new SKImageInfo(uvWidth, uvHeight, SKColorType.Rg88, SKAlphaType.Opaque);
        var uvPixels = new byte[uvInfo.RowBytes * uvHeight];
        // Rg88 交错：r=U, g=V。
        for (int i = 0; i < uvPixels.Length; i += 2)
        {
            uvPixels[i] = u;
            uvPixels[i + 1] = v;
        }

        var yImage = SKImage.FromPixelCopy(yInfo, yPixels);
        var uvImage = SKImage.FromPixelCopy(uvInfo, uvPixels);
        yImage.ShouldNotBeNull();
        uvImage.ShouldNotBeNull();
        return (yImage, uvImage);
    }

    private static SKColor CenterPixel(SKImage image)
    {
        var info = new SKImageInfo(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using var bitmap = new SKBitmap(info);
        image.ReadPixels(bitmap.PeekPixels()).ShouldBeTrue();
        return bitmap.GetPixel(image.Width / 2, image.Height / 2);
    }

    [Fact]
    public void Compose_NarrowRangeWhite_ProducesWhite()
    {
        // 窄带白：Y=235（上限），色度中性。
        var (y, uv) = SolidPlanes(luma: 235, u: 128, v: 128);
        using (y) using (uv)
        {
            using var result = Nv12FrameComposer.Compose(y, uv, 16, 16, grContext: null);

            result.ShouldNotBeNull();
            var px = CenterPixel(result);
            px.Red.ShouldBeGreaterThan((byte)248);
            px.Green.ShouldBeGreaterThan((byte)248);
            px.Blue.ShouldBeGreaterThan((byte)248);
            px.Alpha.ShouldBe((byte)255);
        }
    }

    [Fact]
    public void Compose_NarrowRangeBlack_ProducesBlack()
    {
        // 窄带黑：Y=16（下限）。若实现漏了 -16/255 的窄带偏移，这里会明显偏灰。
        var (y, uv) = SolidPlanes(luma: 16, u: 128, v: 128);
        using (y) using (uv)
        {
            using var result = Nv12FrameComposer.Compose(y, uv, 16, 16, grContext: null);

            result.ShouldNotBeNull();
            var px = CenterPixel(result);
            px.Red.ShouldBeLessThan((byte)8);
            px.Green.ShouldBeLessThan((byte)8);
            px.Blue.ShouldBeLessThan((byte)8);
        }
    }

    [Fact]
    public void Compose_RedChroma_ProducesRedDominantPixel()
    {
        // V（Rg88 的 g 通道）远高于中性 → 红色分量主导。验证 U/V 没有接反。
        var (y, uv) = SolidPlanes(luma: 128, u: 100, v: 230);
        using (y) using (uv)
        {
            using var result = Nv12FrameComposer.Compose(y, uv, 16, 16, grContext: null);

            result.ShouldNotBeNull();
            var px = CenterPixel(result);
            px.Red.ShouldBeGreaterThan(px.Green);
            px.Red.ShouldBeGreaterThan(px.Blue);
        }
    }

    [Fact]
    public void Compose_BlueChroma_ProducesBlueDominantPixel()
    {
        // U（r 通道）远高于中性 → 蓝色分量主导（与上一例互为对照）。
        var (y, uv) = SolidPlanes(luma: 128, u: 230, v: 100);
        using (y) using (uv)
        {
            using var result = Nv12FrameComposer.Compose(y, uv, 16, 16, grContext: null);

            result.ShouldNotBeNull();
            var px = CenterPixel(result);
            px.Blue.ShouldBeGreaterThan(px.Red);
            px.Blue.ShouldBeGreaterThan(px.Green);
        }
    }

    [Fact]
    public void Compose_OutputMatchesRequestedSize()
    {
        var (y, uv) = SolidPlanes(luma: 128, u: 128, v: 128, width: 32, height: 24);
        using (y) using (uv)
        {
            using var result = Nv12FrameComposer.Compose(y, uv, 32, 24, grContext: null);

            result.ShouldNotBeNull();
            result.Width.ShouldBe(32);
            result.Height.ShouldBe(24);
        }
    }

    [Fact]
    public void Compose_ZeroSize_ReturnsNull()
    {
        var (y, uv) = SolidPlanes(luma: 128, u: 128, v: 128);
        using (y) using (uv)
        {
            Nv12FrameComposer.Compose(y, uv, 0, 0, grContext: null).ShouldBeNull();
        }
    }

    [Theory]
    [InlineData(480, VideoColorSpace.Bt601)]
    [InlineData(719, VideoColorSpace.Bt601)]
    [InlineData(720, VideoColorSpace.Bt709)]
    [InlineData(1080, VideoColorSpace.Bt709)]
    public void SelectColorSpace_SwitchesAtHdThreshold(int height, VideoColorSpace expected)
        => Nv12FrameComposer.SelectColorSpace(height).ShouldBe(expected);

    [Fact]
    public void ShaderCompiles_WithoutError()
    {
        // 触发一次合成以强制编译 SkSL，随后断言没有记录编译错误。
        var (y, uv) = SolidPlanes(luma: 128, u: 128, v: 128);
        using (y) using (uv)
        {
            using var _ = Nv12FrameComposer.Compose(y, uv, 16, 16, grContext: null);
        }

        Nv12FrameComposer.ShaderError.ShouldBeNull();
    }
}
