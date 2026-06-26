using Microsoft.Extensions.DependencyInjection;
using Miko.Platform;
using Miko.Testing;

namespace Miko.Ionic.Tests;

/// <summary>
/// Base class for Ionic component tests providing common test utilities.
/// <para>
/// Registers a host <see cref="IPlatformInfo"/> so the components resolve a deterministic Ionic
/// mode (defaults to Android → Material Design). Call <see cref="UsePlatform"/> before rendering
/// to exercise a different platform (e.g. iOS).
/// </para>
/// </summary>
public abstract class IonicComponentTestBase : IDisposable
{
    private readonly PlatformInfo _platform = new(HostPlatform.Android);

    protected TestContext Context { get; }

    protected IonicComponentTestBase()
    {
        Context = new TestContext();
        Context.Services.AddSingleton<IPlatformInfo>(_platform);
    }

    /// <summary>Sets the host platform used for subsequent renders (md for all but iOS).</summary>
    protected void UsePlatform(HostPlatform platform) => _platform.Platform = platform;

    public void Dispose()
    {
        Context.Dispose();
    }
}
