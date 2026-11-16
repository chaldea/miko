using Microsoft.Extensions.DependencyInjection;
using Miko.Components.Webcam;
using Miko.Components.Webcam.Mock;
using Shouldly;

namespace Miko.Components.Tests.Webcam;

public sealed class MockWebcamProviderTests
{
    [Theory]
    [InlineData("https://demo.example/media/webcam.m3u8", "application/vnd.apple.mpegurl")]
    [InlineData("http://localhost:8080/media/demo.mp4", "video/mp4")]
    public async Task Should_use_explicit_http_source_with_panorama_and_loop(string url, string mime)
    {
        var services = new ServiceCollection().AddMikoMockWebcam(url);
        using var container = services.BuildServiceProvider();
        var stream = await container.GetRequiredService<IWebcamProvider>().GetLiveStreamAsync(new WebcamDevice("mock"), WebcamQuality.Smooth);
        stream.Source.ShouldBe(url);
        stream.MimeType.ShouldBe(mime);
        stream.Loop.ShouldBeTrue();
        stream.Panorama.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_clamp_ptz_and_zoom_to_panorama_edges_and_stop_motion()
    {
        var clock = new ManualClock();
        var provider = new MockWebcamProvider(clock: clock);
        var device = new WebcamDevice("mock");
        var stream = await provider.GetLiveStreamAsync(device, WebcamQuality.High);
        stream.Quality.ShouldBe(WebcamQuality.Smooth);
        var view = stream.Panorama!;
        await provider.StartPtzAsync(device, PtzDirection.UpLeft, PtzSpeed.Fast);
        clock.Advance(TimeSpan.FromSeconds(10));
        view.Crop.X.ShouldBe(0); view.Crop.Y.ShouldBe(0);
        await provider.StopPtzAsync(device, PtzDirection.UpLeft);
        var stopped = view.Crop;
        clock.Advance(TimeSpan.FromSeconds(10));
        view.Crop.ShouldBe(stopped);
        await provider.StartPtzAsync(device, PtzDirection.ZoomIn, PtzSpeed.Fast);
        clock.Advance(TimeSpan.FromSeconds(10));
        view.Crop.Height.ShouldBe(.25f);
        await provider.StartPtzAsync(device, PtzDirection.DownRight, PtzSpeed.Fast);
        clock.Advance(TimeSpan.FromSeconds(10));
        (view.Crop.X + view.Crop.Width).ShouldBe(1);
        (view.Crop.Y + view.Crop.Height).ShouldBe(1);
        await provider.StartPtzAsync(device, PtzDirection.ZoomOut, PtzSpeed.Fast);
        clock.Advance(TimeSpan.FromSeconds(10));
        view.Crop.Height.ShouldBe(1);
        (view.Crop.X + view.Crop.Width).ShouldBeLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task Should_isolate_identical_device_instances_and_honor_cancellation()
    {
        var clock = new ManualClock();
        var provider = new MockWebcamProvider(clock: clock);
        var first = new WebcamDevice("mock");
        var second = new WebcamDevice("mock");
        var a = (await provider.GetLiveStreamAsync(first, WebcamQuality.Smooth)).Panorama!;
        var b = (await provider.GetLiveStreamAsync(second, WebcamQuality.Smooth)).Panorama!;
        var before = b.Crop;
        await provider.StartPtzAsync(first, PtzDirection.Right, PtzSpeed.Fast);
        clock.Advance(TimeSpan.FromSeconds(2));
        a.Crop.ShouldNotBe(before); b.Crop.ShouldBe(before);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => provider.StartPtzAsync(second, PtzDirection.Left, PtzSpeed.Fast, cancelled.Token));
        b.Crop.ShouldBe(before);
    }

    [Fact]
    public void Should_keep_mock_registration_when_default_services_are_added()
    {
        var services = new ServiceCollection();
        services.AddMikoWebcam().AddMikoMockWebcam().AddMikoWebcam();
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IWebcamProvider>().ShouldBeOfType<MockWebcamProvider>();
    }

    [Fact]
    public async Task Should_loop_http_fallback_when_backend_cannot_decode_rtsp()
    {
        var services = new ServiceCollection();
        services.AddMikoMockWebcam("rtsp://localhost:8554/webcam", "http://localhost:8080/media/demo.mp4");
        using var container = services.BuildServiceProvider();
        var provider = container.GetRequiredService<IWebcamProvider>();
        var stream = await provider.GetLiveStreamAsync(new WebcamDevice("mock"), WebcamQuality.Smooth);
        stream.Source.ShouldBe("http://localhost:8080/media/demo.mp4");
        stream.Loop.ShouldBeTrue();
        stream.Panorama.ShouldNotBeNull();
    }
}
