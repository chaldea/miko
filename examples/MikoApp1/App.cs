using Miko.Examples.Bootstrap.Examples;
using Miko.Hosting;
using Microsoft.Extensions.Logging;

namespace MikoApp1;

public static class App
{
    public static MikoApp Create()
    {
        var builder = MikoApp.CreateBuilder();
        builder.UseTitle("Miko Demo App");
        builder.UseSize(1024, 768);
        builder.UseStyleSheets([
            Miko.Examples.Bootstrap.BootstrapStyles.CreateBootstrapStyleSheet(),
            MainLayout.CreateLayoutStyleSheet()
        ]);
        builder.UseFonts(fonts =>
        {
            fonts.AddResource("bootstrap-icons",
                typeof(App).Assembly,
                "MikoApp1.Resources.Fonts.bootstrap-icons.woff2");
        });
        builder.UseRouter(typeof(ButtonExample).Assembly);
        builder.UseDefaultLayout(typeof(MainLayout));
        builder.UseLogging(logging => logging.AddConsole());
        return builder.Build();
    }
}
