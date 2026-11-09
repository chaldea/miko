using Foundation;
using Miko.iOS;
using Miko.iOS.Native;
using UIKit;

namespace MikoApp.Vnc.iOS;

[Register("SceneDelegate")]
public class SceneDelegate : UIWindowSceneDelegate
{
    public override UIWindow? Window { get; set; }

    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene)
            return;

        // UseIosNative(): fullscreen locks landscape and hides the status bar; a held touch ticks the haptics.
        // The settings come from the environment: SIMCTL_CHILD_VNC_HOST=… xcrun simctl launch … on the simulator.
        Window = new UIWindow(windowScene)
        {
            RootViewController = new MikoViewController(MikoApp.Vnc.App.CreateContext(builder => builder.UseIosNative()))
        };
        Window.MakeKeyAndVisible();
    }
}
