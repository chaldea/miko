using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Platform;
using Miko.Platform.Camera;
using Miko.Styling;
using Miko.Testing.Camera;
using Shouldly;

namespace Miko.Tests.Core;

/// <summary>
/// The engine owns camera sessions the way it owns video sessions: opened when an active
/// <see cref="CameraElement"/> is in the tree, released when it leaves, is deactivated or asks for
/// another camera; frames are composited upright, mirrored and fitted at paint time.
/// </summary>
public class CameraSessionLifecycleTests : IDisposable
{
    private static readonly Color Red = Color.FromRgb(255, 0, 0);
    private static readonly Color Green = Color.FromRgb(0, 255, 0);
    private static readonly Color Blue = Color.FromRgb(0, 0, 255);
    private static readonly Color White = Color.FromRgb(255, 255, 255);

    private readonly FakeCameraBackend _backend = new();
    private readonly MikoEngine _engine;
    private readonly RenderSurface _surface;
    private readonly DivElement _root = new();

    public CameraSessionLifecycleTests()
    {
        _engine = new MikoEngineBuilder().Build();
        _engine.CameraBackend = _backend;
        _surface = _engine.Graphics.CreateSurface(200, 200);
    }

    public void Dispose()
    {
        _engine.DisposeMediaSessions();
        _surface.Dispose();
    }

