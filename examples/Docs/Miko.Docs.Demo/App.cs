using Miko.Hosting;
using Miko.Ionic;
using Miko.Docs.Demo.Pages;

namespace Miko.Docs.Demo;

/// <summary>Shared configuration for the documentation gallery design prototype.</summary>
public static class App
{
    public static MikoAppContext CreateContext(Action<MikoAppBuilder>? configure = null)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("Miko · Button Gallery");
        builder.UseSize(1280, 900);
        builder.UseFonts(fonts => fonts.AddResource("Inter", typeof(App).Assembly, "Miko.Docs.Demo.Assets.Inter-Latin.ttf"));
        builder.AddIonic(options => options.Theme = new IonicTheme
        {
            Palette = new PaletteToken { Primary = "5267e8" },
        });
        builder.UseRouter(router =>
        {
            router.MapRoute<ButtonExamplesPage>("/");
            router.MapRoute<ButtonExamplesPage>("/button");
        });
        builder.UseDefaultLayout<DemoLayout>();

        configure?.Invoke(builder);
        return builder.Build();
    }
}
