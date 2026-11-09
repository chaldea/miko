using Miko.Components.Vnc;
using Miko.Hosting;

namespace MikoApp.Vnc;

/// <summary>
/// Shared configuration of the VNC demo. Each head registers its native services through <c>configure</c>
/// and drives the render loop.
/// </summary>
/// <remarks>
/// The server comes from the environment, never from source: <c>VNC_HOST</c> (an address the connect form
/// accepts: <c>host</c>, <c>host:display</c>, <c>host::port</c>) and optionally <c>VNC_PASSWORD</c>. Without
/// <c>VNC_HOST</c> the viewer shows its connect form; without <c>VNC_PASSWORD</c> it asks when the server
/// needs one.
/// </remarks>
public static class App
{
    /// <param name="page">Receives the demo page when it is created (the desktop head's offscreen verification).</param>
    public static MikoAppContext CreateContext(Action<MikoAppBuilder>? configure = null, Action<VncDemo>? page = null)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("MikoVnc").UseSize(1280, 800);
        var (host, port) = ServerFromEnvironment();
        string? password = Environment.GetEnvironmentVariable("VNC_PASSWORD");
        builder.UseRootComponent(() =>
        {
            var demo = new VncDemo { Host = host, Port = port, Password = string.IsNullOrEmpty(password) ? null : password };
            page?.Invoke(demo);
            return demo.Build();
        });
        configure?.Invoke(builder);
        return builder.Build();
    }

    /// <summary>The server named by <c>VNC_HOST</c>, or (null, 5900).</summary>
    public static (string? Host, int Port) ServerFromEnvironment()
    {
        var address = Environment.GetEnvironmentVariable("VNC_HOST");
        return VncAddress.TryParse(address, out var host, out var port) ? (host, port) : (null, VncAddress.DefaultPort);
    }
}
