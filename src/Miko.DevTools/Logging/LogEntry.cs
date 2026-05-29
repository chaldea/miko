using Microsoft.Extensions.Logging;

namespace Miko.DevTools.Logging;

public record LogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Category,
    string Message,
    Exception? Exception);
