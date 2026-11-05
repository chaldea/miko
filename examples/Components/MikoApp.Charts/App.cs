using Miko.Charts;
using Miko.Hosting;

namespace MikoApp.Charts;

/// <summary>
/// Shared configuration of the chart gallery. Each head (Desktop, …) calls <see cref="CreateContext"/> and
/// drives the render loop.
/// </summary>
public static class App
{
    public static MikoAppContext CreateContext(Action<MikoAppBuilder>? configure = null)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("Miko Charts").UseSize(1100, 820);
        builder.Services.AddMikoCharts();
        builder.UseRootComponent(() => new ChartsDemo().Build());
        configure?.Invoke(builder);
        return builder.Build();
    }
}
