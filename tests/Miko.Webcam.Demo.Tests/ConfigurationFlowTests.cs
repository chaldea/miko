using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Miko.Common;
using Miko.Components.Webcam;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Layout;
using Miko.Platform;
using Miko.Platform.Video;
using Miko.Routing;
using MikoApp.Webcam;
using Shouldly;

namespace Miko.Webcam.Demo.Tests;

public sealed class ConfigurationFlowTests : IDisposable
{
    private readonly MikoAppContext _context;
    private readonly RenderSurface _surface;
    private readonly Backend _backend = new();
    private readonly PendingApi _pendingApi = new();
    private WebcamDemo _page = null!;
    private WebcamMonitor _monitor = null!;
    private Element Root => _context.Engine.GetRoot()!;
    private NavigationManager Navigation => _context.Services.GetRequiredService<NavigationManager>();

    public ConfigurationFlowTests()
    {
        _context = App.CreateContext(builder =>
        {
            builder.Services.Replace(ServiceDescriptor.Singleton<IVideoBackend>(_backend));
            builder.Services.Replace(ServiceDescriptor.Singleton(new WebcamConnectionSettings()));
            builder.Services.Replace(ServiceDescriptor.Singleton(_ => new HttpClient(_pendingApi)));
        },
            page: p => _page = p, monitor: p => _monitor = p);
        _surface = _context.Engine.Graphics.CreateSurface(420, 860);
        _context.Controller.Initialize(_surface.Context, 420, 860);
        Render();
    }

    public void Dispose()
    {
        _monitor?.Back();
        _context.Engine.DisposeVideoSessions();
        _surface.Dispose();
        ((IDisposable)_context.Services).Dispose();
    }

    [Fact]
    public void Should_show_mock_configuration_without_opening_a_video_session()
    {
        _page.Settings.Provider.ShouldBe("miko");
        Navigation.CurrentPath.ShouldBe("/");
        Navigation.CurrentState.ShouldBeNull();
        Root.FindByClass("webcam-config").ShouldHaveSingleItem();
        Root.FindByTagName("video").ShouldBeEmpty();
        _backend.Sessions.ShouldBeEmpty();
        Root.FindByClass("mock-password").Single().FindByTagName("input").Single()
            .ShouldBeOfType<InputElement>().Type.ShouldBe(InputType.Password);
    }

    [Fact]
    public void Should_switch_provider_fields_through_the_segment_buttons()
    {
        Click("webcam-type-ezviz");
        _page.Settings.Provider.ShouldBe("ezviz");
        Root.FindByClass("webcam-ezviz-fields").ShouldHaveSingleItem();
        Root.FindByClass("webcam-miko-fields").ShouldBeEmpty();
        Root.FindByClass("ezviz-serial").Single().FindByTagName("input").Single()
            .ShouldBeOfType<InputElement>().Value.ShouldBeNullOrEmpty();
        Root.FindByClass("ezviz-token").Single().FindByTagName("input").Single()
            .ShouldBeOfType<InputElement>().Type.ShouldBe(InputType.Password);
        Click("webcam-type-miko");
        Root.FindByClass("webcam-miko-fields").ShouldHaveSingleItem();
        _backend.Sessions.ShouldBeEmpty();
    }

    [Fact]
    public void Should_bind_inputs_validate_and_connect_only_after_clicking()
    {
        Edit("mock-rtsp", "bad address");
        _page.Settings.MockRtsp.ShouldBe("bad address");
        Click("webcam-connect");
        Navigation.CurrentPath.ShouldBe("/");
        Navigation.CanGoBack.ShouldBeFalse();
        Root.FindByClass("webcam-config-error").ShouldHaveSingleItem();
        _backend.Sessions.ShouldBeEmpty();
        Edit("mock-rtsp", "rtsp://camera.test:8554/webcam");
        Edit("mock-http", "http://camera.test:9080");
        Click("webcam-connect");
        Navigation.CurrentPath.ShouldBe("/monitor");
        Navigation.GetState<WebcamConnection>().ShouldNotBeNull();
        Navigation.History.ShouldBe(["/", "/monitor"]);
        Root.FindByClass("webcam-config").ShouldBeEmpty();
        _page.ErrorMessage.ShouldBeNull();
        _backend.Sources.ShouldBe(["http://camera.test:9080/media/demo.mp4"]);
        _backend.Sessions.Single().Loop.ShouldBeTrue();
        _monitor.Webcam.ShouldNotBeNull();
        Click("webcam-back");
        Navigation.CurrentPath.ShouldBe("/");
        Navigation.CurrentState.ShouldBeNull();
        Navigation.CanGoBack.ShouldBeFalse();
        _backend.Sessions.Single().Disposed.ShouldBeTrue();
        Root.FindByTagName("video").ShouldBeEmpty();
        _page.Settings.MockHttp.ShouldBe("http://camera.test:9080");
        Click("webcam-connect");
        _backend.Sessions.Count.ShouldBe(2);
        _backend.Sessions[1].Disposed.ShouldBeFalse();
    }

