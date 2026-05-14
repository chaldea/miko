using Microsoft.Extensions.Logging;
using Miko.Bootstrap;
using Miko.Hosting;

namespace MikoApp1.Razor;

public static class RazorApp
{
    public static MikoApp Create()
    {
        var builder = MikoApp.CreateBuilder();
        builder.UseTitle("Miko Razor Demo");
        builder.AddBootstrap();
        builder.AddStyleSheet(GlobalStyles.Create());
        builder.UseSize(1024, 768);
        builder.UseRouter(typeof(RazorApp).Assembly);
        builder.UseDefaultLayout(typeof(MainLayout));
        builder.UseLogging(logging =>
        {
            logging.AddConsole().SetMinimumLevel(LogLevel.Trace);
        });
        return builder.Build();
    }
}
