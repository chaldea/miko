using Microsoft.Extensions.Logging;
using Miko.Bootstrap;
using Miko.DevTools;
using Miko.Hosting;

namespace MikoApp1.Razor;

public static class RazorApp
{
    public static MikoApp Create()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("Miko Razor Demo");
        builder.AddBootstrap();
        builder.AddDevTools();
        builder.AddStyleSheet(GlobalStyles.Create());
        builder.UseSize(1024, 768);
        builder.UseGeneratedRoutes();
        builder.UseDefaultLayout<MainLayout>();
        builder.UseLogging(logging =>
        {
            logging.AddConsole().SetMinimumLevel(LogLevel.Trace);
        });
        return builder.Build();
    }
}
