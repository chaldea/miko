using Miko.DevTools;
using Miko.Hosting;
using Miko.Ionic;

namespace MikoAppSidemenu;

public static class App
{
    public static MikoAppContext CreateContext()
    {
        var builder = MikoAppBuilder.CreateDefault();

        builder.UseTitle("MikoAppSidemenu");
        // Phone-portrait viewport showcases the Ionic mobile layout.
        builder.UseSize(390, 844);

        builder.AddDevTools();
        builder.AddIonic();
        builder.AddStyleSheet(GlobalStyles.Create());

        // Routes and the default layout are wired up by Miko.Razor.Compiler.
        builder.UseGeneratedRoutes();
        builder.UseDefaultLayout<MainLayout>();

        builder.EnableHotReload();

        return builder.Build();
    }
}
