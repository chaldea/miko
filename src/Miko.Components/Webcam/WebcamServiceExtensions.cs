using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Miko.Components.Webcam.Ezviz;
using Miko.Components.Webcam.Streaming;
using Miko.Platform.Video;

namespace Miko.Components.Webcam;

public static class WebcamServiceExtensions
{
    /// <summary>
    /// Registers <see cref="MikoWebcam"/>'s services: the Ezviz provider configured by
    /// <paramref name="configure"/> as <see cref="IWebcamProvider"/> (unless one is registered already —
    /// register your own provider to replace it, before or after this call) and <c>rtsp://</c> playback
    /// (<see cref="AddMikoRtsp"/>).
    /// </summary>
    public static IServiceCollection AddMikoWebcam(this IServiceCollection services, Action<EzvizOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new EzvizOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);
        services.TryAddSingleton<IWebcamProvider>(sp =>
            new EzvizWebcamProvider(sp.GetRequiredService<EzvizOptions>(), null, sp.GetService<ILogger<EzvizWebcamProvider>>()));
        return services.AddMikoRtsp();
    }

    /// <summary>
    /// Lets <c>&lt;video src="rtsp://user:password@host/path"&gt;</c> play: H.264 / H.265 over RTSP
    /// (TCP interleaved), decoded by the platform's video backend. Needs a backend with a stream
    /// decoder — macOS, iOS and Android.
    /// </summary>
    public static IServiceCollection AddMikoRtsp(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEncodedVideoSourceFactory, RtspVideoSourceFactory>());
        return services;
    }
}
