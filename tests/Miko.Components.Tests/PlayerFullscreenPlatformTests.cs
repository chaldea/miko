using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Components.Player;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Platform;
using Miko.Styling;
using Miko.Graphics;
using Shouldly;

namespace Miko.Components.Tests;

/// <summary>
/// Fullscreen on a phone (ISSUE-149): the player locks landscape, keeps the screen on, and lets the
/// system back button leave fullscreen before it leaves the page. Every request is released exactly
/// once — on leaving fullscreen or on dispose — so the screen is never left locked or awake.
/// </summary>
public class PlayerFullscreenPlatformTests : IDisposable
{
    private readonly RenderSurface _surface;
    private readonly IDrawingContext _canvas;
    private readonly MikoEngine _engine;
    private readonly RecordingOrientation _orientation = new();
    private readonly PlatformInfo _platform;
    private readonly MikoPlayer _player;
    private readonly DivElement _root;

    public PlayerFullscreenPlatformTests()
    {
        _engine = new MikoEngineBuilder().Build();
        _surface = _engine.Graphics.CreateSurface(360, 740);
        _canvas = _surface.Context;
        _platform = new PlatformInfo(HostPlatform.Android) { Orientation = _orientation };
        _player = new MikoPlayer { Engine = _engine, PlatformService = _platform };
        _root = new DivElement { Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Percent(100) } };
        _root.Children.Add(_player.Build());
        _engine.Initialize(_root, [], _canvas, 360, 740);
        _engine.Render(_canvas);
    }

    public void Dispose()
    {
        _player.Dispose();
        _surface.Dispose();
    }

    [Fact]
    public void Should_lock_landscape_and_keep_awake_while_fullscreen()
    {
        _player.ToggleFullscreen();

        _orientation.LockDepth.ShouldBe(1);
        _orientation.LockedOrientation.ShouldBe(ScreenOrientation.Landscape);
        _orientation.AwakeDepth.ShouldBe(1);
        _platform.Back.Count.ShouldBe(1);
    }

    [Fact]
    public void Should_release_every_request_when_leaving_fullscreen()
    {
        _player.ToggleFullscreen();
        _player.ToggleFullscreen();

        _orientation.LockDepth.ShouldBe(0);
        _orientation.AwakeDepth.ShouldBe(0);
        _platform.Back.Count.ShouldBe(0);
    }

    [Fact]
    public void Should_leave_fullscreen_on_system_back_then_pass_back_on()
    {
        var changes = 0;
        _player.FullscreenChanged += () => changes++;
        _player.ToggleFullscreen();

        _platform.Back.Dispatch().ShouldBeTrue();

        _player.IsFullscreen.ShouldBeFalse();
        _orientation.LockDepth.ShouldBe(0);
        _orientation.AwakeDepth.ShouldBe(0);
        changes.ShouldBe(2);
        // Out of fullscreen the player no longer claims back: it reaches the page / platform.
        _platform.Back.Dispatch().ShouldBeFalse();
    }

    /// <summary>The page's back button registered first, at navigation priority, must wait.</summary>
    [Fact]
    public void Should_outrank_page_navigation_on_back()
    {
        var navigated = false;
        using var page = _platform.Back.Register(() => navigated = true, BackButtonPriority.Navigation);
        _player.ToggleFullscreen();

        _platform.Back.Dispatch().ShouldBeTrue();

        _player.IsFullscreen.ShouldBeFalse();
        navigated.ShouldBeFalse();
    }

    [Fact]
    public void Should_release_every_request_when_disposed_while_fullscreen()
    {
        _player.ToggleFullscreen();

        _player.Dispose();

        _orientation.LockDepth.ShouldBe(0);
        _orientation.AwakeDepth.ShouldBe(0);
        _platform.Back.Count.ShouldBe(0);
    }

    /// <summary>
    /// Desktop Escape goes through the back chain, which leaves fullscreen and swallows the key, so
    /// the player's own Escape shortcut must not toggle it a second time.
    /// </summary>
    [Fact]
    public void Should_leave_fullscreen_once_on_escape_through_the_controller()
    {
        var controller = new MikoInteractionController(
            Options.Create(new MikoAppOptions { RootComponentFactory = () => _root }),
            new EmptyServiceProvider(),
            _engine,
            new EventDispatcher(),
            new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance),
            NullLogger<MikoInteractionController>.Instance,
            platformService: _platform);
        controller.Initialize(_canvas, 360, 740);
        _player.ToggleFullscreen();

        controller.OnKeyDown(MikoKey.Escape, MikoKeyModifiers.None).ShouldBeTrue();

        _player.IsFullscreen.ShouldBeFalse();
        _orientation.LockDepth.ShouldBe(0);
    }

    /// <summary>Without a platform service there is no back chain; the Escape shortcut still works.</summary>
    [Fact]
    public void Should_leave_fullscreen_on_escape_without_a_platform_service()
    {
        using var player = new MikoPlayer { Engine = _engine };
        var root = player.Build();
        player.ToggleFullscreen();

        root.OnKeyDown!.Invoke(new KeyboardEventArgs { Key = "Escape", Target = root });

        player.IsFullscreen.ShouldBeFalse();
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class RecordingOrientation : IOrientationService
    {
        public int LockDepth { get; private set; }
        public int AwakeDepth { get; private set; }
        public ScreenOrientation? LockedOrientation { get; private set; }

        public void Lock(ScreenOrientation? orientation = null)
        {
            if (LockDepth++ == 0) LockedOrientation = orientation;
        }

        public void Unlock()
        {
            if (LockDepth == 0) return;
            if (--LockDepth == 0) LockedOrientation = null;
        }

        public Task SetAsync(ScreenOrientation orientation) => Task.CompletedTask;
        public void KeepAwake() => AwakeDepth++;
        public void AllowSleep() => AwakeDepth = Math.Max(0, AwakeDepth - 1);
    }
}
