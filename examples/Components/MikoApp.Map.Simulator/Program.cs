using Miko.Simulator;
using Miko.Simulator.Native;

namespace MikoApp.Map.Simulator;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        using var app = App.CreateContext(builder => builder.UseSimulatorNative());
        app.RunSimulator();
    }
}
