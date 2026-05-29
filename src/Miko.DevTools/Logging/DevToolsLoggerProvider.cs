using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Miko.DevTools.Logging;

internal class DevToolsLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _buffer;
    private readonly ConcurrentDictionary<string, DevToolsLogger> _loggers = new();

    public DevToolsLoggerProvider(ConcurrentQueue<LogEntry> buffer)
    {
        _buffer = buffer;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new DevToolsLogger(name, _buffer));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
