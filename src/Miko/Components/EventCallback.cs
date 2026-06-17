using Miko.Events;

namespace Miko.Components;

public static class EventCallback
{
    public static readonly EventCallbackFactory Factory = new();
}

public readonly struct EventCallback<T> where T : MikoEventArgs
{
    public readonly MikoEventHandler<T>? Handler;
    internal EventCallback(MikoEventHandler<T>? handler) => Handler = handler;
}

public sealed class EventCallbackFactory
{
    public EventCallback<T> Create<T>(object receiver, MikoEventHandler<T> handler)
        where T : MikoEventArgs => new(WrapWithStateChange(receiver, handler));

    public EventCallback<T> Create<T>(object receiver, Action handler)
        where T : MikoEventArgs => new(WrapWithStateChange<T>(receiver, _ => handler()));

    // Async overload: Func<Task>
    public EventCallback<T> Create<T>(object receiver, Func<Task> handler)
        where T : MikoEventArgs => new(WrapWithStateChangeAsync<T>(receiver, _ => handler()));

    // Async overload: Func<T, Task>
    public EventCallback<T> Create<T>(object receiver, Func<T, Task> handler)
        where T : MikoEventArgs => new(WrapWithStateChangeAsync(receiver, handler));

    // After an event handler runs, re-render the receiving component so handlers
    // that only mutate state (e.g. _count++) update the UI without a manual
    // StateHasChanged() call. Mirrors Blazor's EventCallback behavior.
    private static MikoEventHandler<T> WrapWithStateChange<T>(object receiver, MikoEventHandler<T> handler)
        where T : MikoEventArgs
    {
        if (receiver is not ComponentBase component)
            return handler;

        return args =>
        {
            handler(args);
            component.NotifyStateChanged();
        };
    }

    // Async wrapper: awaits the task, then calls StateHasChanged.
    private static MikoEventHandler<T> WrapWithStateChangeAsync<T>(object receiver, Func<T, Task> handler)
        where T : MikoEventArgs
    {
        if (receiver is not ComponentBase component)
        {
            // No component to re-render; just fire and forget the task.
            return args => _ = handler(args);
        }

        return args =>
        {
            var task = handler(args);
            if (task.IsCompleted)
            {
                // Fast path: if already completed (e.g. Task.CompletedTask), StateHasChanged synchronously.
                if (task.IsFaulted)
                    HandleTaskException(task.Exception!);
                else
                    component.NotifyStateChanged();
            }
            else
            {
                // Slow path: schedule continuation to call StateHasChanged when task completes.
                // The continuation runs on the render thread (via SynchronizationContext).
                _ = task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        HandleTaskException(t.Exception!);
                    else
                        component.NotifyStateChanged();
                }, TaskScheduler.Current);
            }
        };
    }

    private static void HandleTaskException(AggregateException ex)
    {
        // Flatten and log the exception. In a real app, this would go to a logging framework.
        var flatten = ex.Flatten();
        Console.Error.WriteLine($"[EventCallback] Unhandled exception in async handler: {flatten.InnerException ?? flatten}");
    }
}
