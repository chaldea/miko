using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Miko.DevTools;

namespace Miko.McpServer;

/// <summary>
/// 轻量日志捕获 <see cref="ILoggerProvider"/>，将应用日志存入环形缓冲，
/// 供 <c>devtools_get_logs</c> 工具读取。独立于 DevTools 窗口，随 MCP 启用即生效。
/// </summary>
internal sealed class McpLogCapture : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntryInfo> _buffer = new();
    private readonly int _capacity;
    private int _count;

    public McpLogCapture(int capacity)
    {
        _capacity = Math.Max(1, capacity);
    }

    public IReadOnlyList<LogEntryInfo> Snapshot() => _buffer.ToArray();

    public ILogger CreateLogger(string categoryName) => new CategoryLogger(this, categoryName);

    public void Dispose() { }

    private void Append(LogEntryInfo entry)
    {
        _buffer.Enqueue(entry);
        if (Interlocked.Increment(ref _count) > _capacity)
        {
            if (_buffer.TryDequeue(out _))
                Interlocked.Decrement(ref _count);
        }
    }

    private sealed class CategoryLogger : ILogger
    {
        private readonly McpLogCapture _owner;
        private readonly string _category;

        public CategoryLogger(McpLogCapture owner, string category)
        {
            _owner = owner;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            if (exception != null) message += $"\n{exception}";
            _owner.Append(new LogEntryInfo(
                logLevel.ToString(),
                message,
                DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")));
        }
    }
}
