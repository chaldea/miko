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
}
