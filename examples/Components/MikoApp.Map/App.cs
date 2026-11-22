using Miko.Components.Map;
using Miko.Hosting;

namespace MikoApp.Map;

public static class App
{
    public static MikoAppContext CreateContext(Action<MikoAppBuilder>? configure = null,
        string? mockBaseUrl = null, string? amapWebKey = null)
    {
        var builder = MikoAppBuilder.CreateDefault().UseTitle("Miko Map · Beijing").UseSize(900, 640);
        amapWebKey ??= Environment.GetEnvironmentVariable("AMAP_WEB_KEY");
        if (string.IsNullOrWhiteSpace(amapWebKey))
            builder.Services.AddMikoMap(mockBaseUrl ?? Environment.GetEnvironmentVariable("MIKO_MOCK_HTTP") ?? MikoProvider.DefaultBaseUrl);
        else
            builder.Services.AddAMap(amapWebKey);

        builder.UseRootComponent(() => new MapDemo().Build());
        configure?.Invoke(builder);
        return builder.Build();
    }
}
