using Miko.Windowing;

namespace MikoApp1.Razor;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        RazorApp.CreateContext().RunDesktop();
    }
}
