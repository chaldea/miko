using Miko.McpServer;
using Miko.Simulator;
using MikoAppTabs;

namespace MikoAppTabs.Simulator;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Enable the MCP debug server (dev builds only). It starts a local HTTP
        // endpoint (default http://localhost:5800) once the simulator window is
        // up, exposing DOM/style/screenshot/device tools to MCP clients such as
        // Claude Code. See docs/mcp-server.md.
        var context = App.CreateContext(builder => builder.AddMikoMcpServer());

        // Hot reload is wired up inside the shared app assembly, same as the desktop head.
        App.InitializeHotReload(context);

        // Run the app inside a device simulator window: the app renders into an
        // independent device-sized canvas on the left, with a Miko-rendered
        // settings panel (device / orientation / safe area) on the right.
        context.RunSimulator();
    }
}
