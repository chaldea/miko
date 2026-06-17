using Miko.Windowing;
using MikoApp.AsyncDemo;

namespace MikoApp.AsyncDemo.Desktop;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var context = App.CreateContext();
        App.InitializeHotReload(context);
        context.RunDesktop();
    }
}
