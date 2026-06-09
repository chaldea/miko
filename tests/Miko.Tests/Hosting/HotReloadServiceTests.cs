using Microsoft.Extensions.Logging.Abstractions;
using Miko.Hosting;
using Shouldly;
using Xunit;

namespace Miko.Tests.Hosting;

public class HotReloadServiceTests
{
    [Fact]
    public void TriggerReload_ShouldInvokeRegisteredCallback()
    {
        // Arrange
        var service = new HotReloadService(NullLogger<HotReloadService>.Instance);
        var callbackInvoked = false;
        service.OnReload(() => callbackInvoked = true);

        // Act
        service.TriggerReload();

        // Assert
        callbackInvoked.ShouldBeTrue();
    }

    [Fact]
    public void TriggerReload_WithoutCallback_ShouldNotThrow()
    {
        // Arrange
        var service = new HotReloadService(NullLogger<HotReloadService>.Instance);

        // Act & Assert
        Should.NotThrow(() => service.TriggerReload());
    }

    [Fact]
    public void OnReload_ShouldReplaceExistingCallback()
    {
        // Arrange
        var service = new HotReloadService(NullLogger<HotReloadService>.Instance);
        var firstCallbackInvoked = false;
        var secondCallbackInvoked = false;

        service.OnReload(() => firstCallbackInvoked = true);
        service.OnReload(() => secondCallbackInvoked = true);

        // Act
        service.TriggerReload();

        // Assert
        firstCallbackInvoked.ShouldBeFalse();
        secondCallbackInvoked.ShouldBeTrue();
    }

    [Fact]
    public void TriggerReload_ShouldInvokeCallbackMultipleTimes()
    {
        // Arrange
        var service = new HotReloadService(NullLogger<HotReloadService>.Instance);
        var invocationCount = 0;
        service.OnReload(() => invocationCount++);

        // Act
        service.TriggerReload();
        service.TriggerReload();
        service.TriggerReload();

        // Assert
        invocationCount.ShouldBe(3);
    }
}
