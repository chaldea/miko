using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;

namespace MikoApp.Localization;

/// <summary>
/// Shared application configuration, consumed by the platform startup projects.
/// </summary>
public static class App
{
    /// <param name="culture">
    /// Starting language (e.g. <c>"ar-EG"</c>); defaults to the OS language when the app offers it.
    /// </param>
    public static MikoAppContext CreateContext(string? culture = null)
    {
        var builder = MikoAppBuilder.CreateDefault();

        builder.UseTitle("Miko Localization");
        builder.UseSize(960, 720);

        // IStringLocalizer<T> reads Resources/<T>.<culture>.resx, as in ASP.NET Core.
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        builder.Services.AddSingleton(new AppLanguage(culture));

        builder.AddStyleSheet(GlobalStyles.Create());
        builder.UseGeneratedRoutes();
        builder.UseDefaultLayout<MainLayout>();
        builder.EnableHotReload();

        return builder.Build();
    }

    public static void InitializeHotReload(MikoAppContext context)
    {
        MikoHotReloadHandler.Initialize(context.GetHotReloadService());
    }
}
