using System.Runtime.Versioning;
using Miko.Browser;
using Miko.Browser.Native;
using Miko.Docs.Demo;
#if DEBUG
using Miko.Diagnostics;
#endif

namespace Miko.Docs.Demo.Browser;

[SupportedOSPlatform("browser")]
public static class Program
{
    public static void Main()
    {
        var context = App.CreateContext(builder => builder.UseBrowserNative());
#if DEBUG
        FrameProfiler.Enable(static profile =>
        {
            if (profile.Rebuilt || profile.LayoutPasses > 0 || profile.PaintPasses > 0)
            {
                Console.WriteLine($"[miko-frame] total={profile.TotalMilliseconds:F2}ms build={profile.BuildMicroseconds / 1000.0:F2}ms style={profile.StyleMicroseconds / 1000.0:F2}ms layout={profile.LayoutMicroseconds / 1000.0:F2}ms paint={profile.PaintMicroseconds / 1000.0:F2}ms allocated={profile.AllocatedBytes}B rebuilt={profile.Rebuilt} layouts={profile.LayoutPasses} paints={profile.PaintPasses}");
            }
        });
#endif
        context.RunBrowser();
    }
}
