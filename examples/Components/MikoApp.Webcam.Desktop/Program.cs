using Miko.Windowing;
using Miko.Windowing.Native;
using Miko.Windowing.Video;
using MikoApp.Webcam.Desktop;

if (args.Length >= 2 && args[0] == "--verify")
    return WebcamVerification.Run(args[1]);

// RTSP streams from the camera decode with VideoToolbox on macOS (UseSystemVideo); Windows and Linux
// have no stream decoder yet and show the backend's error.
MikoApp.Webcam.App.CreateContext(builder =>
{
    builder.UseSystemVideo();
    builder.UseDesktopNative();
}).RunDesktop();
return 0;
