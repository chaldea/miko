using Miko.Simulator;

namespace IonicDemo;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var context = App.CreateContext();
        App.InitializeHotReload(context);
        context.RunSimulator();
    }
}
