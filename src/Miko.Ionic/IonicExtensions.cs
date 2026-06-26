using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Platform;

namespace Miko.Ionic;

/// <summary>
/// Configuration for the Ionic component library.
/// </summary>
public class IonicConfiguration
{
    /// <summary>
    /// Visual mode (Material Design or iOS). Supplied by the platform; defaults to
    /// <see cref="IonicMode.Md"/>. Ignored when <see cref="Theme"/> is set explicitly.
    /// </summary>
    public IonicMode Mode { get; set; } = IonicMode.Md;

    /// <summary>
    /// Optional explicit theme. When null, the default light themes for both modes are used.
    /// </summary>
    public IonicTheme? Theme { get; set; }

    /// <summary>
    /// Optional platform override. When null, the host platform registered in the container
    /// (auto-detected from the OS, or set by the platform host / simulator) decides the mode.
    /// Set this only to force a specific platform regardless of the host.
    /// </summary>
    public HostPlatform? Platform { get; set; }
}

public static class IonicExtensions
{
    /// <summary>
    /// Registers the Ionic component library: applies the Ionic stylesheet (carrying both the
    /// Material Design and iOS rule sets). The active mode follows the host
    /// <see cref="IPlatformInfo"/> (iOS → ios, otherwise → md) and can switch at runtime when the
    /// platform changes (e.g. the simulator swapping the selected device). Icons are bundled as
    /// embedded SVG resources and resolved on demand, so no font registration is required.
    /// </summary>
    public static MikoAppBuilder AddIonic(this MikoAppBuilder builder, Action<IonicConfiguration>? configure = null)
    {
        var config = new IonicConfiguration();
        configure?.Invoke(config);

        var styleSheet = config.Theme != null
            ? IonicStyleSheetFactory.Create(config.Theme)
            : IonicStyleSheetFactory.CreateAllModes();

        // Override the host platform only when explicitly requested (an explicit Platform, or an
        // iOS theme/mode). Otherwise leave the default auto-detected IPlatformInfo in place.
        HostPlatform? forced = config.Platform;
        if (forced == null && (config.Theme?.Mode ?? config.Mode) == IonicMode.Ios)
            forced = HostPlatform.Ios;

        if (forced is { } platform)
            builder.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(platform));

        builder.AddStyleSheet(styleSheet);
        return builder;
    }
}
