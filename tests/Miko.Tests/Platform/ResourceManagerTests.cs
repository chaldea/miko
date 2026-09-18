using System.Reflection;
using Miko.Common;
using Miko.Platform.Resources;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Platform;

/// <summary>
/// 真实 <see cref="ResourceManager"/> 的离线协议解码（file:// / res:// / data: / 失败回退）
/// 与资源诊断（<see cref="IResourceDiagnostics"/>）。
/// http(s):// 由 Media 示例端到端覆盖；这里只验证无需网络的路径。
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

    // ---- res:// 嵌入资源（ISSUE-139）--------------------------------------
    // 资源路径不含程序集名，换算由 EmbeddedResources 完成（详见 EmbeddedResourcesTests）。
    // 这里验证 ResourceManager 真的走了那条换算并解码出位图。

    private sealed class StubAssemblyProvider : IResourceAssemblyProvider
    {
        private readonly Assembly[] _assemblies;
        public StubAssemblyProvider(params Assembly[] assemblies) => _assemblies = assemblies;
        public IEnumerable<Assembly> GetResourceAssemblies() => _assemblies;
    }

    private static ResourceManager WithTestAssembly() =>
        new(assemblyProvider: new StubAssemblyProvider(typeof(ResourceManagerTests).Assembly));

    [Fact]
    public async Task LoadAsync_ResourceScheme_AssemblyAgnosticPath_Decodes()
    {
        var bmp = await WithTestAssembly().LoadAsync("res://TestAssets/Resources/logo.svg");

        bmp.ShouldNotBeNull();
        bmp!.Width.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task LoadAsync_ResourceScheme_NestedPath_Decodes()
    {
        var bmp = await WithTestAssembly().LoadAsync("res://TestAssets/Resources/Nested/badge.svg");

        bmp.ShouldNotBeNull();
        bmp!.Width.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task LoadAsync_ResourceScheme_FullManifestName_StillDecodes()
    {
        // 旧写法（含程序集名）继续可用，迁移无需一次性完成。
        var bmp = await WithTestAssembly().LoadAsync("res://Miko.Tests.TestAssets.Resources.logo.svg");

        bmp.ShouldNotBeNull();
    }

    [Fact]
    public async Task LoadAsync_ResourceScheme_MissingResource_ReturnsNull()
    {
        (await WithTestAssembly().LoadAsync("res://TestAssets/Resources/nope.svg")).ShouldBeNull();
    }

    [Fact]
    public async Task GetIntrinsicSize_ResourceSvg_ReturnsViewBoxSize()
    {
        // 内禀尺寸按 res:// 的原始书写串（source.Raw）记账，而非解析出的清单名。
        var rm = WithTestAssembly();
        const string src = "res://TestAssets/Resources/logo.svg";

        var bmp = await rm.LoadAsync(src);

        rm.GetIntrinsicSize(src).ShouldBe((24, 24));
        bmp!.Width.ShouldNotBe(24);
    }

    // ---- IResourceDiagnostics（DevTools 资源面板）-------------------------

    [Fact]
    public async Task GetCachedResources_ReportsLoadedEntry()
    {
        var rm = WithTestAssembly();
        const string src = "res://TestAssets/Resources/logo.svg";
        var bmp = await rm.LoadAsync(src);

        var entry = rm.GetCachedResources().ShouldHaveSingleItem();
        entry.Source.ShouldBe(src);
        // 协议要保留：缓存字典只按 Raw 索引任务，协议是单独记账的。
        entry.Scheme.ShouldBe(MediaSourceScheme.Resource);
        entry.State.ShouldBe(CachedResourceState.Loaded);
        entry.PixelWidth.ShouldBe(bmp!.Width);
        entry.ByteCount.ShouldBe(bmp.ByteCount);
    }

    [Fact]
    public async Task GetCachedResources_ReportsFailedEntry()
    {
        var rm = WithTestAssembly();
        await rm.LoadAsync("res://TestAssets/Resources/nope.svg");

        var entry = rm.GetCachedResources().ShouldHaveSingleItem();
        entry.State.ShouldBe(CachedResourceState.Failed);
        entry.ByteCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetCachedResources_DeduplicatesRepeatedLoads()
    {
        var rm = WithTestAssembly();
        const string src = "res://TestAssets/Resources/logo.svg";
        await rm.LoadAsync(src);
        await rm.LoadAsync(src);

        rm.GetCachedResources().Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetNetworkResources_EmptyWithoutNetworkLoads()
    {
        var rm = WithTestAssembly();
        await rm.LoadAsync("res://TestAssets/Resources/logo.svg");

        // 只有 http(s) 源才进网络日志——嵌入资源与本地文件不算网络请求。
        rm.GetNetworkResources().ShouldBeEmpty();
    }

    [Fact]
    public async Task Version_AdvancesWhenCacheGainsAnEntry()
    {
        // DevTools 靠这个单调版本号判断「面板是否要重建」（空闲跳帧下不能用轮询）。
        var rm = WithTestAssembly();
        var before = rm.Version;

        await rm.LoadAsync("res://TestAssets/Resources/logo.svg");

        rm.Version.ShouldBeGreaterThan(before);
    }

    [Fact]
    public async Task Version_DoesNotAdvanceOnCacheHit()
    {
        var rm = WithTestAssembly();
        const string src = "res://TestAssets/Resources/logo.svg";
        await rm.LoadAsync(src);
        var afterFirst = rm.Version;

        await rm.LoadAsync(src);

        // 二次加载完全命中缓存：没有新内容，面板不必重建。
        rm.Version.ShouldBe(afterFirst);
    }
}
