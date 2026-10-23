using Miko.Hosting;
using Miko.Ionic;

namespace MikoAppSidemenu;

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
    /// <param name="configure">
    /// Optional hook for a platform head to add head-specific services before the
    /// context is built — e.g. the desktop head enabling DevTools. Kept as a callback
    /// so the shared app assembly does not reference desktop-only packages.
    /// </param>
    public static MikoAppContext CreateContext(Action<MikoAppBuilder>? configure = null)
    {
        var builder = MikoAppBuilder.CreateDefault();

        builder.UseTitle("MikoAppSidemenu");
        // Phone-portrait viewport showcases the Ionic mobile layout.
        builder.UseSize(390, 844);

        builder.AddIonic();
        builder.AddStyleSheet(GlobalStyles.Create());

        // Routes and the default layout are wired up by Miko.Razor.Compiler.
        builder.UseGeneratedRoutes();
        builder.UseDefaultLayout<MainLayout>();

        builder.EnableHotReload();

        configure?.Invoke(builder);

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
