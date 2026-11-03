using Miko.Hosting;

namespace MikoApp.Camera;

/// <summary>
/// Shared configuration of the camera demos. Each head (Desktop / Android / iOS) registers its
/// platform camera backend through <c>configure</c> and drives the render loop.
/// </summary>
public static class App
{
    /// <param name="configure">
    /// The head's registrations: its camera backend (<c>UseSystemCamera()</c> / <c>UseAndroidCamera()</c> /
    /// <c>UseIosCamera()</c>) and native services (<c>UseDesktopNative()</c> / <c>UseAndroidNative()</c> /
    /// <c>UseIosNative()</c>; the gallery button of the QR scanner uses the latter).
    /// </param>
    public static MikoAppContext CreateContext(Action<MikoAppBuilder>? configure = null)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("Miko Camera").UseSize(420, 820);
        builder.UseRootComponent(() => new CameraDemo().Build());
        configure?.Invoke(builder);
        return builder.Build();
    }
}
