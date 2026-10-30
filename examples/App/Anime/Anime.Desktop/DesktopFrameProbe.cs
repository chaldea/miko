// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using Anime.Services;
using Microsoft.Extensions.DependencyInjection;
using Miko.Diagnostics;
using Miko.Hosting;
using Miko.Layout;
using Miko.Native;
using Miko.Platform;
using Silk.NET.Windowing;

namespace Anime.Desktop;

/// <summary>
/// Optional desktop counterpart of the iOS frame probe, used to compare the desktop window
/// backends (OpenGL / Metal) on the same scenarios (ISSUE-147). Frames are timed through the
/// engine's <see cref="FrameProfiler"/>: "frame" is the engine frame including the host's
/// clear and Skia flush; the interval between frames also covers the host's present, which
/// waits for vsync. Automatic input is posted to the render thread through
/// <see cref="MikoDispatcher"/>, the same thread that consumes real window input. When the
/// scenario completes the probe closes the window, so each run also exercises host shutdown.
/// </summary>
internal sealed class DesktopFrameProbe
{
    private static readonly string? Scenario = Environment.GetEnvironmentVariable("MIKO_DESKTOP_PROBE_SCENARIO");
    private static readonly bool Verbose = Environment.GetEnvironmentVariable("MIKO_FRAME_PROBE") == "verbose";
    private readonly MikoAppContext _context;
    private readonly MikoDispatcher _dispatcher;
    private readonly long _started = Stopwatch.GetTimestamp();
    private readonly List<string> _records = [];
    private readonly Dictionary<string, PhaseStats> _stats = [];
    private readonly object _gate = new();
    private readonly float _x;
    private readonly float _y;
    private volatile string _phaseName = "startup";
    private int _phase = -1;
    private long _lastFrame;
    private bool _reportedBackend;

    private DesktopFrameProbe(MikoAppContext context)
    {
        _context = context;
        _dispatcher = context.Services.GetRequiredService<MikoDispatcher>();
        _x = context.Options.Width * 0.5f;
        _y = context.Options.Height * 0.5f;
        FrameProfiler.Enable(OnFrame);
        Log($"[desktop-probe] attached scenario={Scenario}");
        new Thread(Drive) { IsBackground = true, Name = "desktop-probe" }.Start();
    }

    /// <summary>Whether <c>MIKO_DESKTOP_PROBE_SCENARIO</c> is <c>scroll</c> or <c>video</c>.</summary>
    public static bool IsRequested => Scenario is "scroll" or "video";

    /// <summary>Starts the probe when <see cref="IsRequested"/>.</summary>
    public static DesktopFrameProbe? StartIfRequested(MikoAppContext context)
        => IsRequested ? new DesktopFrameProbe(context) : null;

    // Render thread, inside the render lock: only record.
    private void OnFrame(FrameProfile frame)
    {
        var now = Stopwatch.GetTimestamp();
        lock (_gate)
        {
            if (!_reportedBackend)
            {
                _reportedBackend = true;
                Log($"[desktop-renderer] backend={_context.Engine.GraphicsDevice?.Backend} os={Environment.OSVersion}");
            }

            var interval = _lastFrame == 0 ? 0 : Stopwatch.GetElapsedTime(_lastFrame, now).TotalMilliseconds;
            _lastFrame = now;
            Stats(_phaseName).Add(interval, frame.TotalMilliseconds);
            if (Verbose || frame.TotalMilliseconds > 32)
                Log(FormattableString.Invariant(
                    $"[desktop-frame] phase={_phaseName} interval={interval:F3}ms frame={frame.TotalMilliseconds:F3}ms build={frame.BuildMicroseconds / 1000:F1} style={frame.StyleMicroseconds / 1000:F1} layout={frame.LayoutMicroseconds / 1000:F1} paint={frame.PaintMicroseconds / 1000:F1}{(frame.Rebuilt ? " REBUILD" : string.Empty)}"));
        }
    }

    private void Drive()
    {
        while (true)
        {
            var seconds = Stopwatch.GetElapsedTime(_started).TotalSeconds;
            bool done = Scenario == "video" ? RunVideoScenario(seconds) : RunScrollScenario(seconds);
            if (done)
            {
                Complete();
                return;
            }
            Thread.Sleep(16);
        }
    }

