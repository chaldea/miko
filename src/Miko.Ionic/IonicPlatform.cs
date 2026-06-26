using Miko.Platform;

namespace Miko.Ionic;

/// <summary>
/// Maps the host platform to the Ionic visual mode. Ionic ships two design languages — Material
/// Design (Android / web / desktop) and iOS — and picks one from the platform: iOS hosts use
/// <see cref="IonicMode.Ios"/>, every other platform uses <see cref="IonicMode.Md"/>. This mirrors
/// the Ionic framework's platform→mode table. The platform itself is supplied by the platform
/// implementation via <see cref="IPlatformInfo"/> (see <c>Miko.Platform</c>).
/// </summary>
public static class IonicModeResolver
{
    /// <summary>The Ionic mode for a given host platform.</summary>
    public static IonicMode ForPlatform(HostPlatform platform) =>
        platform == HostPlatform.Ios ? IonicMode.Ios : IonicMode.Md;

    /// <summary>
    /// The Ionic mode for the supplied platform info, falling back to Material Design when no
    /// platform service is available (e.g. a bare unit test) — matching Ionic's default.
    /// </summary>
    public static IonicMode Resolve(IPlatformInfo? info) =>
        info is null ? IonicMode.Md : ForPlatform(info.Platform);

    /// <summary>The mode class string (<c>"md"</c> / <c>"ios"</c>) for the supplied platform info.</summary>
    public static string ResolveClass(IPlatformInfo? info) =>
        Resolve(info) == IonicMode.Ios ? "ios" : "md";
}
