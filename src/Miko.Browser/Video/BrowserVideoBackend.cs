using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Platform.Video;

namespace Miko.Browser.Video;

/// <summary>HTTP video decoded by the browser, composited through Miko's normal frame/crop pipeline.</summary>
[SupportedOSPlatform("browser")]
public sealed class BrowserVideoBackend(IGraphicsFactory graphics) : IVideoBackend
{
    public VideoBackendCapabilities Capabilities { get; } = new(false, false, ["video/mp4", "application/vnd.apple.mpegurl"]);
    public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
        => new BrowserVideoSession(graphics, source, options);
}

[SupportedOSPlatform("browser")]
public static class BrowserVideoServiceExtensions
{
    public static MikoAppBuilder UseBrowserVideo(this MikoAppBuilder builder)
    {
        builder.Services.Replace(ServiceDescriptor.Singleton<IVideoBackend, BrowserVideoBackend>());
        return builder;
    }
}

[SupportedOSPlatform("browser")]
internal sealed class BrowserVideoSession : IVideoSession, IVideoFrameSource
{
    private readonly IGraphicsFactory _graphics;
    private readonly int _id;
    private Image? _image;
    private byte[] _pixels = [];
    private bool _dirty, _disposed;

    public BrowserVideoSession(IGraphicsFactory graphics, VideoSourceDescriptor source, VideoSessionOptions options)
    {
        _graphics = graphics;
        _id = BrowserVideoInterop.Create(source.Uri, source.IsHls, OnEvent);
        SetMuted(options.Muted);
        SetVolume(options.InitialVolume);
        SetLoop(options.Loop);
        if (options.AutoPlay) Play();
    }

    public VideoSessionState State { get; private set; } = VideoSessionState.Loading;
    public TimeSpan Duration { get; private set; }
    public TimeSpan Position => TimeSpan.FromSeconds(BrowserVideoInterop.Position(_id));
    public int VideoWidth { get; private set; }
    public int VideoHeight { get; private set; }
    public string? ErrorMessage { get; private set; }
    public IVideoFrameSource FrameSource => this;
    public event Action<VideoSessionEvent>? Event;
    public void Play() => BrowserVideoInterop.Control(_id, "play", 0);
    public void Pause() => BrowserVideoInterop.Control(_id, "pause", 0);
    public void Seek(TimeSpan position) => BrowserVideoInterop.Control(_id, "seek", Math.Max(0, position.TotalSeconds));
    public void SetMuted(bool muted) => BrowserVideoInterop.Control(_id, "muted", muted ? 1 : 0);
    public void SetVolume(float volume) => BrowserVideoInterop.Control(_id, "volume", Math.Clamp(volume, 0, 1));
    public void SetLoop(bool loop) => BrowserVideoInterop.Control(_id, "loop", loop ? 1 : 0);
    public void SetPlaybackRate(float rate) => BrowserVideoInterop.Control(_id, "rate", Math.Clamp(rate, .25, 4));

    private void OnEvent(string notification)
    {
        if (_disposed) return;
        using var json = JsonDocument.Parse(notification);
        var data = json.RootElement;
        string kind = data.GetProperty("kind").GetString()!;
        double seconds = data.GetProperty("seconds").GetDouble();
        int width = data.GetProperty("width").GetInt32(), height = data.GetProperty("height").GetInt32();
        string message = data.GetProperty("message").GetString()!;
        switch (kind)
        {
            case "loaded":
                VideoWidth = (int)width; VideoHeight = (int)height;
                Duration = TimeSpan.FromSeconds(double.IsFinite(seconds) ? seconds : 0);
                State = VideoSessionState.Ready;
                Event?.Invoke(new VideoSessionEvent.Loaded(VideoWidth, VideoHeight, Duration));
                break;
            case "frame":
                _dirty = true;
                Event?.Invoke(new VideoSessionEvent.FrameAvailable(TimeSpan.FromSeconds(seconds)));
                break;
            case "playing": State = VideoSessionState.Playing; Event?.Invoke(new VideoSessionEvent.Buffering(false)); break;
            case "waiting": Event?.Invoke(new VideoSessionEvent.Buffering(true)); break;
            case "pause": State = VideoSessionState.Paused; break;
            case "ended": State = VideoSessionState.Ended; Event?.Invoke(new VideoSessionEvent.Ended()); break;
            case "error":
                State = VideoSessionState.Error; ErrorMessage = message;
                Event?.Invoke(new VideoSessionEvent.Error(message, null));
                break;
        }
    }

    public Image? AcquireCurrentFrame(GraphicsDevice device)
    {
        if (_disposed || !_dirty || VideoWidth <= 0 || VideoHeight <= 0) return _image;
        int size = checked(VideoWidth * VideoHeight * 4);
        if (_pixels.Length != size) _pixels = new byte[size];
        if (BrowserVideoInterop.CopyFrame(_id, _pixels))
        {
            var next = _graphics.CreateImage(VideoWidth, VideoHeight, ImagePixelFormat.Rgba8888, _pixels);
            _image?.Dispose(); _image = next; _dirty = false;
        }
        return _image;
    }
    public void ReleaseCurrentFrame() { }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        BrowserVideoInterop.Dispose(_id);
        _image?.Dispose(); _image = null; _pixels = [];
        Event = null;
    }
}

[SupportedOSPlatform("browser")]
internal static partial class BrowserVideoInterop
{
    private const string Module = "miko-video";
    [JSImport("create", Module)]
    internal static partial int Create(string url, bool hls,
        [JSMarshalAs<JSType.Function<JSType.String>>]
        Action<string> callback);
    [JSImport("control", Module)]
    internal static partial void Control(int id, string action, double value);
    [JSImport("position", Module)]
    internal static partial double Position(int id);
    [JSImport("copyFrame", Module)]
    internal static partial bool CopyFrame(int id, [JSMarshalAs<JSType.MemoryView>] Span<byte> pixels);
    [JSImport("dispose", Module)]
    internal static partial void Dispose(int id);
}
