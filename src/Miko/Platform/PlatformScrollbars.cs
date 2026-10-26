using Miko.Common;

namespace Miko.Platform;

/// <summary>
/// Each platform's default scrollbar presentation — what <c>scrollbar-width: auto</c> means there
/// (ISSUE-156). Mirrors the browsers: classic scrollbars on Windows and Linux, overlay scrollbars
/// that appear while scrolling on macOS, and none on iOS / Android.
/// <para>
/// Apps adjust it per platform with <c>MikoAppBuilder.UseScrollbarStyle</c>; the interaction
/// controller applies the result for the current <see cref="IPlatformInfo.Platform"/> every frame,
/// so the device simulator follows a device switch.
/// </para>
/// </summary>
public static class PlatformScrollbars
{
    /// <summary>The default presentation on <paramref name="platform"/>.</summary>
    public static ScrollbarStyle GetDefault(HostPlatform platform) => platform switch
    {
        HostPlatform.MacOS => ScrollbarStyle.Overlay,
        HostPlatform.Ios or HostPlatform.Android => ScrollbarStyle.None,
        _ => ScrollbarStyle.Classic,
    };
}
