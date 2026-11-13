using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Miko.Charts;
using Miko.Components;
using Miko.Components.Player;
using Miko.Docs.Demo.Pages;
using Miko.Hosting;
using Miko.Ionic;
using Miko.Platform;
using Miko.Platform.Video;
using Miko.Routing;

namespace Miko.Docs.Demo;

/// <summary>Shared configuration for the designer and its independently hosted component preview.</summary>
public static class App
{
    public static MikoAppContext CreateContext(Action<MikoAppBuilder>? configure = null)
    {
        var builder = CreateDefault("Miko · Component Playground", 1440, 960);
        builder.Services.AddSingleton<DemoState>();
        builder.UseRouter(ConfigureDesignerRoutes);
        builder.UseDefaultLayout<DemoLayout>();
        configure?.Invoke(builder);
        return builder.Build();
    }

    /// <summary>Creates the preview's own engine/DI container while sharing only explicit playground state.</summary>
    public static MikoAppContext CreatePreviewContext(DemoState state, PlatformInfo platform)
    {
        var builder = CreateDefault("Miko · Isolated Component Preview", (int)IsolatedDemoPreview.LogicalWidth, (int)IsolatedDemoPreview.LogicalHeight);
        builder.Services.AddSingleton(state);
        builder.Services.AddSingleton(platform);
        builder.Services.AddSingleton<IPlatformInfo>(platform);
        builder.Services.AddSingleton<IPlatformService>(platform);
        builder.UseRouter(ConfigureRoutes);
        builder.UseDefaultLayout<PreviewLayout>();
        return builder.Build();
    }

    /// <summary>Creates a lightweight independent engine for one generated designer region.</summary>
    public static MikoAppContext CreateRegionContext<[DynamicallyAccessedMembers(DemoDescriptor.ComponentActivation)] T>(
        DemoState state, PlatformInfo platform, int width, int height) where T : ComponentBase, new()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle($"Miko · {typeof(T).Name}");
        builder.AddResourceAssembly(typeof(App).Assembly);
        builder.UseSize(width, height);
        builder.UseFonts(fonts => fonts.AddResource("Inter", typeof(App).Assembly, "Miko.Docs.Demo.Assets.Inter-Latin.ttf"));
        builder.Services.AddSingleton(state);
        builder.Services.AddSingleton(platform);
        builder.Services.AddSingleton<IPlatformInfo>(platform);
        builder.Services.AddSingleton<IPlatformService>(platform);
        builder.UseRootComponent(() => new T().Build());
        return builder.Build();
    }

    private static MikoAppBuilder CreateDefault(string title, int width, int height)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle(title);
        builder.UseSize(width, height);
        builder.UseFonts(fonts => fonts.AddResource("Inter", typeof(App).Assembly, "Miko.Docs.Demo.Assets.Inter-Latin.ttf"));
        builder.AddResourceAssembly(typeof(App).Assembly);
        builder.Services.AddMikoCharts(options =>
        {
            options.FontFamily = "Inter";
            options.AnimationsSpeed = TimeSpan.Zero;
        });
        builder.Services.AddMikoPlayer();
        builder.Services.AddSingleton<IVideoBackend, DemoVideoBackend>();
        builder.AddIonic(options => options.Theme = new IonicTheme
        {
            Palette = new PaletteToken { Primary = "512bd4" },
        });
        return builder;
    }

    /// <summary>The preview shows one component per route; the route value picks it.</summary>
    private static void ConfigureRoutes(Router router)
    {
        router.MapRoute<ComponentExamplesPage>("/");
        router.MapRoute<ComponentExamplesPage>("/{id}");
    }

    /// <summary>
    /// The designer shell never renders the routed page: its layout *is* the shell, and the component is drawn
    /// by the isolated preview. A navigation still rebuilds the whole shell, and Miko restores scroll
    /// offsets by page identity (the component type plus its route values). A <c>/{id}</c> route would give every
    /// component its own identity, so picking an entry far down the menu came back with the menu scrolled to the
    /// top. One parameterless route per component keeps a single identity: the offset captured when leaving is
    /// the one replayed on the new shell. <c>/{id}</c> stays as the fallback for an unknown deep link.
    /// </summary>
    private static void ConfigureDesignerRoutes(Router router)
    {
        router.MapRoute<ComponentExamplesPage>("/");
        foreach (var descriptor in DemoCatalog.All)
            router.MapRoute<ComponentExamplesPage>($"/{descriptor.Id}");
        router.MapRoute<ComponentExamplesPage>("/{id}");
    }
}
