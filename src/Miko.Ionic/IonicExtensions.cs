using Miko.Hosting;

namespace Miko.Ionic;

/// <summary>
/// Configuration for the Ionic component library.
/// </summary>
public class IonicConfiguration
{
    /// <summary>Optional explicit theme. When null, the default Ionic light theme is used.</summary>
    public IonicTheme? Theme { get; set; }
}

public static class IonicExtensions
{
    /// <summary>
    /// Registers the Ionic component library: applies the Ionic stylesheet derived from the
    /// configured (or default) theme. Icons are bundled as embedded SVG resources and resolved
    /// on demand, so no font registration is required.
    /// </summary>
    public static MikoAppBuilder AddIonic(this MikoAppBuilder builder, Action<IonicConfiguration>? configure = null)
    {
        var config = new IonicConfiguration();
        configure?.Invoke(config);

        var theme = config.Theme ?? IonicTheme.CreateDefault();
        var styleSheet = IonicStyleSheetFactory.Create(theme);

        builder.AddStyleSheet(styleSheet);
        return builder;
    }
}
