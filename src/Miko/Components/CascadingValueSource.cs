namespace Miko.Components;

/// <summary>
/// Ambient stack of active cascading values, used to flow values from an ancestor
/// <see cref="CascadingValue{TValue}"/> down to descendant components that declare a
/// <see cref="CascadingParameterAttribute"/>.
/// <para>
/// The render tree is built synchronously, top-down, on a single render thread:
/// <see cref="RenderTreeBuilder.CloseComponent"/> invokes a child's <see cref="ComponentBase.Build"/>
/// while the ancestor is still inside its own <c>BuildRenderTree</c> on the call stack. So a
/// <see cref="System.ThreadStaticAttribute">thread-static</see> stack precisely models which
/// cascading values are in scope at the point any given component builds: a provider pushes before
/// rendering its children and pops afterwards (via the returned <see cref="IDisposable"/>), and a
/// descendant reads the nearest matching entry during its own build.
/// </para>
/// </summary>
internal static class CascadingValueSource
{
    private readonly struct Frame
    {
        public readonly Type ValueType;
        public readonly string? Name;
        public readonly object? Value;

        public Frame(Type valueType, string? name, object? value)
        {
            ValueType = valueType;
            Name = name;
            Value = value;
        }
    }

    [ThreadStatic]
    private static Stack<Frame>? _stack;

    private static Stack<Frame> Stack => _stack ??= new Stack<Frame>();

    /// <summary>
    /// Pushes a cascading value onto the ambient stack and returns a handle that pops it when
    /// disposed. <paramref name="valueType"/> is the provider's declared <c>TValue</c> (not the
    /// runtime type of <paramref name="value"/>), so a null value still resolves by its declared
    /// type — mirroring Blazor.
    /// </summary>
    public static IDisposable Push(object? value, Type valueType, string? name)
    {
        var stack = Stack;
        stack.Push(new Frame(valueType, name, value));
        return new PopHandle(stack);
    }

    /// <summary>
    /// Resolves the nearest cascading value matching the requested target type / name.
    /// When <paramref name="name"/> is non-null, only entries with the same name match.
    /// Otherwise the nearest entry whose declared type is assignable to <paramref name="targetType"/>
    /// matches. Returns false (and a null value) when nothing matches.
    /// </summary>
    public static bool TryResolve(Type targetType, string? name, out object? value)
    {
        if (_stack != null)
        {
            // Stack enumeration is newest→oldest, so inner providers shadow outer ones.
            foreach (var frame in _stack)
            {
                bool matches = name != null
                    ? string.Equals(frame.Name, name, StringComparison.Ordinal)
                    : frame.Name == null && targetType.IsAssignableFrom(frame.ValueType);

                if (matches)
                {
                    value = frame.Value;
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    private sealed class PopHandle : IDisposable
    {
        private Stack<Frame>? _stack;

        public PopHandle(Stack<Frame> stack) => _stack = stack;

        public void Dispose()
        {
            // Pop once; tolerate an already-unwound stack (e.g. on an exceptional build path).
            var stack = _stack;
            if (stack is { Count: > 0 })
                stack.Pop();
            _stack = null;
        }
    }
}
