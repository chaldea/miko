using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miko.Hosting;

namespace Miko.Simulator;

/// <summary>
/// 模拟器宿主的便捷扩展，让模拟器启动项目保持极简：
/// <code>App.CreateContext().RunSimulator();</code>
/// 与 <c>RunDesktop()</c> 同级，作为独立平台启动项目使用。
/// </summary>
public static class SimulatorHostExtensions
{
    /// <summary>从应用上下文创建一个模拟器宿主（不启动）。</summary>
    public static SimulatorHost UseSimulator(this MikoAppContext context, Action<SimulatorOptions>? configure = null)
    {
        var options = new SimulatorOptions();
        configure?.Invoke(options);

        var loggerFactory = context.Services.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<SimulatorHost>();
        var host = new SimulatorHost(context, options, logger);

        // 通知所有观察者（如 Miko.McpServer 的 MCP 服务器启动器）宿主已就绪。
        // 观察者按 DI 注册顺序回调，可拿到宿主引用建立调试通道。
        foreach (var observer in context.Services.GetServices<ISimulatorHostObserver>())
            observer.OnHostStarted(host);

        return host;
    }

    /// <summary>从应用上下文创建模拟器宿主并启动（阻塞直到窗口关闭）。</summary>
    public static void RunSimulator(this MikoAppContext context, Action<SimulatorOptions>? configure = null)
    {
        context.UseSimulator(configure).Run();
    }

    /// <summary>构建应用并以模拟器宿主运行。</summary>
    public static void RunSimulator(this MikoAppBuilder builder, Action<SimulatorOptions>? configure = null)
    {
        builder.Build().RunSimulator(configure);
    }
}
