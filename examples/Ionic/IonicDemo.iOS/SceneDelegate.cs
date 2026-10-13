using Foundation;
using Miko.iOS;
using UIKit;

namespace IonicDemo.iOS;

[Register("SceneDelegate")]
public class SceneDelegate : UIWindowSceneDelegate
{
    public override UIWindow? Window { get; set; }

    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene)
            return;

        // iOS 27 requires each window to belong to a scene.
        Window = new UIWindow(windowScene)
        {
            RootViewController = new MikoViewController(IonicDemo.App.CreateContext())
        };
        Window.MakeKeyAndVisible();
    }
}
