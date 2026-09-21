// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.DevTools;
using Miko.Hosting;
using Miko.Ionic;
using Microsoft.Extensions.DependencyInjection;
using Miko.Components.Player;
using Anime.Services;

namespace Anime;

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
    /// context is built — e.g. the simulator head enabling the MCP debug server via
    /// <c>builder.AddMikoMcpServer()</c>. Kept as a callback so the shared app assembly
    /// does not reference head-only packages (Miko.McpServer / Miko.Simulator).
    /// </param>
    public static MikoAppContext CreateContext(Action<MikoAppBuilder>? configure = null)
    {
        var builder = MikoAppBuilder.CreateDefault();

        builder.UseTitle("Anime");
        // Phone-portrait viewport showcases the Ionic mobile layout.
        builder.UseSize(390, 844);

        builder.AddDevTools();
        builder.AddIonic(options => options.Theme = AppTheme.Create());
        builder.Services.AddMikoPlayer();
        builder.Services.AddSingleton<MockAnimeStore>();
        builder.Services.AddSingleton<IAnimeCatalog>(s => s.GetRequiredService<MockAnimeStore>());
        builder.Services.AddSingleton<IUserLibrary>(s => s.GetRequiredService<MockAnimeStore>());
        builder.Services.AddSingleton<ICommunityService>(s => s.GetRequiredService<MockAnimeStore>());
        builder.Services.AddSingleton<IRewardsService>(s => s.GetRequiredService<MockAnimeStore>());
        builder.Services.AddSingleton<IMediaAssets, EmbeddedMediaAssets>();
        builder.Services.AddSingleton<AnimeNavigator>();
        builder.AddResourceAssembly(typeof(App).Assembly);
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
