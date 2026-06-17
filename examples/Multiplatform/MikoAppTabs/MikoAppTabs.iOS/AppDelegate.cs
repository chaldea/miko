using Foundation;
using Miko.iOS;
using UIKit;

namespace MikoAppTabs.iOS;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        Window = new UIWindow(UIScreen.MainScreen.Bounds);

        // Reuse the shared app configuration; Miko.iOS drives rendering and touch input.
        Window.RootViewController = new MikoViewController(MikoAppTabs.App.CreateContext());
        Window.MakeKeyAndVisible();

        return true;
    }
}
