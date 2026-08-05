using Microsoft.Extensions.Logging;

namespace Miko.DevTools;

public class DevToolsOptions
{
    public int Width { get; set; } = 900;
    public int Height { get; set; } = 600;

    /// <summary>
    /// 收集进 Console 面板的最低日志级别。
    /// <para>默认 <see cref="LogLevel.Information"/>：引擎自身在 Trace/Debug 级别记录了大量
    /// 逐帧信息（布局、脏区域、动画），若全部收集，则「渲染产生日志 → 缓冲变化 → 重建 UI →
    /// 再次渲染」会形成自持的反馈环，DevTools 永远无法进入稳态（见 ISSUE-117）。
    /// 需要排查引擎内部时可显式下调。</para>
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// 日志缓冲保留的最大条数。超出后丢弃最旧的条目，避免长时间运行时无界增长
    /// （Elements 面板处于活动状态时缓冲不会被 Console 面板排空）。
    /// </summary>
    public int MaxBufferedEntries { get; set; } = 2000;

    /// <summary>
    /// DevTools 窗口空闲时的轮询频率上限。无视觉工作时窗口不产帧，仅按此频率让出 CPU
    /// 后重新检查是否有工作（见 ISSUE-117）。
    /// </summary>
    public int TargetFramesPerSecond { get; set; } = 60;
}
