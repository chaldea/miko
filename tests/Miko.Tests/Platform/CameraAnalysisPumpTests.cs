using Miko.Graphics;
using Miko.Hosting;
using Miko.Platform.Camera;
using Shouldly;

namespace Miko.Tests.Platform;

/// <summary>
/// The pump hands frames from a backend's capture callback to an analyzer on the thread pool, keeping
/// only the latest: the callback never waits, a busy analyzer drops frames.
/// </summary>
public class CameraAnalysisPumpTests
{
    [Fact]
    public void Should_accept_nothing_without_an_analyzer()
    {
        var pump = new CameraAnalysisPump();

        pump.IsReady.ShouldBeFalse();
        pump.TryPost(new byte[4], 2, 2, 2, 0, false, TimeSpan.Zero).ShouldBeFalse();
    }

    [Fact]
    public void Should_pack_strided_rows_and_carry_the_orientation()
    {
        var analyzer = new BlockingAnalyzer();
        var pump = new CameraAnalysisPump { Analyzer = analyzer };
        byte[] plane = [1, 2, 99, 3, 4, 99];

        pump.TryPost(plane, 2, 2, 3, 90, true, TimeSpan.FromSeconds(1)).ShouldBeTrue();
        analyzer.Release();

        analyzer.Analyzed.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
        analyzer.Luminance.ShouldBe([1, 2, 3, 4]);
        analyzer.Rotation.ShouldBe(90);
        analyzer.Mirrored.ShouldBeTrue();
        analyzer.Timestamp.ShouldBe(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Should_drop_frames_while_the_analyzer_is_busy()
    {
        var analyzer = new BlockingAnalyzer();
        var pump = new CameraAnalysisPump { Analyzer = analyzer };

        pump.TryPost(new byte[4], 2, 2, 2, 0, false, TimeSpan.Zero).ShouldBeTrue();
        pump.IsReady.ShouldBeFalse();
        pump.TryPost(new byte[4], 2, 2, 2, 0, false, TimeSpan.Zero).ShouldBeFalse();

        analyzer.Release();
        analyzer.Analyzed.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
        SpinWait.SpinUntil(() => pump.IsReady, TimeSpan.FromSeconds(5)).ShouldBeTrue();
        pump.TryPost(new byte[4], 2, 2, 2, 0, false, TimeSpan.Zero).ShouldBeTrue();
        analyzer.Release();
    }

    [Fact]
    public void Should_convert_rgb_frames_to_luminance()
    {
        var analyzer = new BlockingAnalyzer();
        var pump = new CameraAnalysisPump { Analyzer = analyzer };
        // White, black, pure red, pure blue (BGRA when bgr: blue first).
        byte[] bgra = [255, 255, 255, 255, 0, 0, 0, 255, 0, 0, 255, 255, 255, 0, 0, 255];

        pump.TryPostRgb(bgra, 4, 1, 16, bgr: true, 0, false, TimeSpan.Zero).ShouldBeTrue();
        analyzer.Release();

        analyzer.Analyzed.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
        analyzer.Luminance.ShouldBe([255, 0, 76, 28]);
    }

    [Fact]
    public void Should_report_analyzer_failures_and_keep_going()
    {
        var pump = new CameraAnalysisPump { Analyzer = new ThrowingAnalyzer() };
        var failed = new ManualResetEventSlim();
        pump.AnalyzerFailed += _ => failed.Set();

        pump.TryPost(new byte[1], 1, 1, 1, 0, false, TimeSpan.Zero).ShouldBeTrue();

        failed.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
        SpinWait.SpinUntil(() => pump.IsReady, TimeSpan.FromSeconds(5)).ShouldBeTrue();
    }

    [Fact]
    public void Should_read_back_raster_and_vector_images()
    {
        var graphics = new MikoEngineBuilder().Build().Graphics;
        using var raster = graphics.CreateImage(2, 1, ImagePixelFormat.Rgba8888, new byte[] { 255, 0, 0, 255, 0, 0, 255, 128 })!;
        var pixels = new byte[8];
        raster.ReadPixels(pixels, ImagePixelFormat.Bgra8888).ShouldBeTrue();
        pixels[..4].ShouldBe(new byte[] { 0, 0, 255, 255 });
        pixels[7].ShouldBe((byte)128);
        raster.ReadPixels(new byte[4], ImagePixelFormat.Rgba8888).ShouldBeFalse();
        raster.ReadPixels(new byte[2], ImagePixelFormat.R8).ShouldBeFalse();

        var svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 4 4' width='4' height='4'><rect width='4' height='4' fill='#00ff00'/></svg>"u8;
        using var vector = graphics.DecodeImage(svg)!;
        var vectorPixels = new byte[4 * 4 * 4];
        vector.ReadPixels(vectorPixels, ImagePixelFormat.Rgba8888).ShouldBeTrue();
        vectorPixels[..4].ShouldBe(new byte[] { 0, 255, 0, 255 });
    }

    private sealed class BlockingAnalyzer : ICameraFrameAnalyzer
    {
        private readonly SemaphoreSlim _gate = new(0);
        public ManualResetEventSlim Analyzed { get; } = new();
        public byte[] Luminance { get; private set; } = [];
        public int Rotation { get; private set; }
        public bool Mirrored { get; private set; }
        public TimeSpan Timestamp { get; private set; }

        public void Release() => _gate.Release();

        public void Analyze(in CameraAnalysisFrame frame)
        {
            _gate.Wait();
            Luminance = frame.Luminance.ToArray();
            Rotation = frame.Rotation;
            Mirrored = frame.IsMirrored;
            Timestamp = frame.Timestamp;
            Analyzed.Set();
        }
    }

    private sealed class ThrowingAnalyzer : ICameraFrameAnalyzer
    {
        public void Analyze(in CameraAnalysisFrame frame) => throw new InvalidOperationException("boom");
    }
}
