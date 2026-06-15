using Microsoft.Extensions.Logging;
using Miko.DevTools;
using Miko.Hosting;
using Miko.Ionic;

namespace MikoApp1.Ionic;

public static class IonicApp
{
    public static MikoAppContext CreateContext()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("Miko Ionic Demo");
        builder.AddDevTools();
        builder.AddIonic();
        builder.AddStyleSheet(GlobalStyles.Create());
        // Portrait, phone-like viewport to showcase the bottom tab bar layout.
        builder.UseSize(390, 844);
        builder.UseGeneratedRoutes();
        builder.UseDefaultLayout<MainLayout>();
        builder.EnableHotReload();
        builder.UseLogging(logging =>
        {
            logging.AddConsole().SetMinimumLevel(LogLevel.Trace);
        });
        return builder.Build();
    }
}
