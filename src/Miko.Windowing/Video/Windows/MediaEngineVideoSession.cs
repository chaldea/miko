using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Miko.Platform.Video;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.MediaFoundation;
using static Vortice.Direct3D11.D3D11;
using static Vortice.MediaFoundation.MediaFactory;

namespace Miko.Windowing.Video.Windows;

/// <summary>System A/V playback and HLS, with D3D11 frame-server output composed by Skia.</summary>
[SupportedOSPlatform("windows")]
internal sealed class MediaEngineVideoSession : IVideoSession
{
    private readonly VideoSourceDescriptor _source;
    private readonly VideoSessionOptions _options;
    private readonly ILogger _logger;
    private readonly MediaFoundationFrameSource _frames = new();
    private readonly ConcurrentQueue<Action<IMFMediaEngine>> _commands = new();
    private readonly ConcurrentQueue<(MediaEngineEvent Event, int Code)> _events = new();
    private Thread? _thread;
    private volatile bool _stopping;
    private volatile VideoSessionState _state = VideoSessionState.Loading;
    private volatile bool _buffering = true;
    private string? _error;
    private long _duration, _position;
    private int _width, _height;
    private IReadOnlyList<VideoTimeRange> _buffered = Array.Empty<VideoTimeRange>();
    private int _disposed;

    public MediaEngineVideoSession(VideoSourceDescriptor source, VideoSessionOptions options, ILogger logger)
    { _source = source; _options = options; _logger = logger; }
    public VideoSessionState State => _state;
    public TimeSpan Duration => TimeSpan.FromTicks(Interlocked.Read(ref _duration));
    public TimeSpan Position => TimeSpan.FromTicks(Interlocked.Read(ref _position));
    public int VideoWidth => Volatile.Read(ref _width);
    public int VideoHeight => Volatile.Read(ref _height);
    public bool IsBuffering => _buffering;
    public string? ErrorMessage => Volatile.Read(ref _error);
    public IReadOnlyList<VideoTimeRange> BufferedRanges => Volatile.Read(ref _buffered);
    public IVideoFrameSource FrameSource => _frames;
    public event Action<VideoSessionEvent>? Event;

