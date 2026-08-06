using Miko.McpServer;
using Miko.Simulator;

namespace IonicDemo;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var context = App.CreateContext(builder => builder.AddMikoMcpServer());
        App.InitializeHotReload(context);
        context.RunSimulator();
    }
}
