using Microsoft.Extensions.Logging;
using Miko.DevTools;
using Miko.DevTools.Logging;
using Shouldly;

namespace Miko.McpServer.Tests;

/// <summary>
/// ISSUE-117 的回归防护：DevTools 的日志收集必须**有界**且**按级别过滤**。
/// <para>这两点不只是省内存。<see cref="DevToolsLogger"/> 曾对所有级别返回
/// <c>IsEnabled == true</c>，于是引擎自身逐帧的 Trace/Debug 日志（布局、脏区域、滚动）
/// 全部灌入缓冲；Console 面板又按缓冲变化触发重建，形成
/// 「渲染 → 产生日志 → 缓冲变化 → 重建 → 渲染」的自持循环，DevTools 永远无法进入稳态。
/// 同时缓冲无上限，Elements 面板活动时无人排空，会持续增长。</para>
/// </summary>
public class DevToolsLogBufferTests
{
    private static LogEntry Entry(string message, LogLevel level = LogLevel.Information) =>
        new(DateTimeOffset.UnixEpoch, level, "Test", message, null);

    private static ILogger CreateLogger(LogBuffer buffer, LogLevel minimumLevel) =>
        new DevToolsLoggerProvider(buffer, minimumLevel).CreateLogger("Miko.Core.MikoEngine");

    [Fact]
    public void LogBuffer_Should_Cap_Retained_Entries_At_Capacity()
    {
        var buffer = new LogBuffer(maxEntries: 10);

        for (int i = 0; i < 500; i++)
            buffer.Enqueue(Entry($"msg {i}"));

        buffer.Count.ShouldBeLessThanOrEqualTo(10);
    }

    [Fact]
    public void LogBuffer_Should_Keep_Sequence_Monotonic_Across_Trimming()
    {
        var buffer = new LogBuffer(maxEntries: 4);

        buffer.Sequence.ShouldBe(0);

        // 远超容量：条目数会封顶，但序号必须持续递增——否则 UI 用「条目数」判断
        // 「是否有新日志」时，裁剪后数量不变会漏掉更新。
        for (int i = 0; i < 50; i++)
            buffer.Enqueue(Entry($"msg {i}"));

        buffer.Sequence.ShouldBe(50);
        buffer.Count.ShouldBeLessThanOrEqualTo(4);
    }

    [Fact]
    public void LogBuffer_Should_Retain_The_Most_Recent_Entries()
    {
        var buffer = new LogBuffer(maxEntries: 3);

        for (int i = 0; i < 10; i++)
            buffer.Enqueue(Entry($"msg {i}"));

        var drained = new List<LogEntry>();
        while (buffer.TryDequeue(out var entry) && entry != null)
            drained.Add(entry);

        // 丢弃的是最旧的条目，保留尾部。
        drained.ShouldNotBeEmpty();
        drained[^1].Message.ShouldBe("msg 9");
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    public void Logger_Should_Drop_Entries_Below_Minimum_Level(LogLevel level)
    {
        var buffer = new LogBuffer();
        var logger = CreateLogger(buffer, LogLevel.Information);

        logger.IsEnabled(level).ShouldBeFalse();

        logger.Log(level, default, "state", null, (s, _) => s);

        // 反馈环的源头：引擎逐帧的 Trace/Debug 不得进入缓冲。
        buffer.Count.ShouldBe(0);
        buffer.Sequence.ShouldBe(0);
    }

    [Theory]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    public void Logger_Should_Keep_Entries_At_Or_Above_Minimum_Level(LogLevel level)
    {
        var buffer = new LogBuffer();
        var logger = CreateLogger(buffer, LogLevel.Information);

        logger.IsEnabled(level).ShouldBeTrue();

        logger.Log(level, default, "state", null, (s, _) => s);

        buffer.Count.ShouldBe(1);
        buffer.Sequence.ShouldBe(1);
    }

    [Fact]
    public void Logger_Should_Never_Enable_LogLevel_None()
    {
        var logger = CreateLogger(new LogBuffer(), LogLevel.Trace);

        logger.IsEnabled(LogLevel.None).ShouldBeFalse();
    }

    [Fact]
    public void Logger_Should_Honour_A_Lowered_Minimum_Level()
    {
        // 排查引擎内部时可显式下调级别，此时 Trace 应当被收集。
        var buffer = new LogBuffer();
        var logger = CreateLogger(buffer, LogLevel.Trace);

        logger.Log(LogLevel.Trace, default, "state", null, (s, _) => s);

        buffer.Count.ShouldBe(1);
    }

    [Fact]
    public void Options_Should_Default_To_Information_And_A_Bounded_Buffer()
    {
        var options = new DevToolsOptions();

        // 默认必须过滤掉引擎的逐帧 Trace/Debug，否则反馈环重现。
        options.MinimumLevel.ShouldBe(LogLevel.Information);
        options.MaxBufferedEntries.ShouldBeGreaterThan(0);
        options.TargetFramesPerSecond.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Bridge_Should_Size_Its_Buffer_From_Options()
    {
        var bridge = new DevToolsBridge(new DevToolsOptions { MaxBufferedEntries = 7 });

        bridge.LogBuffer.Capacity.ShouldBe(7);
    }
}
