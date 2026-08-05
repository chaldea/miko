using Microsoft.Extensions.Logging;

namespace Miko.DevTools.Logging;

internal class DevToolsLogger : ILogger
{
    private readonly string _category;
    private readonly LogBuffer _buffer;
    private readonly LogLevel _minimumLevel;

    public DevToolsLogger(string category, LogBuffer buffer, LogLevel minimumLevel)
    {
        _category = category;
        _buffer = buffer;
        _minimumLevel = minimumLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <summary>
    /// 按最低级别过滤。这不只是省内存：引擎在 Trace/Debug 级别逐帧记录布局/脏区域信息，
    /// 若全部收集，Console 面板会陷入「渲染 → 产生日志 → 缓冲变化 → 重建 → 渲染」的
    /// 自持循环，永远无法进入稳态（见 ISSUE-117）。
    /// <para>返回 false 时 <c>ILogger</c> 的调用方会跳过消息格式化，因此过滤在此处生效
    /// 也顺带省下了字符串分配。</para>
    /// </summary>
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= _minimumLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        _buffer.Enqueue(new LogEntry(DateTimeOffset.Now, logLevel, _category, message, exception));
    }
}
