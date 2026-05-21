using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miko.Animation;
using Miko.Core;
using Miko.Events;
using Miko.Layout;
using Miko.Rendering;
using Miko.Routing;
using Miko.Styling;

namespace Miko.Hosting;

public class MikoAppBuilder
{
    public IServiceCollection Services { get; } = new ServiceCollection();

    public static MikoAppBuilder CreateDefault()
    {
        var builder = new MikoAppBuilder();
        builder.Services.AddLogging();
        builder.Services.AddOptions<MikoAppOptions>();
        builder.Services.AddSingleton<AnimationManager>();
        builder.Services.AddSingleton<NavigationManager>();
        builder.Services.AddSingleton<LayoutEngine>();
        builder.Services.AddSingleton<RenderEngine>();
        builder.Services.AddSingleton<DirtyRegionManager>();
        builder.Services.AddSingleton<EventDispatcher>();
        builder.Services.AddSingleton<MikoEngine>();
        return builder;
    }

    public MikoAppBuilder UseTitle(string title)
    {
        Services.Configure<MikoAppOptions>(o => o.Title = title);
        return this;
    }

    public MikoAppBuilder UseSize(int width, int height)
    {
        Services.Configure<MikoAppOptions>(o => { o.Width = width; o.Height = height; });
        return this;
    }

    public MikoAppBuilder UseRootComponent(Func<Element> factory)
    {
        Services.Configure<MikoAppOptions>(o => o.RootComponentFactory = factory);
        return this;
    }

    public MikoAppBuilder UseStyleSheets(List<StyleSheet> styleSheets)
    {
        Services.Configure<MikoAppOptions>(o => o.StyleSheets = styleSheets);
        return this;
    }

    public MikoAppBuilder AddStyleSheet(StyleSheet styleSheet)
    {
        Services.Configure<MikoAppOptions>(o => o.StyleSheets.Add(styleSheet));
        return this;
    }

    public MikoAppBuilder UseLogging(Action<ILoggingBuilder> configure)
    {
        Services.AddLogging(configure);
        return this;
    }

    public MikoAppBuilder UseRouter(params Assembly[] assemblies)
    {
        Services.Configure<MikoAppOptions>(o => o.RouteAssemblies = assemblies);
        return this;
    }

    public MikoAppBuilder UseRouter(Action<Router> configure)
    {
        Services.Configure<MikoAppOptions>(o => o.RouteConfigurator = configure);
        return this;
    }

    public MikoAppBuilder UseDefaultLayout<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] TLayout>() where TLayout : class
    {
        Services.Configure<MikoAppOptions>(o => o.DefaultLayout = typeof(TLayout));
        return this;
    }

    public MikoAppBuilder UseDefaultLayout(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type layoutType)
    {
        Services.Configure<MikoAppOptions>(o => o.DefaultLayout = layoutType);
        return this;
    }

    public MikoAppBuilder UseFonts(Action<FontBuilder> configure)
    {
        var fontBuilder = new FontBuilder();
        configure(fontBuilder);
        Services.Configure<MikoAppOptions>(o => o.Fonts.AddRange(fontBuilder.Registrations));
        return this;
    }

    public MikoApp Build()
    {
        Services.AddSingleton<MikoApp>();
        var serviceProvider = Services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<MikoApp>();
    }
}

public class FontBuilder
{
    internal List<FontRegistration> Registrations { get; } = new();

    public FontBuilder AddResource(string familyName, Assembly assembly, string resourceName)
    {
        Registrations.Add(new FontRegistration
        {
            FamilyName = familyName,
            Assembly = assembly,
            ResourceName = resourceName
        });
        return this;
    }
}
