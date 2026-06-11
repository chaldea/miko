using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miko.DevTools.Logging;
using Miko.Hosting;
using Miko.Platform;
using Miko.Rendering;

namespace Miko.DevTools;

public static class DevToolsExtensions
{
    public static MikoAppBuilder AddDevTools(this MikoAppBuilder builder, Action<DevToolsOptions>? configure = null)
    {
        var options = new DevToolsOptions();
        configure?.Invoke(options);

        var bridge = new DevToolsBridge(options);
        var loggerProvider = new DevToolsLoggerProvider(bridge.LogBuffer);

        builder.Services.AddSingleton(bridge);
        builder.Services.AddSingleton<ILoggerProvider>(loggerProvider);

        builder.AddGlobalKeyHandler(key =>
        {
            if (key == MikoKey.F12)
            {
                bridge.ToggleDevTools();
                return true;
            }
            return false;
        });

        builder.Services.Configure<MikoAppOptions>(o =>
        {
            o.PostInitHooks.Add(sp =>
            {
                var engine = sp.GetRequiredService<Core.MikoEngine>();
                var renderEngine = sp.GetRequiredService<RenderEngine>();
                bridge.Initialize(engine, renderEngine);
            });
        });

        return builder;
    }
}
