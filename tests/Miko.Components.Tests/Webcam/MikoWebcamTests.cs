using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Components;
using Miko.Components.Webcam;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Layout;
using Miko.Platform;
using Miko.Platform.Video;
using Miko.Styling;
using Shouldly;

namespace Miko.Components.Tests.Webcam;

/// <summary>
/// The webcam view against a scripted provider and video backend, at phone size. Pointer input goes
/// through the real interaction controller wherever hit testing matters (the pan / tilt disk).
/// </summary>
public sealed class MikoWebcamTests : IDisposable
{
    private static readonly WebcamDevice Camera = new("CD7654321") { VerifyCode = "QWERTY" };

    private readonly RenderSurface _surface;
    private readonly IDrawingContext _canvas;
    private readonly MikoEngine _engine;
    private readonly FakeBackend _backend = new();
    private readonly FakeProvider _provider = new();
    private readonly ManualClock _clock = new();
    private readonly PlatformInfo _platform;
    private readonly RecordingOrientation _orientation = new();
    private readonly RecordingSystemBars _systemBars = new();
    private readonly MikoInteractionController _input;
    private readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();
    private readonly DivElement _root;
    private MikoWebcam _webcam;

    public MikoWebcamTests()
    {
        _engine = new MikoEngineBuilder().Build();
        _surface = _engine.Graphics.CreateSurface(390, 844);
        _canvas = _surface.Context;
        _engine.VideoBackend = _backend;
        _platform = new PlatformInfo(HostPlatform.Android) { Orientation = _orientation, SystemBars = _systemBars };
        _root = new DivElement { Style = new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Width = Length.Percent(100), Height = Length.Percent(100) } };
        _webcam = Create();
        _root.Children.Add(_webcam.Build());
        _engine.Initialize(_root, [], _canvas, 390, 844);
        _input = new MikoInteractionController(
            Options.Create(new MikoAppOptions()), _services, _engine, new EventDispatcher(), new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance), NullLogger<MikoInteractionController>.Instance,
            platformService: _platform);
        Render();
    }

    private MikoWebcam Create(WebcamDevice? device = null) => new()
    {
        Engine = _engine, Provider = _provider, PlatformService = _platform, Clock = _clock,
        Device = device ?? Camera, Texts = WebcamTexts.Chinese, Style = new Style { FlexGrow = 1 },
    };

    public void Dispose()
    {
        _webcam.Dispose();
        _engine.DisposeVideoSessions();
        _surface.Dispose();
        _services.Dispose();
    }

    private void Render() { for (int i = 0; i < 4; i++) _engine.Render(_canvas); }

    private Element Single(string className) => _root.FindByClass(className).Single();

    private LayoutBox? LayoutOf(Element element)
    {
        static LayoutBox? Find(LayoutBox box, Element element)
            => box.Element == element ? box : box.Children.Select(c => Find(c, element)).FirstOrDefault(b => b != null);
        return Find(_engine.GetCurrentLayout()!, element);
    }

    private bool IsShown(Element element) => LayoutOf(element) is { } box && box.ComputedStyle.Display != Display.None;

    private RectF Box(Element element) => LayoutOf(element)!.BoxModel.BorderBox;

    private (float X, float Y) PadPoint(PtzDirection direction)
    {
        var pad = Box(Single("miko-webcam-ptz-pad"));
        float cx = pad.X + pad.Width / 2, cy = pad.Y + pad.Height / 2, arm = pad.Width * .35f;
        return direction switch
        {
            PtzDirection.Up => (cx, cy - arm),
            PtzDirection.Down => (cx, cy + arm),
            PtzDirection.Left => (cx - arm, cy),
            _ => (cx + arm, cy),
        };
    }

    private void Tap(Element element)
    {
        var box = Box(element);
        _input.OnPointerDown(box.X + box.Width / 2, box.Y + box.Height / 2, MouseButton.Left, PointerType.Touch);
        _input.OnPointerUp(box.X + box.Width / 2, box.Y + box.Height / 2, MouseButton.Left, PointerType.Touch);
        Render();
    }

    private string StatusText => ((SpanElement)Single("miko-webcam-status").Children[1]).TextContent;

    // ---- stream ----------------------------------------------------------------------------

    [Fact]
    public void Should_Play_The_Providers_Stream_And_Show_How_It_Arrives()
    {
        _webcam.State.ShouldBe(WebcamState.Playing);
        _backend.Sources.ShouldBe(["rtsp://admin:QWERTY@192.168.1.5:554/h264/ch1/sub/av_stream"]);
        _provider.StreamRequests.ShouldBe([WebcamQuality.Smooth]);
        Single("miko-webcam-badge").TextContent.ShouldBe("局域网直连");
        IsShown(Single("miko-webcam-status")).ShouldBeFalse("no overlay over a playing picture");
        Single("miko-webcam-quality").TextContent.ShouldBe("流畅");
    }

    [Fact]
    public void Should_Size_The_Picture_To_16_By_9_And_Give_The_Rest_To_The_Panel()
    {
        Box(Single("miko-webcam-viewport")).Height.ShouldBe(390 * 9 / 16f, .5f);
        Box(Single("miko-webcam-toolbar")).Height.ShouldBe(48);
        var pad = Box(Single("miko-webcam-ptz-pad"));
        pad.Width.ShouldBe(200);
        (pad.X + pad.Width / 2).ShouldBe(195, .5f);
        (pad.Y + pad.Height).ShouldBeLessThan(844);
    }

    [Fact]
    public void Should_Switch_Quality_And_Reconnect()
    {
        Tap(Single("miko-webcam-quality"));

        _provider.StreamRequests.ShouldBe([WebcamQuality.Smooth, WebcamQuality.High]);
        _webcam.CurrentQuality.ShouldBe(WebcamQuality.High);
        _backend.Sources[^1].ShouldEndWith("/main/av_stream");
        _backend.Sessions[0].Disposed.ShouldBeTrue("the old stream is released");
        Single("miko-webcam-quality").TextContent.ShouldBe("高清");
        _webcam.Hint.ShouldBe("已切换为高清");
    }

    [Fact]
    public void Should_Stop_And_Resume_From_The_Toolbar_And_The_Picture()
    {
        Tap(Single("miko-webcam-play"));
        _webcam.State.ShouldBe(WebcamState.Stopped);
        _backend.Sessions[0].State.ShouldBe(VideoSessionState.Paused);
        IsShown(Single("miko-webcam-status")).ShouldBeTrue("a stopped picture shows the play button");

        Tap(Single("miko-webcam-viewport"));
        _webcam.State.ShouldBe(WebcamState.Playing);
        _backend.Sessions.Count.ShouldBe(1, "resuming keeps the session");
    }

    [Fact]
    public void Should_Show_Why_The_Camera_Cannot_Be_Watched_And_Retry()
    {
        _provider.StreamError = new WebcamException("not here", "unreachable");
        _webcam.Retry();
        Render();

        _webcam.State.ShouldBe(WebcamState.Error);
        StatusText.ShouldBe("摄像头不在当前网络中");
        var retry = Single("miko-webcam-status-button");
        retry.TextContent.ShouldBe("重试");

        _provider.StreamError = null;
        Tap(retry);
        _webcam.State.ShouldBe(WebcamState.Playing);
    }

    [Fact]
    public void Should_Show_A_Stream_Failure_In_The_Views_Language()
    {
        _backend.Sessions[0].Fail(new Miko.Components.Webcam.Streaming.RtspException(401, "The camera rejected the user name or password."));
        Render();

        _webcam.State.ShouldBe(WebcamState.Error);
        StatusText.ShouldBe("设备验证码错误");
    }

    // ---- pan / tilt ------------------------------------------------------------------------

    [Fact]
    public void Should_Turn_While_An_Arrow_Is_Held_And_Stop_On_Release()
    {
        var (x, y) = PadPoint(PtzDirection.Right);
        _input.OnPointerDown(x, y, MouseButton.Left, PointerType.Touch);
        Render();

        _webcam.ActivePtz.ShouldBe(PtzDirection.Right);
        _provider.Ptz.ShouldBe(["start Right Normal"]);

        _input.OnPointerMove(x + 30, y + 10);
        _input.OnPointerUp(x + 30, y + 10, MouseButton.Left, PointerType.Touch);
        Render();

        _webcam.ActivePtz.ShouldBeNull();
        _provider.Ptz.ShouldBe(["start Right Normal", "stop Right"]);
    }

    [Theory]
    [InlineData(PtzDirection.Up)]
    [InlineData(PtzDirection.Down)]
    [InlineData(PtzDirection.Left)]
    public void Should_Map_Each_Quarter_Of_The_Disk_To_Its_Direction(PtzDirection direction)
    {
        var (x, y) = PadPoint(direction);
        _input.OnPointerDown(x, y, MouseButton.Left, PointerType.Touch);
        _input.OnPointerUp(x, y, MouseButton.Left, PointerType.Touch);
        Render();

        _provider.Ptz.ShouldBe([$"start {direction} Normal", $"stop {direction}"]);
    }

    [Fact]
    public void Should_Ignore_The_Centre_Of_The_Disk()
    {
        var pad = Box(Single("miko-webcam-ptz-pad"));
        _input.OnPointerDown(pad.X + pad.Width / 2, pad.Y + pad.Height / 2, MouseButton.Left, PointerType.Touch);
        _input.OnPointerUp(pad.X + pad.Width / 2, pad.Y + pad.Height / 2, MouseButton.Left, PointerType.Touch);

        _provider.Ptz.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Let_Go_And_Explain_When_The_Camera_Reaches_Its_Limit()
    {
        _provider.PtzError = new WebcamException("limit", "60005");
        var (x, y) = PadPoint(PtzDirection.Right);
        _input.OnPointerDown(x, y, MouseButton.Left, PointerType.Touch);
        Render();

        _webcam.ActivePtz.ShouldBeNull();
        _webcam.Hint.ShouldBe("云台已转到尽头");
        _clock.Advance(TimeSpan.FromSeconds(3));
        Render();
        _webcam.Hint.ShouldBeNull();
    }

    [Fact]
    public void Should_Disable_The_Disk_For_A_Camera_The_Cloud_Cannot_Steer()
    {
        _provider.Info = _provider.Info with { Features = WebcamFeatures.None, IsOnLocalNetwork = true };
        _webcam.Retry();
        Render();

        Single("miko-webcam-ptz-caption").TextContent.ShouldBe("设备在云端离线，暂不能控制云台");
        var (x, y) = PadPoint(PtzDirection.Up);
        _input.OnPointerDown(x, y, MouseButton.Left, PointerType.Touch);
        _provider.Ptz.ShouldBeEmpty();
        IsShown(Single("miko-webcam-snapshot")).ShouldBeFalse();
        IsShown(Single("miko-webcam-privacy")).ShouldBeFalse();
        IsShown(Single("miko-webcam-fullscreen")).ShouldBeTrue();
    }

    [Fact]
    public void Should_Stop_Turning_When_Disposed_Mid_Press()
    {
        var (x, y) = PadPoint(PtzDirection.Left);
        _input.OnPointerDown(x, y, MouseButton.Left, PointerType.Touch);

        _webcam.Dispose();

        _provider.Ptz.ShouldBe(["start Left Normal", "stop Left"]);
    }

    // ---- actions ---------------------------------------------------------------------------

    [Fact]
    public void Should_Take_A_Snapshot()
    {
        WebcamSnapshot? taken = null;
        _webcam.SnapshotTaken = EventCallback.Factory.Create<WebcamSnapshot>(this, s => taken = s);

        Tap(Single("miko-webcam-snapshot"));
        Render();

        taken.ShouldNotBeNull().Data.ShouldBe(new byte[] { 0xff, 0xd8 });
        _webcam.Hint.ShouldBe("截图成功");
    }

    [Fact]
    public void Should_Cover_The_Picture_In_Privacy_Mode_And_Turn_It_Off()
    {
        Tap(Single("miko-webcam-privacy"));

        _provider.Privacy.ShouldBe([true]);
        _webcam.IsPrivacyEnabled.ShouldBeTrue();
        IsShown(Single("miko-webcam-status")).ShouldBeTrue();
        StatusText.ShouldBe("隐私遮蔽已开启");

        Tap(Single("miko-webcam-status-button"));
        _provider.Privacy.ShouldBe([true, false]);
        IsShown(Single("miko-webcam-status")).ShouldBeFalse();
    }

    [Fact]
    public void Should_Start_Covered_When_The_Camera_Is_In_Privacy_Mode()
    {
        _provider.Info = _provider.Info with { PrivacyEnabled = true };
        _webcam.Retry();
        Render();

        _webcam.IsPrivacyEnabled.ShouldBeTrue();
        StatusText.ShouldBe("隐私遮蔽已开启");
    }

    // ---- fullscreen ------------------------------------------------------------------------

    [Fact]
    public void Should_Go_Fullscreen_With_The_Disk_Over_The_Picture_And_Leave_On_Back()
    {
        Tap(Single("miko-webcam-fullscreen"));
        _engine.SetViewportSize(844, 390);
        Render();

        _webcam.IsFullscreen.ShouldBeTrue();
        _orientation.LockedOrientation.ShouldBe(ScreenOrientation.Landscape);
        _systemBars.HiddenDepth.ShouldBe(1);
        Box(Single("miko-webcam-viewport")).Width.ShouldBe(844);
        IsShown(Single("miko-webcam-ptz")).ShouldBeFalse("the panel makes way for the picture");
        IsShown(Single("miko-webcam-ptz-overlay")).ShouldBeTrue();

        // A tap on the picture hides the overlays, another brings them back.
        Tap(Single("miko-webcam-viewport"));
        IsShown(Single("miko-webcam-ptz-overlay")).ShouldBeFalse();
        Tap(Single("miko-webcam-viewport"));
        IsShown(Single("miko-webcam-ptz-overlay")).ShouldBeTrue();

        _platform.Back.Dispatch().ShouldBeTrue();
        _webcam.IsFullscreen.ShouldBeFalse();
        _orientation.LockDepth.ShouldBe(0);
        _systemBars.HiddenDepth.ShouldBe(0);
        _platform.Back.Count.ShouldBe(0);
    }

    [Fact]
    public void Should_Release_The_Platform_When_Disposed_In_Fullscreen()
    {
        _webcam.ToggleFullscreen();
        _webcam.Dispose();

        _orientation.LockDepth.ShouldBe(0);
        _orientation.AwakeDepth.ShouldBe(0);
        _systemBars.HiddenDepth.ShouldBe(0);
        _platform.Back.Count.ShouldBe(0);
    }

    // ---- bitrate ---------------------------------------------------------------------------

    [Fact]
    public void Should_Format_Bitrates()
    {
        MikoWebcam.FormatRate(900).ShouldBe("0 KB/s");
        MikoWebcam.FormatRate(25 * 1024).ShouldBe("25 KB/s");
        MikoWebcam.FormatRate(3 * 1024 * 1024 / 2).ShouldBe("1.5 MB/s");
    }

    // ---- fakes -----------------------------------------------------------------------------

    private sealed class FakeProvider : IWebcamProvider
    {
        public WebcamInfo Info { get; set; } = new("C6C(CD7654321)", true,
            WebcamFeatures.Pan | WebcamFeatures.Tilt | WebcamFeatures.Privacy | WebcamFeatures.Capture | WebcamFeatures.Mirror);
        public Exception? StreamError { get; set; }
        public Exception? PtzError { get; set; }
        public List<WebcamQuality> StreamRequests { get; } = new();
        public List<string> Ptz { get; } = new();
        public List<bool> Privacy { get; } = new();

        public Task<WebcamInfo> GetInfoAsync(WebcamDevice device, CancellationToken cancellationToken = default) => Task.FromResult(Info);

        public Task<WebcamStream> GetLiveStreamAsync(WebcamDevice device, WebcamQuality quality, CancellationToken cancellationToken = default)
        {
            StreamRequests.Add(quality);
            if (StreamError != null) return Task.FromException<WebcamStream>(StreamError);
            string path = quality == WebcamQuality.High ? "main" : "sub";
            return Task.FromResult(new WebcamStream($"rtsp://admin:{device.VerifyCode}@192.168.1.5:554/h264/ch1/{path}/av_stream", null, WebcamTransport.LocalNetwork, quality));
        }

        public Task StartPtzAsync(WebcamDevice device, PtzDirection direction, PtzSpeed speed, CancellationToken cancellationToken = default)
        {
            Ptz.Add($"start {direction} {speed}");
            return PtzError != null ? Task.FromException(PtzError) : Task.CompletedTask;
        }

        public Task StopPtzAsync(WebcamDevice device, PtzDirection direction, CancellationToken cancellationToken = default)
        {
            Ptz.Add($"stop {direction}");
            return Task.CompletedTask;
        }

        public Task<WebcamSnapshot> CaptureAsync(WebcamDevice device, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WebcamSnapshot([0xff, 0xd8], null));

        public Task SetPrivacyAsync(WebcamDevice device, bool enabled, CancellationToken cancellationToken = default)
        {
            Privacy.Add(enabled);
            return Task.CompletedTask;
        }

        public Task MirrorAsync(WebcamDevice device, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeBackend : IVideoBackend
    {
        public List<string> Sources { get; } = new();
        public List<FakeSession> Sessions { get; } = new();
        public VideoBackendCapabilities Capabilities { get; } = new(false, false, []);

        public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
        {
            Sources.Add(source.Uri);
            var session = new FakeSession(options.AutoPlay);
            Sessions.Add(session);
            return session;
        }
    }

    private sealed class FakeSession(bool autoPlay) : IVideoSession
    {
        public bool Disposed { get; private set; }
        public VideoSessionState State { get; private set; } = autoPlay ? VideoSessionState.Playing : VideoSessionState.Ready;
        public TimeSpan Duration => TimeSpan.Zero;
        public TimeSpan Position => TimeSpan.Zero;
        public string? ErrorMessage { get; private set; }
        public int VideoWidth => 1280;
        public int VideoHeight => 720;
        public IVideoFrameSource FrameSource { get; } = new NoFrames();
        public event Action<VideoSessionEvent>? Event;
        public void Play() => State = VideoSessionState.Playing;
        public void Pause() => State = VideoSessionState.Paused;
        public void Fail(Exception error)
        {
            State = VideoSessionState.Error;
            ErrorMessage = error.Message;
            Event?.Invoke(new VideoSessionEvent.Error(error.Message, error));
        }
        public void Seek(TimeSpan position) { }
        public void SetVolume(float volume) { }
        public void SetMuted(bool muted) { }
        public void SetPlaybackRate(float rate) { }
        public void SetLoop(bool loop) { }
        public void Dispose() => Disposed = true;
    }

    private sealed class NoFrames : IVideoFrameSource
    {
        public Image? AcquireCurrentFrame(GraphicsDevice device) => null;
        public void ReleaseCurrentFrame() { }
    }

    private sealed class RecordingSystemBars : ISystemBarsService
    {
        public int HiddenDepth { get; private set; }
        public void Hide() => HiddenDepth++;
        public void Show() => HiddenDepth = Math.Max(0, HiddenDepth - 1);
    }

    private sealed class RecordingOrientation : IOrientationService
    {
        public int LockDepth { get; private set; }
        public int AwakeDepth { get; private set; }
        public ScreenOrientation? LockedOrientation { get; private set; }
        public void Lock(ScreenOrientation? orientation = null) { if (LockDepth++ == 0) LockedOrientation = orientation; }
        public void Unlock() { if (LockDepth > 0 && --LockDepth == 0) LockedOrientation = null; }
        public Task SetAsync(ScreenOrientation orientation) => Task.CompletedTask;
        public void KeepAwake() => AwakeDepth++;
        public void AllowSleep() => AwakeDepth = Math.Max(0, AwakeDepth - 1);
    }
}
