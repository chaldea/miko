//-:cnd:noEmit
#if DEBUG
using Miko.DevTools;
#endif
//+:cnd:noEmit
using Miko.Hosting;
//#if (useFull)
using Microsoft.Extensions.DependencyInjection;
//#endif
//#if (useIonic)
using Miko.Ionic;
//#endif
//#if (useFull)
using MikoRazorApp.Services;
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

//-:cnd:noEmit
#if DEBUG
        // F12 opens the DevTools inspector. Debug builds only: Release builds do not reference it.
        builder.AddDevTools();
#endif
//+:cnd:noEmit
        //#if (useIonic)
        builder.AddIonic();
        //#endif
        builder.AddStyleSheet(GlobalStyles.Create());
        //#if (useFull)
        builder.Services.AddScoped<ComponentService>();
        builder.AddResourceAssembly(typeof(App).Assembly);
        //#endif

        // Routes and the default layout are wired up by Miko.Razor.Compiler.
        builder.UseGeneratedRoutes();
        builder.UseDefaultLayout<MainLayout>();

        builder.EnableHotReload();

        return builder.Build();
    }
}
