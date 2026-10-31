namespace Miko.Platform;

/// <summary>
/// The operating-system platform the app is running on. Supplied by the platform implementation
/// (desktop / Android / iOS hosts set it; the device simulator sets it from the selected device).
/// Component libraries (e.g. Miko.Ionic) derive their own design choices from it.
/// </summary>
public enum HostPlatform
{
    /// <summary>Apple iOS / iPadOS.</summary>
    Ios,
    /// <summary>Google Android.</summary>
    Android,
    /// <summary>Microsoft Windows.</summary>
    Windows,
    /// <summary>Linux.</summary>
    Linux,
    /// <summary>Apple macOS.</summary>
    MacOS,
}

/// <summary>
/// Exposes the host platform to the application and component libraries. Registered as a singleton
/// by <c>MikoAppBuilder.CreateDefault</c> (auto-detected from the OS); platform hosts and the
/// device simulator may override <see cref="Platform"/> — the simulator mutates it live as the
/// user selects a different device so platform-dependent UI (e.g. Ionic's md/ios mode) switches.
/// </summary>
public interface IPlatformInfo
{
    /// <summary>The host platform.</summary>
    HostPlatform Platform { get; }
}

/// <summary>
/// Default mutable <see cref="IPlatformInfo"/> and <see cref="IPlatformService"/>.
/// <see cref="Platform"/> defaults to the detected OS and can be reassigned at runtime (the
/// simulator does this on device change).
///
/// <para>
/// Because it serves both interfaces, the two DI registrations point at the same instance — so a
/// component that injects <see cref="IPlatformService"/> and the simulator that rewrites
/// <see cref="Platform"/> are always looking at the same object (ISSUE-138).
/// </para>
/// </summary>
public sealed class PlatformInfo : IPlatformService
{
    /// <summary>The current host platform.</summary>
    public HostPlatform Platform { get; set; }

    /// <summary>
    /// The system back-button dispatch chain. Shared by every component in the process — a back
    /// event can only mean one thing at a time, so this is deliberately not per-component.
    /// </summary>
    public BackButtonRegistry Back { get; } = new();

    /// <summary>
    /// Screen orientation / keep-awake. Defaults to the inert implementation so components can call
    /// it unconditionally; <c>MikoAppBuilder.CreateDefault</c> initializes it from the registered
    /// <see cref="IOrientationService"/> (replaced by the platform packages via <c>UseOrientation</c>).
    /// </summary>
    public IOrientationService Orientation { get; init; } = new NullOrientationService();

    /// <summary>
    /// Haptic feedback (ISSUE-149). Defaults to the inert implementation so components can call it
    /// unconditionally; <c>MikoAppBuilder.CreateDefault</c> initializes it from the registered
    /// <see cref="IHapticFeedback"/> (replaced by the platform packages via <c>UseHaptics</c>).
    /// </summary>
    public IHapticFeedback Haptics { get; init; } = new NullHapticFeedback();

    /// <summary>
    /// System bar visibility (immersive fullscreen). Defaults to the inert implementation so
    /// components can call it unconditionally; <c>MikoAppBuilder.CreateDefault</c> initializes it
    /// from the registered <see cref="ISystemBarsService"/> (replaced by the platform packages via
    /// <c>UseSystemBars</c>).
    /// </summary>
    public ISystemBarsService SystemBars { get; init; } = new NullSystemBarsService();

    public PlatformInfo() => Platform = Detect();

    public PlatformInfo(HostPlatform platform) => Platform = platform;

    /// <summary>
    /// Detects the platform from the running operating system. Falls back to
    /// <see cref="HostPlatform.Windows"/> for unknown hosts.
    /// </summary>
    public static HostPlatform Detect()
    {
        if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS()) return HostPlatform.Ios;
        if (OperatingSystem.IsAndroid()) return HostPlatform.Android;
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst()) return HostPlatform.MacOS;
        if (OperatingSystem.IsLinux()) return HostPlatform.Linux;
        return HostPlatform.Windows;
    }
}
