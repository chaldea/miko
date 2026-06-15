using Miko.Hosting;

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
    /// Optional explicit theme. When null, the default light theme for <see cref="Mode"/>
    /// is used.
    /// </summary>
    public IonicTheme? Theme { get; set; }
}

public static class IonicExtensions
{
    /// <summary>
    /// Registers the Ionic component library: applies the Ionic stylesheet derived from the
    /// configured (or default) theme. The visual mode defaults to Material Design and can be
    /// overridden via <see cref="IonicConfiguration.Mode"/> (e.g. by the iOS host). Icons are
    /// bundled as embedded SVG resources and resolved on demand, so no font registration is
    /// required.
    /// </summary>
    public static MikoAppBuilder AddIonic(this MikoAppBuilder builder, Action<IonicConfiguration>? configure = null)
    {
        var config = new IonicConfiguration();
        configure?.Invoke(config);

        var theme = config.Theme ?? IonicTheme.Create(config.Mode);
        var styleSheet = IonicStyleSheetFactory.Create(theme);

        builder.AddStyleSheet(styleSheet);
        return builder;
    }
}
