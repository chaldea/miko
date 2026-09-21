// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using UIKit;

namespace Anime.iOS;

public static class Application
{
    private static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
