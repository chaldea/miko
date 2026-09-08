using Miko.Components.Player;
using Miko.Platform.Video;
using Shouldly;
using SkiaSharp;

namespace Miko.Components.Tests;

public class PlayerControllerTests
{
    [Fact]
    public void Should_Apply_Controls_Requested_Before_Loading()
    {
        using var controller = new PlayerController();
        controller.Play();
        controller.SetVolume(.35f);
        controller.SetMuted(true);
        controller.SetPlaybackRate(1.5f);
        var session = new TestVideoSession();
        controller.Attach(session);
        session.Playing.ShouldBeTrue();
        session.Volume.ShouldBe(.35f);
        session.Muted.ShouldBeTrue();
        session.Rate.ShouldBe(1.5f);
    }

    [Fact]
    public void Should_Clamp_Seek_And_Replay_From_Start()
    {
        using var controller = new PlayerController();
        var session = new TestVideoSession { Duration = TimeSpan.FromSeconds(30) };
        controller.Attach(session);
        controller.Seek(TimeSpan.FromSeconds(90));
        session.Position.ShouldBe(session.Duration);
        controller.Seek(TimeSpan.FromSeconds(-5));
        session.Position.ShouldBe(TimeSpan.Zero);
        session.State = VideoSessionState.Ended;
        session.Position = session.Duration;
        controller.Refresh();
        controller.Play();
        session.Position.ShouldBe(TimeSpan.Zero);
        session.Playing.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Nonfinite_Control_Values()
    {
        using var controller = new PlayerController();
        Should.Throw<ArgumentOutOfRangeException>(() => controller.SetVolume(float.NaN));
        Should.Throw<ArgumentOutOfRangeException>(() => controller.SetPlaybackRate(float.PositiveInfinity));
    }

    [Fact]
    public void Should_Not_Dispose_Engine_Owned_Session()
    {
        var controller = new PlayerController();
        var session = new TestVideoSession();
        controller.Attach(session);
        controller.Dispose();
        session.Disposed.ShouldBeFalse();
    }
}

internal sealed class TestVideoSession : IVideoSession
{
    public bool Playing { get; private set; }
    public float Volume { get; private set; }
    public bool Muted { get; private set; }
    public float Rate { get; private set; }
    public bool Disposed { get; private set; }
    public VideoSessionState State { get; set; } = VideoSessionState.Ready;
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan Position { get; set; }
    public int VideoWidth => 640;
    public int VideoHeight => 360;
    public IVideoFrameSource FrameSource { get; } = new EmptyFrameSource();
    public event Action<VideoSessionEvent>? Event;
    public void Raise(VideoSessionEvent value) => Event?.Invoke(value);
    public void Play() { Playing = true; State = VideoSessionState.Playing; }
    public void Pause() { Playing = false; State = VideoSessionState.Paused; }
    public void Seek(TimeSpan value) => Position = value;
    public void SetVolume(float value) => Volume = value;
    public void SetMuted(bool value) => Muted = value;
    public void SetPlaybackRate(float value) => Rate = value;
    public void SetLoop(bool value) { }
    public void Dispose() => Disposed = true;
    private sealed class EmptyFrameSource : IVideoFrameSource
    {
        public SKImage? AcquireCurrentFrame(GRContext? context) => null;
        public void ReleaseCurrentFrame() { }
    }
}
