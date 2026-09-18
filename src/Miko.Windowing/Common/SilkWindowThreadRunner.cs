using System.Runtime.ExceptionServices;
using Silk.NET.Windowing;

namespace Miko.Windowing.Common;

/// <summary>
/// Runs native window events on the calling thread while a dedicated thread owns the graphics
/// context and rendering. This keeps rendering alive while Windows enters its modal move/size loop.
/// </summary>
public sealed class SilkWindowThreadRunner
{
    private readonly IWindow _window;
    private readonly Action _initializeRenderThread;
    private readonly Func<bool> _renderIteration;
    private readonly Action _shutdownRenderThread;
    private bool _stopRequested;
    private int _renderThreadId;

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

    public void Run()
    {
        _window.Initialize();
        _window.GLContext?.Clear();

        ExceptionDispatchInfo? renderFailure = null;
        var renderThread = new Thread(() =>
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
                Volatile.Write(ref renderFailure, ExceptionDispatchInfo.Capture(ex));
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
                        ref renderFailure,
                        ExceptionDispatchInfo.Capture(ex),
                        null);
                }
                finally
                {
                    _window.GLContext?.Clear();
                    Volatile.Write(ref _renderThreadId, 0);
                }
            }
        })
        {
            IsBackground = true,
            Name = ThreadName,
        };
        renderThread.Start();

        try
        {
            while (!_window.IsClosing && Volatile.Read(ref renderFailure) == null)
            {
                BeforeEvents?.Invoke();
                _window.DoEvents();
                AfterEvents?.Invoke();
                if (_window.IsClosing || Volatile.Read(ref renderFailure) != null) break;
                MainThreadIteration?.Invoke();
                Thread.Sleep(1);
            }
        }
        finally
        {
            Volatile.Write(ref _stopRequested, true);
            renderThread.Join();
        }

        _window.DoEvents();
        Volatile.Read(ref renderFailure)?.Throw();
    }
}
