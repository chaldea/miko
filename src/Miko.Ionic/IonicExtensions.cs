using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;

namespace Miko.Ionic;

/// <summary>
/// Configuration for the Ionic component library.
/// </summary>
public class IonicOptions
{
    /// <summary>
    /// iOS opt-in, retained for compatibility. Otherwise components follow the host platform.
    /// Use <see cref="Platform"/> to force either design mode independently of the host.
    /// </summary>
    public IonicMode Mode { get; set; } = IonicMode.Md;

    /// <summary>
    /// The partial theme applied at application startup. Its explicitly set token values are
    /// resolved against the active mode's defaults for every component.
    /// </summary>
    public IonicTheme Theme { get; set; } = new();

    /// <summary>
    /// Optional Ionic platform override. When null, components follow the host platform unless
    /// an iOS mode/theme was explicitly supplied. This does not replace the host's platform service.
    /// </summary>
    public HostPlatform? Platform { get; set; }
}

/// <summary>Compatibility name for the options passed to older <see cref="IonicExtensions.AddIonic"/> calls.</summary>
[Obsolete("Use IonicOptions.")]
public class IonicConfiguration : IonicOptions
{
}

public static class IonicExtensions
{
    /// <summary>
    /// Registers the Ionic component library. Global utility rules are attached immediately;
    /// individual component rule sets are registered when their first instance is built. The active mode follows the host
    /// <see cref="IPlatformInfo"/> (iOS → ios, otherwise → md) and can switch at runtime when the
    /// platform changes (e.g. the simulator swapping the selected device). Icons are bundled as
    /// embedded SVG resources and resolved on demand, so no font registration is required.
    /// </summary>
    public static MikoAppBuilder AddIonic(this MikoAppBuilder builder, Action<IonicOptions>? configure = null)
    {
        builder.Services.AddOptions<IonicOptions>();
        if (configure != null)
            builder.Services.Configure(configure);

        var styleRegistry = new IonicStyleRegistry();
        builder.Services.AddSingleton(styleRegistry);

        builder.Services.AddSingleton<IonOverlayRegistry>();
        builder.Services.AddSingleton<IonOverlayManager>();
        builder.Services.AddSingleton<IonModalController>();
        builder.Services.AddSingleton<IonAlertController>();
        builder.Services.AddSingleton<IonActionSheetController>();
        builder.Services.AddSingleton<IonLoadingController>();
        builder.Services.AddSingleton<IonPopoverController>();
        builder.Services.AddSingleton<IonToastController>();

        builder.AddStyleSheet(styleRegistry.StyleSheet);
        return builder;
    }
}
