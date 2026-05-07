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

    public MikoApp Build() => new(_config);
}
