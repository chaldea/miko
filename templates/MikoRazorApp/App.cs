using Miko.DevTools;
using Miko.Hosting;
//#if (useIonic)
using Miko.Ionic;
//#endif

namespace MikoRazorApp;

public static class App
{
    public static MikoAppContext CreateContext()
    {
        var builder = MikoAppBuilder.CreateDefault();

        builder.UseTitle("Miko Razor App");
        //#if (useIonic)
        // Phone-portrait viewport showcases the Ionic mobile layout.
        builder.UseSize(390, 844);
        //#else
        builder.UseSize(1024, 768);
        //#endif

        builder.AddDevTools();
        //#if (useIonic)
        builder.AddIonic();
        //#endif
        builder.AddStyleSheet(GlobalStyles.Create());

        // Routes and the default layout are wired up by Miko.Razor.Compiler.
        builder.UseGeneratedRoutes();
        builder.UseDefaultLayout<MainLayout>();

        builder.EnableHotReload();

        return builder.Build();
    }
}
