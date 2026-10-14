using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Platform;
using Miko.Simulator.Native;
using Miko.Windowing.Native;
using Shouldly;

namespace Miko.Native.Tests;

/// <summary>
/// Wiring of the component-facing <see cref="IPlatformService"/> (ISSUE-138): one shared instance
/// behind all three registrations, and the platform package's orientation implementation reaching
/// components — whether the container comes from <c>MikoAppBuilder.Build()</c> or is built
/// directly, as harnesses and these tests do.
/// </summary>
public class PlatformServiceRegistrationTests
{
    public static IEnumerable<object[]> ProviderFactories()
    {
        yield return new object[] { "Build()", (Func<MikoAppBuilder, IServiceProvider>)(b => b.Build().Services) };
        yield return new object[] { "BuildServiceProvider()", (Func<MikoAppBuilder, IServiceProvider>)(b => b.Services.BuildServiceProvider()) };
    }

    [Theory]
    [MemberData(nameof(ProviderFactories))]
    public void PlatformInfo_IPlatformInfo_IPlatformService_AreOneInstance(string _, Func<MikoAppBuilder, IServiceProvider> build)
    {
        var services = build(MikoAppBuilder.CreateDefault());

        var concrete = services.GetRequiredService<PlatformInfo>();
        services.GetRequiredService<IPlatformInfo>().ShouldBeSameAs(concrete);
        services.GetRequiredService<IPlatformService>().ShouldBeSameAs(concrete);
    }

    [Theory]
    [MemberData(nameof(ProviderFactories))]
    public void WithoutPlatformPackage_OrientationIsInert(string _, Func<MikoAppBuilder, IServiceProvider> build)
    {
        var services = build(MikoAppBuilder.CreateDefault());

        services.GetRequiredService<IPlatformService>().Orientation.ShouldBeOfType<NullOrientationService>();
    }

    [Theory]
    [MemberData(nameof(ProviderFactories))]
    public void UseDesktopNative_BindsDesktopOrientation(string _, Func<MikoAppBuilder, IServiceProvider> build)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseDesktopNative();

        var services = build(builder);

        services.GetRequiredService<IPlatformService>().Orientation.ShouldBeOfType<DesktopOrientationService>();
    }

    [Theory]
    [MemberData(nameof(ProviderFactories))]
    public void UseSimulatorNative_BindsDesktopOrientation(string _, Func<MikoAppBuilder, IServiceProvider> build)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseSimulatorNative();

        var services = build(builder);

        services.GetRequiredService<IPlatformService>().Orientation.ShouldBeOfType<DesktopOrientationService>();
    }

    /// <summary>Lock/KeepAwake are reference counted so nested callers do not cancel each other.</summary>
    [Fact]
    public void DesktopOrientation_NestedLocks_KeepOuterOrientationUntilLastUnlock()
    {
        var orientation = new DesktopOrientationService();

        orientation.Lock(ScreenOrientation.Landscape);
        orientation.Lock();               // inner lock without a preference keeps the outer one
        orientation.LockedOrientation.ShouldBe(ScreenOrientation.Landscape);

        orientation.Unlock();
        orientation.LockDepth.ShouldBe(1);
        orientation.LockedOrientation.ShouldBe(ScreenOrientation.Landscape);

        orientation.Unlock();
        orientation.LockDepth.ShouldBe(0);
        orientation.LockedOrientation.ShouldBeNull();

        orientation.Unlock();             // extra unlock is a no-op, never negative
        orientation.LockDepth.ShouldBe(0);
    }

    [Fact]
    public void DesktopOrientation_KeepAwake_IsReferenceCounted()
    {
        var orientation = new DesktopOrientationService();

        orientation.KeepAwake();
        orientation.KeepAwake();
        orientation.AllowSleep();
        orientation.AwakeDepth.ShouldBe(1);

        orientation.AllowSleep();
        orientation.AllowSleep();
        orientation.AwakeDepth.ShouldBe(0);
    }
}
