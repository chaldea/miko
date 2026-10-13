// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using Anime.Services;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using MetalKit;
using Microsoft.Extensions.DependencyInjection;
using Miko.Diagnostics;
using Miko.Events;
using Miko.Hosting;
using Miko.iOS;
using Miko.Layout;
using ObjCRuntime;
using UIKit;

namespace Anime.iOS;

/// <summary>
/// Optional iOS measurements around the existing engine probe. Metal draw covers acquiring
/// the drawable (which waits when the GPU falls behind), the engine frame, Skia's flush and
/// the present commit; GPU execution and display composition happen afterwards.
/// Automatic input uses the same engine pointer entry points as MikoMetalView, on the UI
/// thread, without replacing its normal display-link scheduling. The "video" scenario opens
/// a detail page, whose player autoplays the bundled video through the iOS video backend.
/// </summary>
internal sealed class IosFrameProbe : MTKViewDelegate
{
    private static readonly long Started = Stopwatch.GetTimestamp();
    private static readonly string? Scenario = Environment.GetEnvironmentVariable("MIKO_IOS_PROBE_SCENARIO");
    private static readonly bool Automatic = Scenario is "scroll" or "video";
    private static readonly bool Verbose = Environment.GetEnvironmentVariable("MIKO_FRAME_PROBE") == "verbose";
    private static readonly List<string> Records = [];
    private readonly MikoAppContext _context;
    private readonly MikoMetalView _view;
    private readonly IMTKViewDelegate _original;
    private readonly CADisplayLink _clock;
    private readonly NSTimer? _timer;
    private readonly long _started = Stopwatch.GetTimestamp();
    private readonly Dictionary<string, PhaseStats> _stats = [];
    private long _lastDraw;
    private long _lastTick;
    private int _phase = -1;
    private bool _dragging;
    private bool _complete;
    private bool _reportedRenderer;
    private string _phaseName = "startup";
    private readonly float _x;
    private readonly float _top;
    private readonly float _bottom;

    private IosFrameProbe(MikoAppContext context, MikoMetalView view)
    {
        _context = context;
        _view = view;
        _original = view.Delegate!;
        view.Delegate = this;
        _x = (float)view.Bounds.Width * 0.5f;
        _top = (float)view.Bounds.Height * 0.30f;
        _bottom = (float)view.Bounds.Height * 0.72f;
        _clock = CADisplayLink.Create(OnDisplayTick);
        _clock.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
        if (Automatic)
        {
            _timer = NSTimer.CreateRepeatingTimer(TimeSpan.FromSeconds(1.0 / 60), _ => OnTimer());
            NSRunLoop.Main.AddTimer(_timer, NSRunLoopMode.Common);
        }
        Log($"[ios-probe] attached scale={view.ContentScaleFactor} samples={view.SampleCount} scenario={(Automatic ? Scenario : "manual")}");
    }

    public static IosFrameProbe? AttachIfRequested(MikoAppContext context, MikoViewController controller)
        => FrameProfiler.IsEnabled
            ? new IosFrameProbe(context, controller.View!.Subviews.OfType<MikoMetalView>().Single())
            : null;

    public static void Log(string message)
    {
        var line = $"[ios-time {Stopwatch.GetElapsedTime(Started).TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture)}ms] {message}";
        if (Automatic)
            Records.Add(line);
        else
            Console.WriteLine(line);
    }

    public override void DrawableSizeWillChange(MTKView view, CGSize size)
        => _original.DrawableSizeWillChange(view, size);

    public override void Draw(MTKView view)
    {
        if (_complete)
        {
            _original.Draw(view);
            return;
        }

        var start = Stopwatch.GetTimestamp();
        if (!_reportedRenderer)
        {
            _reportedRenderer = true;
            Log($"[ios-renderer] device={view.Device?.Name} simulator={Runtime.Arch == Arch.SIMULATOR} pixels={view.DrawableSize.Width}x{view.DrawableSize.Height} format={view.ColorPixelFormat}");
        }
        var interval = _lastDraw == 0 ? 0 : Stopwatch.GetElapsedTime(_lastDraw, start).TotalMilliseconds;
        _lastDraw = start;
        _original.Draw(view);
        var duration = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        if (Automatic)
            Stats(_phaseName).Add(interval, duration);
        if (Automatic || Verbose || duration > 32)
            Log(FormattableString.Invariant($"[ios-draw] phase={_phaseName} interval={interval:F3}ms draw={duration:F3}ms"));
    }

    private void OnDisplayTick()
    {
        if (_complete) return;
        var now = Stopwatch.GetTimestamp();
        if (_lastTick != 0)
        {
            var interval = Stopwatch.GetElapsedTime(_lastTick, now).TotalMilliseconds;
            if (Automatic || Verbose || interval > 32)
                Log(FormattableString.Invariant($"[ios-tick] phase={_phaseName} interval={interval:F3}ms target={(_clock.TargetTimestamp - _clock.Timestamp) * 1000:F3}ms pending={_context.Controller.HasPendingWork}"));
        }
        _lastTick = now;
    }

