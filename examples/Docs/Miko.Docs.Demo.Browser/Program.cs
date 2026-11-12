using System.Runtime.Versioning;
using Miko.Browser;
using Miko.Browser.Native;
using Miko.Docs.Demo;

namespace Miko.Docs.Demo.Browser;

[SupportedOSPlatform("browser")]
public static class Program
{
    public static void Main()
    {
        var context = App.CreateContext(builder => builder.UseBrowserNative());
        context.RunBrowser();
    }
}