    [Fact]
    public void Should_leave_fullscreen_before_returning_to_configuration()
    {
        Click("webcam-connect");
        _monitor.Webcam!.ToggleFullscreen(); Render();
        _context.Controller.OnBackButton().ShouldBeTrue(); Render();
        Navigation.CurrentPath.ShouldBe("/monitor");
        _monitor.Webcam!.IsFullscreen.ShouldBeFalse();
        _context.Controller.OnBackButton().ShouldBeTrue(); Render();
        Navigation.CurrentPath.ShouldBe("/");
        _backend.Sessions.Single().Disposed.ShouldBeTrue();
        _context.Controller.OnBackButton().ShouldBeFalse();
    }

    [Fact]
    public void Should_pass_a_connection_snapshot_to_a_separate_monitor_page()
    {
        var configuration = _page;
        var settings = _page.Settings;
        Edit("mock-http", "http://camera.test:9080");
        Click("webcam-connect");

        var connection = Navigation.GetState<WebcamConnection>().ShouldNotBeNull();
        _monitor.Webcam!.Device.ShouldBeSameAs(connection.Device);
        _monitor.Webcam.Quality.ShouldBe(connection.Quality);
        Root.FindByClass("webcam-monitor").ShouldHaveSingleItem();
        Root.FindByClass("webcam-config").ShouldBeEmpty();
        settings.MockHttp = "http://changed.test:9081";
        _monitor.Webcam.Stream!.Source.ShouldBe("http://camera.test:9080/media/demo.mp4");

        Navigation.NavigateBack().ShouldBeTrue(); Render();

        _page.ShouldNotBeSameAs(configuration);
        _page.Settings.ShouldBeSameAs(settings);
        _page.Settings.MockHttp.ShouldBe("http://changed.test:9081");
        Root.FindByClass("webcam-config").ShouldHaveSingleItem();
        Root.FindByClass("webcam-monitor").ShouldBeEmpty();
        _monitor.Webcam.ShouldBeNull();
        _backend.Sessions.Single().Disposed.ShouldBeTrue();
        Navigation.CurrentState.ShouldBeNull();
        _context.Controller.OnBackButton().ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_return_to_configuration_when_the_monitor_has_no_connection(bool wrongStateType)
    {
        Navigation.NavigateTo("/monitor", wrongStateType ? new object() : null);
        Render();

        Navigation.CurrentPath.ShouldBe("/");
        Navigation.CurrentState.ShouldBeNull();
        Navigation.CanGoBack.ShouldBeFalse();
        Root.FindByClass("webcam-config").ShouldHaveSingleItem();
        Root.FindByClass("webcam-monitor").ShouldBeEmpty();
        _backend.Sessions.ShouldBeEmpty();
        _context.Controller.OnBackButton().ShouldBeFalse();
    }

    [Fact]
    public void Should_return_from_playback_failure_and_reconnect_with_new_parameters()
    {
        Click("webcam-connect");
        _backend.Sessions.Single().Fail(); Render();
        _monitor.Webcam!.State.ShouldBe(WebcamState.Error);
        Click("webcam-back");
        Edit("mock-http", "http://other.test:8081");
        Click("webcam-connect");
        _backend.Sources.Last().ShouldBe("http://other.test:8081/media/demo.mp4");
        _monitor.Webcam!.State.ShouldBe(WebcamState.Playing);
    }

    private void Edit(string className, string value)
    {
        var input = Root.FindByClass(className).Single().FindByTagName("input").Single().ShouldBeOfType<InputElement>();
        input.Value = value;
        input.OnInput!.Invoke(new InputEventArgs { Target = input, Data = value });
        Render();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_cancel_pending_cloud_requests_when_leaving_and_keep_the_new_connection(bool rootNavigation)
    {
        _page.Settings.Provider = "ezviz";
        _page.Settings.Serial = "AB1234";
        _page.Settings.AccessToken = "test-token";
        _page.Settings.EnableCloudRelay = true;
        _page.Connect(); Render();
        await _pendingApi.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        if (rootNavigation) Navigation.NavigateTo("/", NavigationDirection.Root);
        else _monitor.Back();
        Render();
        await _pendingApi.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Navigation.CurrentPath.ShouldBe("/");
        _page.SelectProvider("miko");
        _page.Connect(); Render();
        _monitor.Webcam!.Stream!.Source.ShouldBe("http://localhost:8080/media/demo.mp4");
        _monitor.Webcam.State.ShouldBe(WebcamState.Playing);
        _backend.Sessions.ShouldHaveSingleItem();
    }

    [Fact]
    public void Should_keep_configuration_usable_at_phone_width()
    {
        _context.Engine.SetViewportSize(360, 740);
        for (int i = 0; i < 4; i++)
            _context.Controller.RenderFrame(_surface.Context, 360, 740, 1 / 60f, c => _context.Engine.Render(c));
        var button = Find(_context.Engine.GetCurrentLayout()!, Root.FindByClass("webcam-connect").Single())!.BoxModel.BorderBox;
        button.Width.ShouldBeGreaterThan(250);
        button.Right.ShouldBeLessThanOrEqualTo(360);
        button.Bottom.ShouldBeLessThan(740);
        Root.FindByClass("mock-rtsp").Single().OffsetWidth.ShouldBeLessThanOrEqualTo(360);
    }

    private void Click(string className)
    {
        var element = Root.FindByClass(className).Single();
        var box = Find(_context.Engine.GetCurrentLayout()!, element)!.BoxModel.BorderBox;
        float x = box.X + box.Width / 2, y = box.Y + box.Height / 2;
        _context.Controller.OnPointerDown(x, y, MouseButton.Left);
        _context.Controller.OnPointerUp(x, y, MouseButton.Left);
        Render();
    }

    private static LayoutBox? Find(LayoutBox box, Element element) => box.Element == element ? box : box.Children.Select(c => Find(c, element)).FirstOrDefault(b => b != null);
    private void Render()
    {
        for (int i = 0; i < 4; i++)
            _context.Controller.RenderFrame(_surface.Context, 420, 860, 1 / 60f, c => _context.Engine.Render(c));
    }

    private sealed class Backend : IVideoBackend
    {
        public List<Session> Sessions { get; } = [];
        public List<string> Sources { get; } = [];
        public VideoBackendCapabilities Capabilities => new(false, false, ["video/mp4"]);
        public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
        {
            Sources.Add(source.Uri);
            var session = new Session(options.Loop); Sessions.Add(session); return session;
        }
    }

    private sealed class Session(bool loop) : IVideoSession
    {
        public bool Disposed { get; private set; }
        public bool Loop { get; private set; } = loop;
        public VideoSessionState State { get; private set; } = VideoSessionState.Playing;
        public TimeSpan Duration => TimeSpan.FromSeconds(12);
        public TimeSpan Position => TimeSpan.Zero;
        public int VideoWidth => 1280;
        public int VideoHeight => 360;
        public IVideoFrameSource FrameSource { get; } = new NoFrames();
        public event Action<VideoSessionEvent>? Event;
        public void Fail() { State = VideoSessionState.Error; Event?.Invoke(new VideoSessionEvent.Error("Unavailable", null)); }
        public void Play() => State = VideoSessionState.Playing;
        public void Pause() => State = VideoSessionState.Paused;
        public void Seek(TimeSpan position) { }
        public void SetVolume(float volume) { }
        public void SetMuted(bool muted) { }
        public void SetPlaybackRate(float rate) { }
        public void SetLoop(bool loop) => Loop = loop;
        public void Dispose() => Disposed = true;
    }

    private sealed class NoFrames : IVideoFrameSource
    {
        public Image? AcquireCurrentFrame(GraphicsDevice device) => null;
        public void ReleaseCurrentFrame() { }
    }

    private sealed class PendingApi : HttpMessageHandler
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Started.TrySetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            catch (OperationCanceledException) { Cancelled.TrySetResult(); throw; }
            throw new InvalidOperationException("Request should have been cancelled.");
        }
    }
}
