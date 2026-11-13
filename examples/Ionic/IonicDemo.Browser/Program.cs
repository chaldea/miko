using System.Runtime.Versioning;
using IonicDemo;
using Miko.Browser;
using Miko.Browser.Native;

namespace IonicDemo.Browser;

[SupportedOSPlatform("browser")]
public static class Program
{
    public static void Main()
    {
        var context = App.CreateContext(builder => builder.UseBrowserNative());
        App.InitializeHotReload(context);
        context.RunBrowser();
    }
}
