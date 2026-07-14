using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miko.DevTools;
using Miko.Hosting;
using Miko.McpServer.Tools;
using Miko.Simulator;

namespace Miko.McpServer;

/// <summary>
/// Miko MCP 服务器扩展。将 Simulator/DevTools 的调试能力通过标准 MCP 协议
/// （Streamable HTTP）暴露，供 Claude Code 等 MCP 客户端连接。
/// </summary>
public static class MikoMcpServerExtensions
{
    /// <summary>
    /// 注册 Miko MCP 服务器。仅在被调用时生效——因此把它放在开发用的模拟器启动项目里，
    /// 发布版不调用即不启动，无需额外的开关。模拟器宿主创建后会自动在本地 HTTP 端口
    /// （默认 5800）启动 MCP 服务。
    /// </summary>
    /// <example>
    /// <code>
    /// // 仅在模拟器 head 中调用
    /// var context = App.CreateContext(builder => builder.AddMikoMcpServer());
    /// context.RunSimulator();
    /// </code>
    /// </example>
    public static MikoAppBuilder AddMikoMcpServer(this MikoAppBuilder builder, Action<MikoMcpOptions>? configure = null)
    {
        var options = new MikoMcpOptions();
        configure?.Invoke(options);
        builder.Services.AddSingleton(options);

        // 捕获应用日志到独立缓冲，供 devtools_get_logs 读取（不依赖 DevTools 窗口）。
        var logCapture = new McpLogCapture(options.LogBufferSize);
        builder.Services.AddSingleton(logCapture);
        builder.Services.AddSingleton<ILoggerProvider>(logCapture);

        // 观察者：模拟器宿主就绪后启动 MCP HTTP 服务器。
        builder.Services.AddSingleton<ISimulatorHostObserver>(sp =>
            new McpServerLauncher(options, logCapture));

        return builder;
    }
}

/// <summary>
/// 模拟器宿主观察者：拿到宿主后构建并启动一个独立的 Kestrel + MCP Web 应用。
/// 服务器在后台线程运行，与模拟器渲染循环并行；工具调用通过宿主marshal回渲染线程。
/// </summary>
internal sealed class McpServerLauncher : ISimulatorHostObserver
{
    private readonly MikoMcpOptions _options;
    private readonly McpLogCapture _logCapture;

    public McpServerLauncher(MikoMcpOptions options, McpLogCapture logCapture)
    {
        _options = options;
        _logCapture = logCapture;
    }

    public void OnHostStarted(SimulatorHost host)
    {
        // 用宿主构建调试服务：DevTools 走渲染线程marshal的 DOM 访问器；日志走捕获缓冲。
        var domAccessor = new SimulatorDomAccessor(host);
        var devToolsService = new DevToolsService(domAccessor, _logCapture.Snapshot);
        var simulatorService = new SimulatorService(host);

        var thread = new Thread(() => RunServer(simulatorService, devToolsService))
        {
            IsBackground = true,
            Name = "Miko-MCP-Server",
        };
        thread.Start();
    }

    private void RunServer(ISimulatorService simulatorService, IDevToolsService devToolsService)
    {
        try
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(k => k.ListenLocalhost(_options.Port));

            // 调试服务注入容器，供 MCP 工具（SimulatorTools/DevToolsTools）构造函数解析。
            builder.Services.AddSingleton(simulatorService);
            builder.Services.AddSingleton(devToolsService);

            // 官方 MCP SDK：Streamable HTTP 传输 + 反射发现工具类型。
            builder.Services
                .AddMcpServer(o =>
                {
                    o.ServerInfo = new() { Name = "miko", Version = "1.0.0" };
                })
                .WithHttpTransport()
                .WithTools<SimulatorTools>()
                .WithTools<DevToolsTools>();

            var app = builder.Build();

            // MCP 端点挂载到根路径，匹配 settings.json 中的 http://localhost:<port>。
            app.MapMcp();

            Console.WriteLine($"[Miko MCP] Server listening at http://localhost:{_options.Port}");
            Console.WriteLine($"[Miko MCP] Configure .mcp.json:");
            Console.WriteLine("  {");
            Console.WriteLine("    \"mcpServers\": {");
            Console.WriteLine("      \"miko\": {");
            Console.WriteLine("        \"type\": \"http\",");
            Console.WriteLine($"        \"url\": \"http://localhost:{_options.Port}\"");
            Console.WriteLine("      }");
            Console.WriteLine("    }");
            Console.WriteLine("  }");

            app.Run();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Miko MCP] Failed to start server: {ex.Message}");
        }
    }
}
