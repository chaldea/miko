using Foundation;
using Miko.iOS;
using Miko.iOS.Native;
using UIKit;

namespace MikoApp.Reader.iOS;

[Register("SceneDelegate")]
public class SceneDelegate : UIWindowSceneDelegate
{
    public override UIWindow? Window { get; set; }

    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene)
            return;

        Window = new UIWindow(windowScene)
        {
            RootViewController = new MikoViewController(MikoApp.Reader.App.CreateContext(builder => builder.UseIosNative()))
        };
        Window.MakeKeyAndVisible();
    }
}
