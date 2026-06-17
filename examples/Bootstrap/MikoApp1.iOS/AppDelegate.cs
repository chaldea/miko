using Foundation;
using Miko.iOS;
using UIKit;

namespace MikoApp1.iOS;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        Window = new UIWindow(UIScreen.MainScreen.Bounds);

        // 复用共享 UI 工程的应用配置，由 Miko.iOS 承载渲染与触摸输入。
        Window.RootViewController = new MikoViewController(MikoApp1.App.CreateContext());
        Window.MakeKeyAndVisible();

        return true;
    }
}
