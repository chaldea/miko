using Microsoft.Extensions.Logging;
using Miko.Hosting;
using MikoApp1.Components;

namespace MikoApp1;

public static class MikoProgram
{
    public static MikoApp CreateMikoApp()
    {
        var builder = MikoApp.CreateBuilder();
        builder.UseTitle("Miko Demo App");
        builder.UseSize(1024, 768);
        builder.UseRootComponent(() => new App().Build());
        builder.UseLogging(logging => logging.AddConsole());
        return builder.Build();
    }
}
