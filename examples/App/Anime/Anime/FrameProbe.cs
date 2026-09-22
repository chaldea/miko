// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Miko.Diagnostics;

namespace Anime;

/// <summary>
/// ISSUE-144 的现场探针：把每帧的分段耗时（构建 / 样式 / 布局 / 绘制）写到宿主的日志里，
/// 使「Tab 切换有明显延迟」「返回详情页一帧一帧地卡」这类只在 Android 上出现的问题
/// 可以被<b>测量</b>，而不是靠桌面基准推断。
///
/// <para><b>为什么要按帧、而不是只报平均值</b>：这两个症状都是<b>单帧</b>过长——一次
/// 路由导航把整棵 DOM 树重建一遍，那一帧可能要几百毫秒，而其后的稳态帧完全正常。
/// 平均值会把它摊平到看不见。因此这里逐帧记录，并单独标出「慢帧」与重建帧。</para>
///
/// <para>开关：环境变量 <c>MIKO_FRAME_PROBE=1</c>，或平台启动项目显式调用
/// <see cref="Enable"/>。默认关闭——探针本身每帧要读两次时钟并统计分配，不该出现在
/// 正常运行的应用里。</para>
/// </summary>
public static class FrameProbe
{
    /// <summary>超过该耗时（毫秒）的帧被记为「慢帧」（60fps 的预算是 16.7ms）。</summary>
    private const double SlowFrameMilliseconds = 32.0;

    private static readonly object Gate = new();
    private static Action<string>? _log;
    private static bool _verbose;
    private static int _slowFrames;
    private static int _frames;
    private static double _totalMilliseconds;
    private static double _worstMilliseconds;

    /// <summary>
    /// 按环境变量 <c>MIKO_FRAME_PROBE</c> 决定是否启用。平台启动项目在创建应用上下文前调用。
    /// </summary>
    /// <param name="log">日志输出（Android 传 <c>Android.Util.Log</c>，桌面传 <c>Console.WriteLine</c>）。</param>
    public static void EnableIfRequested(Action<string> log)
    {
        var flag = Environment.GetEnvironmentVariable("MIKO_FRAME_PROBE");
        if (string.IsNullOrEmpty(flag) || flag == "0") return;
        Enable(log, verbose: flag == "verbose");
    }

    /// <summary>
    /// 无条件启用探针。
    /// </summary>
    /// <param name="log">日志输出。</param>
    /// <param name="verbose">
    /// 是否输出<b>每一帧</b>。默认只输出重建帧与慢帧——稳态每秒 60 行会冲掉 logcat，也会让
    /// 探针自己成为瓶颈。但测量「转场期间到底出了几帧」时必须逐帧输出，因为那里的问题
    /// 恰恰是「帧数太少」而不是「某一帧太慢」。
    /// </param>
    public static void Enable(Action<string> log, bool verbose = false)
    {
        ArgumentNullException.ThrowIfNull(log);
        lock (Gate)
        {
            _log = log;
            _verbose = verbose;
            _slowFrames = 0;
            _frames = 0;
            _totalMilliseconds = 0;
            _worstMilliseconds = 0;
        }

        FrameProfiler.Enable(Report);
        log($"[frame-probe] enabled{(verbose ? " (verbose)" : string.Empty)}");
    }

    /// <summary>关闭探针并输出一次汇总。</summary>
    public static void Disable()
    {
        FrameProfiler.Disable();
        lock (Gate)
        {
            _log?.Invoke(Summary());
            _log = null;
        }
    }

    /// <summary>自启用以来的汇总行（帧数、平均/最差耗时、慢帧数）。</summary>
    public static string Summary()
    {
        lock (Gate)
        {
            var average = _frames == 0 ? 0 : _totalMilliseconds / _frames;
            return $"[frame-probe] frames={_frames} avg={average:F1}ms worst={_worstMilliseconds:F1}ms slow={_slowFrames}";
        }
    }

    private static void Report(FrameProfile frame)
    {
        Action<string>? log;
        bool verbose;
        lock (Gate)
        {
            _frames++;
            _totalMilliseconds += frame.TotalMilliseconds;
            if (frame.TotalMilliseconds > _worstMilliseconds) _worstMilliseconds = frame.TotalMilliseconds;
            if (frame.TotalMilliseconds > SlowFrameMilliseconds) _slowFrames++;
            log = _log;
            verbose = _verbose;
        }

        if (log == null) return;

        // 默认只打印值得看的帧：重建帧（路由导航/热重载）与慢帧。稳态帧每秒 60 行会把
        // logcat 冲掉，也会让探针本身成为瓶颈。verbose 下逐帧打印（测量转场帧率用）。
        if (!verbose && !frame.Rebuilt && frame.TotalMilliseconds <= SlowFrameMilliseconds) return;

        var line = new StringBuilder(160);
        line.Append("[frame-probe] #").Append(frame.FrameIndex)
            .Append(frame.Rebuilt ? " REBUILD" : string.Empty)
            .Append(" total=").Append(frame.TotalMilliseconds.ToString("F1")).Append("ms")
            .Append(" build=").Append((frame.BuildMicroseconds / 1000).ToString("F1"))
            .Append(" style=").Append((frame.StyleMicroseconds / 1000).ToString("F1"))
            .Append(" layout=").Append((frame.LayoutMicroseconds / 1000).ToString("F1"))
            .Append(" paint=").Append((frame.PaintMicroseconds / 1000).ToString("F1"))
            .Append(" passes=").Append(frame.BuildPasses).Append('/').Append(frame.LayoutPasses)
            .Append('/').Append(frame.PaintPasses)
            .Append(" boxes=").Append(frame.BoxDraws)
            .Append(" text=").Append(frame.TextDraws).Append('@')
            .Append((frame.TextMicroseconds / 1000).ToString("F1"))
            .Append(" img=").Append(frame.ImageDraws).Append('@')
            .Append((frame.ImageMicroseconds / 1000).ToString("F1"))
            .Append(" alloc=").Append(frame.AllocatedBytes / 1024).Append("KB");
        log(line.ToString());
    }
}
