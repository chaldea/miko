using Miko.Windowing;
using Miko.Windowing.Native;
using Miko.Windowing.Video;
using MikoApp.Player.Desktop;

if (args.Contains("--verify"))
{
    return PlayerVerification.Run(args.SkipWhile(a => a != "--verify").Skip(1).FirstOrDefault());
}

// Media Foundation / AVFoundation / GStreamer through UseSystemVideo(); Escape leaves fullscreen
// through the back-button chain.
MikoApp.Player.App.CreateContext(builder =>
{
    builder.UseSystemVideo();
    builder.UseDesktopNative();
}).RunDesktop();
return 0;
