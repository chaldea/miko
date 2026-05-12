using Microsoft.Extensions.Logging;
using Miko.Hosting;

namespace MikoApp1.Razor;

public static class RazorApp
{
    public static MikoApp Create()
    {
        var builder = MikoApp.CreateBuilder();
        builder.UseTitle("Miko Razor Demo");
        builder.UseStyleSheets([
            BootstrapStyles.CreateBootstrapStyleSheet(),
            MainLayout.CreateLayoutStyleSheet()
        ]);
        builder.UseSize(1024, 768);
        builder.UseFonts(fonts =>
        {
            fonts.AddResource("bootstrap-icons",
                typeof(RazorApp).Assembly,
                "MikoApp1.Razor.Resources.Fonts.bootstrap-icons.woff2");
        });
        builder.UseRouter(typeof(RazorApp).Assembly);
        builder.UseDefaultLayout(typeof(MainLayout));
        builder.UseLogging(logging =>
        {
            logging.AddConsole().SetMinimumLevel(LogLevel.Trace);
        });
        return builder.Build();
    }
}
