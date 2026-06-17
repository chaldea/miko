using Miko.DevTools;
using Miko.Hosting;

namespace MikoAppBlank;

public static class App
{
    public static MikoAppContext CreateContext()
    {
        var builder = MikoAppBuilder.CreateDefault();

        builder.UseTitle("MikoAppBlank");
        builder.UseSize(1024, 768);

        builder.AddDevTools();
        builder.AddStyleSheet(GlobalStyles.Create());

        // Routes and the default layout are wired up by Miko.Razor.Compiler.
        builder.UseGeneratedRoutes();
        builder.UseDefaultLayout<MainLayout>();

        builder.EnableHotReload();

        return builder.Build();
    }
}
