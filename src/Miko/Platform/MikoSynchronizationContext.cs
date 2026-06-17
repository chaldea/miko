namespace Miko.Platform;

/// <summary>
/// A SynchronizationContext that posts continuations to the <see cref="MikoDispatcher"/>,
/// ensuring async/await continuations resume on the render thread.
/// <para>
/// Installed as <see cref="SynchronizationContext.Current"/> before executing event handlers
/// and component lifecycle methods, so user code that awaits (e.g. HttpClient.GetAsync)
/// automatically marshals its continuation back to the render thread without explicit locking.
/// </para>
/// </summary>
public sealed class MikoSynchronizationContext : SynchronizationContext
{
    private readonly MikoDispatcher _dispatcher;

    public MikoSynchronizationContext(MikoDispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <summary>
    /// Posts the callback to the dispatcher queue so it executes on the render thread.
    /// </summary>
    public override void Post(SendOrPostCallback callback, object? state)
    {
        _dispatcher.Post(() => callback(state));
    }

    /// <summary>
    /// Synchronous send is not supported (would deadlock if called from the render thread).
    /// Throws NotSupportedException.
    /// </summary>
    public override void Send(SendOrPostCallback callback, object? state)
    {
        throw new NotSupportedException(
            "Synchronous Send is not supported on MikoSynchronizationContext. Use Post or await instead.");
    }
}
