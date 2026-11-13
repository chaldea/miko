using Miko.Simulator;
using Miko.Simulator.Native;
using Miko.Windowing.Video;

namespace MikoApp.Webcam.Simulator;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        App.CreateContext(builder =>
        {
            builder.UseSystemVideo();
            builder.UseSimulatorNative();
        }).RunSimulator();
    }
}
