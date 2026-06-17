using System.Threading;
using Miko.Events;

namespace Miko.Components;

/// <summary>
/// A bound event handler delegate. When invoked, it triggers a re-render on the owning
/// component (<see cref="Receiver"/>). Mirrors Blazor's non-generic <c>EventCallback</c>.
/// <para>
/// Use this for events that take no argument:
/// <code>
/// [Parameter] public EventCallback OnClick { get; set; }
/// </code>
/// The Razor compiler generates <see cref="EventCallbackFactory.Create(object, Action)"/> calls
/// automatically for parameter types named <c>EventCallback</c>, so you can pass any of:
/// <list type="bullet">
///   <item><c>Action</c> (sync handler)</item>
///   <item><c>Func{Task}</c> (async handler)</item>
///   <item>another <see cref="EventCallback"/></item>
/// </list>
/// </para>
/// </summary>
public readonly struct EventCallback : IEquatable<EventCallback>
{
    /// <summary>Static factory exposed for Razor-generated code.</summary>
    public static readonly EventCallbackFactory Factory = new();

    /// <summary>An empty (no-op) <see cref="EventCallback"/>.</summary>
    public static readonly EventCallback Empty = default;

    /// <summary>The component that owns this callback, used to re-render on completion.</summary>
    public readonly object? Receiver;
    internal readonly Delegate? Delegate;

    internal EventCallback(object? receiver, Delegate? @delegate)
    {
        Receiver = receiver;
        Delegate = @delegate;
    }

    /// <summary>True if a handler is bound.</summary>
    public bool HasDelegate => Delegate is not null;

    /// <summary>
    /// Invokes the handler. Returns a Task that completes when:
    /// - sync handlers: immediately after they run,
    /// - async handlers: when the returned Task completes.
    /// After the Task completes, the receiver component is re-rendered (StateHasChanged).
    /// </summary>
    public Task InvokeAsync()
    {
        if (Delegate is null) return Task.CompletedTask;

        Task task;
        try
        {
            switch (Delegate)
            {
                case Action a: a(); task = Task.CompletedTask; break;
                case Func<Task> f: task = f() ?? Task.CompletedTask; break;
                default:
                    var result = Delegate.DynamicInvoke();
                    task = result is Task t ? t : Task.CompletedTask;
                    break;
            }
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }

        return EventCallbackHelper.AwaitAndNotifyStateChanged(task, Receiver);
    }

    public bool Equals(EventCallback other) =>
        ReferenceEquals(Receiver, other.Receiver) && ReferenceEquals(Delegate, other.Delegate);

    public override bool Equals(object? obj) => obj is EventCallback ec && Equals(ec);

    public override int GetHashCode() => HashCode.Combine(Receiver, Delegate);
}

/// <summary>
/// A bound event handler delegate that takes a single argument of type <typeparamref name="TValue"/>.
/// Mirrors Blazor's <c>EventCallback{TValue}</c>.
/// <para>
/// Use this for events that take a payload, e.g. mouse events, change events:
/// <code>
/// [Parameter] public EventCallback&lt;MouseEventArgs&gt; OnClick { get; set; }
/// </code>
/// The Razor compiler generates <see cref="EventCallbackFactory.Create{TValue}(object, Action{TValue})"/>
/// calls automatically; the user-provided handler can be any of:
/// <list type="bullet">
///   <item><c>Action</c> / <c>Action{TValue}</c></item>
///   <item><c>Func{Task}</c> / <c>Func{TValue, Task}</c></item>
///   <item>another <see cref="EventCallback{TValue}"/></item>
/// </list>
/// </para>
/// </summary>
public readonly struct EventCallback<TValue> : IEquatable<EventCallback<TValue>>
{
    public readonly object? Receiver;
    internal readonly Delegate? Delegate;

    internal EventCallback(object? receiver, Delegate? @delegate)
    {
        Receiver = receiver;
        Delegate = @delegate;
    }

    public bool HasDelegate => Delegate is not null;

    /// <summary>Invokes the handler with the given argument.</summary>
    public Task InvokeAsync(TValue arg)
    {
        if (Delegate is null) return Task.CompletedTask;

        Task task;
        try
        {
            switch (Delegate)
            {
                case Action a: a(); task = Task.CompletedTask; break;
                case Action<TValue> at: at(arg); task = Task.CompletedTask; break;
                case Func<Task> f: task = f() ?? Task.CompletedTask; break;
                case Func<TValue, Task> ft: task = ft(arg) ?? Task.CompletedTask; break;
                default:
                    var result = Delegate.DynamicInvoke(arg);
                    task = result is Task t ? t : Task.CompletedTask;
                    break;
            }
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }

        return EventCallbackHelper.AwaitAndNotifyStateChanged(task, Receiver);
    }

    /// <summary>Invokes the handler with the default argument value.</summary>
    public Task InvokeAsync() => InvokeAsync(default!);

    public bool Equals(EventCallback<TValue> other) =>
        ReferenceEquals(Receiver, other.Receiver) && ReferenceEquals(Delegate, other.Delegate);

    public override bool Equals(object? obj) => obj is EventCallback<TValue> ec && Equals(ec);

    public override int GetHashCode() =>
        HashCode.Combine(Receiver, Delegate);
}

