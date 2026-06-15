using Miko.Windowing;

namespace MikoApp2.Ionic;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var context = IonicApp.CreateContext();
        // Initialize hot reload handler
        MikoHotReloadHandler.Initialize(context.GetHotReloadService());
        context.RunDesktop();
    }
}
