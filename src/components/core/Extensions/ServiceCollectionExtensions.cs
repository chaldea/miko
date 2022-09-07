﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Miko
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMiko(this IServiceCollection services, Action<MikoOptions> configure = null)
        {
            services.TryAddScoped<MikoJsInterop>();
            services.TryAddScoped<AnimationService>();
            services.TryAddScoped<MenuService>();
            services.Configure(configure);
            return services;
        }
    }
}
