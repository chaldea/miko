// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Foundation;
using UIKit;

namespace Anime.iOS;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
        => true;

    public override UISceneConfiguration GetConfiguration(
        UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options)
        => new("Default Configuration", connectingSceneSession.Role)
        {
            DelegateType = typeof(SceneDelegate)
        };
}
