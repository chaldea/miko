using Miko.DevTools;
using Miko.Hosting;
using Miko.Ionic;
using Microsoft.Extensions.Logging;

namespace MikoAppTransitions;

public static class App
{
    public static MikoAppContext CreateContext()
    {
        var builder = MikoAppBuilder.CreateDefault();

        builder.UseTitle("MikoAppTransitions");
        // Phone-portrait viewport showcases the Ionic mobile layout.
        builder.UseSize(390, 844);

        // 控制台输出导航/页面转场的跟踪日志（MikoInteractionController / MikoEngine 的 Debug 级日志；
        // 转场逐帧推进为 Trace 级，把最低级别改为 LogLevel.Trace 可查看）。
        builder.UseLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Debug));

        builder.AddDevTools();
        builder.AddIonic();
        builder.AddStyleSheet(GlobalStyles.Create());

        // Routes are wired up by Miko.Razor.Compiler. No default layout: each page is a
        // full IonPage (with its own header) so the whole page participates in the
        // navigation transition (ISSUE-108).
        builder.UseGeneratedRoutes();

        builder.EnableHotReload();

        return builder.Build();
    }
}