/// <summary>Shared logic for awaiting a callback Task and notifying the receiver to re-render.</summary>
internal static class EventCallbackHelper
{
    public static Task AwaitAndNotifyStateChanged(Task task, object? receiver)
    {
        var component = receiver as ComponentBase;

        if (task.IsCompletedSuccessfully)
        {
            // Sync path: re-render immediately, on the current (render) thread.
            component?.NotifyStateChanged();
            return Task.CompletedTask;
        }

        if (task.IsFaulted)
        {
            HandleTaskException(task.Exception!);
            return task;
        }

        // Async path. The handler ran up to its first await and yielded. Re-render NOW so any
        // state set synchronously (e.g. _isLoading = true) is reflected immediately — this
        // mirrors Blazor, which calls StateHasChanged both before and after the await.
        component?.NotifyStateChanged();

        // Capture the SynchronizationContext (set by MikoInteractionController when dispatching
        // the event) so the completion continuation runs back on the render thread.
        var capturedCtx = SynchronizationContext.Current;

        return task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                HandleTaskException(t.Exception!);
                return;
            }

            if (component != null)
            {
                if (capturedCtx != null)
                {
                    capturedCtx.Post(_ => component.NotifyStateChanged(), null);
                }
                else
                {
                    component.NotifyStateChanged();
                }
            }
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private static void HandleTaskException(AggregateException ex)
    {
        var flatten = ex.Flatten();
        Console.Error.WriteLine($"[EventCallback] Unhandled exception in handler: {flatten.InnerException ?? flatten}");
    }
}

/// <summary>
/// Factory for creating <see cref="EventCallback"/> and <see cref="EventCallback{TValue}"/> instances.
/// The Razor compiler emits calls into this class for component-parameter event bindings.
/// <para>
/// All <c>Create</c> overloads accept the receiver (the component that owns the parameter,
/// passed by Razor as <c>this</c>) and a delegate. The receiver is used to drive auto re-render
/// after the handler completes.
/// </para>
/// </summary>
public sealed class EventCallbackFactory
{
    // ---------------- Non-generic EventCallback (no payload) ----------------

    public EventCallback Create(object receiver, EventCallback callback) => callback;

    public EventCallback Create(object receiver, Action callback)
        => new(receiver, callback);

    public EventCallback Create(object receiver, Func<Task> callback)
        => new(receiver, callback);

    // ---------------- EventCallback<TValue> (with payload) ------------------

    public EventCallback<TValue> Create<TValue>(object receiver, EventCallback<TValue> callback) => callback;

    public EventCallback<TValue> Create<TValue>(object receiver, Action callback)
        => new(receiver, callback);

    public EventCallback<TValue> Create<TValue>(object receiver, Action<TValue> callback)
        => new(receiver, callback);

    public EventCallback<TValue> Create<TValue>(object receiver, Func<Task> callback)
        => new(receiver, callback);

    public EventCallback<TValue> Create<TValue>(object receiver, Func<TValue, Task> callback)
        => new(receiver, callback);
}
