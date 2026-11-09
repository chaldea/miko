using Miko.Windowing;
using Miko.Windowing.Native;
using MikoApp.Vnc.Desktop;

if (args.Length >= 2 && args[0] == "--verify")
    return VncVerification.Run(args[1]);

MikoApp.Vnc.App.CreateContext(builder => builder.UseDesktopNative()).RunDesktop();
return 0;
