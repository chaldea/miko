namespace Miko.Components.CompilerServices;

/// <summary>
/// Helpers the Razor compiler emits calls to. These exist so that generated code never has to
/// spell out an <see cref="EventCallback{T}"/>'s type argument: the C# compiler infers it from the
/// lambda, which keeps generic components working (the generic-type lowering pass runs after
/// <c>@bind</c> lowering, so the type name is not yet known at that point).
/// </summary>
public static class RuntimeHelpers
{
    /// <summary>
    /// Identity function used to type-check a value against the target parameter type, producing a
    /// compile-time error at the attribute's source location rather than deep inside generated code.
    /// </summary>
    public static T TypeCheck<T>(T value) => value;

    /// <summary>
    /// Creates an <see cref="EventCallback{TValue}"/> whose <c>TValue</c> is inferred from
    /// <paramref name="callback"/>. Emitted for <c>@bind-Value</c> on a component whose change
    /// parameter is an <see cref="EventCallback{TValue}"/>.
    /// </summary>
    /// <param name="receiver">The component that owns the binding (re-rendered after invocation).</param>
    /// <param name="callback">The setter that writes the new value back to the bound field.</param>
    /// <param name="value">
    /// The currently bound value. Unused at runtime — it is present only so the C# compiler can
    /// infer <typeparamref name="TValue"/> when <paramref name="callback"/> alone is ambiguous.
    /// </param>
    public static EventCallback<TValue> CreateInferredEventCallback<TValue>(
        object receiver,
        Action<TValue> callback,
        TValue value)
        => EventCallback.Factory.Create(receiver, callback);

    /// <inheritdoc cref="CreateInferredEventCallback{TValue}(object, Action{TValue}, TValue)"/>
    public static EventCallback<TValue> CreateInferredEventCallback<TValue>(
        object receiver,
        Func<TValue, Task> callback,
        TValue value)
        => EventCallback.Factory.Create(receiver, callback);

    /// <summary>
    /// Returns <paramref name="callback"/> unchanged, with <c>TValue</c> inferred from
    /// <paramref name="value"/>. Emitted for <c>@bind:set</c> / <c>@bind:after</c>, where the user
    /// supplies the setter and only the value type needs pinning down.
    /// </summary>
    public static Func<TValue, Task> CreateInferredBindSetter<TValue>(
        Func<TValue, Task> callback,
        TValue value)
        => callback;

    /// <inheritdoc cref="CreateInferredBindSetter{TValue}(Func{TValue, Task}, TValue)"/>
    public static Action<TValue> CreateInferredBindSetter<TValue>(
        Action<TValue> callback,
        TValue value)
        => callback;

    /// <summary>
    /// Invokes a synchronous <c>@bind:after</c> handler. Exists so generated code can call a
    /// delegate of unknown arity uniformly.
    /// </summary>
    public static void InvokeSynchronousDelegate(Action callback) => callback();

    /// <summary>
    /// Invokes an asynchronous <c>@bind:after</c> handler, tolerating a synchronous
    /// <see cref="Action"/> in the same slot.
    /// </summary>
    public static Task InvokeAsynchronousDelegate(Func<Task> callback) => callback() ?? Task.CompletedTask;

    /// <inheritdoc cref="InvokeAsynchronousDelegate(Func{Task})"/>
    public static Task InvokeAsynchronousDelegate(Action callback)
    {
        callback();
        return Task.CompletedTask;
    }
}
