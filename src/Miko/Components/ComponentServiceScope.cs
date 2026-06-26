namespace Miko.Components;

/// <summary>
/// Ambient stack of <see cref="IServiceProvider"/> instances available during component render.
/// Used so that a child component instantiated by <see cref="RenderTreeBuilder.OpenComponent{T}"/>
/// — which uses <c>new T()</c> and therefore has no constructor-injected services — can still
/// resolve <see cref="InjectAttribute"/> properties from the same container that built its parent.
/// <para>
/// The render tree is built synchronously, top-down, on a single render thread (see
/// <see cref="CascadingValueSource"/> for the same model). A parent component pushes its provider
/// before invoking <c>BuildRenderTree</c>; nested <see cref="ComponentBase.Build"/> calls running
/// on the same call stack peek the current provider to inject their own services and re-push it
/// for their own descendants.
/// </para>
/// </summary>
internal static class ComponentServiceScope
{
    [ThreadStatic]
    private static Stack<IServiceProvider>? _stack;

    /// <summary>The provider in scope at the current render position, or null when none was pushed.</summary>
    public static IServiceProvider? Current
        => (_stack != null && _stack.Count > 0) ? _stack.Peek() : null;

    /// <summary>
    /// Pushes <paramref name="provider"/> onto the ambient stack and returns a handle that pops
    /// it when disposed. Tolerates a null provider (returns a no-op handle) so callers don't need
    /// to branch.
    /// </summary>
    public static IDisposable Push(IServiceProvider? provider)
    {
        if (provider == null) return NoopHandle.Instance;
        var stack = _stack ??= new Stack<IServiceProvider>();
        stack.Push(provider);
        return new PopHandle(stack);
    }

    private sealed class PopHandle : IDisposable
    {
        private Stack<IServiceProvider>? _stack;

        public PopHandle(Stack<IServiceProvider> stack) => _stack = stack;

        public void Dispose()
        {
            // Pop once; tolerate an already-unwound stack (e.g. on an exceptional build path).
            var stack = _stack;
            if (stack is { Count: > 0 })
                stack.Pop();
            _stack = null;
        }
    }

    private sealed class NoopHandle : IDisposable
    {
        public static readonly NoopHandle Instance = new();
        public void Dispose() { }
    }
}
