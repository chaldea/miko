using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Miko.DevTools.Logging;

internal class DevToolsLoggerProvider : ILoggerProvider
{
    private readonly LogBuffer _buffer;
    private readonly LogLevel _minimumLevel;
    private readonly ConcurrentDictionary<string, DevToolsLogger> _loggers = new();

    public DevToolsLoggerProvider(LogBuffer buffer, LogLevel minimumLevel)
    {
        _buffer = buffer;
        _minimumLevel = minimumLevel;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new DevToolsLogger(name, _buffer, _minimumLevel));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
