using Miko.Components.Player;
using Miko.Hosting;

namespace MikoApp.Player;

/// <summary>
/// Shared configuration of the player demo. Each head (Desktop / Android / iOS) registers its
/// platform video backend and native services through <c>configure</c> and drives the render loop.
/// </summary>
public static class App
{
    /// <summary>
    /// Where the heads bundle the sample clip: next to the app on the desktop, inside the app bundle
    /// on iOS. Relative paths resolve against those directories, not the working directory.
    /// </summary>
    public const string BundledClip = "Assets/miko-local.mp4";

    /// <summary>A public HLS stream (internet access required).</summary>
    public const string HlsSample = "https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8";

    /// <param name="configure">
    /// The head's registrations: its video backend (<c>UseSystemVideo()</c> / <c>UseAndroidVideo()</c> /
    /// <c>UseIosVideo()</c>) and native services (<c>UseDesktopNative()</c> / <c>UseAndroidNative()</c> /
    /// <c>UseIosNative()</c>) — the latter make fullscreen lock landscape and hide the system bars on phones.
    /// </param>
    /// <param name="localSource">The sample clip; Android passes the copy it extracted from its assets.</param>
    /// <param name="initialSource">The source to open first; the sample clip when null.</param>
    public static MikoAppContext CreateContext(
        Action<MikoAppBuilder>? configure = null, string localSource = BundledClip, string? initialSource = null)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("MikoPlayer").UseSize(960, 760);
        builder.Services.AddMikoPlayer();
        builder.UseRootComponent(() => new PlayerDemo { LocalSource = localSource, InitialSource = initialSource }.Build());
        configure?.Invoke(builder);
        return builder.Build();
    }
}
