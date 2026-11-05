using Miko.Components.Webcam;
using Miko.Components.Webcam.Ezviz;
using Miko.Hosting;

namespace MikoApp.Webcam;

/// <summary>
/// Shared configuration of the webcam demo. Each head registers its platform video backend and native
/// services through <c>configure</c> and drives the render loop.
/// </summary>
/// <remarks>
/// The Ezviz credentials and the device come from environment variables, never from source:
/// <c>EZVIZ_APP_KEY</c> + <c>EZVIZ_APP_SECRET</c> (or <c>EZVIZ_ACCESS_TOKEN</c>), and
/// <c>EZVIZ_DEVICE</c> — an EZOpen address, <c>ezopen://VERIFYCODE@open.ys7.com/SERIAL/1.live</c>
/// (the verification code is on the device label and is what LAN streaming needs) — optionally
/// <c>EZVIZ_DEVICE_IP</c> for a camera the cloud does not know the address of.
/// </remarks>
public static class App
{
    /// <param name="page">Receives the demo page when it is created (the desktop head's offscreen verification).</param>
    public static MikoAppContext CreateContext(Action<MikoAppBuilder>? configure = null, WebcamDevice? device = null, Action<WebcamDemo>? page = null)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("MikoWebcam").UseSize(420, 860);
        builder.Services.AddMikoWebcam(options =>
        {
            options.AppKey = Environment.GetEnvironmentVariable("EZVIZ_APP_KEY");
            options.AppSecret = Environment.GetEnvironmentVariable("EZVIZ_APP_SECRET");
            options.AccessToken = Environment.GetEnvironmentVariable("EZVIZ_ACCESS_TOKEN");
            options.EnableCloudRelay = Environment.GetEnvironmentVariable("EZVIZ_CLOUD_RELAY") == "1";
        });
        device ??= DeviceFromEnvironment();
        builder.UseRootComponent(() =>
        {
            var demo = new WebcamDemo { Device = device };
            page?.Invoke(demo);
            return demo.Build();
        });
        configure?.Invoke(builder);
        return builder.Build();
    }

    /// <summary>The device named by <c>EZVIZ_DEVICE</c> / <c>EZVIZ_DEVICE_IP</c>, or null.</summary>
    public static WebcamDevice? DeviceFromEnvironment()
    {
        var address = Environment.GetEnvironmentVariable("EZVIZ_DEVICE");
        if (string.IsNullOrWhiteSpace(address)) return null;
        var device = WebcamDevice.ParseEzopen(address.Trim(), out _);
        var ip = Environment.GetEnvironmentVariable("EZVIZ_DEVICE_IP");
        return string.IsNullOrWhiteSpace(ip) ? device : device with { LocalAddress = ip.Trim() };
    }
}
