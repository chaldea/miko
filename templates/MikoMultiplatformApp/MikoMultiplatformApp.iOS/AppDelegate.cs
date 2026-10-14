using Foundation;
using Miko.iOS;
using Miko.iOS.Native;
using UIKit;

namespace MikoMultiplatformApp.iOS;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        Window = new UIWindow(UIScreen.MainScreen.Bounds);

        // Reuse the shared app configuration; Miko.iOS drives rendering and touch input.
        // UseIosNative() supplies the Miko.Native capabilities and the platform services
        // components use, such as picker / reorder haptics.
        Window.RootViewController = new MikoViewController(
            MikoMultiplatformApp.App.CreateContext(builder => builder.UseIosNative()));
        Window.MakeKeyAndVisible();

        return true;
    }
}