    private static List<StyleSheet> Sheet(float width = 200, float height = 200)
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { Display = Display.Block, Width = Length.Px(200), Height = Length.Px(200), BackgroundColor = Color.Black },
            ["camera"] = new() { Display = Display.Block, Width = Length.Px(width), Height = Length.Px(height) },
        });
        return [sheet];
    }

    private void Initialize(params Element[] children)
    {
        foreach (var child in children) _root.Children.Add(child);
        _engine.Initialize(_root, Sheet(), _surface.Context, 200, 200);
    }

    private void Render() => _engine.Render(_surface.Context);

    [Fact]
    public void Should_open_a_session_with_the_element_options()
    {
        var camera = new CameraElement { Facing = CameraFacing.Front, Resolution = CameraResolution.Low };
        Initialize(camera);

        _backend.Sessions.Count.ShouldBe(1);
        _backend.Sessions[0].Options.Facing.ShouldBe(CameraFacing.Front);
        _backend.Sessions[0].Options.Resolution.ShouldBe(CameraResolution.Low);
        camera.Session.ShouldBeSameAs(_backend.Sessions[0]);
    }

    [Fact]
    public void Should_not_open_sessions_without_a_backend()
    {
        _engine.CameraBackend = new NullCameraBackend();
        var camera = new CameraElement();
        Initialize(camera);
        Render();

        camera.Session.ShouldBeNull();
    }

    [Fact]
    public void Should_reopen_when_the_element_asks_for_another_camera()
    {
        var camera = new CameraElement();
        Initialize(camera);
        var first = _backend.Sessions.Single();

        camera.Facing = CameraFacing.Front;
        Render();

        first.Disposed.ShouldBeTrue();
        _backend.Sessions.Count.ShouldBe(2);
        _backend.Sessions[1].Options.Facing.ShouldBe(CameraFacing.Front);
        camera.Session.ShouldBeSameAs(_backend.Sessions[1]);
    }

    [Fact]
    public void Should_keep_the_session_across_frames_and_unrelated_changes()
    {
        var camera = new CameraElement();
        Initialize(camera);
        _root.Children.Add(new DivElement());
        camera.Torch = true;
        camera.Fit = CameraFit.Contain;
        Render();
        Render();

        _backend.Sessions.Count.ShouldBe(1);
        _backend.Sessions[0].Disposed.ShouldBeFalse();
    }

    [Fact]
    public void Should_release_the_device_while_inactive_and_reopen_after()
    {
        var camera = new CameraElement();
        Initialize(camera);

        camera.Active = false;
        Render();
        _backend.Sessions[0].Disposed.ShouldBeTrue();
        camera.Session.ShouldBeNull();

        camera.Active = true;
        Render();
        _backend.Sessions.Count.ShouldBe(2);
        camera.Session.ShouldBeSameAs(_backend.Sessions[1]);
    }

    [Fact]
    public void Should_release_the_session_when_the_element_leaves_the_tree()
    {
        var camera = new CameraElement();
        var changes = new List<ICameraSession?>();
        camera.SessionChanged += changes.Add;
        Initialize(camera);

        _root.Children.Remove(camera);
        Render();

        _backend.Sessions[0].Disposed.ShouldBeTrue();
        camera.Session.ShouldBeNull();
        changes.ShouldBe([_backend.Sessions[0], null]);
    }

    [Fact]
    public void Should_not_retry_a_failed_open_every_frame_but_retry_on_restart()
    {
        _backend.CreateFailure = new InvalidOperationException("busy");
        var camera = new CameraElement();
        var events = new List<CameraSessionEvent>();
        camera.SessionEvent += events.Add;
        Initialize(camera);
        Render();
        Render();

        events.OfType<CameraSessionEvent.Error>().Count().ShouldBe(1);

        _backend.CreateFailure = null;
        camera.Restart();
        Render();
        camera.Session.ShouldNotBeNull();
    }

    [Fact]
    public void Should_restart_a_failed_session_with_a_new_one()
    {
        var camera = new CameraElement();
        Initialize(camera);
        var failed = _backend.Sessions[0];
        failed.Fail(CameraErrorKind.PermissionDenied, "denied");
        Render();
        camera.Session.ShouldBeSameAs(failed);

        camera.Restart();
        Render();

        failed.Disposed.ShouldBeTrue();
        camera.Session.ShouldBeSameAs(_backend.Sessions[1]);
    }

    [Fact]
    public void Should_install_the_analyzer_on_every_session()
    {
        var analyzer = new CountingAnalyzer();
        var camera = new CameraElement { Analyzer = analyzer };
        Initialize(camera);
        _backend.Sessions[0].Analyzer.ShouldBeSameAs(analyzer);

        camera.Facing = CameraFacing.Front;
        Render();
        _backend.Sessions[1].Analyzer.ShouldBeSameAs(analyzer);

        camera.Analyzer = null;
        _backend.Sessions[1].Analyzer.ShouldBeNull();
    }

    [Fact]
    public void Should_push_torch_and_clamped_zoom_to_a_running_session()
    {
        var camera = new CameraElement { Torch = true, Zoom = 10 };
        Initialize(camera);
        var session = _backend.Sessions[0];
        session.TorchCalls.ShouldBeEmpty(); // still starting

        session.Start(40, 30);
        Render();
        session.TorchCalls.ShouldBe([true]);
        session.ZoomCalls.ShouldBe([4f]);

        Render();
        session.TorchCalls.Count.ShouldBe(1);
        session.ZoomCalls.Count.ShouldBe(1);

        camera.Torch = false;
        camera.Zoom = 2;
        Render();
        session.TorchCalls.ShouldBe([true, false]);
        session.ZoomCalls.ShouldBe([4f, 2f]);
    }

    [Fact]
    public void Should_lay_out_an_auto_sized_camera_at_its_upright_frame_size()
    {
        _backend.Rotation = 90;
        var camera = new CameraElement();
        _root.Children.Add(camera);
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { Display = Display.Block, Width = Length.Px(200), Height = Length.Px(200) },
            ["camera"] = new() { Display = Display.Block, Width = Length.Px(60) },
        });
        _engine.Initialize(_root, [sheet], _surface.Context, 200, 200);
        camera.OffsetHeight.ShouldBe(30f, 0.5f); // 300×150 before the first frame

        _backend.Sessions[0].Start(160, 90); // sensor landscape, shown portrait
        Render();
        Render();

        camera.OffsetHeight.ShouldBe(60 * 160 / 90f, 0.5f);
    }

    [Fact]
    public void Should_paint_the_frame_upright_and_cover_the_box()
    {
        // Raw frame: red | green. Rotated 90° clockwise it reads red over green.
        var camera = new CameraElement();
        Initialize(camera);
        var session = _backend.Sessions[0];
        session.Rotation = 90;
        session.Start(20, 10, Blocks(2, 1, 10, Red, Green));
        Render();

        _surface.GetPixel(100, 30).ShouldBe(Red);
        _surface.GetPixel(100, 170).ShouldBe(Green);
    }

    [Fact]
    public void Should_mirror_front_camera_frames_unless_the_element_overrides_it()
    {
        var camera = new CameraElement { Facing = CameraFacing.Front };
        Initialize(camera);
        var session = _backend.Sessions[0];
        session.Start(20, 10, Blocks(2, 1, 10, Red, Green));
        Render();
        _surface.GetPixel(30, 100).ShouldBe(Green);
        _surface.GetPixel(170, 100).ShouldBe(Red);

        camera.Mirror = false;
        _engine.RequestRepaint();
        _engine.InvalidateElement(camera);
        Render();
        _surface.GetPixel(30, 100).ShouldBe(Red);
        _surface.GetPixel(170, 100).ShouldBe(Green);
    }

    [Fact]
    public void Should_letterbox_with_contain_and_crop_with_cover()
    {
        // 4×2 frame: blue left half, white right half; the box is square.
        var camera = new CameraElement { Fit = CameraFit.Contain };
        Initialize(camera);
        var session = _backend.Sessions[0];
        session.Start(40, 20, Blocks(2, 1, 20, Blue, White));
        Render();

        _surface.GetPixel(100, 20).ShouldBe(Color.Black); // letterbox band above the 200×100 picture
        _surface.GetPixel(20, 100).ShouldBe(Blue);
        _surface.GetPixel(180, 100).ShouldBe(White);

        camera.Fit = CameraFit.Cover;
        _engine.InvalidateElement(camera);
        Render();
        _surface.GetPixel(100, 20).ShouldNotBe(Color.Black);
    }

    [Fact]
    public void Should_schedule_a_frame_when_the_camera_delivers_one()
    {
        var camera = new CameraElement();
        Initialize(camera);
        var session = _backend.Sessions[0];
        session.Start(4, 4);
        Render();
        _engine.HasPendingVisualWork.ShouldBeFalse();

        session.PushFrame(4, 4, new byte[64]);

        _engine.HasPendingVisualWork.ShouldBeTrue();
        _engine.Dispatcher.HasPendingActions.ShouldBeTrue();
    }

    [Fact]
    public void Should_pause_cameras_in_the_background_and_resume_them()
    {
        var root = new DivElement();
        var camera = new CameraElement();
        root.Children.Add(camera);
        var engine = new MikoEngineBuilder().Build();
        engine.CameraBackend = _backend;
        var controller = new MikoInteractionController(
            Options.Create(new MikoAppOptions { RootComponentFactory = () => root, StyleSheets = { Sheet()[0] } }),
            new ServiceCollection().BuildServiceProvider(),
            engine,
            new EventDispatcher(),
            new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance),
            NullLogger<MikoInteractionController>.Instance);
        using var surface = engine.Graphics.CreateSurface(200, 200);
        controller.Initialize(surface.Context, 200, 200);
        engine.Render(surface.Context);
        var session = _backend.Sessions.Single();
        session.Start(4, 4);

        controller.Suspend();
        session.PauseCount.ShouldBe(1);
        session.State.ShouldBe(CameraSessionState.Paused);

        controller.Resume();
        session.ResumeCount.ShouldBe(1);
        session.State.ShouldBe(CameraSessionState.Running);
        engine.DisposeMediaSessions();
        session.Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_capture_an_upright_photo()
    {
        var camera = new CameraElement();
        Initialize(camera);
        var session = _backend.Sessions[0];
        session.Start(4, 4);
        // 2×1 still: red | green, to be rotated a quarter turn clockwise.
        session.StillFactory = _ => CameraStill.FromPixels(2, 1, ImagePixelFormat.Rgba8888, Rgba(Red, Green), 8, rotation: 90);

        var photo = await camera.CapturePhotoAsync(new CameraCaptureOptions { Format = CameraPhotoFormat.Png });

        photo.Width.ShouldBe(1);
        photo.Height.ShouldBe(2);
        photo.MimeType.ShouldBe("image/png");
        using var decoded = _engine.Graphics.DecodeImage(photo.Data)!;
        var pixels = new byte[8];
        decoded.ReadPixels(pixels, ImagePixelFormat.Rgba8888).ShouldBeTrue();
        pixels.ShouldBe(Rgba(Red, Green));
    }

    [Fact]
    public async Task Should_unmirror_front_photos_unless_asked_and_scale_to_the_max_dimension()
    {
        var camera = new CameraElement { Facing = CameraFacing.Front };
        Initialize(camera);
        var session = _backend.Sessions[0];
        session.Start(4, 4);
        session.StillFactory = _ => CameraStill.FromPixels(2, 1, ImagePixelFormat.Rgba8888, Rgba(Red, Green), 8, isMirrored: true);

        var plain = await camera.CapturePhotoAsync(new CameraCaptureOptions { Format = CameraPhotoFormat.Png });
        var mirrored = await camera.CapturePhotoAsync(new CameraCaptureOptions { Format = CameraPhotoFormat.Png, MirrorFrontCamera = true });

        LeftPixel(plain).ShouldBe(Red);
        LeftPixel(mirrored).ShouldBe(Green);

        session.StillFactory = _ => CameraStill.FromPixels(400, 100, ImagePixelFormat.Rgba8888, new byte[400 * 100 * 4], 1600);
        var small = await camera.CapturePhotoAsync(new CameraCaptureOptions { MaxDimension = 100 });
        (small.Width, small.Height).ShouldBe((100, 25));
        small.Format.ShouldBe(CameraPhotoFormat.Jpeg);
    }

    [Fact]
    public async Task Should_return_an_untouched_jpeg_as_it_came()
    {
        using var source = _engine.Graphics.CreateSurface(8, 4);
        var png = source.EncodePng();
        var camera = new CameraElement();
        Initialize(camera);
        var session = _backend.Sessions[0];
        session.Start(4, 4);
        var jpeg = await camera.CapturePhotoAsync(new CameraCaptureOptions { Quality = 80 }.WithStill(session, CameraStill.FromEncoded(png)));
        session.StillFactory = _ => CameraStill.FromEncoded(jpeg.Data);

        var again = await camera.CapturePhotoAsync();

        again.Data.ShouldBeSameAs(jpeg.Data);
        (again.Width, again.Height).ShouldBe((8, 4));
    }

    [Fact]
    public async Task Should_refuse_to_capture_without_a_running_session()
    {
        var camera = new CameraElement();
        Initialize(camera);

        await Should.ThrowAsync<InvalidOperationException>(() => camera.CapturePhotoAsync());
    }

    [Theory]
    [InlineData(SkiaSharp.SKEncodedOrigin.TopLeft, 0, false, 0, false)]
    [InlineData(SkiaSharp.SKEncodedOrigin.RightTop, 0, false, 90, false)]
    [InlineData(SkiaSharp.SKEncodedOrigin.RightTop, 90, false, 180, false)]
    [InlineData(SkiaSharp.SKEncodedOrigin.TopRight, 90, false, 270, true)]
    [InlineData(SkiaSharp.SKEncodedOrigin.LeftBottom, 0, true, 270, true)]
    [InlineData(SkiaSharp.SKEncodedOrigin.LeftTop, 0, false, 90, true)]
    public void Should_compose_exif_orientation_rotation_and_mirror(SkiaSharp.SKEncodedOrigin origin, int extra, bool mirror, int rotation, bool flip)
        => CameraPhotoEncoder.Compose(origin, extra, mirror).ShouldBe((rotation, flip));

    [Theory]
    [InlineData(90, 0, CameraFacing.Back, 90)]
    [InlineData(90, 90, CameraFacing.Back, 0)]
    [InlineData(90, 270, CameraFacing.Back, 180)]
    [InlineData(270, 0, CameraFacing.Front, 270)]
    [InlineData(270, 90, CameraFacing.Front, 0)]
    [InlineData(0, 0, CameraFacing.External, 0)]
    public void Should_compute_the_preview_rotation(int sensor, int display, CameraFacing facing, int expected)
        => CameraOrientation.PreviewRotation(sensor, display, facing).ShouldBe(expected);

    [Fact]
    public void Should_parse_camera_attributes_from_markup()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "camera");
        builder.AddAttribute(1, "facing", "front");
        builder.AddAttribute(2, "active", "false");
        builder.AddAttribute(3, "torch");
        builder.AddAttribute(4, "fit", "contain");
        builder.CloseElement();

        var camera = builder.Build().ShouldBeOfType<CameraElement>();
        camera.Facing.ShouldBe(CameraFacing.Front);
        camera.Active.ShouldBeFalse();
        camera.Torch.ShouldBeTrue();
        camera.Fit.ShouldBe(CameraFit.Contain);
    }

    private Color LeftPixel(CameraPhoto photo)
    {
        using var decoded = _engine.Graphics.DecodeImage(photo.Data)!;
        var pixels = new byte[decoded.Width * decoded.Height * 4];
        decoded.ReadPixels(pixels, ImagePixelFormat.Rgba8888).ShouldBeTrue();
        return Color.FromRgb(pixels[0], pixels[1], pixels[2]);
    }

    private static byte[] Rgba(params Color[] colors)
    {
        var pixels = new byte[colors.Length * 4];
        for (int i = 0; i < colors.Length; i++)
        {
            pixels[i * 4] = colors[i].R;
            pixels[i * 4 + 1] = colors[i].G;
            pixels[i * 4 + 2] = colors[i].B;
            pixels[i * 4 + 3] = 255;
        }
        return pixels;
    }

    /// <summary>An RGBA frame of <paramref name="columns"/> × <paramref name="rows"/> solid square blocks.</summary>
    private static byte[] Blocks(int columns, int rows, int size, params Color[] colors)
    {
        colors.Length.ShouldBe(columns * rows);
        int width = columns * size;
        var pixels = new byte[width * rows * size * 4];
        for (int y = 0; y < rows * size; y++)
        for (int x = 0; x < width; x++)
        {
            var c = colors[y / size * columns + x / size];
            int i = (y * width + x) * 4;
            (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]) = (c.R, c.G, c.B, 255);
        }
        return pixels;
    }

    private sealed class CountingAnalyzer : ICameraFrameAnalyzer
    {
        public int Count;
        public void Analyze(in CameraAnalysisFrame frame) => Interlocked.Increment(ref Count);
    }
}

internal static class CameraCaptureTestExtensions
{
    /// <summary>Makes the next capture of <paramref name="session"/> return <paramref name="still"/>.</summary>
    public static CameraCaptureOptions WithStill(this CameraCaptureOptions options, FakeCameraSession session, CameraStill still)
    {
        session.StillFactory = _ => still;
        return options;
    }
}
