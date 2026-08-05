using Microsoft.Extensions.Logging;
using Miko.Common;
using Miko.Core.DomElements;
using Miko.DevTools.Logging;
using Miko.Styling;

namespace Miko.DevTools.Panels;

internal static class ConsolePanel
{
    /// <summary>单次构建最多渲染的日志行数（每行会展开成多个元素，需要限制 DOM 规模）。</summary>
    private const int MaxRenderedEntries = 500;

    private static readonly List<LogEntry> _entries = new();

    public static DivElement Build(DevToolsBridge bridge, LogLevel filterLevel, bool visible, Action<LogLevel>? onFilterChange = null)
    {
        var panel = new DivElement { Class = "console-panel" };
        if (!visible)
        {
            panel.Style = new Style { Display = Display.None };
        }

        DrainBuffer(bridge);

        var filterBar = BuildFilterBar(bridge, filterLevel, onFilterChange ?? (_ => { }));
        panel.AddChild(filterBar);

        var output = new DivElement { Class = "console-output" };

        // 只渲染最近 MaxRenderedEntries 条：从尾部反向扫描收集，避免先物化整份过滤列表
        // 再丢弃绝大部分（历史可达数千条，而可见的只有几百条）。
        var rendered = new List<LogEntry>(MaxRenderedEntries);
        for (int i = _entries.Count - 1; i >= 0 && rendered.Count < MaxRenderedEntries; i--)
        {
            if (_entries[i].Level >= filterLevel)
                rendered.Add(_entries[i]);
        }
        rendered.Reverse();

        if (rendered.Count == 0)
        {
            output.AddChild(new DivElement
            {
                Class = "console-empty",
                TextContent = "No log entries"
            });
        }
        else
        {
            foreach (var entry in rendered)
            {
                output.AddChild(BuildLogEntry(entry));
            }
        }

        panel.AddChild(output);
        return panel;
    }

    private static void DrainBuffer(DevToolsBridge bridge)
    {
        var buffer = bridge.LogBuffer;
        while (buffer.TryDequeue(out var entry))
        {
            if (entry != null) _entries.Add(entry);
        }

        // 面板保留的历史不超过缓冲容量的若干倍，避免长会话下无界增长。
        // RemoveRange 一次性裁剪，优于逐条 RemoveAt(0) 的 O(n²)。
        int limit = buffer.Capacity * 4;
        if (_entries.Count > limit)
            _entries.RemoveRange(0, _entries.Count - limit);
    }

    private static DivElement BuildFilterBar(DevToolsBridge bridge, LogLevel currentLevel, Action<LogLevel> onFilterChange)
    {
        var bar = new DivElement { Class = "console-filter-bar" };
        bar.AddChild(new SpanElement { Class = "console-filter-label", TextContent = "Level:" });

        var levels = new[] { LogLevel.Trace, LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error };
        foreach (var level in levels)
        {
            var capturedLevel = level;
            var btn = new DivElement
            {
                Class = level == currentLevel
                    ? "console-filter-btn console-filter-btn-active"
                    : "console-filter-btn",
                TextContent = FormatLevel(level)
            };
            btn.OnClick = _ =>
            {
                onFilterChange(capturedLevel);
            };
            bar.AddChild(btn);
        }

        return bar;
    }

    private static DivElement BuildLogEntry(LogEntry entry)
    {
        var levelClass = entry.Level switch
        {
            LogLevel.Trace => "console-entry-trace",
            LogLevel.Debug => "console-entry-debug",
            LogLevel.Information => "console-entry-info",
            LogLevel.Warning => "console-entry-warning",
            LogLevel.Error => "console-entry-error",
            LogLevel.Critical => "console-entry-critical",
            _ => "console-entry-info"
        };

        var row = new DivElement { Class = $"console-entry {levelClass}" };

        var timestamp = new SpanElement
        {
            Class = "console-timestamp",
            TextContent = entry.Timestamp.ToString("HH:mm:ss.fff")
        };
        row.AddChild(timestamp);

        var shortCategory = ShortenCategory(entry.Category);
        var category = new SpanElement
        {
            Class = "console-category",
            TextContent = $"[{shortCategory}]"
        };
        row.AddChild(category);

        row.AddChild(new SpanElement { TextContent = entry.Message });

        if (entry.Exception != null)
        {
            row.AddChild(new DivElement
            {
                Class = "console-entry-error",
                TextContent = entry.Exception.Message
            });
        }

        return row;
    }

    private static string ShortenCategory(string category)
    {
        var lastDot = category.LastIndexOf('.');
        return lastDot >= 0 ? category[(lastDot + 1)..] : category;
    }

    private static string FormatLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => "Trace",
        LogLevel.Debug => "Debug",
        LogLevel.Information => "Info",
        LogLevel.Warning => "Warn",
        LogLevel.Error => "Error",
        _ => level.ToString()
    };
}
