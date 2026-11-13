using Miko.Windowing;
using Miko.Windowing.Native;
using Miko.Windowing.Video;
using MikoApp.Webcam.Desktop;

if (args.Length >= 2 && args[0] == "--verify")
    return WebcamVerification.Run(args[1]);
if (args.Length >= 2 && args[0] == "--verify-config")
    return WebcamVerification.Run(args[1], connect: false);

// Starts on the configuration page. Mock uses the HTTP fallback on Windows/Linux;
// Ezviz can use cloud relay there. macOS also decodes RTSP through VideoToolbox.
MikoApp.Webcam.App.CreateContext(builder =>
{
    builder.UseSystemVideo();
    builder.UseDesktopNative();
}).RunDesktop();
return 0;
