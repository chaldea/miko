using System.Runtime.CompilerServices;

namespace Miko.Components.Webcam.Mock;

/// <summary>Plays Miko.MockServer's panorama and simulates PTZ locally, without cloud credentials.</summary>
public sealed class MockWebcamProvider : IWebcamProvider
{
    private readonly Uri _source;
    private readonly TimeProvider _clock;
    private readonly string? _fallbackSource;
    // Weak keys release state with the demo device; each device instance has its own view.
    private readonly ConditionalWeakTable<WebcamDevice, WebcamPanorama> _views = new();

    public MockWebcamProvider(string source = "rtsp://127.0.0.1:8554/webcam", TimeProvider? clock = null, string? fallbackSource = null)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || uri.Scheme != "rtsp")
            throw new ArgumentException("Expected an rtsp:// URI.", nameof(source));
        _source = uri;
        _clock = clock ?? TimeProvider.System;
        if (fallbackSource != null && (!Uri.TryCreate(fallbackSource, UriKind.Absolute, out var fallback) || fallback.Scheme is not ("http" or "https")))
            throw new ArgumentException("Expected an HTTP(S) MP4 fallback URI.", nameof(fallbackSource));
        _fallbackSource = fallbackSource;
    }

    public Task<WebcamInfo> GetInfoAsync(WebcamDevice device, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new WebcamInfo(device.Name ?? "Miko panorama", true, WebcamFeatures.Pan | WebcamFeatures.Tilt | WebcamFeatures.Zoom)
        { IsOnLocalNetwork = true, Model = "Mock 1280x360 / 5 fps", Qualities = [WebcamQuality.Smooth] });
    }

    public Task<WebcamStream> GetLiveStreamAsync(WebcamDevice device, WebcamQuality quality, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new WebcamStream(_fallbackSource ?? _source.AbsoluteUri,
            _fallbackSource == null ? "application/x-rtsp" : "video/mp4", WebcamTransport.LocalNetwork, WebcamQuality.Smooth)
        { Panorama = View(device), Loop = _fallbackSource != null });
    }

    public Task StartPtzAsync(WebcamDevice device, PtzDirection direction, PtzSpeed speed, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); View(device).Start(direction, speed); return Task.CompletedTask; }

    public Task StopPtzAsync(WebcamDevice device, PtzDirection direction, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); View(device).Stop(direction); return Task.CompletedTask; }

    public Task<WebcamSnapshot> CaptureAsync(WebcamDevice device, CancellationToken cancellationToken = default) => throw new NotSupportedException("Mock webcam does not offer remote snapshots.");
    public Task SetPrivacyAsync(WebcamDevice device, bool enabled, CancellationToken cancellationToken = default) => throw new NotSupportedException("Mock webcam does not offer privacy mode.");
    public Task MirrorAsync(WebcamDevice device, CancellationToken cancellationToken = default) => throw new NotSupportedException("Mock webcam does not offer mirroring.");
    private WebcamPanorama View(WebcamDevice device) => _views.GetValue(device, _ => new WebcamPanorama(_clock));
}
