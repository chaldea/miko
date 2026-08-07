using IonicDemo.Services;
using Microsoft.Extensions.DependencyInjection;
using Miko.DevTools;
using Miko.Hosting;
using Miko.Ionic;

namespace IonicDemo;

public static class App
{
    public static MikoAppContext CreateContext(Action<MikoAppBuilder>? configure = null)
    {
        var builder = MikoAppBuilder.CreateDefault();

        builder.UseTitle("MikoAppTabs");
        // Phone-portrait viewport showcases the Ionic mobile layout.
        builder.UseSize(390, 844);

        builder.AddDevTools();
        builder.AddIonic();
        builder.AddStyleSheet(GlobalStyles.Create());

        // Routes and the default layout are wired up by Miko.Razor.Compiler.
        builder.UseGeneratedRoutes();
        builder.UseDefaultLayout<MainLayout>();

        builder.EnableHotReload();

        builder.Services.AddScoped<ComponentService>();
        builder.AddResourceAssembly(typeof(App).Assembly);
        configure?.Invoke(builder);

        return builder.Build();
    }

    public static void InitializeHotReload(MikoAppContext context)
    {
        MikoHotReloadHandler.Initialize(context.GetHotReloadService());
    }
}
