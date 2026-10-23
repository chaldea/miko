using Foundation;
using Miko.iOS;
using UIKit;

namespace MikoAppTabs.iOS;

[Register("SceneDelegate")]
public class SceneDelegate : UIWindowSceneDelegate
{
    public override UIWindow? Window { get; set; }

    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene)
            return;

        // Newer iOS versions require every window to belong to a scene.
        // Reuse the shared app configuration; Miko.iOS drives rendering and touch input.
        Window = new UIWindow(windowScene)
        {
            RootViewController = new MikoViewController(
                MikoAppTabs.App.CreateContext())
        };
        Window.MakeKeyAndVisible();
    }
}