    public void Start()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = "miko-media-engine" };
        _thread.SetApartmentState(ApartmentState.MTA);
        _thread.Start();
    }
    private void Enqueue(Action<IMFMediaEngine> action) { if (!_stopping) _commands.Enqueue(action); }
    public void Play() => Enqueue(m => m.Play());
    public void Pause() => Enqueue(m => m.Pause());
    public void Seek(TimeSpan position) => Enqueue(m => m.CurrentTime = Math.Max(0, position.TotalSeconds));
    public void SetVolume(float volume) => Enqueue(m => m.Volume = Math.Clamp(volume, 0, 1));
    public void SetMuted(bool muted) => Enqueue(m => m.Muted = muted);
    public void SetPlaybackRate(float rate) => Enqueue(m => m.PlaybackRate = rate);
    public void SetLoop(bool loop) => Enqueue(m => m.Loop = loop);

    private void Run()
    {
        bool started = false;
        IMFMediaEngine? media = null;
        ID3D11Device? device = null;
        ID3D11DeviceContext? context = null;
        IMFDXGIDeviceManager? manager = null;
        ID3D11Texture2D? target = null, staging = null;
        try
        {
            MFStartup().CheckError();
            started = true;
            var flags = DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport;
            var result = D3D11CreateDevice(null, DriverType.Hardware, flags, Array.Empty<FeatureLevel>(), out device, out context);
            if (result.Failure)
                D3D11CreateDevice(null, DriverType.Warp, DeviceCreationFlags.BgraSupport, Array.Empty<FeatureLevel>(), out device, out context).CheckError();
            using (var multithread = device!.QueryInterface<ID3D11Multithread>()) multithread.SetMultithreadProtected(true);
            manager = MFCreateDXGIDeviceManager();
            manager.ResetDevice(device);
            using var attributes = MFCreateAttributes(3);
            attributes.Set(MediaEngineAttributeKeys.DxgiManager.Guid, manager);
            attributes.Set(MediaEngineAttributeKeys.VideoOutputFormat.Guid, (int)Format.B8G8R8A8_UNorm);
            using var factory = new IMFMediaEngineClassFactory();
            media = factory.CreateInstance(MediaEngineCreateFlags.None, attributes,
                (evt, _, code) => _events.Enqueue((evt, code)));
            media.AutoPlay = _options.AutoPlay;
            media.Muted = _options.Muted;
            media.Volume = Math.Clamp(_options.InitialVolume, 0, 1);
            media.Loop = _options.Loop;
            media.SetSource(_source.ResolveForBackend());
            media.Load();
            int frameWidth = 0, frameHeight = 0;
            long lastPts = long.MinValue, lastBufferPoll = 0;
            while (!_stopping)
            {
                while (_commands.TryDequeue(out var command)) command(media);
                while (_events.TryDequeue(out var evt)) HandleEvent(media, evt.Event, evt.Code);
                Interlocked.Exchange(ref _position, ToTicks(media.CurrentTime));
                if (_width > 0 && Environment.TickCount64 - lastBufferPoll >= 250)
                {
                    lastBufferPoll = Environment.TickCount64;
                    using var ranges = media.Buffered;
                    var buffered = new VideoTimeRange[ranges.Length];
                    for (int i = 0; i < ranges.Length; i++)
                        buffered[i] = new(TimeSpan.FromTicks(ToTicks(ranges.GetStart(i))), TimeSpan.FromTicks(ToTicks(ranges.GetEnd(i))));
                    Volatile.Write(ref _buffered, buffered);
                }
                if (media.OnVideoStreamTick(out long pts) && pts != lastPts && _width > 0 && _height > 0)
                {
                    lastPts = pts;
                    if (frameWidth != _width || frameHeight != _height)
                    {
                        staging?.Dispose();
                        target?.Dispose();
                        frameWidth = _width;
                        frameHeight = _height;
                        var description = new Texture2DDescription(Format.B8G8R8A8_UNorm, (uint)frameWidth, (uint)frameHeight,
                            1, 1, BindFlags.RenderTarget, ResourceUsage.Default);
                        target = device.CreateTexture2D(description);
                        description.BindFlags = BindFlags.None;
                        description.Usage = ResourceUsage.Staging;
                        description.CPUAccessFlags = CpuAccessFlags.Read;
                        staging = device.CreateTexture2D(description);
                    }
                    media.TransferVideoFrame(target!, new RectI(0, 0, frameWidth, frameHeight));
                    context!.CopyResource(staging!, target!);
                    var mapped = context.Map(staging!, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
                    try
                    {
                        int stride = frameWidth * 4;
                        byte[] bytes = new byte[stride * frameHeight];
                        for (int y = 0; y < frameHeight; y++)
                            Marshal.Copy(mapped.DataPointer + y * (int)mapped.RowPitch, bytes, y * stride, stride);
                        _frames.PushFrame(VideoFrameBuffer.FromCpuPlanes(frameWidth, frameHeight, VideoPixelFormat.Bgra8888,
                            TimeSpan.FromTicks(pts), [bytes], [stride]));
                        Event?.Invoke(new VideoSessionEvent.FrameAvailable(TimeSpan.FromTicks(pts)));
                    }
                    finally { context.Unmap(staging!, 0); }
                }
                Thread.Sleep(8);
            }
        }
        catch (Exception ex)
        {
            if (!_stopping)
            {
                _error = ex.Message;
                _state = VideoSessionState.Error;
                _buffering = false;
                _logger.LogError(ex, "Media Engine playback failed");
                Event?.Invoke(new VideoSessionEvent.Error(ex.Message, ex));
            }
        }
        finally
        {
            media?.Shutdown();
            media?.Dispose();
            staging?.Dispose();
            target?.Dispose();
            manager?.Dispose();
            context?.Dispose();
            device?.Dispose();
            if (started) MFShutdown();
        }
    }

    private void HandleEvent(IMFMediaEngine media, MediaEngineEvent evt, int code)
    {
        switch (evt)
        {
            case MediaEngineEvent.LoadedMetadata:
            case MediaEngineEvent.FormatChange:
                var size = media.NativeVideoSize;
                _width = size.Width;
                _height = size.Height;
                Interlocked.Exchange(ref _duration, ToTicks(media.Duration));
                if (_state == VideoSessionState.Loading) _state = VideoSessionState.Ready;
                Event?.Invoke(new VideoSessionEvent.Loaded(_width, _height, Duration));
                break;
            case MediaEngineEvent.DurationChange:
                Interlocked.Exchange(ref _duration, ToTicks(media.Duration));
                break;
            case MediaEngineEvent.Waiting:
            case MediaEngineEvent.Stalled:
                _buffering = true;
                Event?.Invoke(new VideoSessionEvent.Buffering(true));
                break;
            case MediaEngineEvent.CanPlay:
            case MediaEngineEvent.Playing:
            case MediaEngineEvent.Seeked:
                _buffering = false;
                _state = media.IsPaused ? VideoSessionState.Paused : VideoSessionState.Playing;
                Event?.Invoke(new VideoSessionEvent.Buffering(false));
                break;
            case MediaEngineEvent.Pause:
                _state = VideoSessionState.Paused;
                _buffering = false;
                Event?.Invoke(new VideoSessionEvent.Buffering(false));
                break;
            case MediaEngineEvent.Ended:
                _state = VideoSessionState.Ended;
                _buffering = false;
                Event?.Invoke(new VideoSessionEvent.Ended());
                break;
            case MediaEngineEvent.Error:
                using (var error = media.Error)
                    _error = $"Media Engine error: {error.GetErrorCode()} (0x{code:X8}).";
                _state = VideoSessionState.Error;
                _buffering = false;
                Event?.Invoke(new VideoSessionEvent.Error(_error, null));
                break;
        }
    }
    private static long ToTicks(double seconds) => double.IsFinite(seconds) && seconds > 0 && seconds < TimeSpan.MaxValue.TotalSeconds
        ? TimeSpan.FromSeconds(seconds).Ticks : 0;
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _stopping = true;
        _thread?.Join(500);
        _frames.Dispose();
    }
}
