using LiveChartsCore;
using LiveChartsCore.MikoView;
using LiveChartsCore.Themes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Miko.Charts;

/// <summary>Registers the chart components.</summary>
public static class ChartsServiceExtensions
{
    /// <summary>
    /// Registers the chart components and configures LiveCharts to draw through Miko: the provider, the
    /// default (light or dark) theme and the text settings.
    /// </summary>
    /// <remarks>
    /// LiveCharts settings are process-wide (<see cref="LiveCharts.Configure"/>): the theme and font chosen
    /// here apply to every chart in the process. Call it once, before the first chart renders.
    /// </remarks>
    public static IServiceCollection AddMikoCharts(this IServiceCollection services, Action<MikoChartsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MikoChartsOptions();
        configure?.Invoke(options);
        if (string.IsNullOrWhiteSpace(options.FontFamily))
            throw new ArgumentException("The chart font family cannot be empty.", nameof(configure));

        LiveCharts.Configure(settings =>
        {
            settings.UseDefaults();
            settings.HasTextSettings(new TextSettings { DefaultFontFamily = options.FontFamily });
            if (options.DarkTheme) settings.AddDarkTheme();
            else settings.AddLightTheme();
        });

        services.TryAddSingleton(options);
        return services;
    }
}
