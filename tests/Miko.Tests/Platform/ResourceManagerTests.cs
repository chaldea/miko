using Miko.Common;
using Miko.Platform.Resources;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Platform;

/// <summary>
/// 真实 <see cref="ResourceManager"/> 的离线协议解码（file:// / data: / 失败回退）。
/// http(s):// 与 res:// 由 Media 示例端到端覆盖；这里只验证无需网络/嵌入资源的路径。
/// </summary>
public class ResourceManagerTests
{
    private static byte[] EncodePng(int w, int h)
    {
        using var bmp = new SKBitmap(w, h);
        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    [Fact]
    public async Task LoadAsync_FileScheme_DecodesLocalPng()
    {
        var path = Path.Combine(Path.GetTempPath(), $"miko-rm-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(path, EncodePng(32, 24));
        try
        {
            var rm = new ResourceManager();
            var bmp = await rm.LoadAsync($"file://{path}");

            bmp.ShouldNotBeNull();
            bmp!.Width.ShouldBe(32);
            bmp.Height.ShouldBe(24);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAsync_BarePath_DecodesLocalPng()
    {
        var path = Path.Combine(Path.GetTempPath(), $"miko-rm-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(path, EncodePng(10, 10));
        try
        {
            var bmp = await new ResourceManager().LoadAsync(path);
            bmp.ShouldNotBeNull();
            bmp!.Width.ShouldBe(10);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAsync_DataUri_DecodesBase64Png()
    {
        var b64 = Convert.ToBase64String(EncodePng(16, 16));
        var bmp = await new ResourceManager().LoadAsync($"data:image/png;base64,{b64}");

        bmp.ShouldNotBeNull();
        bmp!.Width.ShouldBe(16);
        bmp.Height.ShouldBe(16);
    }

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsNull_DoesNotThrow()
    {
        var bmp = await new ResourceManager().LoadAsync("file://does/not/exist.png");
        bmp.ShouldBeNull();
    }

    [Fact]
    public async Task LoadAsync_EmptySource_ReturnsNull()
    {
        (await new ResourceManager().LoadAsync(MediaSource.Empty)).ShouldBeNull();
    }

    // ---- SVG 过采样栅格化（ISSUE-137）------------------------------------
    // <img> 的显示尺寸在加载时还未知（布局尚未发生），无法像背景图那样按绘制尺寸栅格化。
    // 改为一次性过采样到足够分辨率，使之后无论布局到多大、设备密度多高，都是缩小采样（无损）
    // 而非放大插值（模糊）。内禀尺寸仍须为 viewBox，否则布局会按位图像素尺寸炸开。

    private static async Task<string> WriteSvgAsync(string viewBox)
    {
        var path = Path.Combine(Path.GetTempPath(), $"miko-rm-{Guid.NewGuid():N}.svg");
        await File.WriteAllTextAsync(path,
            $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='{viewBox}'>" +
            "<rect width='100%' height='100%' fill='black'/></svg>");
        return path;
    }

    [Fact]
    public async Task LoadAsync_SmallSvg_RasterizesAboveViewBox()
    {
        var path = await WriteSvgAsync("0 0 24 24");
        try
        {
            var bmp = await new ResourceManager().LoadAsync(path);

            bmp.ShouldNotBeNull();
            // 24×24 的 viewBox 按原尺寸栅格化，在 3× 屏上放到 72px 就是放大插值。
            bmp!.Width.ShouldBeGreaterThan(24);
            bmp.Height.ShouldBeGreaterThan(24);
            // 等比放大，纵横比不变。
            bmp.Width.ShouldBe(bmp.Height);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task GetIntrinsicSize_OversampledSvg_ReturnsViewBoxSize()
    {
        var path = await WriteSvgAsync("0 0 24 24");
        try
        {
            var rm = new ResourceManager();
            var bmp = await rm.LoadAsync(path);

            // 内禀尺寸（布局输入）必须是 viewBox，而不是过采样后的位图尺寸。
            rm.GetIntrinsicSize(path).ShouldBe((24, 24));
            bmp!.Width.ShouldNotBe(24);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task LoadAsync_LargeSvg_KeepsViewBoxResolution()
    {
        // viewBox 已足够大（头像类资源常见）：不额外放大，也不报告替代内禀尺寸。
        var path = await WriteSvgAsync("0 0 512 512");
        try
        {
            var rm = new ResourceManager();
            var bmp = await rm.LoadAsync(path);

            bmp!.Width.ShouldBe(512);
            rm.GetIntrinsicSize(path).ShouldBeNull();
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task GetIntrinsicSize_BitmapSource_ReturnsNull()
    {
        // 位图源的内禀尺寸就是位图像素尺寸，引擎无需替代值。
        var path = Path.Combine(Path.GetTempPath(), $"miko-rm-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(path, EncodePng(16, 16));
        try
        {
            var rm = new ResourceManager();
            await rm.LoadAsync(path);

            rm.GetIntrinsicSize(path).ShouldBeNull();
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task LoadAsync_SameSource_IsCached()
    {
        var path = Path.Combine(Path.GetTempPath(), $"miko-rm-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(path, EncodePng(8, 8));
        try
        {
            var rm = new ResourceManager();
            MediaSource src = $"file://{path}";
            var a = await rm.LoadAsync(src);
            var b = await rm.LoadAsync(src);

            // 缓存按 Raw 命中，二次加载返回同一位图实例。
            a.ShouldNotBeNull();
            b.ShouldBeSameAs(a);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
