namespace MikoApp1.Razor;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var app = RazorApp.Create();
        // Initialize hot reload handler
        MikoHotReloadHandler.Initialize(app.GetHotReloadService());
        app.Run();
    }
}
