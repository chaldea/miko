using Miko.Hosting;
using Miko.Bootstrap.Themes;

namespace Miko.Bootstrap;

public class BootstrapConfiguration
{
    public string DefaultTheme { get; set; } = "light";
    public Theme? Theme { get; set; }
}

public static class BootstrapExtensions
{
    public static MikoAppBuilder AddBootstrap(this MikoAppBuilder builder, Action<BootstrapConfiguration>? configure = null)
    {
        var config = new BootstrapConfiguration();
        configure?.Invoke(config);

        var theme = config.Theme ?? (config.DefaultTheme == "dark" ? DarkTheme.Create() : LightTheme.Create());
        var styleSheet = BootstrapStyleSheetFactory.Create(theme);

        builder.AddStyleSheet(styleSheet);
        builder.UseFonts(fonts =>
        {
            fonts.AddResource("bootstrap-icons",
                typeof(BootstrapExtensions).Assembly,
                "Miko.Bootstrap.Resources.Fonts.bootstrap-icons.woff2");
        });
        return builder;
    }
}
