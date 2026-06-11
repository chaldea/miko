using UIKit;

namespace MikoMultiplatformApp.iOS;

public static class Application
{
    private static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
