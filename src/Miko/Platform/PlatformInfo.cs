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
/// Default mutable <see cref="IPlatformInfo"/>. <see cref="Platform"/> defaults to the detected OS
/// and can be reassigned at runtime (the simulator does this on device change).
/// </summary>
public sealed class PlatformInfo : IPlatformInfo
{
    /// <summary>The current host platform.</summary>
    public HostPlatform Platform { get; set; }

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
