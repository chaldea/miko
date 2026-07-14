namespace Miko.McpServer;

/// <summary>
/// Miko MCP 服务器配置。
/// </summary>
public sealed class MikoMcpOptions
{
    /// <summary>MCP 服务监听的本地 HTTP 端口。默认 5800。</summary>
    public int Port { get; set; } = 5800;

    /// <summary>捕获日志的最大条数（环形缓冲）。默认 1000。</summary>
    public int LogBufferSize { get; set; } = 1000;
}
