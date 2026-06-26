using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miko.Animation;
using Miko.Core;
using Miko.Events;
using Miko.Layout;
using Miko.Platform;
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
        // Host platform info (auto-detected from the OS). Singleton so platform hosts / the
        // device simulator can resolve and override it; component libraries derive UI choices
        // from it (e.g. Miko.Ionic's md/ios mode).
        builder.Services.AddSingleton<Platform.IPlatformInfo, Platform.PlatformInfo>();
        builder.Services.AddSingleton<LayoutEngine>();
        builder.Services.AddSingleton<RenderEngine>();
        builder.Services.AddSingleton<DirtyRegionManager>();
        builder.Services.AddSingleton<EventDispatcher>();
        builder.Services.AddSingleton<Platform.MikoDispatcher>();
        builder.Services.AddSingleton<MikoEngine>();
        builder.Services.AddSingleton<HotReloadService>();
        builder.Services.AddSingleton<MikoInteractionController>();
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

    public MikoAppBuilder AddGlobalKeyHandler(Func<MikoKey, bool> handler)
    {
        Services.Configure<MikoAppOptions>(o => o.GlobalKeyDownHandlers.Add(handler));
        return this;
    }

    public MikoAppBuilder UseFonts(Action<FontBuilder> configure)
    {
        var fontBuilder = new FontBuilder();
        configure(fontBuilder);
        Services.Configure<MikoAppOptions>(o => o.Fonts.AddRange(fontBuilder.Registrations));
        return this;
    }

    public MikoAppBuilder EnableHotReload()
    {
        Services.Configure<MikoAppOptions>(o => o.EnableHotReload = true);
        return this;
    }

    /// <summary>
    /// 构建平台无关的应用上下文。各平台启动项目（Miko.Windowing / Miko.Android / Miko.iOS）
    /// 通过该上下文获取已配置的服务、选项、交互控制器与引擎，并驱动各自的渲染循环。
    /// </summary>
    public MikoAppContext Build()
    {
        Services.AddSingleton<MikoAppContext>();
        var serviceProvider = Services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<MikoAppContext>();
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
