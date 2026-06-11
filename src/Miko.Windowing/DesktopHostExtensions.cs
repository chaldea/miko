using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miko.Hosting;

namespace Miko.Windowing;

/// <summary>
/// 桌面宿主的便捷扩展，让桌面启动项目保持极简：
/// <code>App.CreateContext().RunDesktop();</code>
/// </summary>
public static class DesktopHostExtensions
{
    /// <summary>从应用上下文创建一个桌面宿主（不启动）。</summary>
    public static SilkDesktopHost UseDesktop(this MikoAppContext context)
    {
        var loggerFactory = context.Services.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<SilkDesktopHost>();
        return new SilkDesktopHost(context, logger);
    }

    /// <summary>从应用上下文创建桌面宿主并启动渲染循环（阻塞直到窗口关闭）。</summary>
    public static void RunDesktop(this MikoAppContext context)
    {
        context.UseDesktop().Run();
    }

    /// <summary>构建应用并以桌面宿主运行。</summary>
    public static void RunDesktop(this MikoAppBuilder builder)
    {
        builder.Build().RunDesktop();
    }
}
