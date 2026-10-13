// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Foundation;
using Miko.iOS;
using Miko.iOS.Video;
using UIKit;

namespace Anime.iOS;

[Register("SceneDelegate")]
public class SceneDelegate : UIWindowSceneDelegate
{
    private IosFrameProbe? _frameProbe;

    public override UIWindow? Window { get; set; }

    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene)
            return;

        // iOS 27 requires each window to belong to a scene.
        var context = Anime.App.CreateContext(builder => builder.UseIosVideo());
        var controller = new MikoViewController(context);
        Window = new UIWindow(windowScene)
        {
            RootViewController = controller
        };
        Window.MakeKeyAndVisible();
        _frameProbe = IosFrameProbe.AttachIfRequested(context, controller);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _frameProbe?.Dispose();
        base.Dispose(disposing);
    }
}
