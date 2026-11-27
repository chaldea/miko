using Miko.Windowing;
using Miko.Windowing.Native;
using MikoAppBlank;

namespace PackagingDemo;

internal static class Program
{
    [STAThread]
    private static void Main() => App.CreateContext(builder => builder.UseDesktopNative()).RunDesktop();
}
