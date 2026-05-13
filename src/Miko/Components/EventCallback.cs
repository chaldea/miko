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
        where T : MikoEventArgs => new(handler);

    public EventCallback<T> Create<T>(object receiver, Action handler)
        where T : MikoEventArgs => new(_ => handler());
}
