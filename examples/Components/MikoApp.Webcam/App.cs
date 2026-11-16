using Microsoft.Extensions.DependencyInjection;
using Miko.Components.Webcam;
using Miko.Hosting;
using Miko.Ionic;
using Miko.Routing;

namespace MikoApp.Webcam;

/// <summary>Shared webcam configuration UI. Each head adds its video backend and native services.</summary>
public static class App
{
    /// <param name="device">Optional Ezviz form preset; it never starts a connection automatically.</param>
    /// <param name="page">Receives each configuration page for offscreen verification.</param>
    /// <param name="monitor">Receives each monitor page for offscreen verification.</param>
    public static MikoAppContext CreateContext(Action<MikoAppBuilder>? configure = null, WebcamDevice? device = null,
        Action<WebcamDemo>? page = null, Action<WebcamMonitor>? monitor = null)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("MikoWebcam").UseSize(420, 860);
        builder.AddIonic();
        builder.Services.AddMikoRtsp();
        builder.Services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(15) });
        builder.Services.AddSingleton(WebcamConnectionSettings.FromEnvironment(device));
        if (page != null) builder.Services.AddSingleton(page);
        if (monitor != null) builder.Services.AddSingleton(monitor);
        // A provider belongs to a mounted monitor. Reconnecting uses a fresh snapshot;
        // pending operations on a discarded monitor still target its original provider.
        builder.Services.AddTransient<IWebcamProvider>(sp =>
            sp.GetRequiredService<NavigationManager>().GetState<WebcamConnection>()?.Provider
            ?? throw new InvalidOperationException("Navigate with a connection before mounting the webcam."));
        builder.UseGeneratedRoutes();
        configure?.Invoke(builder);
        return builder.Build();
    }
}