    private void OnTimer()
    {
        if (_complete || !Automatic) return;
        var seconds = Stopwatch.GetElapsedTime(_started).TotalSeconds;
        if (Scenario == "video")
            RunVideoScenario(seconds);
        else
            RunScrollScenario(seconds);
    }

    private void RunVideoScenario(double seconds)
    {
        // Startup, open the first title's detail page, then measure muted looped playback.
        var phase = seconds < 3 ? 0 : seconds < 5 ? 1 : 2;
        if (phase != _phase)
        {
            _phase = phase;
            _phaseName = phase switch { 0 => "startup", 1 => "open-detail", _ => "video" };
            _lastDraw = 0;
            LogPhase();
            if (phase == 1)
            {
                var services = _context.Services;
                var first = services.GetRequiredService<IAnimeCatalog>().Titles[0];
                services.GetRequiredService<AnimeNavigator>().Open(first.Id);
                _view.SetNeedsDisplay();
            }
        }

        if (seconds >= 15)
            Complete();
    }

    private void RunScrollScenario(double seconds)
    {
        // Startup, continuous redraw, alternating drags and inertia, then idle.
        var phase = seconds < 4 ? 0 : seconds < 9 ? 1 : seconds < 33 ? 2 + (int)((seconds - 9) / 3) : 10;
        if (phase != _phase)
        {
            EndDrag();
            _phase = phase;
            _phaseName = phase switch
            {
                0 => "startup",
                1 => "redraw",
                10 => "idle",
                _ => phase % 2 == 0 ? "drag-up" : "drag-down"
            };
            _lastDraw = 0;
            LogPhase();
        }

        if (phase == 1)
        {
            _context.Engine.RequestRepaint();
        }
        else if (phase is >= 2 and <= 9)
        {
            var progress = (seconds - 9 - (phase - 2) * 3) / 1.0;
            var from = phase % 2 == 0 ? _bottom : _top;
            var to = phase % 2 == 0 ? _top : _bottom;
            if (progress < 1)
            {
                if (!_dragging)
                {
                    _context.Controller.OnPointerDown(_x, from, MouseButton.Left, PointerType.Touch);
                    _dragging = true;
                }
                _context.Controller.OnPointerMove(_x, from + (to - from) * (float)progress);
                _view.SetNeedsDisplay();
            }
            else if (_dragging)
            {
                _context.Controller.OnPointerUp(_x, to, MouseButton.Left, PointerType.Touch);
                _dragging = false;
                _phaseName = "inertia";
                _lastDraw = 0;
                LogPhase();
                _view.SetNeedsDisplay();
            }
        }

        if (seconds >= 37)
            Complete();
    }

    private void Complete()
    {
        _complete = true;
        _clock.Invalidate();
        _timer?.Invalidate();
        _view.Delegate = _original;
        FrameProbe.Disable();
        foreach (var line in Records) Console.WriteLine(line);
        Records.Clear();
        foreach (var (name, stats) in _stats)
            Console.WriteLine(stats.Summary(name));
        Console.WriteLine("[ios-probe] complete");
    }

    private PhaseStats Stats(string phase)
    {
        if (!_stats.TryGetValue(phase, out var stats))
            _stats[phase] = stats = new PhaseStats();
        return stats;
    }

    private void LogPhase()
        => Log(FormattableString.Invariant($"[ios-phase] {_phaseName} step={_phase} scrollTop={FindContent(_context.Engine.GetCurrentLayout())?.ScrollTop:F1}"));

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

    private void EndDrag()
    {
        if (!_dragging) return;
        _context.Controller.OnPointerCancel(_x, _top);
        _dragging = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            EndDrag();
            _clock.Invalidate();
            _clock.Dispose();
            _timer?.Invalidate();
            _timer?.Dispose();
            _view.Delegate = _original;
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Per-phase draw statistics. Intervals only count consecutive draws within one phase
    /// segment, so the median interval is the sustained frame rate while frames are being
    /// requested; idle gaps after inertia settles do not dilute it.
    /// </summary>
    private sealed class PhaseStats
    {
        private readonly List<double> _intervals = [];
        private readonly List<double> _draws = [];

        public void Add(double interval, double draw)
        {
            if (interval > 0) _intervals.Add(interval);
            _draws.Add(draw);
        }

        public string Summary(string phase)
        {
            var intervalP50 = Percentile(_intervals, 0.50);
            return FormattableString.Invariant(
                $"[ios-summary] phase={phase} draws={_draws.Count} fps(p50)={(intervalP50 > 0 ? 1000 / intervalP50 : 0):F1} interval p50={intervalP50:F2}ms p95={Percentile(_intervals, 0.95):F2}ms draw avg={(_draws.Count > 0 ? _draws.Average() : 0):F2}ms p95={Percentile(_draws, 0.95):F2}ms max={(_draws.Count > 0 ? _draws.Max() : 0):F2}ms");
        }

        private static double Percentile(List<double> values, double percentile)
        {
            if (values.Count == 0) return 0;
            var sorted = values.Order().ToList();
            return sorted[(int)Math.Min(sorted.Count - 1, Math.Round(percentile * (sorted.Count - 1)))];
        }
    }
}
