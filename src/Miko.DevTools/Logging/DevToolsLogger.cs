using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Miko.DevTools.Logging;

internal class DevToolsLogger : ILogger
{
    private readonly string _category;
    private readonly ConcurrentQueue<LogEntry> _buffer;

    public DevToolsLogger(string category, ConcurrentQueue<LogEntry> buffer)
    {
        _category = category;
        _buffer = buffer;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        _buffer.Enqueue(new LogEntry(DateTimeOffset.Now, logLevel, _category, message, exception));
    }
}
