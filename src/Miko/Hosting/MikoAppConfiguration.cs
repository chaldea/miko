using Microsoft.Extensions.Logging;
using Miko.Core;
using Miko.Styling;

namespace Miko.Hosting;

internal class MikoAppConfiguration
{
    public string Title { get; set; } = "Miko Application";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public Func<Element>? RootComponentFactory { get; set; }
    public List<StyleSheet> StyleSheets { get; set; } = new();
    public Action<ILoggingBuilder>? LoggingConfiguration { get; set; }
}
