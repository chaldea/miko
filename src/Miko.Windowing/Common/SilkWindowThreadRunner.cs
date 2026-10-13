using System.Runtime.ExceptionServices;
using Silk.NET.Windowing;

namespace Miko.Windowing.Common;

/// <summary>
/// Runs native window events on the calling thread while a dedicated thread owns the graphics
/// context and rendering. This keeps rendering alive while Windows enters its modal move/size loop.
/// <para>
/// <see cref="Run"/> drives the window on a loop of its own. The first such loop in the process is
/// also the <see cref="SilkEventLoop"/>, and pumps every window attached to it. An attached window
/// (DevTools) cannot run a loop of its own — macOS only lets the main thread create windows and
/// process their events (ISSUE-148) — so it drives its runner one step at a time from the event
/// loop instead: <see cref="Start"/>, then <see cref="PumpEvents"/> every iteration, then
/// <see cref="Stop"/>.
/// </para>
/// </summary>
public sealed class SilkWindowThreadRunner
{
    private readonly IWindow _window;
    private readonly Action _initializeRenderThread;
    private readonly Func<bool> _renderIteration;
    private readonly Action _shutdownRenderThread;
    private bool _stopRequested;
    private int _renderThreadId;
    private Thread? _renderThread;
    private ExceptionDispatchInfo? _renderFailure;

    public SilkWindowThreadRunner(
        IWindow window,
        Action initializeRenderThread,
        Func<bool> renderIteration,
        Action shutdownRenderThread)
    {
        _window = window;
        _initializeRenderThread = initializeRenderThread;
        _renderIteration = renderIteration;
        _shutdownRenderThread = shutdownRenderThread;
    }

    public string ThreadName { get; init; } = "miko-render";
    public int IdleDelayMilliseconds { get; init; } = 1;
    public Action? BeforeEvents { get; init; }
    public Action? AfterEvents { get; init; }
    public Action? MainThreadIteration { get; init; }
    public bool IsRenderThread => Environment.CurrentManagedThreadId == Volatile.Read(ref _renderThreadId);

    /// <summary>Runs the window until it closes, blocking the calling thread.</summary>
    public void Run()
    {
        Start();

        bool ownsEventLoop = SilkEventLoop.TryEnter(this);
        try
        {
            while (PumpEvents())
            {
                if (ownsEventLoop) SilkEventLoop.PumpAttachedWindows();
                Thread.Sleep(1);
            }
        }
        finally
        {
            try
            {
                if (ownsEventLoop) SilkEventLoop.Exit(this);
            }
            finally
            {
                StopRenderThread();
            }
        }

        Complete();
    }

    /// <summary>Initializes the window on the calling thread and starts the render thread.</summary>
    public void Start()
    {
        if (_renderThread != null)
            throw new InvalidOperationException("The window runner has already been started.");

        _window.Initialize();
        _window.GLContext?.Clear();

        _renderThread = new Thread(RenderThreadMain)
        {
            IsBackground = true,
            Name = ThreadName,
        };
        _renderThread.Start();
    }

    /// <summary>
    /// One event-loop iteration on the thread that called <see cref="Start"/>: pumps native events
    /// and runs <see cref="MainThreadIteration"/>. Returns <c>false</c> once the window is closing or
    /// the render thread has failed; <see cref="Stop"/> should follow.
    /// </summary>
    public bool PumpEvents()
    {
        if (_window.IsClosing || Volatile.Read(ref _renderFailure) != null) return false;

        BeforeEvents?.Invoke();
        _window.DoEvents();
        AfterEvents?.Invoke();
        if (_window.IsClosing || Volatile.Read(ref _renderFailure) != null) return false;

        MainThreadIteration?.Invoke();
        return true;
    }

    /// <summary>
    /// Stops and joins the render thread, then flushes pending events. Rethrows a render-thread
    /// failure, if any.
    /// </summary>
    public void Stop()
    {
        StopRenderThread();
        Complete();
    }

    private void StopRenderThread()
    {
        Volatile.Write(ref _stopRequested, true);
        _renderThread?.Join();
    }

    private void Complete()
    {
        _window.DoEvents();
        Volatile.Read(ref _renderFailure)?.Throw();
    }

    private void RenderThreadMain()
    {
        Volatile.Write(ref _renderThreadId, Environment.CurrentManagedThreadId);
        try
        {
            _window.GLContext?.MakeCurrent();
            _initializeRenderThread();
            while (!Volatile.Read(ref _stopRequested))
            {
                if (!_renderIteration() && IdleDelayMilliseconds > 0)
                    Thread.Sleep(IdleDelayMilliseconds);
            }
        }
        catch (Exception ex)
        {
            Volatile.Write(ref _renderFailure, ExceptionDispatchInfo.Capture(ex));
        }
        finally
        {
            try
            {
                _shutdownRenderThread();
            }
            catch (Exception ex)
            {
                Interlocked.CompareExchange(
                    ref _renderFailure,
                    ExceptionDispatchInfo.Capture(ex),
                    null);
            }
            finally
            {
                _window.GLContext?.Clear();
                Volatile.Write(ref _renderThreadId, 0);
            }
        }
    }
}
