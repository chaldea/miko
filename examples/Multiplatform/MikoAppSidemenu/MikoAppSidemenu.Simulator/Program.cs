using Miko.Simulator;
using MikoAppSidemenu;

namespace MikoAppSidemenu.Simulator;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var context = App.CreateContext();

        // Hot reload is wired up inside the shared app assembly, same as the desktop head.
        App.InitializeHotReload(context);

        // Run the app inside a device simulator window: the app renders into an
        // independent device-sized canvas on the left, with a Miko-rendered
        // settings panel (device / orientation / safe area) on the right.
        context.RunSimulator();
    }
}
