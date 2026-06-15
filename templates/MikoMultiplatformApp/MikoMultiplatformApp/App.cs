using Miko.DevTools;
using Miko.Hosting;
//#if (useIonic)
using Miko.Ionic;
//#endif

namespace MikoMultiplatformApp;

/// <summary>
/// Shared application configuration. Each platform startup project
/// (Desktop / Android / iOS) consumes the <see cref="MikoAppContext"/>
/// returned here and drives its own render loop.
/// </summary>
public static class App
{
    /// <summary>
    /// Builds an app context with the shared configuration.
    /// </summary>
    public static MikoAppContext CreateContext()
    {
        var builder = MikoAppBuilder.CreateDefault();

        builder.UseTitle("Miko Multiplatform App");
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

    /// <summary>
    /// Wires up hot reload. The hot-reload handler is generated (by
    /// Miko.Razor.Compiler) into this shared assembly because that is where
    /// EnableHotReload() is called, so it is initialized from here. Called by
    /// the desktop startup, which is where hot reload is supported.
    /// </summary>
    public static void InitializeHotReload(MikoAppContext context)
    {
        MikoHotReloadHandler.Initialize(context.GetHotReloadService());
    }
}
