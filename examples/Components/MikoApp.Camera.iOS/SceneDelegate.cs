using Foundation;
using Miko.iOS;
using Miko.iOS.Camera;
using Miko.iOS.Native;
using UIKit;

namespace MikoApp.Camera.iOS;

[Register("SceneDelegate")]
public class SceneDelegate : UIWindowSceneDelegate
{
    public override UIWindow? Window { get; set; }

    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene)
            return;

        // UseIosCamera(): AVCaptureSession with zero-copy Metal preview frames. UseIosNative(): haptics
        // and the system back gesture.
        Window = new UIWindow(windowScene)
        {
            RootViewController = new MikoViewController(MikoApp.Camera.App.CreateContext(builder =>
            {
                builder.UseIosCamera();
                builder.UseIosNative();
            }))
        };
        Window.MakeKeyAndVisible();
    }
}
