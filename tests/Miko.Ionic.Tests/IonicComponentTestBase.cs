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
    private readonly PlatformInfo _platform;

    protected TestContext Context { get; }

    /// <summary>
    /// The shared platform service backing <see cref="Context"/>. Tests use it to drive the system
    /// back button (<c>Platform.Back.Dispatch()</c>) and to observe registrations (ISSUE-138).
    /// One instance backs both <see cref="IPlatformInfo"/> and <see cref="IPlatformService"/>, as
    /// in <c>MikoAppBuilder.CreateDefault</c>.
    /// </summary>
    protected PlatformInfo Platform => _platform;

    /// <summary>
    /// The haptic feedback behind <see cref="Platform"/>: records every call so tests can assert
    /// when components tick (ISSUE-149).
    /// </summary>
    protected RecordingHapticFeedback Haptics { get; } = new();

    protected IonicComponentTestBase()
    {
        _platform = new PlatformInfo(HostPlatform.Android) { Haptics = Haptics };
        Context = new TestContext();
        Context.Services.AddSingleton<IPlatformInfo>(_platform);
        Context.Services.AddSingleton<IPlatformService>(_platform);
    }

    /// <summary>Sets the host platform used for subsequent renders (md for all but iOS).</summary>
    protected void UsePlatform(HostPlatform platform) => _platform.Platform = platform;

    /// <summary>Simulates a system back button press. Returns whether a component consumed it.</summary>
    protected bool PressBack() => _platform.Back.Dispatch();

    public void Dispose()
    {
        Context.Dispose();
    }
}
