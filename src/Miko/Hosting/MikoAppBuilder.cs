using System.Reflection;
using Microsoft.Extensions.Logging;
using Miko.Core;
using Miko.Styling;

namespace Miko.Hosting;

public class MikoAppBuilder
{
    private readonly MikoAppConfiguration _config = new();

    public MikoAppBuilder UseTitle(string title) { _config.Title = title; return this; }
    public MikoAppBuilder UseSize(int width, int height) { _config.Width = width; _config.Height = height; return this; }
    public MikoAppBuilder UseRootComponent(Func<Element> factory) { _config.RootComponentFactory = factory; return this; }
    public MikoAppBuilder UseStyleSheets(List<StyleSheet> styleSheets) { _config.StyleSheets = styleSheets; return this; }
    public MikoAppBuilder UseLogging(Action<ILoggingBuilder> configure) { _config.LoggingConfiguration = configure; return this; }
    public MikoAppBuilder UseRouter(params Assembly[] assemblies) { _config.RouteAssemblies = assemblies; return this; }
    public MikoAppBuilder UseDefaultLayout(Type layoutType) { _config.DefaultLayout = layoutType; return this; }

    public MikoAppBuilder UseFonts(Action<FontBuilder> configure)
    {
        var fontBuilder = new FontBuilder();
        configure(fontBuilder);
        _config.Fonts = fontBuilder.Registrations;
        return this;
    }

    public MikoApp Build() => new(_config);
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
