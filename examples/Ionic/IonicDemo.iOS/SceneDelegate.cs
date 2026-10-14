using Foundation;
using Miko.iOS;
using Miko.iOS.Native;
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
        // UseIosNative() also supplies the component-facing platform services, e.g. the
        // picker / reorder haptics (ISSUE-149); without it those stay silent.
        Window = new UIWindow(windowScene)
        {
            RootViewController = new MikoViewController(
                IonicDemo.App.CreateContext(builder => builder.UseIosNative()))
        };
        Window.MakeKeyAndVisible();
    }
}
