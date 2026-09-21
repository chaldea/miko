// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Foundation;
using Miko.iOS;
using Miko.iOS.Video;
using UIKit;

namespace Anime.iOS;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        Window = new UIWindow(UIScreen.MainScreen.Bounds);

        // Reuse the shared app configuration; Miko.iOS drives rendering and touch input.
        Window.RootViewController = new MikoViewController(Anime.App.CreateContext(builder => builder.UseIosVideo()));
        Window.MakeKeyAndVisible();

        return true;
    }
}
