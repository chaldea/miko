using UIKit;

namespace MikoApp1.iOS;

public static class Application
{
    private static void Main(string[] args)
    {
        // 入口：将控制权交给 UIKit，由 AppDelegate 完成启动。
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
