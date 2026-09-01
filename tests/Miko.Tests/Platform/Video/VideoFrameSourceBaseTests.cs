using Miko.Platform.Video;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Platform.Video;

/// <summary>
/// 帧源骨架的线程与生命周期契约：CPU 回退包装、只保留最近一帧、GPU 资源按时归还、
/// 释放后不再产帧。五个平台后端都继承这套行为，因此在这里集中验证一次。
/// </summary>
public class VideoFrameSourceBaseTests
{
    /// <summary>可实例化的最小子类（基类为抽象，仅提供骨架）。</summary>
    private sealed class TestFrameSource : VideoFrameSourceBase;

    private static VideoFrameBuffer RgbaFrame(int width = 4, int height = 4, byte fill = 0x7F, Action? onReleased = null)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        var pixels = new byte[info.RowBytes * height];
        Array.Fill(pixels, fill);
        return VideoFrameBuffer.FromCpuPlanes(
            width, height, VideoPixelFormat.Rgba8888, TimeSpan.Zero,
            [pixels], [info.RowBytes], onReleased);
    }

    [Fact]
    public void AcquireCurrentFrame_BeforeAnyPush_ReturnsNull()
    {
        using var source = new TestFrameSource();

        source.AcquireCurrentFrame(null).ShouldBeNull();
    }

    [Fact]
    public void AcquireCurrentFrame_AfterCpuPush_WrapsWithoutGpuContext()
    {
        using var source = new TestFrameSource();
        source.PushFrame(RgbaFrame(8, 6));

        // grContext == null 是离屏/软件渲染路径：CPU 帧仍须成像。
        var image = source.AcquireCurrentFrame(null);

        image.ShouldNotBeNull();
        image.Width.ShouldBe(8);
        image.Height.ShouldBe(6);
        source.LastFrameWasZeroCopy.ShouldBeFalse();
    }

    [Fact]
    public void AcquireCurrentFrame_WithoutNewFrame_KeepsShowingPreviousImage()
    {
        using var source = new TestFrameSource();
        source.PushFrame(RgbaFrame());

        var first = source.AcquireCurrentFrame(null);
        var second = source.AcquireCurrentFrame(null);

        // 无新帧时应继续返回同一张图像，而不是闪回 null（否则画面会闪黑）。
        first.ShouldNotBeNull();
        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void PushFrame_ReplacingUnconsumedFrame_ReleasesDroppedFrame()
    {
        using var source = new TestFrameSource();
        bool firstReleased = false;

        source.PushFrame(RgbaFrame(onReleased: () => firstReleased = true));
        source.PushFrame(RgbaFrame());

        // 渲染慢于解码时旧帧被丢弃，其 GPU 资源必须立即交还解码器，否则解码器会缺缓冲停摆。
        firstReleased.ShouldBeTrue();
    }

    [Fact]
    public void AcquireCurrentFrame_ReleasesConsumedFrame()
    {
        using var source = new TestFrameSource();
        bool released = false;
        source.PushFrame(RgbaFrame(onReleased: () => released = true));

        source.AcquireCurrentFrame(null);

        released.ShouldBeTrue();
    }

    [Fact]
    public void PushFrame_AfterDispose_ReleasesImmediatelyAndDropsFrame()
    {
        var source = new TestFrameSource();
        source.Dispose();
        bool released = false;

        source.PushFrame(RgbaFrame(onReleased: () => released = true));

        released.ShouldBeTrue();
        source.AcquireCurrentFrame(null).ShouldBeNull();
    }

    [Fact]
    public void Dispose_WithUnconsumedFrame_ReleasesIt()
    {
        var source = new TestFrameSource();
        bool released = false;
        source.PushFrame(RgbaFrame(onReleased: () => released = true));

        source.Dispose();

        released.ShouldBeTrue();
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var source = new TestFrameSource();
        source.PushFrame(RgbaFrame());
        source.AcquireCurrentFrame(null);

        source.Dispose();
        Should.NotThrow(source.Dispose);
    }

    [Fact]
    public void AcquireCurrentFrame_GpuFrameWithoutContext_ReturnsNullAndReleases()
    {
        using var source = new TestFrameSource();
        bool released = false;
        var gpuFrame = VideoFrameBuffer.FromTextures(
            4, 4, VideoPixelFormat.Rgba8888, TimeSpan.Zero,
            [new VideoTexturePlane(Target: 0x0DE1, Id: 1, Format: 0x8058, Width: 4, Height: 4)],
            onReleased: () => released = true);

        source.PushFrame(gpuFrame);

        // 无 GPU 上下文时纹理句柄无从采样：放弃该帧，但资源仍须归还。
        source.AcquireCurrentFrame(null).ShouldBeNull();
        released.ShouldBeTrue();
    }

    [Fact]
    public void UploadPixels_WithRowPadding_ProducesCorrectlySizedImage()
    {
        using var source = new TestFrameSource();
        const int width = 5, height = 4;
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        int paddedStride = info.RowBytes + 16;      // 硬解器常见的行填充
        var pixels = new byte[paddedStride * height];
        Array.Fill(pixels, (byte)0x40);

        source.PushFrame(VideoFrameBuffer.FromCpuPlanes(
            width, height, VideoPixelFormat.Rgba8888, TimeSpan.Zero,
            [pixels], [paddedStride]));

        var image = source.AcquireCurrentFrame(null);

        // stride 未被正确处理时画面会斜切；尺寸正确是必要条件。
        image.ShouldNotBeNull();
        image.Width.ShouldBe(width);
        image.Height.ShouldBe(height);
    }

    [Fact]
    public void Nv12CpuFrame_ComposesThroughShader()
    {
        using var source = new TestFrameSource();
        const int width = 16, height = 16;

        var yInfo = new SKImageInfo(width, height, SKColorType.R8Unorm, SKAlphaType.Opaque);
        var yPixels = new byte[yInfo.RowBytes * height];
        Array.Fill(yPixels, (byte)235);

        var uvInfo = new SKImageInfo(width / 2, height / 2, SKColorType.Rg88, SKAlphaType.Opaque);
        var uvPixels = new byte[uvInfo.RowBytes * (height / 2)];
        Array.Fill(uvPixels, (byte)128);

        source.PushFrame(VideoFrameBuffer.FromCpuPlanes(
            width, height, VideoPixelFormat.Nv12, TimeSpan.Zero,
            [yPixels, uvPixels], [yInfo.RowBytes, uvInfo.RowBytes]));

        var image = source.AcquireCurrentFrame(null);

        image.ShouldNotBeNull();
        image.Width.ShouldBe(width);
        image.Height.ShouldBe(height);
    }

    [Fact]
    public void Nv12CpuFrame_WithPaddedChromaPlane_DoesNotTintTopRows()
    {
        // 回归：硬解器把 Y 平面行数向上对齐（180 → 192），若按未对齐高度定位 UV，
        // 就会把 Y 尾部的零填充行当成色度读入，画面顶部出现一条绿边（U=V=0 即纯绿）。
        // 这里模拟"UV 平面自身带零填充尾部"的布局，断言顶部若干行仍是中性灰而非绿色。
        using var source = new TestFrameSource();
        const int width = 32, height = 18;
        int uvHeight = (height + 1) / 2;

        var yInfo = new SKImageInfo(width, height, SKColorType.R8Unorm, SKAlphaType.Opaque);
        var yPixels = new byte[yInfo.RowBytes * height];
        Array.Fill(yPixels, (byte)128);

        var uvInfo = new SKImageInfo(width / 2, uvHeight, SKColorType.Rg88, SKAlphaType.Opaque);
        var uvPixels = new byte[uvInfo.RowBytes * uvHeight];
        Array.Fill(uvPixels, (byte)128);    // 中性色度：应得灰色，绿色即为错位

        source.PushFrame(VideoFrameBuffer.FromCpuPlanes(
            width, height, VideoPixelFormat.Nv12, TimeSpan.Zero,
            [yPixels, uvPixels], [yInfo.RowBytes, uvInfo.RowBytes]));

        var image = source.AcquireCurrentFrame(null);
        image.ShouldNotBeNull();

        var outInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using var bitmap = new SKBitmap(outInfo);
        image.ReadPixels(bitmap.PeekPixels()).ShouldBeTrue();

        // 中性色度 → R/G/B 应彼此接近；绿色偏移会让 G 远高于 R 与 B。
        for (int y = 0; y < 4; y++)
        {
            var px = bitmap.GetPixel(width / 2, y);
            Math.Abs(px.Green - px.Red).ShouldBeLessThan(24);
            Math.Abs(px.Green - px.Blue).ShouldBeLessThan(24);
        }
    }

    [Fact]
    public async Task PushAndAcquire_FromDifferentThreads_StaysConsistent()
    {
        using var source = new TestFrameSource();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));

        // 模拟真实线程模型：解码线程持续推帧，渲染线程持续取帧。
        var producer = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
                source.PushFrame(RgbaFrame());
        });

        var consumer = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
                source.AcquireCurrentFrame(null);
        });

        await Should.NotThrowAsync(Task.WhenAll(producer, consumer));
    }
}
