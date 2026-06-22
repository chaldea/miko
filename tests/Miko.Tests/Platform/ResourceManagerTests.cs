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
