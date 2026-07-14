using IonicComponents;
using Microsoft.Extensions.Logging;
using Miko.DevTools;
using Miko.Hosting;
using Miko.Ionic;

namespace MikoApp1.Razor;

public static class RazorApp
{
    public static MikoAppContext CreateContext()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("Miko Razor Demo");
        builder.AddIonic();
        builder.AddDevTools();
        builder.AddStyleSheet(GlobalStyles.Create());
        builder.UseSize(1024, 768);
        builder.UseGeneratedRoutes();
        builder.UseDefaultLayout<MainLayout>();
        builder.EnableHotReload();
        builder.UseLogging(logging =>
        {
            logging.AddConsole().SetMinimumLevel(LogLevel.Trace);
        });
        builder.AddResourceAssembly(typeof(RazorApp).Assembly);
        return builder.Build();
    }
}