    private bool RunVideoScenario(double seconds)
    {
        // Startup, open the first title's detail page, then measure muted looped playback.
        var phase = seconds < 3 ? 0 : seconds < 5 ? 1 : 2;
        if (EnterPhase(phase, phase switch { 0 => "startup", 1 => "open-detail", _ => "video" }) && phase == 1)
        {
            _dispatcher.Post(() =>
            {
                var services = _context.Services;
                var first = services.GetRequiredService<IAnimeCatalog>().Titles[0];
                services.GetRequiredService<AnimeNavigator>().Open(first.Id);
            });
        }

        return seconds >= 15;
    }

    private bool RunScrollScenario(double seconds)
    {
        // Startup, continuous redraw, alternating wheel scrolls, then idle.
        var phase = seconds < 4 ? 0 : seconds < 9 ? 1 : seconds < 33 ? 2 + (int)((seconds - 9) / 3) : 10;
        EnterPhase(phase, phase switch
        {
            0 => "startup",
            1 => "redraw",
            10 => "idle",
            _ => phase % 2 == 0 ? "wheel-down" : "wheel-up"
        });

        if (phase == 1)
        {
            _context.Engine.RequestRepaint();
        }
        else if (phase is >= 2 and <= 9 && seconds - 9 - (phase - 2) * 3 < 1)
        {
            // One wheel notch per tick for the first second of each window, then let it settle.
            var delta = phase % 2 == 0 ? 40f : -40f;
            _dispatcher.Post(() => _context.Controller.OnScroll(_x, _y, 0, delta));
        }

        return seconds >= 37;
    }

    private bool EnterPhase(int phase, string name)
    {
        if (phase == _phase) return false;
        lock (_gate)
        {
            _phase = phase;
            _phaseName = name;
            _lastFrame = 0;
        }
        _dispatcher.Post(() => Log(FormattableString.Invariant(
            $"[desktop-phase] {name} step={phase} scrollTop={FindContent(_context.Engine.GetCurrentLayout())?.ScrollTop:F1}")));
        return true;
    }

    private void Complete()
    {
        FrameProfiler.Disable();
        lock (_gate)
        {
            foreach (var line in _records) Console.WriteLine(line);
            _records.Clear();
            foreach (var (name, stats) in _stats)
                Console.WriteLine(stats.Summary(name));
        }
        Console.WriteLine("[desktop-probe] complete");

        // GLFW accepts a close request from any thread; the host then shuts down normally.
        (_context.Services.GetService<INativeHostContext>()?.Host as IWindow)?.Close();
    }

    private void Log(string message)
    {
        var line = $"[desktop-time {Stopwatch.GetElapsedTime(_started).TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture)}ms] {message}";
        lock (_gate) _records.Add(line);
    }

    private PhaseStats Stats(string phase)
    {
        if (!_stats.TryGetValue(phase, out var stats))
            _stats[phase] = stats = new PhaseStats();
        return stats;
    }

    private static LayoutBox? FindContent(LayoutBox? box)
    {
        if (box == null || box.Element.HasClass("inner-scroll")) return box;
        foreach (var child in box.Children)
        {
            var found = FindContent(child);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Per-phase frame statistics. Intervals only count consecutive frames within one phase,
    /// so the median interval is the sustained frame rate while frames are being produced.
    /// </summary>
    private sealed class PhaseStats
    {
        private readonly List<double> _intervals = [];
        private readonly List<double> _frames = [];

        public void Add(double interval, double frame)
        {
            if (interval > 0) _intervals.Add(interval);
            _frames.Add(frame);
        }

        public string Summary(string phase)
        {
            var intervalP50 = Percentile(_intervals, 0.50);
            return FormattableString.Invariant(
                $"[desktop-summary] phase={phase} frames={_frames.Count} fps(p50)={(intervalP50 > 0 ? 1000 / intervalP50 : 0):F1} interval p50={intervalP50:F2}ms p95={Percentile(_intervals, 0.95):F2}ms frame avg={(_frames.Count > 0 ? _frames.Average() : 0):F2}ms p95={Percentile(_frames, 0.95):F2}ms max={(_frames.Count > 0 ? _frames.Max() : 0):F2}ms");
        }

        private static double Percentile(List<double> values, double percentile)
        {
            if (values.Count == 0) return 0;
            var sorted = values.Order().ToList();
            return sorted[(int)Math.Min(sorted.Count - 1, Math.Round(percentile * (sorted.Count - 1)))];
        }
    }
}
