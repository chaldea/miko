using Miko.Windowing;

namespace MikoApp1.Razor;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var context = RazorApp.CreateContext();
        // Initialize hot reload handler
        MikoHotReloadHandler.Initialize(context.GetHotReloadService());
        context.RunDesktop();
    }
}
