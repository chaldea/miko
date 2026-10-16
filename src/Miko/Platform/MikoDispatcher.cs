using System.Collections.Concurrent;

namespace Miko.Platform;

/// <summary>
/// Thread-safe action queue drained each frame on the render thread.
/// Used to marshal async continuations back onto the render thread
/// so they can safely mutate the DOM (via StateHasChanged, etc.).
/// </summary>
public sealed class MikoDispatcher
{
    private readonly ConcurrentQueue<Action> _queue = new();

    /// <summary>
    /// Raised on the posting thread after an action is enqueued.
    /// <para>
    /// <see cref="Drain"/> only runs inside a frame, so a host that draws on demand (Android's
    /// render-when-dirty surface) subscribes and requests one. Without that, an action posted while
    /// the host is idle — an awaited load finishing, a timer firing — waits for the next touch.
    /// Hosts that check <see cref="HasPendingActions"/> on every vsync or loop iteration need not
    /// subscribe. Handlers must be cheap and thread-safe.
    /// </para>
    /// </summary>
    public event Action? Posted;

    /// <summary>
    /// Thread-safe. Enqueues an action to be executed on the render thread
    /// during the next frame (when <see cref="Drain"/> is called).
    /// </summary>
    public void Post(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        _queue.Enqueue(action);
        Posted?.Invoke();
    }

    /// <summary>
    /// Executes all queued actions on the calling (render) thread.
    /// Called once per frame at the start of rendering.
    /// </summary>
    public void Drain()
    {
        while (_queue.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                // Log but don't crash the render loop. Application-level
                // exception handlers should catch unhandled async exceptions.
                Console.Error.WriteLine($"[MikoDispatcher] Unhandled exception in queued action: {ex}");
            }
        }
    }

    /// <summary>
    /// Returns true if there are pending actions in the queue.
    /// </summary>
    public bool HasPendingActions => !_queue.IsEmpty;
}
