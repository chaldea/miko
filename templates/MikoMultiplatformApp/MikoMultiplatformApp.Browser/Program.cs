using System.Runtime.Versioning;
using Miko.Browser;
using Miko.Browser.Native;
using MikoMultiplatformApp;

namespace MikoMultiplatformApp.Browser;

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
