using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miko.Bootstrap;
using Miko.DevTools;
using Miko.Hosting;

namespace MikoApp.AsyncDemo;

public static class App
{
    public static MikoAppContext CreateContext()
    {
        var builder = MikoAppBuilder.CreateDefault();

        builder.UseTitle("Miko Async Demo");
        builder.UseSize(1024, 768);

        // Register HttpClient for API calls
        builder.Services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000")
        });

        builder.AddDevTools();
        builder.AddBootstrap();
        builder.AddStyleSheet(GlobalStyles.Create());

        builder.UseGeneratedRoutes();
        builder.UseDefaultLayout<MainLayout>();

        builder.EnableHotReload();
		builder.UseLogging(logging =>
        {
            logging.AddConsole().SetMinimumLevel(LogLevel.Trace);
        });
        return builder.Build();
    }

    public static void InitializeHotReload(MikoAppContext context)
    {
        MikoHotReloadHandler.Initialize(context.GetHotReloadService());
    }
}
