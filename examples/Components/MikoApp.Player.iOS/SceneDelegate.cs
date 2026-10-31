using Foundation;
using Miko.iOS;
using Miko.iOS.Native;
using Miko.iOS.Video;
using UIKit;

namespace MikoApp.Player.iOS;

[Register("SceneDelegate")]
public class SceneDelegate : UIWindowSceneDelegate
{
    public override UIWindow? Window { get; set; }

    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene)
            return;

        // UseIosVideo(): AVPlayer with zero-copy Metal frames. UseIosNative(): fullscreen locks
        // landscape, keeps the screen on and hides the status bar; the long press ticks the haptics.
        Window = new UIWindow(windowScene)
        {
            RootViewController = new MikoViewController(MikoApp.Player.App.CreateContext(builder =>
            {
                builder.UseIosVideo();
                builder.UseIosNative();
            }))
        };
        Window.MakeKeyAndVisible();
    }
}
