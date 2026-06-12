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
/// Verifies that global key handlers registered via <see cref="MikoAppBuilder.AddGlobalKeyHandler"/>
/// (used e.g. by DevTools to toggle on F12) are invoked by the platform-neutral
/// <see cref="MikoInteractionController.OnKeyDown"/> entry point that every host calls.
/// </summary>
public class GlobalKeyHandlerTests
{
    private static MikoInteractionController CreateController(MikoAppOptions options, MikoEngine engine)
    {
        return new MikoInteractionController(
            Options.Create(options),
            new EmptyServiceProvider(),
            engine,
            new EventDispatcher(),
            new HotReloadService(NullLogger<HotReloadService>.Instance),
            NullLogger<MikoInteractionController>.Instance);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private static MikoInteractionController CreateInitializedController(MikoAppOptions options)
    {
        options.RootComponentFactory ??= () => new DivElement();
        var engine = new MikoEngine();
        var controller = CreateController(options, engine);
        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        controller.Initialize(surface.Canvas, 100, 100);
        return controller;
    }

    [Fact]
    public void OnKeyDown_F12_InvokesGlobalKeyHandler()
    {
        // Arrange - a DevTools-style handler that only reacts to F12
        MikoKey? receivedKey = null;
        var options = new MikoAppOptions
        {
            GlobalKeyDownHandlers =
            {
                key =>
                {
                    if (key == MikoKey.F12)
                    {
                        receivedKey = key;
                        return true;
                    }
                    return false;
                }
            }
        };
        var controller = CreateInitializedController(options);

        // Act
        bool consumed = controller.OnKeyDown(MikoKey.F12, MikoKeyModifiers.None);

        // Assert - handler ran and reported the key as consumed
        receivedKey.ShouldBe(MikoKey.F12);
        consumed.ShouldBeTrue();
    }

    [Fact]
    public void OnKeyDown_NonMatchingKey_DoesNotConsume()
    {
        // Arrange - handler reacts only to F12
        var options = new MikoAppOptions
        {
            GlobalKeyDownHandlers = { key => key == MikoKey.F12 }
        };
        var controller = CreateInitializedController(options);

        // Act - a different key
        bool consumed = controller.OnKeyDown(MikoKey.Enter, MikoKeyModifiers.None);

        // Assert - not consumed by the F12 handler
        consumed.ShouldBeFalse();
    }

    [Fact]
    public void OnKeyDown_MultipleHandlers_StopsAtFirstConsumer()
    {
        // Arrange - two handlers; the first consumes F12, the second must not run for F12
        bool secondRan = false;
        var options = new MikoAppOptions
        {
            GlobalKeyDownHandlers =
            {
                key => key == MikoKey.F12,
                _ => { secondRan = true; return false; }
            }
        };
        var controller = CreateInitializedController(options);

        // Act
        bool consumed = controller.OnKeyDown(MikoKey.F12, MikoKeyModifiers.None);

        // Assert - first handler consumed; second handler skipped
        consumed.ShouldBeTrue();
        secondRan.ShouldBeFalse();
    }
}
