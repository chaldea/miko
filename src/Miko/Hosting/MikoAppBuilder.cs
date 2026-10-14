using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        // 引擎及其全部依赖（含变更计数器、图片加载、语法高亮）统一由此注册，
        // 与 MikoEngineBuilder 共用同一套定义（ISSUE-129）。
        builder.Services.AddMikoEngine();

        builder.Services.AddOptions<MikoAppOptions>();
        builder.Services.AddSingleton<NavigationManager>();
        // Host platform info + component-facing platform service (auto-detected from the OS).
        // One shared instance backs both interfaces: platform hosts / the device simulator mutate
        // the concrete PlatformInfo (they resolve it by concrete type), while component libraries
        // read it through IPlatformInfo / IPlatformService — so a platform switch is seen by both
        // (e.g. Miko.Ionic's md/ios mode, the back-button chain, ISSUE-138).
        // The orientation implementation is bound here, at resolution, rather than after Build():
        // harnesses and tests build the container directly, and a post-Build hook would leave
        // them with the inert default while the app proper gets the real one.
        builder.Services.AddSingleton(sp => new PlatformInfo
        {
            Orientation = sp.GetRequiredService<IOrientationService>(),
        });
        builder.Services.AddSingleton<IPlatformInfo>(sp => sp.GetRequiredService<PlatformInfo>());
        builder.Services.AddSingleton<IPlatformService>(sp => sp.GetRequiredService<PlatformInfo>());
        // Hosts replace this inert endpoint with their native Android/iOS/desktop implementation.
        builder.Services.TryAddSingleton<IInputMethodService, NullInputMethod>();
        builder.Services.TryAddSingleton<IScrollBehavior, DefaultScrollBehavior>();
        // Inert by default so components may call orientation/keep-awake unconditionally; the
        // platform packages override it via UseOrientation (ISSUE-138).
        builder.Services.TryAddSingleton<IOrientationService, NullOrientationService>();
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

    public MikoAppBuilder UseDefaultLayout<[DynamicallyAccessedMembers(Components.ComponentTypeMembers.Activation)] TLayout>() where TLayout : class
    {
        Services.Configure<MikoAppOptions>(o => o.DefaultLayout = typeof(TLayout));
        return this;
    }

    public MikoAppBuilder UseDefaultLayout(
        [DynamicallyAccessedMembers(Components.ComponentTypeMembers.Activation)] Type layoutType)
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
    /// Supplies the platform's screen-orientation / keep-awake implementation (ISSUE-138).
    /// Called by the platform packages (<c>UseDesktopNative()</c> / <c>UseAndroidNative()</c> /
    /// <c>UseIosNative()</c> / <c>UseSimulatorNative()</c>) with their own implementation, so
    /// components reach it through the shared <see cref="IPlatformService"/>.
    /// <para>
    /// Without this the inert <see cref="NullOrientationService"/> stays in place, which is the
    /// right behavior for bare tests and hosts that never call it.
    /// </para>
    /// </summary>
    public MikoAppBuilder UseOrientation(Func<IServiceProvider, IOrientationService> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Services.AddSingleton(factory);
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
