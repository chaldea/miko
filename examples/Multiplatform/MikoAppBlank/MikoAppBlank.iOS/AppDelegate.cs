using Foundation;
using Miko.iOS;
using Miko.iOS.Native;
using UIKit;

namespace MikoAppBlank.iOS;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        Window = new UIWindow(UIScreen.MainScreen.Bounds);

        // Reuse the shared app configuration; Miko.iOS drives rendering and touch input.
        // UseIosNative() supplies the iOS implementations of the Miko.Native capability
        // interfaces; MikoViewController attaches itself to them in ViewDidLoad.
        Window.RootViewController = new MikoViewController(
            MikoAppBlank.App.CreateContext(builder => builder.UseIosNative()));
        Window.MakeKeyAndVisible();

        return true;
    }
}
