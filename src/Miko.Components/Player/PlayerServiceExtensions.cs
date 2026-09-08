using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Miko.Components.Player;

public sealed class MikoPlayerOptions
{
    public int MaxVisibleDanmaku { get; set; } = 80;
}

public static class PlayerServiceExtensions
{
    public static IServiceCollection AddMikoPlayer(this IServiceCollection services, Action<MikoPlayerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new MikoPlayerOptions();
        configure?.Invoke(options);
        if (options.MaxVisibleDanmaku is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(configure));
        services.TryAddSingleton(options);
        return services;
    }
}
