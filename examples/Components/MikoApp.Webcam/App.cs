using Microsoft.Extensions.DependencyInjection;
using Miko.Components.Webcam;
using Miko.Hosting;
using Miko.Ionic;

namespace MikoApp.Webcam;

/// <summary>Shared webcam configuration UI. Each head adds its video backend and native services.</summary>
public static class App
{
    /// <param name="device">Optional Ezviz form preset; it never starts a connection automatically.</param>
    /// <param name="page">Receives the page for the desktop head's offscreen verification.</param>
    public static MikoAppContext CreateContext(Action<MikoAppBuilder>? configure = null, WebcamDevice? device = null, Action<WebcamDemo>? page = null)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("MikoWebcam").UseSize(420, 860);
        builder.AddIonic();
        builder.Services.AddMikoRtsp();
        builder.Services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(15) });
        builder.Services.AddSingleton<WebcamDemoSession>();
        // A provider belongs to a mounted monitor. Reconnecting uses a fresh snapshot;
        // pending operations on a discarded monitor still target its original provider.
        builder.Services.AddTransient<IWebcamProvider>(sp => sp.GetRequiredService<WebcamDemoSession>().RequireProvider());
        var settings = WebcamConnectionSettings.FromEnvironment(device);
        builder.UseRootComponent(() =>
        {
            var demo = new WebcamDemo { Settings = settings };
            page?.Invoke(demo);
            return demo.Build();
        });
        configure?.Invoke(builder);
        return builder.Build();
    }
}
