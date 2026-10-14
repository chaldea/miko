using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Platform;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace Miko.Tests.Platform;

/// <summary>
/// Verifies that the desktop back key (Escape) is routed into the platform back-button chain before
/// ordinary key handling (ISSUE-138). Without this, a modal would close on Escape while a
/// component also listening for Escape (e.g. the player's fullscreen toggle) acted on the same
/// press — one key, two behaviors.
/// </summary>
public class BackButtonEscapeTests
{
    private static MikoInteractionController CreateController(MikoAppOptions options, IPlatformService? platform)
    {
        var engine = new MikoEngineBuilder().Build();
        var controller = new MikoInteractionController(
            Options.Create(options),
            new EmptyServiceProvider(),
            engine,
            new EventDispatcher(),
            new MikoDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance),
            NullLogger<MikoInteractionController>.Instance,
            platformService: platform);

        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        controller.Initialize(surface.Canvas, 100, 100);
        return controller;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    [Fact]
    public void OnKeyDown_Escape_WithNoHandlers_FallsThroughToKeyHandling()
    {
        var options = new MikoAppOptions { RootComponentFactory = () => new DivElement() };
        var platform = new PlatformInfo(HostPlatform.Windows);
        var controller = CreateController(options, platform);

        // No back handler registered: Escape is not consumed by the chain, so ordinary key
        // handling runs (here it simply reports "not handled").
        controller.OnKeyDown(MikoKey.Escape, MikoKeyModifiers.None).ShouldBeFalse();
    }

    [Fact]
    public void OnKeyDown_Escape_ConsumedByBackHandler_IsNotDispatchedFurther()
    {
        // A stand-in for any component that reacts to Escape itself (the player's fullscreen
        // toggle is the real one). It must not fire when the back chain already consumed the key.
        var escaped = false;
        var options = new MikoAppOptions
        {
            RootComponentFactory = () => new DivElement(),
            GlobalKeyDownHandlers = { key => { if (key == MikoKey.Escape) escaped = true; return false; } },
        };
        var platform = new PlatformInfo(HostPlatform.Windows);
        platform.Back.Register(() => true);

        var controller = CreateController(options, platform);

        controller.OnKeyDown(MikoKey.Escape, MikoKeyModifiers.None).ShouldBeTrue();

        // The global key handler must not see the key once the back chain consumed it.
        escaped.ShouldBeFalse();
    }

    [Fact]
    public void OnKeyDown_NonEscapeKey_IsNotRoutedToBackChain()
    {
        var options = new MikoAppOptions { RootComponentFactory = () => new DivElement() };
        var platform = new PlatformInfo(HostPlatform.Windows);
        var backCalls = 0;
        platform.Back.Register(() => { backCalls++; return true; });

        var controller = CreateController(options, platform);

        controller.OnKeyDown(MikoKey.Enter, MikoKeyModifiers.None);

        backCalls.ShouldBe(0);
    }

    [Fact]
    public void OnKeyDown_Escape_WithoutPlatformService_IsHarmless()
    {
        var options = new MikoAppOptions { RootComponentFactory = () => new DivElement() };
        var controller = CreateController(options, platform: null);

        // A controller built without a platform service (bare tests, engine-only hosts) must
        // still take the key without throwing.
        controller.OnKeyDown(MikoKey.Escape, MikoKeyModifiers.None).ShouldBeFalse();
    }
}
