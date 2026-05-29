using Microsoft.Extensions.Logging;
using Miko.Common;
using Miko.Core.DomElements;
using Miko.DevTools.Logging;
using Miko.Styling;

namespace Miko.DevTools.Panels;

internal static class ConsolePanel
{
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

        var filtered = _entries.Where(e => e.Level >= filterLevel).ToList();

        if (filtered.Count == 0)
        {
            output.AddChild(new DivElement
            {
                Class = "console-empty",
                TextContent = "No log entries"
            });
        }
        else
        {
            foreach (var entry in filtered.TakeLast(500))
            {
                output.AddChild(BuildLogEntry(entry));
            }
        }

        panel.AddChild(output);
        return panel;
    }

    private static void DrainBuffer(DevToolsBridge bridge)
    {
        while (bridge.LogBuffer.TryDequeue(out var entry))
        {
            _entries.Add(entry);
            if (_entries.Count > 10000)
                _entries.RemoveAt(0);
        }
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
