using Foundation;
using UIKit;

namespace MikoMultiplatformApp.iOS;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
        => true;

    // The window is created per scene (SceneDelegate); Info.plist declares the scene manifest.
    public override UISceneConfiguration GetConfiguration(
        UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options)
        => new("Default Configuration", connectingSceneSession.Role)
        {
            DelegateType = typeof(SceneDelegate)
        };
}
