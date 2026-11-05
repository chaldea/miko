using Foundation;
using Miko.iOS;
using Miko.iOS.Native;
using Miko.iOS.Video;
using UIKit;

namespace MikoApp.Webcam.iOS;

[Register("SceneDelegate")]
public class SceneDelegate : UIWindowSceneDelegate
{
    public override UIWindow? Window { get; set; }

    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene)
            return;

        // UseIosVideo(): VideoToolbox decodes the camera's RTSP stream into zero-copy Metal frames.
        // UseIosNative(): fullscreen locks landscape and hides the status bar; the disk ticks the haptics.
        // The settings come from the environment: SIMCTL_CHILD_EZVIZ_DEVICE=… xcrun simctl launch … on the simulator.
        Window = new UIWindow(windowScene)
        {
            RootViewController = new MikoViewController(MikoApp.Webcam.App.CreateContext(builder =>
            {
                builder.UseIosVideo();
                builder.UseIosNative();
            }))
        };
        Window.MakeKeyAndVisible();
    }
}
